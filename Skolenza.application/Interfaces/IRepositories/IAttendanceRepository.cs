using skolenza.application.DTOs.Attendance;

namespace skolenza.application.Interfaces.IRepositories;

public interface IAttendanceRepository
{
    Task<AttendancePunchResponseDTO> MarkAsync(long institutionId, long staffMemberId,
        AttendanceRequestDTO request, DateTime serverUtcNow, CancellationToken cancellationToken);
    Task<AttendanceTodayResponseDTO> GetTodayAsync(long institutionId, long staffMemberId,
        DateTime serverUtcNow, CancellationToken cancellationToken);
    Task<IReadOnlyList<AttendanceDeviceTypeOptionDTO>> GetActiveDeviceTypesAsync(
        CancellationToken cancellationToken);
}
