// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines persistence operations for subscription plans.
// ================================================================

using skolenza.application.DTOs.SubscriptionModule;
using skolenza.application.DTOS.Pagination;
using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IRepositories;

/// <summary>
/// Defines persistence operations for Host-managed subscription plans.
/// </summary>
public interface ISubscriptionRepository
{
    #region Commands

    /// <summary>
    /// Adds a new subscription plan entity.
    /// </summary>
    /// <param name="entity">The plan entity to add.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The added subscription plan entity.</returns>
    Task<SubscriptionPlan> AddSubscriptionPlanAsync(
        SubscriptionPlan entity,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stages an existing, non-deleted subscription plan entity prepared by the handler.
    /// </summary>
    /// <param name="entity">The tracked plan entity containing updated business fields.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The updated subscription plan entity staged for the Unit of Work transaction.</returns>
    Task<SubscriptionPlan> UpdateSubscriptionPlanAsync(
        SubscriptionPlan entity,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stages the server-prepared soft-delete state for a subscription plan entity.
    /// </summary>
    /// <param name="entity">The subscription plan entity prepared for soft deletion.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The soft-deleted subscription plan entity staged for the Unit of Work transaction.</returns>
    Task<SubscriptionPlan> SoftDeleteSubscriptionPlanAsync(
        SubscriptionPlan entity,
        CancellationToken cancellationToken);

    #endregion

    #region Queries

    /// <summary>
    /// Retrieves non-deleted subscription plans matching the requested active status.
    /// </summary>
    /// <param name="isActive">Whether to return active or inactive plans.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The matching subscription plan read models.</returns>
    Task<List<SubscriptionActivePlanDTO>> GetAllPlansAsync(
        bool isActive,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a database-paged set of non-deleted subscription plans for Host administration.
    /// </summary>
    /// <param name="search">Optional plan-name search text.</param>
    /// <param name="isActive">When supplied, limits plans to the requested active status.</param>
    /// <param name="pageNumber">The normalized one-based page number.</param>
    /// <param name="pageSize">The normalized number of rows per page.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The requested Host subscription-plan page.</returns>
    Task<PagedResponseDTO<SubscriptionActivePlanDTO>> GetHostPlansAsync(
        string? search,
        bool? isActive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a non-deleted subscription plan response by identifier.
    /// </summary>
    /// <param name="id">The subscription plan identifier.</param>
    /// <returns>The plan response, when found.</returns>
    Task<SubscriptionPlanResponseDTO> GetPlanByIdAsync(int id);

    #endregion

    #region Validation Queries

    /// <summary>
    /// Retrieves a tracked, non-deleted subscription plan entity for a server-side operation.
    /// </summary>
    /// <param name="id">The subscription plan identifier.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>The subscription plan entity, or <see langword="null"/> when it is unavailable.</returns>
    Task<SubscriptionPlan?> GetNonDeletedSubscriptionPlanByIdAsync(
        int id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Determines whether an active, legitimate institution currently uses the subscription plan.
    /// </summary>
    /// <param name="subscriptionPlanId">The subscription plan identifier.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns><see langword="true"/> when the plan is assigned to an active institution; otherwise, <see langword="false"/>.</returns>
    Task<bool> IsSubscriptionPlanAssignedToAnyActiveInstitutionAsync(
        int subscriptionPlanId,
        CancellationToken cancellationToken);

    #endregion
}
