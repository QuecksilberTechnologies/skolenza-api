using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class PlanModuleMapping
{
    public int Id { get; set; }

    public int SubscriptionPlanId { get; set; }

    public int ModuleId { get; set; }

    public bool? IsActive { get; set; }

    public string? Remark { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public virtual Module Module { get; set; } = null!;

    public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;
}
