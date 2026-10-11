using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICacheService;
using skolenza.application.Interfaces.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace skolenza.infrastructure.CacheMemory
{
    public class InstitutionKeyResolver : IInstitutionKeyResolver
    {
        private readonly IInstitutionKeyCache _cache;
        private readonly IUnitOfWork _unitOfWork;

        public InstitutionKeyResolver(
            IInstitutionKeyCache cache,
            IUnitOfWork unitOfWork)
        {
            _cache = cache;
            _unitOfWork = unitOfWork;
        }

        public async Task<string> ResolveAsync(long institutionId)
        {
            // 1️⃣ Try RAM first
            var key = _cache.Get(institutionId);
            if (!string.IsNullOrEmpty(key))
                return key;

            // 2️⃣ DB hit (only once)
            var institution = await _unitOfWork.InstitutionEncryptionKeyRepository.GetActiveKeyByInstitutionIdAsync(institutionId);

            if (institution == null)
                throw new UnauthorizedAccessException("Invalid institution");

            // 3️⃣ Store in RAM
            _cache.Set(institutionId, institution.EncryptionKey);

            return institution.EncryptionKey;
        }
    }
}
