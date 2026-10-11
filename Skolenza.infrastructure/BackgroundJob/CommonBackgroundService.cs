using skolenza.application.Interfaces.IRepositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace skolenza.infrastructure.BackgroundJob
{
    // skolenza.infrastructure/Services/Background/ModuleSyncBackgroundService.cs
    public class CommonBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CommonBackgroundService> _logger;

        public CommonBackgroundService(IServiceScopeFactory scopeFactory, ILogger<CommonBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("✅ Common background service started.");
            _logger.LogInformation("✅ Common background service started.");

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(2));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    Console.WriteLine($"⏳ Background task running at {DateTime.Now}");
                    _logger.LogInformation("⏳ Background task running at {Time}", DateTime.Now);

                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var syncService = scope.ServiceProvider.GetRequiredService<ICommonServiceSyncRepository>();

                        await syncService.SyncAllInstitutionsNewModulesAndOperationsAsync();

                        Console.WriteLine("✅ Module sync task completed.");
                        _logger.LogInformation("✅ Module sync task completed at {Time}", DateTime.Now);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error in background service: {ex.Message}");
                        _logger.LogError(ex, "❌ Error occurred while syncing modules & operations.");
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Common background service is stopping.");
            }
        }
    }

}
