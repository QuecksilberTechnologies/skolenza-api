using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class UserAttendanceSetting
{
    public int Id { get; set; }

    public long StaffMemberId { get; set; }

    public int AttendanceDeviceTypeId { get; set; }

    public int WorkstationTypeId { get; set; }

    public bool IsAllowed { get; set; }

    public string? Remark { get; set; }

    public decimal? GeofenceLatitude { get; set; }

    public decimal? GeofenceLongitude { get; set; }

    public bool IsGeofenceEnabled { get; set; }

    public bool IsActive { get; set; }

    public long AddedById { get; set; }

    public DateTime AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public DateTime? ReportingTime { get; set; }

    public virtual AttendanceDeviceType AttendanceDeviceType { get; set; } = null!;

    public virtual StaffMember StaffMember { get; set; } = null!;

    public virtual WorkstationType WorkstationType { get; set; } = null!;
}
