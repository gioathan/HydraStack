using System.Text.Json;
using StackExchange.Redis;

namespace Hydra.Api.Caching;

public class RedisCache : ICache
{
    private readonly IDatabase _db;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
    private static readonly Random Rng = new();

    public RedisCache(IConnectionMultiplexer mux)
    {
        _db = mux.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var val = await _db.StringGetAsync(key);
            if (!val.HasValue) return default;
            return JsonSerializer.Deserialize<T>(val!, JsonOpts);
        }
        catch
        {
            // Redis unavailable? Fail open (no cache) to keep API responsive.
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        try
        {
            await _db.StringSetAsync(key, JsonSerializer.Serialize(value, JsonOpts), ttl);
        }
        catch
        {
            // swallow cache errors
        }
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken ct = default)
    {
        try { return await _db.KeyDeleteAsync(key); }
        catch { return false; }
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        bool cacheNull = false,
        TimeSpan? jitter = null,
        CancellationToken ct = default)
    {
        // 1) Try cache
        try
        {
            var cached = await _db.StringGetAsync(key);
            if (cached.HasValue)
                return JsonSerializer.Deserialize<T>(cached!, JsonOpts)!;
        }
        catch { /* ignore and fall back */ }

        // 2) Load from source
        var data = await factory(ct);

        // 3) Store with TTL (+/- jitter if provided)
        try
        {
            if (data is not null || cacheNull)
            {
                var finalTtl = ttl;
                if (jitter is { } j && j > TimeSpan.Zero)
                {
                    // random between -j..+j
                    var ticks = (long)((Rng.NextDouble() * 2 - 1) * j.Ticks);
                    finalTtl = ttl + TimeSpan.FromTicks(ticks);
                    if (finalTtl <= TimeSpan.Zero) finalTtl = ttl; // guard
                }

                await _db.StringSetAsync(key, JsonSerializer.Serialize(data, JsonOpts), finalTtl);
            }
        }
        catch { /* ignore cache write failures */ }

        return data!;
    }

    public async Task<int> GetTokenAsync(string key, int defaultValue = 1, CancellationToken ct = default)
    {
        try
        {
            var val = await _db.StringGetAsync(key);
            if (val.HasValue && int.TryParse(val.ToString(), out var n))
                return n;
        }
        catch { /* ignore */ }
        return defaultValue;
    }

    public async Task<int> BumpTokenAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var v = await _db.StringIncrementAsync(key);
            return checked((int)v);
        }
        catch
        {
            // if bump fails, keep going; next read will still work from DB
            return await GetTokenAsync(key);
        }
    }
}
