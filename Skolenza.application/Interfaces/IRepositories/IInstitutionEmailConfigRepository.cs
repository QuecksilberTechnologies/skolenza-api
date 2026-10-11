using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IRepositories;

/// <summary>
/// Provides Institution-scoped SMTP configuration persistence. SMTP secrets are
/// handled only by command handlers and are never projected to an API response.
/// </summary>
public interface IInstitutionEmailConfigRepository
{
    Task<InstitutionEmailConfig?> GetActiveEmailConfigAsync(long? institutionId);
    Task<InstitutionEmailConfig?> UpdateEmailConfigAsync(InstitutionEmailConfig? config);
    Task<InstitutionEmailConfig?> InsertEmailConfigAsync(InstitutionEmailConfig? config);

    Task<InstitutionEmailConfig?> GetByIdAsync(long institutionId, int id, CancellationToken cancellationToken = default);
    Task<InstitutionEmailConfig?> GetForUpdateAsync(long institutionId, int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InstitutionEmailConfig>> GetAllAsync(long institutionId, CancellationToken cancellationToken = default);
    Task DeactivateOtherActiveAsync(long institutionId, int? excludedId, CancellationToken cancellationToken = default);
    Task AddAsync(InstitutionEmailConfig configuration, CancellationToken cancellationToken = default);
    void Remove(InstitutionEmailConfig configuration);
}
