using System.ComponentModel.DataAnnotations;
using skolenza.domain.Entity;

namespace skolenza.application.DTOs.Attendance;

public sealed class AttendanceRequestDTO
{
    public AttendancePunchAction Action { get; set; }
    [Range(1, int.MaxValue)] public int AttendanceDeviceTypeId { get; set; }
    public long? CampusId { get; set; }
    [Range(-90, 90)] public decimal? Latitude { get; set; }
    [Range(-180, 180)] public decimal? Longitude { get; set; }
    [Range(0, 10000)] public decimal? AccuracyMeters { get; set; }
    public DateTime? ClientOccurredAt { get; set; }
    public Guid IdempotencyKey { get; set; }
}

public sealed record AttendancePunchResponseDTO(long Id, AttendancePunchAction Action,
    int AttendanceDeviceTypeId, string AttendanceDeviceTypeCode, string AttendanceDeviceType,
    DateOnly WorkDate, DateTime OccurredAtUtc,
    long? CampusId, decimal? DistanceFromLocationMeters, bool IsCurrentlyCheckedIn);

public sealed record AttendancePunchItemDTO(long Id, AttendancePunchAction Action,
    int AttendanceDeviceTypeId, string AttendanceDeviceTypeCode, string AttendanceDeviceType,
    DateTime OccurredAtUtc, long? CampusId);

public sealed record AttendanceTodayResponseDTO(DateOnly WorkDate, bool IsCurrentlyCheckedIn,
    IReadOnlyList<AttendancePunchItemDTO> Punches);

public sealed record AttendanceDeviceTypeOptionDTO(int Id, string Code, string Name,
    bool RequiresDeviceRegistration);
