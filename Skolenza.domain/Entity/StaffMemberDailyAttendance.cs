using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class StaffMemberDailyAttendance
{
    public int Id { get; set; }

    public long StaffMemberId { get; set; }

    public DateTime AttendanceDate { get; set; }

    public int AttendanceDeviceTypeId { get; set; }

    public int WorkstationTypeId { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public bool? IsLate { get; set; }

    public byte[]? ClickedImage { get; set; }

    public bool IsActive { get; set; }

    public bool IsMarked { get; set; }

    public long AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public virtual AttendanceDeviceType AttendanceDeviceType { get; set; } = null!;

    public virtual StaffMember StaffMember { get; set; } = null!;

    public virtual WorkstationType WorkstationType { get; set; } = null!;
}
