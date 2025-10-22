using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Hydra.Api.Caching;

/// <summary>
/// Redis-backed implementation of ICache with built-in resilience and observability.
/// All operations fail gracefully if Redis is unavailable - exceptions are caught and logged,
/// but never propagated to callers. This ensures the application remains functional even
/// when the cache layer is down.
/// </summary>
public class RedisCache : ICache
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisCache> _logger;

    /// <summary>
    /// JSON serialization options for cache values.
    /// Uses web defaults (camelCase) for consistency with API responses.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Initializes a new instance of the RedisCache class.
    /// </summary>
    /// <param name="mux">Redis connection multiplexer (manages connection pool)</param>
    /// <param name="logger">Logger for diagnostics and error tracking</param>
    public RedisCache(IConnectionMultiplexer mux, ILogger<RedisCache> logger)
    {
        _db = mux.GetDatabase();
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        ValidateKey(key);

        try
        {
            var val = await _db.StringGetAsync(key);

            if (!val.HasValue)
            {
                _logger.LogDebug("Cache miss for key: {Key}", key);
                return default;
            }

            _logger.LogDebug("Cache hit for key: {Key}", key);
            return JsonSerializer.Deserialize<T>(val!, JsonOpts);
        }
        catch (Exception ex)
        {
            // Redis unavailable? Fail open (no cache) to keep API responsive.
            // Log the error but don't throw - caller will just fetch from source.
            _logger.LogWarning(ex,
                "Redis GET failed for key {Key}. Falling back to source.", key);
            return default;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        ValidateKey(key);
        ValidateTtl(ttl);

        try
        {
            var json = JsonSerializer.Serialize(value, JsonOpts);
            await _db.StringSetAsync(key, json, ttl);

            _logger.LogDebug("Cached value for key: {Key}, TTL: {Ttl}", key, ttl);
        }
        catch (Exception ex)
        {
            // Swallow cache write errors - if we can't cache, just continue without caching.
            // The application should work fine without cache, just slower.
            _logger.LogWarning(ex,
                "Redis SET failed for key {Key}. Continuing without cache.", key);
        }
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(string key, CancellationToken ct = default)
    {
        ValidateKey(key);

        try
        {
            var removed = await _db.KeyDeleteAsync(key);

            if (removed)
                _logger.LogDebug("Removed cache key: {Key}", key);

            return removed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Redis DELETE failed for key {Key}.", key);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<T> GetOrSetAsync<T>(
        string key,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        bool cacheNull = false,
        TimeSpan? jitter = null,
        CancellationToken ct = default)
    {
        ValidateKey(key);
        ValidateTtl(ttl);

        // Step 1: Try to get from cache
        try
        {
            var cached = await _db.StringGetAsync(key);
            if (cached.HasValue)
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return JsonSerializer.Deserialize<T>(cached!, JsonOpts)!;
            }
        }
        catch (Exception ex)
        {
            // Cache read failed - log and continue to factory
            _logger.LogWarning(ex,
                "Redis GET failed in GetOrSetAsync for key {Key}. Loading from source.", key);
        }

        // Step 2: Cache miss - load from source (DB, API, etc.)
        _logger.LogDebug("Cache miss for key: {Key}. Executing factory.", key);
        var data = await factory(ct);

        // Step 3: Store in cache with TTL (±jitter if specified)
        try
        {
            // Only cache if data is not null, OR if cacheNull is true
            if (data is not null || cacheNull)
            {
                var finalTtl = ttl;

                // Apply jitter to prevent cache stampede
                if (jitter is { } j && j > TimeSpan.Zero)
                {
                    // Generate random value between -jitter and +jitter
                    // Example: jitter=30s → random between -30s and +30s
                    var randomFactor = (Random.Shared.NextDouble() * 2) - 1;  // -1.0 to +1.0
                    var ticks = (long)(randomFactor * j.Ticks);
                    finalTtl = ttl + TimeSpan.FromTicks(ticks);

                    // Safety check: don't allow negative or zero TTL
                    if (finalTtl <= TimeSpan.Zero)
                    {
                        _logger.LogWarning(
                            "Jitter calculation resulted in invalid TTL. Using original TTL. Key: {Key}", key);
                        finalTtl = ttl;
                    }

                    _logger.LogTrace(
                        "Applied jitter to TTL for key {Key}. Original: {OriginalTtl}, Final: {FinalTtl}",
                        key, ttl, finalTtl);
                }

                var json = JsonSerializer.Serialize(data, JsonOpts);
                await _db.StringSetAsync(key, json, finalTtl);

                _logger.LogDebug(
                    "Cached factory result for key: {Key}, TTL: {Ttl}", key, finalTtl);
            }
            else
            {
                _logger.LogDebug(
                    "Skipping cache for null result (key: {Key})", key);
            }
        }
        catch (Exception ex)
        {
            // Cache write failed - log but don't throw
            // Data was successfully loaded from source, so return it
            _logger.LogWarning(ex,
                "Redis SET failed in GetOrSetAsync for key {Key}. Returning uncached data.", key);
        }

        return data!;
    }

    /// <inheritdoc />
    public async Task<int> GetTokenAsync(string key, int defaultValue = 1, CancellationToken ct = default)
    {
        ValidateKey(key);

        try
        {
            var val = await _db.StringGetAsync(key);

            if (val.HasValue && int.TryParse(val.ToString(), out var version))
            {
                _logger.LogTrace("Got token {Key} = {Version}", key, version);
                return version;
            }

            _logger.LogDebug(
                "Token {Key} not found. Returning default: {DefaultValue}", key, defaultValue);
            return defaultValue;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Redis GET TOKEN failed for key {Key}. Returning default: {DefaultValue}",
                key, defaultValue);
            return defaultValue;
        }
    }

    /// <inheritdoc />
    public async Task<int> BumpTokenAsync(string key, CancellationToken ct = default)
    {
        ValidateKey(key);

        try
        {
            // StringIncrementAsync is atomic - safe under concurrent access
            var newVersion = await _db.StringIncrementAsync(key);

            // Convert long to int with overflow checking
            var version = checked((int)newVersion);

            _logger.LogInformation(
                "Bumped token {Key} to version {Version}. All caches using this token are now invalidated.",
                key, version);

            return version;
        }
        catch (Exception ex)
        {
            // If bump fails, log error and return current token value
            // This is safe - next read will work from DB, just might use stale cache briefly
            _logger.LogError(ex,
                "Redis INCREMENT failed for token {Key}. Cache invalidation may be delayed.", key);

            return await GetTokenAsync(key);
        }
    }

    /// <summary>
    /// Validates that the cache key is not null, empty, or too long.
    /// Redis has a 512MB key size limit, but we enforce a much smaller limit for sanity.
    /// </summary>
    /// <param name="key">Cache key to validate</param>
    /// <exception cref="ArgumentException">Thrown if key is invalid</exception>
    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));
        }

        // Redis supports keys up to 512MB, but practical limit is much lower
        // Long keys waste memory and slow down lookups
        if (key.Length > 512)
        {
            throw new ArgumentException(
                $"Cache key too long ({key.Length} chars, max 512). Key: {key[..100]}...",
                nameof(key));
        }
    }

    /// <summary>
    /// Validates that the TTL is positive and reasonable.
    /// Prevents accidental misconfiguration that could cause issues.
    /// </summary>
    /// <param name="ttl">Time to live to validate</param>
    /// <exception cref="ArgumentException">Thrown if TTL is invalid</exception>
    private static void ValidateTtl(TimeSpan ttl)
    {
        if (ttl <= TimeSpan.Zero)
        {
            throw new ArgumentException(
                $"TTL must be positive. Got: {ttl}", nameof(ttl));
        }

        // Warn about unreasonably long cache times (more than 24 hours)
        // This might indicate a configuration error
        if (ttl > TimeSpan.FromHours(24))
        {
            // Don't throw - just let it through, but this is unusual
            // You might want to add logging here if you want to track this
        }
    }
}