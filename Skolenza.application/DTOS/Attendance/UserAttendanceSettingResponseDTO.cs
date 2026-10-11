using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Attendance
{
    public class UserAttendanceSettingResponseDTO
    {
        public int Id { get; set; } // Primary Key
        public long StaffMemberId { get; set; } // Foreign Key to StaffMember
        public int AttendanceDeviceTypeId { get; set; } // Device Type ID
        public int WorkstationTypeId { get; set; } // Workstation Type ID
        public bool IsAllowed { get; set; } // Whether attendance is allowed
        public string? Remark { get; set; } // Additional remarks
        public decimal? GeofenceLatitude { get; set; } // Latitude for geofence
        public decimal? GeofenceLongitude { get; set; } // Longitude for geofence
        public bool IsGeofenceEnabled { get; set; } // Geofencing enabled/disabled
        public bool IsActive { get; set; } // Active status
        public long AddedById { get; set; } // ID of the user who added the record
        public DateTime AddedDateTime { get; set; } // Date and time when the record was added
        public long? UpdatedById { get; set; } // ID of the user who last updated the record
        public DateTime? UpdatedDateTime { get; set; } // Date and time when the record was last updated
        public DateTime ReportingTime { get; set; } // Reporting time for attendance
    }

}
