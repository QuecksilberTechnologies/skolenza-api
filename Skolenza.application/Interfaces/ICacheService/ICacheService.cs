using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Interfaces.ICacheService
{
    public interface ICacheService
    {
        Task SetAsync<T>(string key, T value, int minutes = 30);
        Task<T?> GetAsync<T>(string key);
        Task RemoveAsync(string key);
    }

    public interface IInstitutionKeyCache
    {
        string? Get(long institutionId);
        void Set(long institutionId, string encryptedKey);
        void Remove(long institutionId);
    }
}
