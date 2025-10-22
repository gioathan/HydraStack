using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hydra.Api.Caching;

/// <summary>
/// Distributed cache abstraction with built-in resilience and version-based invalidation.
/// All operations fail gracefully if the cache is unavailable - they never throw exceptions
/// that would crash the application. This "fail-open" strategy ensures the API remains
/// responsive even when Redis is down (it just runs slower without caching).
/// </summary>
public interface ICache
{
    /// <summary>
    /// Gets a cached value by key.
    /// </summary>
    /// <typeparam name="T">Type of the cached value</typeparam>
    /// <param name="key">Cache key (should be generated using CacheKeys class)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>
    /// The cached value if found and cache is available, otherwise default(T).
    /// Returns default if key doesn't exist OR if cache is unavailable.
    /// </returns>
    /// <example>
    /// var venue = await _cache.GetAsync&lt;VenueDto&gt;("hb:venues:v1:guid");
    /// if (venue != null) { /* cache hit */ }
    /// </example>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>
    /// Stores a value in the cache with a specified expiration time.
    /// </summary>
    /// <typeparam name="T">Type of the value to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache (will be serialized to JSON)</param>
    /// <param name="ttl">Time to live - how long before the cache entry expires</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Task that completes when the value is stored (or fails silently)</returns>
    /// <example>
    /// await _cache.SetAsync("hb:venues:v1:guid", venue, TimeSpan.FromMinutes(20));
    /// </example>
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    /// <param name="key">Cache key to remove</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if the key was removed, false if it didn't exist or cache unavailable</returns>
    Task<bool> RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Cache-aside pattern helper: tries to get from cache, if not found executes the factory
    /// function to load data, then caches the result before returning it.
    /// This is the recommended method for most caching scenarios.
    /// </summary>
    /// <typeparam name="T">Type of data to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="ttl">Time to live for the cached value</param>
    /// <param name="factory">
    /// Function to execute if cache miss. This typically queries the database.
    /// </param>
    /// <param name="cacheNull">
    /// If true, cache null results. Useful for preventing repeated DB queries for non-existent data.
    /// Default is false (don't cache nulls).
    /// </param>
    /// <param name="jitter">
    /// Optional random time variation to add to TTL. Prevents cache stampede by ensuring
    /// not all cache entries expire at exactly the same time.
    /// Example: jitter of 30 seconds means actual TTL will be (ttl ± 30 seconds).
    /// </param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>
    /// The cached or freshly loaded value. Never throws - if cache fails, just executes factory.
    /// </returns>
    /// <remarks>
    /// <para><strong>Flow:</strong></para>
    /// <list type="number">
    /// <item>Try to get value from cache</item>
    /// <item>If found → return immediately (cache hit)</item>
    /// <item>If not found → execute factory to load from source (DB)</item>
    /// <item>Store result in cache with TTL (±jitter if specified)</item>
    /// <item>Return the loaded value</item>
    /// </list>
    /// <para><strong>Resilience:</strong></para>
    /// If Redis is down, the method gracefully falls back to just executing the factory.
    /// Your application continues working, just without the caching benefit.
    /// </remarks>
    /// <example>
    /// <code>
    /// var venue = await _cache.GetOrSetAsync(
    ///     key: CacheKeys.VenueDetail(id, version),
    ///     ttl: CacheKeys.Ttl.VenueDetail,
    ///     factory: async ct => await _db.Venues.FindAsync(id, ct),
    ///     jitter: CacheKeys.Jitter.Venues,
    ///     ct: cancellationToken);
    /// </code>
    /// </example>
    Task<T> GetOrSetAsync<T>(
        string key,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        bool cacheNull = false,
        TimeSpan? jitter = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a version token used for group-based cache invalidation.
    /// Version tokens are small integers stored in Redis that increment when data changes.
    /// All cache keys include the version number, so bumping the version effectively
    /// invalidates all caches using that version without having to delete individual keys.
    /// </summary>
    /// <param name="key">Token key (e.g., CacheKeys.VenuesToken)</param>
    /// <param name="defaultValue">Value to return if token doesn't exist yet (default is 1)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Current version number, or defaultValue if not found/unavailable</returns>
    /// <example>
    /// <code>
    /// var version = await _cache.GetTokenAsync(CacheKeys.VenuesToken);  // Returns: 1
    /// var key = CacheKeys.VenuesList(version);  // "hb:venues:list:v1"
    /// </code>
    /// </example>
    Task<int> GetTokenAsync(string key, int defaultValue = 1, CancellationToken ct = default);

    /// <summary>
    /// Increments a version token to invalidate all caches using that version.
    /// This is an atomic operation - thread-safe even under high concurrency.
    /// Use this after creating, updating, or deleting data to ensure cached data doesn't go stale.
    /// </summary>
    /// <param name="key">Token key to bump (e.g., CacheKeys.VenuesToken)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>New version number after increment</returns>
    /// <remarks>
    /// <para><strong>How version-based invalidation works:</strong></para>
    /// <list type="number">
    /// <item>Initially: version = 1, caches use keys like "hb:venues:v1:..."</item>
    /// <item>Data changes: BumpTokenAsync increments version to 2</item>
    /// <item>New requests: use version 2, create keys like "hb:venues:v2:..."</item>
    /// <item>Old "v1" keys are ignored and expire naturally based on TTL</item>
    /// </list>
    /// <para><strong>Benefits:</strong></para>
    /// <list type="bullet">
    /// <item>No need to track and delete individual cache keys</item>
    /// <item>Atomic - no race conditions</item>
    /// <item>Works even if you don't know all the cached keys</item>
    /// <item>Old caches clean themselves up via TTL expiration</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // After updating a venue
    /// await _cache.BumpTokenAsync(CacheKeys.VenuesToken);
    /// // All venue-related caches are now invalidated
    /// </code>
    /// </example>
    Task<int> BumpTokenAsync(string key, CancellationToken ct = default);
}