// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request contract for deleting a subscription plan.
// ================================================================

namespace skolenza.application.DTOS.SubscriptionModule;

/// <summary>
/// Represents the client request to soft delete a subscription plan.
/// </summary>
public sealed class DeleteSubscriptionPlanRequestDTO
{
    /// <summary>
    /// Gets or sets the subscription plan identifier selected for deletion.
    /// </summary>
    public int Id { get; set; }
}
