// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the summary returned after synchronizing Subscription Plan Module mappings.
// ================================================================

namespace skolenza.application.DTOs.PlanModule;

/// <summary>
/// Represents the delta applied while synchronizing a Subscription Plan's Module assignments.
/// </summary>
public sealed class SavePlanModuleMappingResponseDTO
{
    #region Properties

    /// <summary>
    /// Gets or sets the Subscription Plan identifier whose mappings were synchronized.
    /// </summary>
    public int SubscriptionPlanId { get; set; }

    /// <summary>
    /// Gets or sets the final count of selected, eligible Module assignments.
    /// </summary>
    public int SelectedModuleCount { get; set; }

    /// <summary>
    /// Gets or sets the number of new mapping rows staged for insertion.
    /// </summary>
    public int AddedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of inactive mappings staged for reactivation.
    /// </summary>
    public int ReactivatedCount { get; set; }

    /// <summary>
    /// Gets or sets the number of active mappings staged for deactivation.
    /// </summary>
    public int DeactivatedCount { get; set; }

    #endregion
}
