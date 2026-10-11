// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines subscription plan read models with their available modules.
// ================================================================

namespace skolenza.application.DTOs.SubscriptionModule;

/// <summary>
/// Represents a subscription plan returned by the active-status plan query.
/// </summary>
public sealed class SubscriptionActivePlanDTO
{
    /// <summary>Gets or sets the subscription plan identifier.</summary>
    public long Id { get; set; }

    /// <summary>Gets or sets the subscription plan name.</summary>
    public string? PlanName { get; set; }

    /// <summary>Gets or sets whether the subscription plan is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the maximum users allowed by the plan.</summary>
    public int? MaxUsers { get; set; }

    /// <summary>Gets or sets whether the plan is marked as most popular.</summary>
    public bool? IsMostPopular { get; set; }

    /// <summary>Gets or sets whether the plan is custom.</summary>
    public bool? IsCustom { get; set; }

    /// <summary>Gets or sets whether the plan is free.</summary>
    public bool? IsFree { get; set; }

    /// <summary>Gets or sets the plan currency key.</summary>
    public string? CurrencyKey { get; set; }

    /// <summary>Gets or sets the optional per-day price.</summary>
    public decimal? PerDayPrice { get; set; }

    /// <summary>Gets or sets the optional monthly price.</summary>
    public decimal? MonthlyPrice { get; set; }

    /// <summary>Gets or sets the optional yearly price.</summary>
    public decimal? YearlyPrice { get; set; }

    /// <summary>Gets or sets the available modules organized into their hierarchy.</summary>
    public List<ModuleActiveDTO> Modules { get; set; } = new();
}

/// <summary>
/// Represents a module available through a subscription plan.
/// </summary>
public sealed class ModuleActiveDTO
{
    /// <summary>Gets or sets the module identifier.</summary>
    public long Id { get; set; }

    /// <summary>Gets or sets the module name.</summary>
    public string? ModuleName { get; set; }

    /// <summary>Gets or sets the display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Gets or sets the parent module identifier, or zero for a root module.</summary>
    public long ParentModuleId { get; set; }

    /// <summary>Gets or sets the child modules.</summary>
    public List<ModuleActiveDTO> ChildModules { get; set; } = new();

    /// <summary>Gets or sets the operations retained by the existing response contract.</summary>
    public List<OperationActiveDTO> Operations { get; set; } = new();
}

/// <summary>
/// Represents an operation available through a subscription module.
/// </summary>
public sealed class OperationActiveDTO
{
    /// <summary>Gets or sets the operation identifier.</summary>
    public long Id { get; set; }

    /// <summary>Gets or sets the operation display name.</summary>
    public string? DisplayName { get; set; }
}
