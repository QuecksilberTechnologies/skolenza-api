// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines subscription plan persistence properties and relationships.
// ================================================================

using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

/// <summary>
/// Represents a Host-managed subscription plan and its related institution subscriptions.
/// </summary>
public partial class SubscriptionPlan
{
    public int Id { get; set; }

    public string PlanName { get; set; } = null!;

    public int MaxUsers { get; set; }

    public decimal? PerDayPrice { get; set; }

    public decimal MonthlyPrice { get; set; }

    public decimal? YearlyPrice { get; set; }

    public bool? IsFree { get; set; }

    public bool IsActive { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? AddedById { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public string? CurrencyKey { get; set; }

    public bool? IsMostPopular { get; set; }

    public bool? IsCustom { get; set; }

    #region Soft-delete Audit

    /// <summary>
    /// Gets or sets whether the plan has been soft deleted and must be excluded from normal use.
    /// </summary>
    public bool IsSoftDeleted { get; set; } = false;

    /// <summary>
    /// Gets or sets the authenticated Host user that soft deleted the plan.
    /// </summary>
    public int? DeletedById { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp at which the plan was soft deleted.
    /// </summary>
    public DateTime? DeletedDateTime { get; set; }

    #endregion

    public virtual ICollection<PlanModuleMapping> PlanModuleMapping { get; set; } = new List<PlanModuleMapping>();

    public virtual ICollection<InstitutionSubscription> InstitutionSubscription { get; set; } = new List<InstitutionSubscription>();
}
