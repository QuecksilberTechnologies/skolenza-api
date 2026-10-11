using skolenza.application.Interfaces.ICacheService;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace skolenza.infrastructure.CacheMemory
{
    public class InstitutionKeyMemoryCache : IInstitutionKeyCache
    {
        private readonly IMemoryCache _cache;

        public InstitutionKeyMemoryCache(IMemoryCache cache)
        {
            _cache = cache;
        }

        private static string CacheKey(long institutionId)
            => $"INSTITUTION_KEY_{institutionId}";

        public string? Get(long institutionId)
        {
            _cache.TryGetValue(CacheKey(institutionId), out string? key);
            return key;
        }

        public void Set(long institutionId, string encryptedKey)
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6),
                SlidingExpiration = TimeSpan.FromHours(2),
                Priority = CacheItemPriority.High
            };

            _cache.Set(CacheKey(institutionId), encryptedKey, options);
        }

        public void Remove(long institutionId)
        {
            _cache.Remove(CacheKey(institutionId));
        }
    }
}
