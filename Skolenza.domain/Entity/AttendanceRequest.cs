using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class AttendanceRequest
{
    public int Id { get; set; }

    public long StaffMemberId { get; set; }

    public DateTime AttendanceDate { get; set; }

    public int AttendanceDeviceTypeId { get; set; }

    public int WorkstationTypeId { get; set; }

    public double? Latitude { get; set; }

    public bool? IsActive { get; set; }

    public string? Remark { get; set; }

    public double? Longitude { get; set; }

    public string? AttendanceImagePath { get; set; }

    public string? AttendanceImageUrl { get; set; }
}
