// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines Host subscription-plan list filters and pagination criteria.
// ================================================================

namespace skolenza.application.DTOs.SubscriptionModule;

/// <summary>
/// Represents filters for a Host-managed subscription-plan listing.
/// </summary>
public sealed class HostSubscriptionPlanListRequestDTO
{
    #region Filters

    /// <summary>
    /// Gets or sets the optional plan-name search text.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Gets or sets the optional active-status filter.
    /// </summary>
    public bool? IsActive { get; set; }

    #endregion

    #region Paging

    /// <summary>
    /// Gets or sets the requested one-based page number.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the requested number of plans per page.
    /// </summary>
    public int PageSize { get; set; } = 10;

    #endregion
}
