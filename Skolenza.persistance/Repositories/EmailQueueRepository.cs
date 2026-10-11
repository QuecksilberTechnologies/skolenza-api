using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Repositories;

public sealed class EmailQueueRepository(SkolenzaDbContext context) : IEmailQueueRepository
{
    public async Task<int?> EnqueueAsync(string templateCode, string toEmail, long? institutionId, string placeholdersJson, CancellationToken cancellationToken = default)
    {
        var normalizedCode = templateCode.Trim().ToUpperInvariant();
        var defaultTemplate = await context.EmailTemplates.AsNoTracking()
            .Where(x => x.IsActive && x.TemplateCode != null && x.TemplateCode.ToUpper() == normalizedCode)
            .OrderByDescending(x => x.AddedDateTime).ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (defaultTemplate is null) return null;

        var queue = new EmailQueue
        {
            TemplateId = defaultTemplate.Id,
            InstitutionId = institutionId,
            TemplateCode = normalizedCode,
            ToEmail = toEmail.Trim(),
            PlaceholdersJson = placeholdersJson,
            IsSent = false,
            RetryCount = 0,
            IsProcessing = false,
            AddedDateTime = DateTime.UtcNow
        };
        await context.EmailQueues.AddAsync(queue, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return queue.Id;
    }

    public async Task<EmailQueue?> ClaimNextAsync(int maximumRetries, DateTime staleBeforeUtc, CancellationToken cancellationToken = default)
    {
        var claimed = await context.EmailQueues.FromSqlInterpolated($@"
            WITH candidate AS (
                SELECT ""Id"" FROM skolenza.""EmailQueue""
                WHERE COALESCE(""IsSent"", FALSE) = FALSE
                  AND COALESCE(""RetryCount"", 0) < {maximumRetries}
                  AND (COALESCE(""IsProcessing"", FALSE) = FALSE OR ""ProcessingStartedDateTime"" < {staleBeforeUtc})
                ORDER BY ""AddedDateTime"", ""Id""
                FOR UPDATE SKIP LOCKED LIMIT 1)
            UPDATE skolenza.""EmailQueue"" queue
            SET ""IsProcessing"" = TRUE, ""ProcessingStartedDateTime"" = CURRENT_TIMESTAMP
            FROM candidate WHERE queue.""Id"" = candidate.""Id""
            RETURNING queue.*")
            .AsNoTracking().ToListAsync(cancellationToken);
        return claimed.SingleOrDefault();
    }

    public async Task MarkSentAsync(int id, DateTime sentUtc, CancellationToken cancellationToken = default) =>
        await context.EmailQueues.Where(x => x.Id == id).ExecuteUpdateAsync(setters => setters
            .SetProperty(x => x.IsSent, true).SetProperty(x => x.SendDateTime, sentUtc)
            .SetProperty(x => x.ErrorMessage, (string?)null).SetProperty(x => x.IsProcessing, false)
            .SetProperty(x => x.ProcessingStartedDateTime, (DateTime?)null), cancellationToken);

    public async Task MarkFailedAsync(int id, string errorMessage, CancellationToken cancellationToken = default) =>
        await context.EmailQueues.Where(x => x.Id == id).ExecuteUpdateAsync(setters => setters
            .SetProperty(x => x.RetryCount, x => (x.RetryCount ?? 0) + 1)
            .SetProperty(x => x.ErrorMessage, errorMessage.Length <= 2000 ? errorMessage : errorMessage[..2000])
            .SetProperty(x => x.IsProcessing, false).SetProperty(x => x.ProcessingStartedDateTime, (DateTime?)null), cancellationToken);
}
