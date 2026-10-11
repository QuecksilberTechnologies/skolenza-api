// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Provides persistence and hierarchy queries for Subscription Plan Module mappings.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOs.PlanModule;
using skolenza.application.DTOs.Institution;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using InstitutionPlanModuleMappingResponseDTO = skolenza.application.DTOs.Institution.PlanModuleMappingResponseDTO;

namespace skolenza.persistance.Repositories;

/// <summary>
/// Provides read-only hierarchy queries and staged delta commands for Subscription Plan Module mappings.
/// </summary>
public sealed class PlanModuleMappingRepository : IPlanModuleMappingRepository
{
    #region Fields

    private readonly SkolenzaDbContext _context;
    private readonly ILogger<PlanModuleMappingRepository> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="PlanModuleMappingRepository"/> class.
    /// </summary>
    /// <param name="context">The persistence context for mappings and Modules.</param>
    /// <param name="logger">The logger used for mapping synchronization diagnostics.</param>
    public PlanModuleMappingRepository(
        SkolenzaDbContext context,
        ILogger<PlanModuleMappingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    #endregion

    #region Institution Module Queries

    /// <inheritdoc />
    public async Task<List<Module>> GetAllSubscribedModuleAsync(int? subscriptionPlanId)
    {
        if (subscriptionPlanId is null or <= 0)
        {
            return new List<Module>();
        }

        var mappedModuleIds = await _context.PlanModuleMappings
            .AsNoTracking()
            .Where(mapping =>
                mapping.SubscriptionPlanId == subscriptionPlanId.Value &&
                mapping.IsActive == true)
            .Select(mapping => mapping.ModuleId)
            .Distinct()
            .ToListAsync();

        if (mappedModuleIds.Count == 0)
        {
            return new List<Module>();
        }

        // Load eligible Modules once, then compose selected branches in memory.
        var eligibleModules = await GetEligibleModulesForPlanMappingAsync(CancellationToken.None);
        var modulesById = eligibleModules.ToDictionary(module => module.Id);
        var childrenByParentId = eligibleModules
            .Where(module => module.ParentModuleId.HasValue && module.ParentModuleId.Value > 0)
            .GroupBy(module => module.ParentModuleId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());
        var resultById = new Dictionary<int, Module>();

        foreach (var mappedModuleId in mappedModuleIds)
        {
            AddModuleBranch(mappedModuleId, modulesById, childrenByParentId, resultById, new HashSet<int>());
        }

        return resultById.Values
            .OrderBy(module => module.ItemPriority ?? int.MaxValue)
            .ThenBy(module => module.ModuleName)
            .ThenBy(module => module.Id)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<InstitutionPlanModuleMappingResponseDTO> GetModulesBySubscriptionPlanIdAsync(int? subscriptionPlanId)
    {
        if (subscriptionPlanId is null or <= 0)
        {
            return new InstitutionPlanModuleMappingResponseDTO();
        }

        var mappings = await _context.PlanModuleMappings
            .AsNoTracking()
            .Where(mapping =>
                mapping.SubscriptionPlanId == subscriptionPlanId.Value &&
                mapping.IsActive == true &&
                mapping.Module.IsActive &&
                mapping.Module.IsModuleDisplayInUI &&
                (mapping.Module.ModuleScope == (short)AppConstants.InstitutionModuleScope ||
                 mapping.Module.ModuleScope == (short)AppConstants.CommonModuleScope))
            .Include(mapping => mapping.Module)
                .ThenInclude(module => module.ModuleOperationMapping)
                    .ThenInclude(moduleOperation => moduleOperation.Operation)
            .Include(mapping => mapping.Module)
                .ThenInclude(module => module.ParentModule)
            .ToListAsync();

        return new InstitutionPlanModuleMappingResponseDTO
        {
            SubscriptionPlanId = subscriptionPlanId.Value,
            Modules = mappings.Select(mapping => new ModuleWithOperationsDTO
            {
                ModuleId = mapping.Module.Id,
                ModuleName = mapping.Module.ModuleName,
                DisplayName = mapping.Module.DisplayName ?? string.Empty,
                ParentModuleId = mapping.Module.ParentModuleId,
                MainModuleId = mapping.Module.ParentModule?.Id,
                MainModuleName = mapping.Module.ParentModule?.ModuleName ?? string.Empty,
                Operations = mapping.Module.ModuleOperationMapping
                    .Where(moduleOperation => moduleOperation.IsActive == true && moduleOperation.Operation != null)
                    .Select(moduleOperation => new OperationResponseDTO
                    {
                        OperationId = moduleOperation.Operation.Id
                    })
                    .ToList()
            }).ToList()
        };
    }

    #endregion

    #region Mapping Queries

    /// <inheritdoc />
    public Task<List<Module>> GetEligibleModulesForPlanMappingAsync(CancellationToken cancellationToken)
    {
        // Module does not have a soft-delete column; visibility, activity, and Institution/Common scope are canonical eligibility.
        return _context.Modules
            .AsNoTracking()
            .Where(module =>
                (module.ModuleScope == (short)AppConstants.InstitutionModuleScope ||
                 module.ModuleScope == (short)AppConstants.CommonModuleScope) &&
                module.IsModuleDisplayInUI &&
                module.IsActive)
            .OrderBy(module => module.ItemPriority ?? int.MaxValue)
            .ThenBy(module => module.ModuleName)
            .ThenBy(module => module.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<List<int>> GetActiveMappedModuleIdsAsync(
        int subscriptionPlanId,
        CancellationToken cancellationToken)
    {
        return _context.PlanModuleMappings
            .AsNoTracking()
            .Where(mapping =>
                mapping.SubscriptionPlanId == subscriptionPlanId &&
                mapping.IsActive == true)
            .Select(mapping => mapping.ModuleId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Mapping Commands

    /// <inheritdoc />
    public async Task<SavePlanModuleMappingResponseDTO> SynchronizeMappingsAsync(
        int subscriptionPlanId,
        IReadOnlyCollection<int> selectedModuleIds,
        string? remark,
        long hostUserId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var selectedModuleIdSet = selectedModuleIds.ToHashSet();
        var existingMappings = await _context.PlanModuleMappings
            .Where(mapping => mapping.SubscriptionPlanId == subscriptionPlanId)
            .OrderBy(mapping => mapping.Id)
            .ToListAsync(cancellationToken);
        var mappingsByModuleId = existingMappings
            .GroupBy(mapping => mapping.ModuleId)
            .ToDictionary(group => group.Key, group => group.ToList());
        var result = new SavePlanModuleMappingResponseDTO
        {
            SubscriptionPlanId = subscriptionPlanId,
            SelectedModuleCount = selectedModuleIdSet.Count
        };

        // Compute the mapping delta before applying tracked persistence changes.
        foreach (var selectedModuleId in selectedModuleIdSet)
        {
            if (!mappingsByModuleId.TryGetValue(selectedModuleId, out var mappings))
            {
                await _context.PlanModuleMappings.AddAsync(new PlanModuleMapping
                {
                    SubscriptionPlanId = subscriptionPlanId,
                    ModuleId = selectedModuleId,
                    IsActive = true,
                    Remark = remark,
                    AddedById = hostUserId,
                    AddedDateTime = utcNow,
                    UpdatedById = null,
                    UpdatedDateTime = null
                }, cancellationToken);

                result.AddedCount++;
                continue;
            }

            var canonicalMapping = mappings.FirstOrDefault(mapping => mapping.IsActive == true) ?? mappings[0];
            if (canonicalMapping.IsActive != true)
            {
                canonicalMapping.IsActive = true;
                canonicalMapping.UpdatedById = hostUserId;
                canonicalMapping.UpdatedDateTime = utcNow;

                if (!string.IsNullOrWhiteSpace(remark))
                {
                    canonicalMapping.Remark = remark;
                }

                result.ReactivatedCount++;
            }

            // Retain one active row per logical plan and Module combination.
            foreach (var duplicateMapping in mappings.Where(mapping => mapping.Id != canonicalMapping.Id && mapping.IsActive == true))
            {
                DeactivateMapping(duplicateMapping, hostUserId, utcNow);
                result.DeactivatedCount++;
            }
        }

        foreach (var mapping in existingMappings.Where(mapping =>
                     !selectedModuleIdSet.Contains(mapping.ModuleId) &&
                     mapping.IsActive == true))
        {
            DeactivateMapping(mapping, hostUserId, utcNow);
            result.DeactivatedCount++;
        }

        _logger.LogInformation(
            "Prepared Subscription Plan Module mapping delta. SubscriptionPlanId: {SubscriptionPlanId}; Selected: {SelectedCount}; Added: {AddedCount}; Reactivated: {ReactivatedCount}; Deactivated: {DeactivatedCount}.",
            subscriptionPlanId,
            result.SelectedModuleCount,
            result.AddedCount,
            result.ReactivatedCount,
            result.DeactivatedCount);

        return result;
    }

    /// <inheritdoc />
    public async Task<int> SynchronizePlanMappingStatusAsync(
        int subscriptionPlanId,
        bool isPlanActive,
        IReadOnlyCollection<int> eligibleModuleIds,
        long hostUserId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var mappings = await _context.PlanModuleMappings
            .Where(mapping => mapping.SubscriptionPlanId == subscriptionPlanId)
            .ToListAsync(cancellationToken);
        var eligibleModuleIdSet = eligibleModuleIds.ToHashSet();
        var changedCount = 0;

        foreach (var mapping in mappings)
        {
            // Reactivate only mappings whose Modules are still part of the current eligible hierarchy.
            var shouldBeActive = isPlanActive && eligibleModuleIdSet.Contains(mapping.ModuleId);
            if (mapping.IsActive == shouldBeActive)
            {
                continue;
            }

            mapping.IsActive = shouldBeActive;
            mapping.UpdatedById = hostUserId;
            mapping.UpdatedDateTime = utcNow;
            changedCount++;
        }

        _logger.LogInformation(
            "Prepared Subscription Plan Module status synchronization. SubscriptionPlanId: {SubscriptionPlanId}; IsPlanActive: {IsPlanActive}; ChangedMappings: {ChangedMappings}.",
            subscriptionPlanId,
            isPlanActive,
            changedCount);

        return changedCount;
    }

    /// <inheritdoc />
    public Task<int> DeleteAllBySubscriptionPlanIdAsync(
        int subscriptionPlanId,
        CancellationToken cancellationToken)
    {
        // This cleanup is intentionally physical because mappings are owned configuration rows of a soft-deleted plan.
        return _context.PlanModuleMappings
            .Where(mapping => mapping.SubscriptionPlanId == subscriptionPlanId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    #endregion

    #region Helpers

    private static void AddModuleBranch(
        int moduleId,
        IReadOnlyDictionary<int, Module> modulesById,
        IReadOnlyDictionary<int, List<Module>> childrenByParentId,
        IDictionary<int, Module> resultById,
        ISet<int> ancestry)
    {
        if (!ancestry.Add(moduleId) || !modulesById.TryGetValue(moduleId, out var module))
        {
            return;
        }

        resultById[module.Id] = module;

        if (!childrenByParentId.TryGetValue(moduleId, out var children))
        {
            return;
        }

        foreach (var child in children)
        {
            AddModuleBranch(child.Id, modulesById, childrenByParentId, resultById, new HashSet<int>(ancestry));
        }
    }

    private static void DeactivateMapping(PlanModuleMapping mapping, long hostUserId, DateTime timestamp)
    {
        mapping.IsActive = false;
        mapping.UpdatedById = hostUserId;
        mapping.UpdatedDateTime = timestamp;
    }

    #endregion
}
