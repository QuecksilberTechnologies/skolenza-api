using AutoMapper;
using skolenza.application.DTOs.Institution;
using skolenza.application.Interfaces.IRepositories;

using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using skolenza.domain.Entity;

namespace skolenza.persistance.Repositories
{
    public class InstitutionModuleConfigurationRepository : IInstitutionModuleConfigurationRepository
    {
        private readonly SkolenzaDbContext _context;

        private readonly IMapper _mapper;
        private readonly ILogger<InstitutionModuleConfigurationRepository> _logger;

        public InstitutionModuleConfigurationRepository(
            SkolenzaDbContext context,
            ILogger<InstitutionModuleConfigurationRepository> logger,
            IMapper mapper
            )
        {
            _context = context;
            this._logger = logger;
            _mapper = mapper;

        }


       
      
        public async Task<GetModuleHierarchyResponseDTO> GetAllInstitutionEnabledModulesAsync(InstitutionEnabledOperation dto)
        {
            try
            {
                long? institutionId = dto.InstitutionId;

                // Step 1: Get all enabled modules for institution
                var moduleEntities = await _context.InstitutionEnabledModules
                    .Where(t => t.InstitutionId == institutionId && t.IsEnabled && t.IsLeafNode != true)
                    .Include(t => t.Module)
                    .ThenInclude(m => m.ParentModule)
                    .ToListAsync();

                // Step 2: Map to flat list
                var flatList = moduleEntities.Select(t => new ModuleNodedto
                {
                    Id = t.Module.Id,
                    ModuleName = t.Module.ModuleName,
                    ParentModuleId = t.Module.ParentModuleId,
                    IsEnabled = t.IsEnabled
                }).ToList();

                // Step 3: Build hierarchy
                var lookup = flatList.ToDictionary(x => x.Id, x => x);
                List<ModuleNodedto> rootModules = new();

                foreach (var module in flatList)
                {
                    if (module.ParentModuleId.HasValue && lookup.ContainsKey(module.ParentModuleId.Value))
                    {
                        lookup[module.ParentModuleId.Value].Children.Add(module);
                    }
                    else
                    {
                        rootModules.Add(module);
                    }
                }

                return new GetModuleHierarchyResponseDTO
                {
                    InstitutionId = institutionId,
                    Modules = rootModules
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching institution enabled modules.");
                return new GetModuleHierarchyResponseDTO
                {
                    InstitutionId = dto.InstitutionId,
                    Modules = new List<ModuleNodedto>()
                };
            }
        }

        

        public async Task CreateByDefaultEnabledModulesAsync(
      long institutionId,
      List<InstitutionEnabledModule> moduleEntities,
      List<InstitutionEnabledOperation> operationEntities)
        {
            try
            {
                if ((moduleEntities == null || !moduleEntities.Any()) &&
                    (operationEntities == null || !operationEntities.Any()))
                {
                    _logger.LogWarning(
                        "No module or operation entities found to add for InstitutionId: {InstitutionId}",
                        institutionId);
                    return;
                }

                if (moduleEntities != null && moduleEntities.Any())
                {
                    foreach (var module in moduleEntities)
                    {
                        module.InstitutionId = institutionId;
                        module.IsEnabled = true;
                        module.AddedById = institutionId;
                        module.AddedDateTime = DateTime.UtcNow;
                    }

                    await _context.InstitutionEnabledModules.AddRangeAsync(moduleEntities);

                    _logger.LogInformation(
                        "Institution enabled modules added to DbContext. Count: {Count}, InstitutionId: {InstitutionId}",
                        moduleEntities.Count,
                        institutionId);
                }

                if (operationEntities != null && operationEntities.Any())
                {
                    foreach (var operation in operationEntities)
                    {
                        operation.InstitutionId = institutionId;
                        operation.IsEnabled = true;
                        operation.IsOperationUsed = true;
                        operation.AddedById = institutionId;
                        operation.AddedDateTime = DateTime.UtcNow;
                    }

                    await _context.InstitutionEnabledOperations.AddRangeAsync(operationEntities);

                    _logger.LogInformation(
                        "Institution enabled operations added to DbContext. Count: {Count}, InstitutionId: {InstitutionId}",
                        operationEntities.Count,
                        institutionId);
                }

                _logger.LogInformation(
                    "Institution default module/operation entities prepared successfully for InstitutionId: {InstitutionId}",
                    institutionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while preparing default enabled modules/operations for InstitutionId: {InstitutionId}",
                    institutionId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<InstitutionPlanEntitlementSyncResult?> SynchronizeMissingActivePlanEntitlementsAsync(
            long institutionId,
            long addedById,
            CancellationToken cancellationToken = default)
        {
            var activeSubscription = await _context.InstitutionSubscriptions
                .AsNoTracking()
                .Where(subscription =>
                    subscription.InstitutionId == institutionId &&
                    subscription.IsActive &&
                    subscription.SubscriptionPlan.IsActive &&
                    !subscription.SubscriptionPlan.IsSoftDeleted)
                .OrderByDescending(subscription => subscription.SubscriptionStartDate)
                .ThenByDescending(subscription => subscription.Id)
                .Select(subscription => new { subscription.SubscriptionPlanId })
                .FirstOrDefaultAsync(cancellationToken);

            if (activeSubscription is null)
            {
                return null;
            }

            // InstitutionEnabledModule receives every directly mapped, active Institution-scope Module.
            // Common and leaf-node restrictions do not apply to this module snapshot.
            var sourceModules = (await _context.PlanModuleMappings
                    .AsNoTracking()
                    .Where(mapping =>
                        mapping.SubscriptionPlanId == activeSubscription.SubscriptionPlanId &&
                        mapping.IsActive == true &&
                        mapping.Module.IsActive &&
                        mapping.Module.ModuleScope == (short)skolenza.application.Constants.AppConstants.InstitutionModuleScope)
                    .Select(mapping => new InstitutionPlanEntitlementModuleSyncResponseDTO
                    {
                        ModuleId = mapping.ModuleId,
                        ModuleName = mapping.Module.ModuleName,
                        DisplayName = mapping.Module.DisplayName,
                        ParentModuleId = mapping.Module.ParentModuleId,
                        IsLeafNode = mapping.Module.IsLeafNode
                    })
                    .ToListAsync(cancellationToken))
                .GroupBy(module => module.ModuleId)
                .Select(group => group.First())
                .ToList();

            var sourceModuleIds = sourceModules.Select(module => module.ModuleId).ToList();
            var existingModuleIds = (await _context.InstitutionEnabledModules
                    .Where(module => module.InstitutionId == institutionId && sourceModuleIds.Contains(module.ModuleId))
                    .Select(module => module.ModuleId)
                    .ToListAsync(cancellationToken))
                .ToHashSet();

            sourceModules.ForEach(module => module.AlreadyEnabled = existingModuleIds.Contains(module.ModuleId));

            var utcNow = DateTime.UtcNow;
            var modulesToAdd = sourceModules
                .Where(module => !existingModuleIds.Contains(module.ModuleId))
                .Select(module => new InstitutionEnabledModule
                {
                    InstitutionId = institutionId,
                    ModuleId = module.ModuleId,
                    ParentModuleId = module.ParentModuleId,
                    IsLeafNode = module.IsLeafNode,
                    IsEnabled = true,
                    AddedById = addedById,
                    AddedDateTime = utcNow
                })
                .ToList();

            // Keep InstitutionEnabledOperation sourcing unchanged: only non-common Institution leaf Modules
            // feed the active, non-common ModuleOperationMapping snapshot.
            var operationSourceModuleIds = await _context.PlanModuleMappings
                .AsNoTracking()
                .Where(mapping =>
                    mapping.SubscriptionPlanId == activeSubscription.SubscriptionPlanId &&
                    mapping.IsActive == true &&
                    mapping.Module.IsActive &&
                    !mapping.Module.IsCommonMenu &&
                    mapping.Module.ModuleScope == (short)skolenza.application.Constants.AppConstants.InstitutionModuleScope &&
                    mapping.Module.IsLeafNode == true)
                .Select(mapping => mapping.ModuleId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var sourceOperations = operationSourceModuleIds.Count == 0
                ? new List<InstitutionPlanEntitlementOperationSyncResponseDTO>()
                : (await _context.ModuleOperationMappings
                        .AsNoTracking()
                        .Where(mapping =>
                            operationSourceModuleIds.Contains(mapping.ModuleId) &&
                            mapping.IsActive == true &&
                            mapping.IsCommonItem != true &&
                            mapping.Operation.IsActive)
                        .Select(mapping => new InstitutionPlanEntitlementOperationSyncResponseDTO
                        {
                            ModuleId = mapping.ModuleId,
                            ModuleName = mapping.Module!.ModuleName,
                            OperationId = mapping.OperationId,
                            OperationName = mapping.Operation.OperationName,
                            IsOperational = mapping.IsOperational,
                            PageUrl = mapping.PageUrl
                        })
                        .ToListAsync(cancellationToken))
                    .GroupBy(operation => new { operation.ModuleId, operation.OperationId })
                    .Select(group => group.First())
                    .ToList();

            var sourceOperationKeys = sourceOperations
                .Select(operation => (operation.ModuleId, operation.OperationId))
                .ToHashSet();
            var existingOperationKeys = (await _context.InstitutionEnabledOperations
                    .Where(operation => operation.InstitutionId == institutionId)
                    .Select(operation => new { operation.ModuleId, operation.OperationId })
                    .ToListAsync(cancellationToken))
                .Select(operation => (operation.ModuleId, operation.OperationId))
                .Where(sourceOperationKeys.Contains)
                .ToHashSet();

            sourceOperations.ForEach(operation =>
                operation.AlreadyEnabled = existingOperationKeys.Contains((operation.ModuleId, operation.OperationId)));

            var operationsToAdd = sourceOperations
                .Where(operation => !existingOperationKeys.Contains((operation.ModuleId, operation.OperationId)))
                .Select(operation => new InstitutionEnabledOperation
                {
                    InstitutionId = institutionId,
                    ModuleId = operation.ModuleId,
                    OperationId = operation.OperationId,
                    IsOperationUsed = operation.IsOperational ?? true,
                    IsEnabled = true,
                    AddedById = addedById,
                    AddedDateTime = utcNow
                })
                .ToList();

            if (modulesToAdd.Count > 0)
            {
                await _context.InstitutionEnabledModules.AddRangeAsync(modulesToAdd, cancellationToken);
            }

            if (operationsToAdd.Count > 0)
            {
                await _context.InstitutionEnabledOperations.AddRangeAsync(operationsToAdd, cancellationToken);
            }

            _logger.LogInformation(
                "Prepared additive plan-entitlement sync for InstitutionId: {InstitutionId}, PlanId: {PlanId}, AddedModules: {AddedModules}, AddedOperations: {AddedOperations}",
                institutionId,
                activeSubscription.SubscriptionPlanId,
                modulesToAdd.Count,
                operationsToAdd.Count);

            return new InstitutionPlanEntitlementSyncResult
            {
                SubscriptionPlanId = activeSubscription.SubscriptionPlanId,
                SourceModuleCount = sourceModules.Count,
                AddedModuleCount = modulesToAdd.Count,
                ExistingModuleCount = existingModuleIds.Count,
                SourceOperationCount = sourceOperations.Count,
                AddedOperationCount = operationsToAdd.Count,
                ExistingOperationCount = existingOperationKeys.Count,
                Modules = sourceModules,
                Operations = sourceOperations
            };
        }

        public Task<GetModuleHierarchyResponseDTO> GetAllInstitutionEnabledModulesAsync(InstitutionEnabledModuleRequestDTO dto)
        {
            throw new NotImplementedException();
        }

       
    }

}
