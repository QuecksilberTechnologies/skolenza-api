using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class InstitutionSubscription
{
    public long Id { get; set; }

    public long InstitutionId { get; set; }

    public int SubscriptionPlanId { get; set; }

    public DateTime SubscriptionStartDate { get; set; }

    public DateTime SubscriptionEndDate { get; set; }

    public bool IsActive { get; set; }

    public bool IsTrial { get; set; }

    public string? PaymentTxnId { get; set; }

    public string? PaymentMode { get; set; }

    public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;

    public virtual Institution Institution { get; set; } = null!;
}
