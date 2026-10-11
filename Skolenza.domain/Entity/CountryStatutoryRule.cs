using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class CountryStatutoryRule
{
    public int Id { get; set; }

    public int CountryId { get; set; }

    public int StatutoryTypeId { get; set; }

    public bool IsMandatory { get; set; }

    public decimal? SalaryThreshold { get; set; }

    public bool IsActive { get; set; }

    public long? AddedById { get; set; }

    public long? UpdatedById { get; set; }

    public long? DeletedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public virtual Country Country { get; set; } = null!;

    public virtual StatutoryType StatutoryType { get; set; } = null!;
}
