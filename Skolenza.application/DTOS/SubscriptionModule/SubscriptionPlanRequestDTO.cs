// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request criteria used to retrieve subscription plans.
// ================================================================

namespace skolenza.application.DTOs.SubscriptionModule;

/// <summary>
/// Defines the filter criteria for retrieving subscription plans.
/// </summary>
public sealed class SubscriptionPlanRequestDTO
{
    /// <summary>
    /// Gets or sets an optional plan name criterion retained for API compatibility.
    /// </summary>
    public string? PlanName { get; set; }

    /// <summary>
    /// Gets or sets an optional maximum-user criterion retained for API compatibility.
    /// </summary>
    public int? MaxUsers { get; set; }

    /// <summary>
    /// Gets or sets an optional daily-price criterion retained for API compatibility.
    /// </summary>
    public decimal? PerDayPrice { get; set; }

    /// <summary>
    /// Gets or sets an optional monthly-price criterion retained for API compatibility.
    /// </summary>
    public decimal? MonthlyPrice { get; set; }

    /// <summary>
    /// Gets or sets an optional yearly-price criterion retained for API compatibility.
    /// </summary>
    public decimal? YearlyPrice { get; set; }

    /// <summary>
    /// Gets or sets the legacy active-status value retained for request compatibility. The public plan endpoint always returns active plans only.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
