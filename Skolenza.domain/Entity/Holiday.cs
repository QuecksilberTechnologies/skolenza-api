using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class Holiday
{
    public long Id { get; set; }

    public long? InstitutionId { get; set; }

    public long CampusId { get; set; }

    public string HolidayName { get; set; } = null!;

    public DateOnly HolidayDate { get; set; }

    public bool IsOptional { get; set; }

    public string? Description { get; set; }

    public string? Icon { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public long? SoftDeletedById { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public virtual Institution? Institution { get; set; }

    public virtual Campus Campus { get; set; } = null!;
}
