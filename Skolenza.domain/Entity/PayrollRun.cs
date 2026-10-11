using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class PayrollRun
{
    public long Id { get; set; }

    public long InstitutionId { get; set; }

    public int PayrollMonth { get; set; }

    public int PayrollYear { get; set; }

    public int Status { get; set; }

    public DateTime? ProcessedOn { get; set; }

    public bool? IsLocked { get; set; }

    public long? LockedBy { get; set; }

    public DateTime? LockedOn { get; set; }

    public bool IsActive { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public virtual ICollection<PayrollStaffMember> PayrollStaffMember { get; set; } = new List<PayrollStaffMember>();

    public virtual Institution Institution { get; set; } = null!;
}
