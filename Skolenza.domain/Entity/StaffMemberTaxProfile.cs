using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class StaffMemberTaxProfile
{
    public long Id { get; set; }

    public long StaffMemberId { get; set; }

    public int CountryId { get; set; }

    public int? StateId { get; set; }

    public long RegimeId { get; set; }

    public string FinancialYear { get; set; } = null!;

    public DateOnly EffectiveFrom { get; set; }

    public bool IsActive { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public virtual Country Country { get; set; } = null!;

    public virtual StaffMember StaffMember { get; set; } = null!;

    public virtual TaxRegimeMaster Regime { get; set; } = null!;

    public virtual State? State { get; set; }
}
