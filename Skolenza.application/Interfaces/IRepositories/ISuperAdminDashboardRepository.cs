using skolenza.application.DTOS.Dashboard;

namespace skolenza.application.Interfaces.IRepositories;

public interface ISuperAdminDashboardRepository
{
    Task<DashboardSummaryDTO> GetSummaryAsync(long institutionId, DateTime today, CancellationToken cancellationToken);
    Task<StaffMemberOverviewDTO> GetStaffMemberOverviewAsync(long institutionId, DateOnly today, CancellationToken cancellationToken);
    Task<IReadOnlyList<DashboardStaffMemberDTO>> GetUpcomingBirthdaysAsync(long institutionId, DateTime today, int days, CancellationToken cancellationToken);
    Task<IReadOnlyList<DashboardStaffMemberDTO>> GetRecentOnboardingAsync(long institutionId, int limit, CancellationToken cancellationToken);
    Task<IReadOnlyList<DashboardStaffMemberDTO>> GetRecentOnboardingAsync(long institutionId, DateTime fromDate, int limit, CancellationToken cancellationToken);
    Task<IReadOnlyList<RecentExitedStaffMemberDTO>> GetRecentExitedStaffMembersAsync(long institutionId, DateTime fromDate, DateTime throughDate, int limit, CancellationToken cancellationToken);
    Task<IReadOnlyList<DepartmentHeadcountDTO>> GetDepartmentHeadcountAsync(long institutionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<DashboardLeaveDTO>> GetCurrentlyOnLeaveAsync(long institutionId, DateOnly today, CancellationToken cancellationToken);
    Task<LocationOverviewDTO> GetLocationsAsync(long institutionId, int limit, CancellationToken cancellationToken);
}
