using Gringotts.Contracts.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Gringotts.Infrastructure.Caching;

internal class MemoryCache(IMemoryCache cache) : ICache
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
        => Task.FromResult(cache.TryGetValue(key, out T? value) ? value : default);

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var opts = new MemoryCacheEntryOptions();
        if (ttl is not null) opts.SetAbsoluteExpiration(ttl.Value);
        cache.Set(key, value, opts);
        return Task.CompletedTask;
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken ct = default)
    {
        if (cache.TryGetValue(key, out T? value) && value is not null)
            return value;

        var created = await factory(ct).ConfigureAwait(false);
        await SetAsync(key, created, ttl, ct);
        return created;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        cache.Remove(key);
        return Task.CompletedTask;
    }
}
