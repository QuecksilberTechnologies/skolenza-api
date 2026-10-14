using skolenza.application.Common.Models;
using skolenza.application.Interfaces.IRepositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace skolenza.infrastructure.BackgroundJob;

/// <summary>Runs bounded durable batches. PostgreSQL transaction locks provide restart recovery and multi-instance exclusion.</summary>
public sealed class BulkImportWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<BulkImportOptions> options,
    ILogger<BulkImportWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.WorkerEnabled)
        {
            logger.LogInformation("Bulk import worker disabled. Enable BulkImport:WorkerEnabled after applying its migrations.");
            return;
        }
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Value.PollIntervalSeconds));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                using var batchCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                batchCancellation.CancelAfter(TimeSpan.FromSeconds(options.Value.BatchTimeoutSeconds));
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<IBulkImportRepository>()
                        .ProcessNextBatchAsync(batchCancellation.Token);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception error)
                {
                    // No cursor/business inserts commit on an interrupted batch. A fresh scope retries next tick.
                    logger.LogError(error, "Bulk import batch interrupted; durable uncommitted work will be retried.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal host shutdown can cancel while the worker is waiting for the next poll.
        }
    }
}
