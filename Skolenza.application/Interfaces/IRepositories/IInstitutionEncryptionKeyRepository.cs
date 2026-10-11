using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IRepositories
{
    public interface IInstitutionEncryptionKeyRepository
    {
        Task<InstitutionEncryptionKeys?> GetActiveKeyByInstitutionIdAsync(long institutionId, CancellationToken cancellationToken = default);
        Task AddAsync(InstitutionEncryptionKeys institutionKey, CancellationToken cancellationToken = default);
       Task UpdateAsync(InstitutionEncryptionKeys institutionKey, CancellationToken cancellationToken = default);

    }
}