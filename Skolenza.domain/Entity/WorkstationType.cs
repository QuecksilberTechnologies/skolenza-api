using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class WorkstationType
{
    public int Id { get; set; }

    public string Workstation { get; set; } = null!;

    public bool? IsActive { get; set; }

    public long AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public virtual ICollection<StaffMemberDailyAttendance> StaffMemberDailyAttendance { get; set; } = new List<StaffMemberDailyAttendance>();

    public virtual ICollection<UserAttendanceSetting> UserAttendanceSetting { get; set; } = new List<UserAttendanceSetting>();
}
