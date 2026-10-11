using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class TaxRegimeMaster
{
    public long Id { get; set; }

    public int CountryId { get; set; }

    public long TaxSystemId { get; set; }

    public string RegimeName { get; set; } = null!;

    public bool? IsDefault { get; set; }

    public bool IsActive { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public virtual Country Country { get; set; } = null!;

    public virtual ICollection<StaffMemberTaxProfile> StaffMemberTaxProfile { get; set; } = new List<StaffMemberTaxProfile>();

    public virtual ICollection<TaxSlab> TaxSlab { get; set; } = new List<TaxSlab>();

    public virtual TaxSystemMaster TaxSystem { get; set; } = null!;
}
