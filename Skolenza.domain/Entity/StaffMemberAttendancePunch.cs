namespace skolenza.domain.Entity;

public enum AttendancePunchAction : short { CheckIn = 1, CheckOut = 2 }

/// <summary>Immutable attendance event recorded from an authorized attendance channel.</summary>
public sealed class StaffMemberAttendancePunch
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StaffMemberId { get; set; }
    public long StaffMemberWorkArrangementId { get; set; }
    public long PolicyVersionId { get; set; }
    public long? CampusId { get; set; }
    public DateOnly WorkDate { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime? ClientOccurredAt { get; set; }
    public int AttendanceDeviceTypeId { get; set; }
    public short PunchAction { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public decimal? AccuracyMeters { get; set; }
    public decimal? DistanceFromLocationMeters { get; set; }
    public Guid IdempotencyKey { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public AttendanceDeviceType AttendanceDeviceType { get; set; } = null!;
}
