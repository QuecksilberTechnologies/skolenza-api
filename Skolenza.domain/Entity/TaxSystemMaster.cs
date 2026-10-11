using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class TaxSystemMaster
{
    public long Id { get; set; }

    public int CountryId { get; set; }

    public string TaxSystemName { get; set; } = null!;

    public bool IsActive { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public virtual Country Country { get; set; } = null!;

    public virtual ICollection<TaxRegimeMaster> TaxRegimeMaster { get; set; } = new List<TaxRegimeMaster>();
}
