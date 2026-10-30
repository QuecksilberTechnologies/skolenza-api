using System.Text.Json;
using skolenza.application.Constants;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IEmail;
using skolenza.application.Interfaces.IRepositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace skolenza.infrastructure.BackgroundJob;

public sealed class EmailQueueWorker(IServiceScopeFactory scopeFactory, ILogger<EmailQueueWorker> logger) : BackgroundService
{
    private const int MaximumRetries = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var queueRepository = scope.ServiceProvider.GetRequiredService<IEmailQueueRepository>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var item = await queueRepository.ClaimNextAsync(MaximumRetries, DateTime.UtcNow.AddMinutes(-10), stoppingToken);
                if (item is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                try
                {
                    var placeholders = string.IsNullOrWhiteSpace(item.PlaceholdersJson)
                        ? new Dictionary<string, string>()
                        : JsonSerializer.Deserialize<Dictionary<string, string>>(item.PlaceholdersJson) ?? [];
                    var templateCode = item.TemplateCode;
                    if (string.IsNullOrWhiteSpace(templateCode))
                    {
                        await queueRepository.MarkFailedAsync(item.Id, "Template code is missing.", stoppingToken);
                        continue;
                    }

                    if ((templateCode == StudentConstants.PortalInvitationTemplateCode ||
                         templateCode == StudentConstants.PortalPasswordResetTemplateCode) &&
                        placeholders.TryGetValue(StudentConstants.PortalEmailProtectedPayloadKey, out var protectedPayload))
                    {
                        if (!item.InstitutionId.HasValue)
                        {
                            throw new InvalidOperationException("A Student portal email requires an institution.");
                        }

                        var protector = scope.ServiceProvider.GetRequiredService<IStudentPortalEmailProtector>();
                        var decrypted = await protector.UnprotectAsync(item.InstitutionId.Value, protectedPayload, stoppingToken);
                        placeholders = JsonSerializer.Deserialize<Dictionary<string, string>>(decrypted)
                            ?? throw new InvalidOperationException("The protected portal email payload is invalid.");
                    }

                    var sent = await emailService.SendTemplatedEmailAsync(
                        templateCode,
                        item.ToEmail,
                        item.InstitutionId,
                        placeholders);
                    if (sent)
                    {
                        await queueRepository.MarkSentAsync(item.Id, DateTime.UtcNow, stoppingToken);
                    }
                    else
                    {
                        await queueRepository.MarkFailedAsync(
                            item.Id,
                            "Email delivery failed; see service logs.",
                            stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Email queue item {EmailQueueId} failed.", item.Id);
                    await queueRepository.MarkFailedAsync(item.Id, exception.Message, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception)
            {
                logger.LogError(exception, "Email queue processing failed.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
