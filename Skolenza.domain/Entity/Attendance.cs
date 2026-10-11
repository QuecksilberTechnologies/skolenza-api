using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class Attendance
{
    public long Id { get; set; }

    public long StaffMemberId { get; set; }

    public DateOnly AttendanceDate { get; set; }

    public int DeviceId { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? AttendanceImagePath { get; set; }

    public string? AttendanceImageUrl { get; set; }
}
