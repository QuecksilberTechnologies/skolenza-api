// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines persistence and hierarchy operations for Subscription Plan Module mappings.
// ================================================================

using skolenza.application.DTOs.PlanModule;
using skolenza.application.DTOs.Institution;
using skolenza.domain.Entity;
using InstitutionPlanModuleMappingResponseDTO = skolenza.application.DTOs.Institution.PlanModuleMappingResponseDTO;

namespace skolenza.application.Interfaces.IRepositories;

/// <summary>
/// Defines persistence and hierarchy queries for Subscription Plan to Module mappings.
/// </summary>
public interface IPlanModuleMappingRepository
{
    #region Institution Module Queries

    /// <summary>
    /// Retrieves active mapped Modules and their operations for the existing institution-facing subscription query.
    /// </summary>
    /// <param name="subscriptionPlanId">The subscription plan identifier.</param>
    /// <returns>The institution-facing mapped Module response.</returns>
    Task<InstitutionPlanModuleMappingResponseDTO> GetModulesBySubscriptionPlanIdAsync(int? subscriptionPlanId);

    /// <summary>
    /// Retrieves active Modules available to a subscription plan for existing institution provisioning.
    /// </summary>
    /// <param name="subscriptionPlanId">The subscription plan identifier.</param>
    /// <returns>The active mapped Modules and their eligible descendants.</returns>
    Task<List<Module>> GetAllSubscribedModuleAsync(int? subscriptionPlanId);

    #endregion

    #region Mapping Queries

    /// <summary>
    /// Retrieves all Modules currently eligible for Subscription Plan assignment.
    /// </summary>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>Read-only Module entities used to compose and validate the mapping hierarchy.</returns>
    Task<List<Module>> GetEligibleModulesForPlanMappingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves active mapping identifiers for the specified Subscription Plan.
    /// </summary>
    /// <param name="subscriptionPlanId">The subscription plan identifier.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The distinct Module identifiers currently mapped to the plan.</returns>
    Task<List<int>> GetActiveMappedModuleIdsAsync(
        int subscriptionPlanId,
        CancellationToken cancellationToken);

    #endregion

    #region Mapping Commands

    /// <summary>
    /// Stages a delta synchronization of a plan's selected Module assignments.
    /// </summary>
    /// <param name="subscriptionPlanId">The subscription plan identifier.</param>
    /// <param name="selectedModuleIds">The validated and hierarchy-expanded selected Module identifiers.</param>
    /// <param name="remark">The optional remark for newly created or reactivated mappings.</param>
    /// <param name="hostUserId">The authenticated Host user performing the change.</param>
    /// <param name="utcNow">The single UTC audit timestamp captured for this request.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The staged synchronization counts.</returns>
    Task<SavePlanModuleMappingResponseDTO> SynchronizeMappingsAsync(
        int subscriptionPlanId,
        IReadOnlyCollection<int> selectedModuleIds,
        string? remark,
        long hostUserId,
        DateTime utcNow,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stages active-state changes for every mapping owned by a Subscription Plan.
    /// </summary>
    /// <param name="subscriptionPlanId">The subscription plan identifier.</param>
    /// <param name="isPlanActive">The new Subscription Plan active state.</param>
    /// <param name="eligibleModuleIds">The currently eligible Module identifiers used during activation.</param>
    /// <param name="hostUserId">The authenticated Host user performing the status change.</param>
    /// <param name="utcNow">The single UTC audit timestamp captured for this request.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The number of mapping rows whose active state changed.</returns>
    Task<int> SynchronizePlanMappingStatusAsync(
        int subscriptionPlanId,
        bool isPlanActive,
        IReadOnlyCollection<int> eligibleModuleIds,
        long hostUserId,
        DateTime utcNow,
        CancellationToken cancellationToken);

    /// <summary>
    /// Permanently deletes every mapping row owned by a Subscription Plan.
    /// </summary>
    /// <param name="subscriptionPlanId">The subscription plan identifier.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The number of deleted mapping rows.</returns>
    Task<int> DeleteAllBySubscriptionPlanIdAsync(
        int subscriptionPlanId,
        CancellationToken cancellationToken);

    #endregion
}
