using skolenza.application.DTOs.EmailTemplate;
using skolenza.application.DTOS.Pagination;
using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IRepositories;

public interface IInstitutionEmailTemplateRepository
{
    Task<InstitutionEmailTemplate?> GetActiveByCodeAsync(long institutionId, string templateCode, CancellationToken cancellationToken = default);
    Task<InstitutionEmailTemplate?> GetByIdAsync(long institutionId, int id, CancellationToken cancellationToken = default);
    Task<InstitutionEmailTemplate?> GetForUpdateAsync(long institutionId, int id, CancellationToken cancellationToken = default);
    Task<PagedResponseDTO<InstitutionEmailTemplate>> GetPagedAsync(long institutionId, InstitutionEmailTemplateListRequestDTO filter, CancellationToken cancellationToken = default);
    Task<bool> TemplateCodeExistsAsync(long institutionId, string templateCode, int? excludedId = null, CancellationToken cancellationToken = default);
    Task<List<string>> GetTemplateCodesAsync(long institutionId, CancellationToken cancellationToken = default);
    Task AddAsync(InstitutionEmailTemplate template, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<InstitutionEmailTemplate> templates, CancellationToken cancellationToken = default);
    void Remove(InstitutionEmailTemplate template);
}
