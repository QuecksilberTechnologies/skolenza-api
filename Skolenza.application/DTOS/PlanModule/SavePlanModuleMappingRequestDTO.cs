// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the Host request used to atomically synchronize Subscription Plan Module selections.
// ================================================================

namespace skolenza.application.DTOs.PlanModule;

/// <summary>
/// Defines the client-selectable Module assignments for one Subscription Plan.
/// </summary>
public sealed class SavePlanModuleMappingRequestDTO
{
    #region Properties

    /// <summary>
    /// Gets or sets the Subscription Plan identifier to synchronize.
    /// </summary>
    public int SubscriptionPlanId { get; set; }

    /// <summary>
    /// Gets or sets the selected Module identifiers. An empty collection unmaps all active assignments.
    /// </summary>
    public IReadOnlyCollection<int> ModuleIds { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Gets or sets the optional remark stored on newly created or reactivated mappings.
    /// </summary>
    public string? Remark { get; set; }

    #endregion
}
