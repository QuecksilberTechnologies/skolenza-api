using skolenza.application.Interfaces.IRepositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using skolenza.domain.Entity;

namespace skolenza.persistance.Repositories
{
    public class CommonServiceSyncRepository : ICommonServiceSyncRepository
    {
        private readonly ILogger<CommonServiceSyncRepository> _logger;

        public CommonServiceSyncRepository(ILogger<CommonServiceSyncRepository> logger)
        {
            _logger = logger;
        }

        public async Task SyncAllInstitutionsNewModulesAndOperationsAsync()
        {
            // your actual module/operation sync logic here
            _logger.LogInformation("Sync logic started...");

            await Task.CompletedTask;
        }
    }

}
