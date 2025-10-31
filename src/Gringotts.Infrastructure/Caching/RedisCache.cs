using Gringotts.Contracts.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace Gringotts.Infrastructure.Caching;

public sealed class RedisCache(IDistributedCache dist) : ICache
{
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        return await GetInternalAsync<T>(key, ct);

        async Task<TT?> GetInternalAsync<TT>(string k, CancellationToken c)
        {
            var bytes = await dist.GetAsync(k, c).ConfigureAwait(false);
            return CacheJson.Deserialize<TT>(bytes);
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var bytes = CacheJson.Serialize(value);
        var opts = new DistributedCacheEntryOptions();
        if (ttl is not null) opts.SetAbsoluteExpiration(ttl.Value);
        return dist.SetAsync(key, bytes, opts, ct);
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken ct = default)
    {
        var existing = await GetAsync<T>(key, ct).ConfigureAwait(false);
        if (existing is not null) return existing;

        var created = await factory(ct).ConfigureAwait(false);
        await SetAsync(key, created, ttl, ct).ConfigureAwait(false);
        return created;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
        => dist.RemoveAsync(key, ct);
}
