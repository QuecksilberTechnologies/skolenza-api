// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the client-editable request contract for updating a subscription plan.
// ================================================================

namespace skolenza.application.DTOS.SubscriptionModule;

/// <summary>
/// Defines client-editable values for updating a Host-managed subscription plan.
/// </summary>
public sealed class UpdateSubscriptionRequestDTO
{
    /// <summary>
    /// Gets or sets the legacy body identifier; the handler uses the route identifier as the trusted target.
    /// </summary>
    public long Id { get; set; }

    /// <summary>Gets or sets the subscription plan name.</summary>
    public string PlanName { get; set; } = string.Empty;

    /// <summary>Gets or sets the maximum number of users allowed by the plan.</summary>
    public int MaxUsers { get; set; }

    /// <summary>Gets or sets whether the plan is highlighted as most popular.</summary>
    public bool IsMostPopular { get; set; }

    /// <summary>Gets or sets whether the plan is a custom plan.</summary>
    public bool IsCustom { get; set; }

    /// <summary>Gets or sets the currency key used for plan pricing.</summary>
    public string CurrencyKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional per-day price.</summary>
    public decimal? PerDayPrice { get; set; }

    /// <summary>Gets or sets whether the plan is free.</summary>
    public bool IsFree { get; set; }

    /// <summary>Gets or sets the optional monthly price.</summary>
    public decimal? MonthlyPrice { get; set; }

    /// <summary>Gets or sets the optional yearly price.</summary>
    public decimal? YearlyPrice { get; set; }

    /// <summary>Gets or sets whether the plan is active.</summary>
    public bool IsActive { get; set; }
}
