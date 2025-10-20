using System.Threading;

namespace Hydra.Api.Caching;

public interface ICache
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);
    Task<bool> RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Cache-aside helper: try Redis, otherwise load via factory, then store with TTL.
    /// Never throws because Redis is down; it just falls back to the factory.
    /// </summary>
    Task<T> GetOrSetAsync<T>(
        string key,
        TimeSpan ttl,
        Func<CancellationToken, Task<T>> factory,
        bool cacheNull = false,
        TimeSpan? jitter = null,
        CancellationToken ct = default);

    /// <summary>Get a small integer token used to version a group of keys.</summary>
    Task<int> GetTokenAsync(string key, int defaultValue = 1, CancellationToken ct = default);

    /// <summary>Increment a token to invalidate a whole group (bump version).</summary>
    Task<int> BumpTokenAsync(string key, CancellationToken ct = default);
}
