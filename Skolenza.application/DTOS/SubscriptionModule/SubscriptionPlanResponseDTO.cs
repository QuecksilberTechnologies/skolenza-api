// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the detailed response model for a subscription plan.
// ================================================================

using skolenza.application.DTOS.Module.ParentModule;

namespace skolenza.application.DTOs.SubscriptionModule;

/// <summary>
/// Represents a detailed, non-deleted subscription plan response.
/// </summary>
public sealed class SubscriptionPlanResponseDTO
{
    /// <summary>Gets or sets the subscription plan identifier.</summary>
    public int? Id { get; set; }

    /// <summary>Gets or sets the subscription plan name.</summary>
    public string? PlanName { get; set; }

    /// <summary>Gets or sets the maximum number of users allowed by the plan.</summary>
    public int? MaxUsers { get; set; }

    /// <summary>Gets or sets whether the plan is marked as most popular.</summary>
    public bool IsMostPopular { get; set; }

    /// <summary>Gets or sets whether the plan is custom.</summary>
    public bool IsCustom { get; set; }

    /// <summary>Gets or sets the plan currency key.</summary>
    public string? CurrencyKey { get; set; }

    /// <summary>Gets or sets the optional per-day price.</summary>
    public decimal? PerDayPrice { get; set; }

    /// <summary>Gets or sets whether the plan is free.</summary>
    public bool? IsFree { get; set; }

    /// <summary>Gets or sets the optional monthly price.</summary>
    public decimal? MonthlyPrice { get; set; }

    /// <summary>Gets or sets the optional yearly price.</summary>
    public decimal? YearlyPrice { get; set; }

    /// <summary>Gets or sets whether the plan is active.</summary>
    public bool? IsActive { get; set; }

    /// <summary>Gets or sets the modules associated with the subscription plan.</summary>
    public List<ModuleResponseDTO> Modules { get; set; } = new();
}
