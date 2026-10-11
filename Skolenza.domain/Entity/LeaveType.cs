using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class LeaveType
{
    public int Id { get; set; }

    public long? InstitutionId { get; set; }

    public string LeaveName { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public long? AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdateById { get; set; }

    public DateTime? UpdateDateTime { get; set; }

    public bool? IsSoftDeleted { get; set; }

    public long? SoftDeletedBy { get; set; }

    public DateTime? SoftDeletedDateTime { get; set; }

    public virtual ICollection<LeaveRequest> LeaveRequest { get; set; } = new List<LeaveRequest>();

    public virtual Institution? Institution { get; set; }
}
