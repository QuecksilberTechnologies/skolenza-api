using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IRepositories;

/// <summary>
/// Provides persistence operations for the Host-managed default SMTP configuration
/// used when a Institution is registered for the first time.
/// </summary>
public interface IDefaultEmailConfigRepository
{
    Task<DefaultEmailConfig?> GetActiveDefaultEmailConfigAsync(CancellationToken cancellationToken = default);
    Task<DefaultEmailConfig?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<DefaultEmailConfig?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DefaultEmailConfig>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ConfigNameExistsAsync(string configName, int? excludedId = null, CancellationToken cancellationToken = default);
    Task<bool> HasAnotherActiveConfigAsync(int excludedId, CancellationToken cancellationToken = default);
    Task ClearExistingDefaultAsync(int? excludedId, CancellationToken cancellationToken = default);
    Task AddAsync(DefaultEmailConfig configuration, CancellationToken cancellationToken = default);
    void Remove(DefaultEmailConfig configuration);
}
