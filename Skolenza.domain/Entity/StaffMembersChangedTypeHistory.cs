using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class StaffMembersChangedTypeHistory
{
    public int Id { get; set; }

    public long StaffMemberId { get; set; }

    public int OldStaffEmploymentTypeId { get; set; }

    public int NewStaffEmploymentTypeId { get; set; }

    public DateTime? ChangeDateTime { get; set; }

    public long ChangedById { get; set; }

    public string? Remark { get; set; }

    public bool IsActive { get; set; }

    public long AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public virtual StaffMember StaffMember { get; set; } = null!;

    public virtual StaffEmploymentType NewStaffEmploymentType { get; set; } = null!;

    public virtual StaffEmploymentType OldStaffEmploymentType { get; set; } = null!;
}
