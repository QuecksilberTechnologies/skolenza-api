using skolenza.application.DTOs.EmailTemplate;
using skolenza.application.DTOS.Pagination;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Repositories;

public sealed class InstitutionEmailTemplateRepository(SkolenzaDbContext context) : IInstitutionEmailTemplateRepository
{
    public Task<InstitutionEmailTemplate?> GetActiveByCodeAsync(
        long institutionId,
        string templateCode,
        CancellationToken cancellationToken = default)
    {
        if (institutionId <= 0 || string.IsNullOrWhiteSpace(templateCode))
        {
            return Task.FromResult<InstitutionEmailTemplate?>(null);
        }

        var normalizedCode = templateCode.Trim().ToUpperInvariant();
        return context.InstitutionEmailTemplates
            .AsNoTracking()
            .Where(template =>
                template.InstitutionId == institutionId &&
                template.IsActive &&
                template.TemplateCode != null &&
                template.TemplateCode.ToUpper() == normalizedCode)
            .OrderByDescending(template => template.AddedDateTime)
            .ThenByDescending(template => template.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<InstitutionEmailTemplate?> GetByIdAsync(long institutionId, int id, CancellationToken cancellationToken = default) =>
        context.InstitutionEmailTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.InstitutionId == institutionId && x.Id == id, cancellationToken);

    public Task<InstitutionEmailTemplate?> GetForUpdateAsync(long institutionId, int id, CancellationToken cancellationToken = default) =>
        context.InstitutionEmailTemplates.FirstOrDefaultAsync(x => x.InstitutionId == institutionId && x.Id == id, cancellationToken);

    public async Task<PagedResponseDTO<InstitutionEmailTemplate>> GetPagedAsync(long institutionId, InstitutionEmailTemplateListRequestDTO filter, CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(filter.PageNumber, 1);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var query = context.InstitutionEmailTemplates.AsNoTracking().Where(x => x.InstitutionId == institutionId);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = $"%{filter.Search.Trim()}%";
            query = query.Where(x => EF.Functions.ILike(x.TemplateName, search) ||
                                     (x.TemplateCode != null && EF.Functions.ILike(x.TemplateCode, search)) ||
                                     EF.Functions.ILike(x.Subject, search) ||
                                     (x.Category != null && EF.Functions.ILike(x.Category, search)));
        }
        if (!string.IsNullOrWhiteSpace(filter.Category)) query = query.Where(x => x.Category != null && EF.Functions.ILike(x.Category, filter.Category.Trim()));
        if (!string.IsNullOrWhiteSpace(filter.LanguageCode)) query = query.Where(x => x.LanguageCode != null && EF.Functions.ILike(x.LanguageCode, filter.LanguageCode.Trim()));
        if (filter.IsActive.HasValue) query = query.Where(x => x.IsActive == filter.IsActive.Value);
        var totalCount = await query.CountAsync(cancellationToken);
        var data = await query.OrderByDescending(x => x.IsActive).ThenBy(x => x.TemplateName).ThenBy(x => x.Id)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PagedResponseDTO<InstitutionEmailTemplate>(data, totalCount, pageNumber, pageSize)
        {
            TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize))
        };
    }

    public Task<bool> TemplateCodeExistsAsync(long institutionId, string templateCode, int? excludedId = null, CancellationToken cancellationToken = default)
    {
        var code = templateCode.Trim().ToUpperInvariant();
        return context.InstitutionEmailTemplates.AnyAsync(x => x.InstitutionId == institutionId && x.TemplateCode != null &&
            x.TemplateCode.ToUpper() == code && (!excludedId.HasValue || x.Id != excludedId.Value), cancellationToken);
    }

    public Task<List<string>> GetTemplateCodesAsync(
        long institutionId,
        CancellationToken cancellationToken = default) =>
        context.InstitutionEmailTemplates
            .AsNoTracking()
            .Where(template => template.InstitutionId == institutionId && template.TemplateCode != null)
            .Select(template => template.TemplateCode!)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(InstitutionEmailTemplate template, CancellationToken cancellationToken = default) =>
        await context.InstitutionEmailTemplates.AddAsync(template, cancellationToken);

    public async Task AddRangeAsync(
        IEnumerable<InstitutionEmailTemplate> templates,
        CancellationToken cancellationToken = default) =>
        await context.InstitutionEmailTemplates.AddRangeAsync(templates, cancellationToken);

    public void Remove(InstitutionEmailTemplate template) => context.InstitutionEmailTemplates.Remove(template);
}
