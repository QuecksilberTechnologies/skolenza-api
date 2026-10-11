using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IRepositories;

public interface IEmailQueueRepository
{
    Task<int?> EnqueueAsync(string templateCode, string toEmail, long? institutionId, string placeholdersJson, CancellationToken cancellationToken = default);
    Task<EmailQueue?> ClaimNextAsync(int maximumRetries, DateTime staleBeforeUtc, CancellationToken cancellationToken = default);
    Task MarkSentAsync(int id, DateTime sentUtc, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(int id, string errorMessage, CancellationToken cancellationToken = default);
}
