// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the lightweight eligible Module hierarchy returned by the Subscription Plan mapping popup.
// ================================================================

namespace skolenza.application.DTOs.PlanModule;

/// <summary>
/// Represents the selectable Module hierarchy and current mapping state for one Subscription Plan.
/// </summary>
public sealed class PlanModuleMappingOptionsResponseDTO
{
    #region Properties

    /// <summary>
    /// Gets or sets the Subscription Plan identifier for the returned hierarchy.
    /// </summary>
    public int SubscriptionPlanId { get; set; }

    /// <summary>
    /// Gets or sets the eligible institution-scope Module Header hierarchy.
    /// </summary>
    public IReadOnlyCollection<PlanModuleOptionResponseDTO> Modules { get; set; } = Array.Empty<PlanModuleOptionResponseDTO>();

    #endregion
}

/// <summary>
/// Represents one eligible, selectable Module within a Subscription Plan mapping hierarchy.
/// </summary>
public sealed class PlanModuleOptionResponseDTO
{
    #region Properties

    /// <summary>
    /// Gets or sets the Module identifier.
    /// </summary>
    public int ModuleId { get; set; }

    /// <summary>
    /// Gets or sets the Module name.
    /// </summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional display name for the Module.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets whether the Module currently has an active mapping to the plan.
    /// </summary>
    public bool IsMapped { get; set; }

    /// <summary>
    /// Gets or sets the current Module active state.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the eligible child Modules. Leaf nodes return an empty collection.
    /// </summary>
    public IReadOnlyCollection<PlanModuleOptionResponseDTO> Children { get; set; } = Array.Empty<PlanModuleOptionResponseDTO>();

    #endregion
}
