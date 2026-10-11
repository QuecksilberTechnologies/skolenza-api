using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICacheService;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace skolenza.infrastructure.CacheMemory
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;

        public CacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task SetAsync<T>(string key, T value, int minutes = 30)
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(minutes)
            };

            _cache.Set(key, value, options);
            return Task.CompletedTask;
        }

        public Task<T?> GetAsync<T>(string key)
        {
            if (_cache.TryGetValue(key, out var cachedValue))
            {
                return Task.FromResult((T?)cachedValue);
            }
            return Task.FromResult(default(T));
        }

        public Task RemoveAsync(string key)
        {
            _cache.Remove(key);
            return Task.CompletedTask;
        }
    }
}
