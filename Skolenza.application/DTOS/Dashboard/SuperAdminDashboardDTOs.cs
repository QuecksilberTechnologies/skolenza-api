namespace skolenza.application.DTOS.Dashboard;

public sealed record DashboardSummaryDTO(int TotalStaffMembers, int NewHiresThisMonth, int OpenPositions, int PendingApprovals);
public sealed record StaffMemberOverviewDTO(int Total, int Active, int Inactive, int OnLeave);
public sealed record DashboardStaffMemberDTO(string StaffMemberId, string StaffMemberName, string? DepartmentName, string? DesignationName, string? RoleName, DateTime? Date);
public sealed record DashboardLeaveDTO(string StaffMemberId, string StaffMemberName, string? DepartmentName, DateOnly FromDate, DateOnly ToDate, string? MobileNumber);
public sealed record DashboardLocationDTO(long Id, string Code, string Name, short LocationType, bool IsHeadOffice, bool IsActive);
public sealed record LocationOverviewDTO(int Total, int Active, int HeadOffice, IReadOnlyList<DashboardLocationDTO> Locations);
public sealed record DepartmentHeadcountDTO(int DepartmentId, string DepartmentName, int Headcount);
public sealed record StorageCategoryDTO(string Name, decimal UsedGigabytes);
public sealed record StorageStatusDTO(decimal CapacityGigabytes, decimal UsedGigabytes, IReadOnlyList<StorageCategoryDTO> Categories);
public sealed record HiringStageDTO(string Stage, int Count);
public sealed record HiringPipelineDTO(int TotalApplicants, IReadOnlyList<HiringStageDTO> Stages);

/// <summary>Represents one dashboard metric.</summary>
public sealed record DashboardMetricDTO(string Code, string Label, decimal Value, string? Unit = null);

/// <summary>Represents one lightweight dashboard list item.</summary>
public sealed record DashboardWidgetItemDTO(
    string Code,
    string Title,
    string? Subtitle,
    string? Status,
    string? Date);

public sealed record DashboardSectionDTO<T>(
    T Data,
    string Source,
    bool IsPlaceholder,
    IReadOnlyList<string>? PlaceholderFields = null);

public sealed record DashboardMetricCollectionDTO(IReadOnlyList<DashboardMetricDTO> Metrics);

public sealed record RecentExitedStaffMemberDTO(
    string StaffMemberId,
    string StaffMemberName,
    string? DepartmentName,
    string? DesignationName,
    DateTime DateOfExit);

public sealed record NoticePeriodStaffMemberDTO(
    string Code,
    string StaffMemberName,
    string? DepartmentName,
    string? DesignationName,
    DateOnly ExpectedLastWorkingDate,
    int NoticeDaysRemaining,
    string Status);

public sealed record InstitutionAdministratorDashboardDTO(
    DashboardSectionDTO<DashboardSummaryDTO> Summary,
    DashboardSectionDTO<StaffMemberOverviewDTO> StaffMemberOverview,
    DashboardSectionDTO<IReadOnlyList<DashboardLeaveDTO>> CurrentlyOnLeave,
    DashboardSectionDTO<IReadOnlyList<NoticePeriodStaffMemberDTO>> StaffMembersOnNotice,
    DashboardSectionDTO<IReadOnlyList<RecentExitedStaffMemberDTO>> ExitedStaffMembers,
    DashboardSectionDTO<LocationOverviewDTO> Locations,
    DashboardSectionDTO<IReadOnlyList<DashboardStaffMemberDTO>> UpcomingBirthdays,
    DashboardSectionDTO<IReadOnlyList<DashboardStaffMemberDTO>> RecentOnboarding,
    DashboardSectionDTO<IReadOnlyList<DepartmentHeadcountDTO>> DepartmentHeadcount,
    DashboardSectionDTO<StorageStatusDTO> StorageStatus,
    DashboardSectionDTO<HiringPipelineDTO> HiringPipeline);

public sealed record StaffManagerDashboardDTO(
    DashboardSectionDTO<DashboardMetricCollectionDTO> TeamSummary,
    DashboardSectionDTO<DashboardMetricCollectionDTO> TeamAttendance,
    DashboardSectionDTO<IReadOnlyList<NoticePeriodStaffMemberDTO>> TeamMembersOnNotice,
    DashboardSectionDTO<IReadOnlyList<DashboardWidgetItemDTO>> TeamLeave,
    DashboardSectionDTO<IReadOnlyList<DashboardWidgetItemDTO>> TeamBirthdays,
    DashboardSectionDTO<IReadOnlyList<DashboardWidgetItemDTO>> RecentTeamOnboarding);

public sealed record StaffMemberDashboardDTO(
    DashboardSectionDTO<DashboardMetricCollectionDTO> MyProfile,
    DashboardSectionDTO<DashboardMetricCollectionDTO> MyAttendance,
    DashboardSectionDTO<DashboardMetricCollectionDTO> MyLeave,
    DashboardSectionDTO<IReadOnlyList<DashboardWidgetItemDTO>> MyTasks,
    DashboardSectionDTO<IReadOnlyList<DashboardWidgetItemDTO>> MyDocuments,
    DashboardSectionDTO<IReadOnlyList<DashboardWidgetItemDTO>> Announcements);

public sealed record ExternalUserDashboardDTO(
    DashboardSectionDTO<DashboardMetricCollectionDTO> ClientSummary,
    DashboardSectionDTO<IReadOnlyList<DashboardWidgetItemDTO>> OpenTickets,
    DashboardSectionDTO<IReadOnlyList<DashboardWidgetItemDTO>> RecentActivity,
    DashboardSectionDTO<IReadOnlyList<DashboardWidgetItemDTO>> ClientSiteStaffMembers,
    DashboardSectionDTO<IReadOnlyList<DashboardWidgetItemDTO>> Documents);

public sealed record DashboardDataDTO(
    string RoleTypeCode,
    string RoleTypeName,
    DateTime GeneratedAtUtc,
    InstitutionAdministratorDashboardDTO? InstitutionAdministrator,
    StaffManagerDashboardDTO? StaffManager,
    StaffMemberDashboardDTO? StaffMember,
    ExternalUserDashboardDTO? ExternalUser);
