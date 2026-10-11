using skolenza.application.DTOs.EmailTemplate;
using skolenza.application.DTOS.Pagination;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Repositories;

/// <summary>
/// Persists centrally managed email templates and protects delivery history from unsafe deletion.
/// </summary>
public sealed class EmailTemplateRepository(SkolenzaDbContext context) : IEmailTemplateRepository
{
    public Task<EmailTemplate?> GetTemplateByCodeAsync(
        string templateCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templateCode))
        {
            return Task.FromResult<EmailTemplate?>(null);
        }

        var normalizedCode = templateCode.Trim().ToUpperInvariant();
        return context.EmailTemplates
            .AsNoTracking()
            .Where(template => template.IsActive &&
                               template.TemplateCode != null &&
                               template.TemplateCode.ToUpper() == normalizedCode)
            .OrderByDescending(template => template.AddedDateTime)
            .ThenByDescending(template => template.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<EmailTemplate?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        context.EmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(template => template.Id == id, cancellationToken);

    public Task<List<EmailTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default) =>
        context.EmailTemplates
            .AsNoTracking()
            .Where(template => template.IsActive && template.TemplateCode != null)
            .OrderBy(template => template.TemplateCode)
            .ThenBy(template => template.Id)
            .ToListAsync(cancellationToken);

    public Task<EmailTemplate?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        context.EmailTemplates
            .FirstOrDefaultAsync(template => template.Id == id, cancellationToken);

    public async Task<PagedResponseDTO<EmailTemplate>> GetPagedAsync(
        EmailTemplateListRequestDTO filter,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(filter.PageNumber, 1);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        IQueryable<EmailTemplate> query = context.EmailTemplates.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = $"%{filter.Search.Trim()}%";
            query = query.Where(template =>
                EF.Functions.ILike(template.TemplateName, search) ||
                (template.TemplateCode != null && EF.Functions.ILike(template.TemplateCode, search)) ||
                EF.Functions.ILike(template.Subject, search) ||
                (template.Category != null && EF.Functions.ILike(template.Category, search)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            query = query.Where(template =>
                template.Category != null &&
                EF.Functions.ILike(template.Category, filter.Category.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(filter.LanguageCode))
        {
            query = query.Where(template =>
                template.LanguageCode != null &&
                EF.Functions.ILike(template.LanguageCode, filter.LanguageCode.Trim()));
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(template => template.IsActive == filter.IsActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var data = await query
            .OrderByDescending(template => template.IsActive)
            .ThenBy(template => template.TemplateName)
            .ThenBy(template => template.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponseDTO<EmailTemplate>(data, totalCount, pageNumber, pageSize)
        {
            TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize))
        };
    }

    public Task<bool> TemplateCodeExistsAsync(
        string templateCode,
        int? excludedId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = templateCode.Trim().ToUpperInvariant();
        return context.EmailTemplates.AnyAsync(template =>
            template.TemplateCode != null &&
            template.TemplateCode.ToUpper() == normalizedCode &&
            (!excludedId.HasValue || template.Id != excludedId.Value),
            cancellationToken);
    }

    public Task<bool> HasEmailQueueEntriesAsync(int templateId, CancellationToken cancellationToken = default) =>
        context.EmailQueues.AnyAsync(queue => queue.TemplateId == templateId, cancellationToken);

    public async Task AddAsync(EmailTemplate template, CancellationToken cancellationToken = default) =>
        await context.EmailTemplates.AddAsync(template, cancellationToken);

    public void Remove(EmailTemplate template) => context.EmailTemplates.Remove(template);
}
