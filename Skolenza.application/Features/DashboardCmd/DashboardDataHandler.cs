// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Returns one complete dashboard payload for the authenticated RoleType.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Dashboard;
using skolenza.application.Exceptions;
using skolenza.application.Features.InstitutionConfigurationCmd.Handlers;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.DashboardCmd;

public sealed record GetDashboardDataQuery(
    string RoleTypeCode,
    PermissionRequestDTO Permission)
    : IRequest<ApiResponse<DashboardDataDTO>>;

public sealed class GetDashboardDataHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    ILogger<InstitutionConfigurationHandlerBase> logger,
    ISuperAdminDashboardRepository repository)
    : InstitutionConfigurationHandlerBase(unitOfWork, commonRequestService, logger),
      IRequestHandler<GetDashboardDataQuery, ApiResponse<DashboardDataDTO>>
{
    public async Task<ApiResponse<DashboardDataDTO>> Handle(
        GetDashboardDataQuery request,
        CancellationToken cancellationToken)
    {
        var (institutionId, _) = await ValidateInstitutionPermissionAsync(
            request.Permission,
            cancellationToken);
        var context = await ValidateInstitutionDataAccessContextAsync();
        var roleTypeCode = ConstantValues.GetRoleTypeCode(context.RoleTypeId);
        if (string.IsNullOrWhiteSpace(roleTypeCode) ||
            !string.Equals(
                request.RoleTypeCode?.Trim(),
                roleTypeCode,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
        }

        var generatedAtUtc = DateTime.UtcNow;
        var data = roleTypeCode switch
        {
            ConstantValues.RoleTypeAdminCode => await BuildInstitutionAdministratorAsync(
                institutionId,
                generatedAtUtc,
                context.RoleTypeId,
                cancellationToken),
            ConstantValues.RoleTypeManagerCode => BuildStaffManager(
                generatedAtUtc,
                context.RoleTypeId),
            ConstantValues.RoleTypeStaffMemberCode => BuildStaffMember(
                generatedAtUtc,
                context.RoleTypeId),
            ConstantValues.RoleTypeClientCode => BuildExternalUser(
                generatedAtUtc,
                context.RoleTypeId),
            _ => throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied)
        };

        return ApiResponse<DashboardDataDTO>.Success(
            data,
            "Dashboard data fetched successfully.");
    }

    private async Task<DashboardDataDTO> BuildInstitutionAdministratorAsync(
        long institutionId,
        DateTime generatedAtUtc,
        int roleTypeId,
        CancellationToken cancellationToken)
    {
        var summary = await repository.GetSummaryAsync(institutionId, generatedAtUtc, cancellationToken);
        var overview = await repository.GetStaffMemberOverviewAsync(
            institutionId,
            DateOnly.FromDateTime(generatedAtUtc),
            cancellationToken);
        var onLeave = await repository.GetCurrentlyOnLeaveAsync(
            institutionId,
            DateOnly.FromDateTime(generatedAtUtc),
            cancellationToken);
        var locations = await repository.GetLocationsAsync(institutionId, 3, cancellationToken);
        var birthdays = await repository.GetUpcomingBirthdaysAsync(
            institutionId,
            generatedAtUtc,
            30,
            cancellationToken);
        var onboarding = await repository.GetRecentOnboardingAsync(
            institutionId,
            generatedAtUtc.Date.AddDays(-30),
            50,
            cancellationToken);
        var departments = await repository.GetDepartmentHeadcountAsync(institutionId, cancellationToken);
        var exited = await repository.GetRecentExitedStaffMembersAsync(
            institutionId,
            generatedAtUtc.Date.AddDays(-7),
            generatedAtUtc,
            50,
            cancellationToken);

        var storage = new StorageStatusDTO(
            50m,
            31.4m,
            [
                new("Documents", 14.2m),
                new("Media", 9.8m),
                new("Videos", 5.6m),
                new("Other", 1.8m)
            ]);
        var hiring = new HiringPipelineDTO(
            148,
            [
                new("Applied", 148),
                new("Screening", 89),
                new("Interview", 42),
                new("Offer", 14),
                new("Hired", 8)
            ]);
        NoticePeriodStaffMemberDTO[] noticeStaffMembers =
        [
            new(
                "SAMPLE-NOTICE-001",
                "Neha Singh",
                "Human Resources",
                "HR Executive",
                DateOnly.FromDateTime(generatedAtUtc.Date.AddDays(18)),
                18,
                "NOTICE_PERIOD")
        ];

        var dashboard = new InstitutionAdministratorDashboardDTO(
            Dynamic(summary, ["OpenPositions", "PendingApprovals"], "MIXED"),
            Dynamic(overview),
            Dynamic<IReadOnlyList<DashboardLeaveDTO>>(onLeave),
            Placeholder<IReadOnlyList<NoticePeriodStaffMemberDTO>>(noticeStaffMembers),
            Dynamic<IReadOnlyList<RecentExitedStaffMemberDTO>>(exited),
            Dynamic(locations),
            Dynamic<IReadOnlyList<DashboardStaffMemberDTO>>(birthdays),
            Dynamic<IReadOnlyList<DashboardStaffMemberDTO>>(onboarding),
            Dynamic<IReadOnlyList<DepartmentHeadcountDTO>>(departments),
            Placeholder(storage),
            Placeholder(hiring));

        return Envelope(roleTypeId, generatedAtUtc, institutionAdministrator: dashboard);
    }

    private static DashboardDataDTO BuildStaffManager(DateTime now, int roleTypeId)
    {
        var summary = Metrics(
            new DashboardMetricDTO("TEAM_MEMBERS", "Team Members", 8),
            new DashboardMetricDTO("PRESENT_TODAY", "Present Today", 6),
            new DashboardMetricDTO("ON_LEAVE", "On Leave", 1),
            new DashboardMetricDTO("ON_NOTICE", "On Notice", 1),
            new DashboardMetricDTO("PENDING_APPROVALS", "Pending Approvals", 2));
        var attendance = Metrics(
            new DashboardMetricDTO("PRESENT", "Present", 6),
            new DashboardMetricDTO("ABSENT", "Absent", 1),
            new DashboardMetricDTO("ON_LEAVE", "On Leave", 1));
        NoticePeriodStaffMemberDTO[] notice =
        [new("SAMPLE-NOTICE-001", "Neha Singh", "Human Resources", "HR Executive", DateOnly.FromDateTime(now.AddDays(18)), 18, "NOTICE_PERIOD")];
        DashboardWidgetItemDTO[] leave =
        [new("SAMPLE-LEAVE-001", "Aarav Sharma", "Annual Leave", "APPROVED", DateRange(now.AddDays(2), now.AddDays(3)))];
        DashboardWidgetItemDTO[] birthdays =
        [new("SAMPLE-BIRTHDAY-001", "Meera Patel", "Software Engineer", null, now.AddDays(8).ToString("yyyy-MM-dd"))];
        DashboardWidgetItemDTO[] onboarding =
        [new("SAMPLE-ONBOARD-001", "Kabir Singh", "Quality Analyst", "ACTIVE", now.AddDays(-3).ToString("yyyy-MM-dd"))];

        var dashboard = new StaffManagerDashboardDTO(
            Placeholder(summary),
            Placeholder(attendance),
            Placeholder<IReadOnlyList<NoticePeriodStaffMemberDTO>>(notice),
            Placeholder<IReadOnlyList<DashboardWidgetItemDTO>>(leave),
            Placeholder<IReadOnlyList<DashboardWidgetItemDTO>>(birthdays),
            Placeholder<IReadOnlyList<DashboardWidgetItemDTO>>(onboarding));
        return Envelope(roleTypeId, now, staffManager: dashboard);
    }

    private static DashboardDataDTO BuildStaffMember(DateTime now, int roleTypeId)
    {
        var dashboard = new StaffMemberDashboardDTO(
            Placeholder(Metrics(new DashboardMetricDTO("PROFILE_COMPLETION", "Profile Completion", 92, "%"))),
            Placeholder(Metrics(new DashboardMetricDTO("WORKING_DAYS", "Working Days", 20), new DashboardMetricDTO("PRESENT_DAYS", "Present Days", 18), new DashboardMetricDTO("LEAVE_DAYS", "Leave Days", 1))),
            Placeholder(Metrics(new DashboardMetricDTO("AVAILABLE_DAYS", "Available", 12, "days"), new DashboardMetricDTO("USED_DAYS", "Used", 6, "days"))),
            Placeholder<IReadOnlyList<DashboardWidgetItemDTO>>([new("SAMPLE-TASK-001", "Complete profile verification", "Upload remaining employment document", "PENDING", null)]),
            Placeholder<IReadOnlyList<DashboardWidgetItemDTO>>([new("SAMPLE-DOC-001", "Employment Agreement", "StaffMember document", "AVAILABLE", null)]),
            Placeholder<IReadOnlyList<DashboardWidgetItemDTO>>([new("SAMPLE-NEWS-001", "Monthly town hall", "Company-wide meeting", "PUBLISHED", now.AddDays(4).ToString("yyyy-MM-dd"))]));
        return Envelope(roleTypeId, now, staffMember: dashboard);
    }

    private static DashboardDataDTO BuildExternalUser(DateTime now, int roleTypeId)
    {
        var dashboard = new ExternalUserDashboardDTO(
            Placeholder(Metrics(new DashboardMetricDTO("ACTIVE_ENGAGEMENTS", "Active Engagements", 1), new DashboardMetricDTO("CLIENT_SITE_STAFF_MEMBERS", "Client-site StaffMembers", 6), new DashboardMetricDTO("OPEN_TICKETS", "Open Tickets", 3))),
            Placeholder<IReadOnlyList<DashboardWidgetItemDTO>>([new("SAMPLE-TKT-1042", "Access request", "Client portal", "OPEN", now.AddDays(-1).ToString("yyyy-MM-dd")), new("SAMPLE-TKT-1038", "Device connectivity", "Assigned support team", "IN_PROGRESS", now.AddDays(-2).ToString("yyyy-MM-dd"))]),
            Placeholder<IReadOnlyList<DashboardWidgetItemDTO>>([new("SAMPLE-ACT-001", "Weekly status report shared", "Project delivery", "COMPLETED", now.AddDays(-1).ToString("yyyy-MM-dd"))]),
            Placeholder<IReadOnlyList<DashboardWidgetItemDTO>>([new("SAMPLE-EMP-001", "Riya Mehta", "Implementation Consultant", "ACTIVE", null), new("SAMPLE-EMP-002", "Arjun Nair", "Support Engineer", "ACTIVE", null)]),
            Placeholder<IReadOnlyList<DashboardWidgetItemDTO>>([new("SAMPLE-CLIENT-DOC-001", "Service Agreement", "Shared document", "AVAILABLE", null)]));
        return Envelope(roleTypeId, now, externalUser: dashboard);
    }

    private static DashboardDataDTO Envelope(
        int roleTypeId,
        DateTime generatedAtUtc,
        InstitutionAdministratorDashboardDTO? institutionAdministrator = null,
        StaffManagerDashboardDTO? staffManager = null,
        StaffMemberDashboardDTO? staffMember = null,
        ExternalUserDashboardDTO? externalUser = null)
    {
        return new DashboardDataDTO(
            ConstantValues.GetRoleTypeCode(roleTypeId),
            ConstantValues.GetRoleTypeDisplayName(roleTypeId),
            generatedAtUtc,
            institutionAdministrator,
            staffManager,
            staffMember,
            externalUser);
    }

    private static DashboardMetricCollectionDTO Metrics(params DashboardMetricDTO[] metrics) => new(metrics);

    private static DashboardSectionDTO<T> Dynamic<T>(
        T data,
        IReadOnlyList<string>? placeholderFields = null,
        string source = "DATABASE") => new(data, source, false, placeholderFields);

    private static DashboardSectionDTO<T> Placeholder<T>(T data) =>
        new(data, "TEMPORARY_STATIC", true);

    private static string DateRange(DateTime fromDate, DateTime toDate) =>
        $"{fromDate:yyyy-MM-dd}/{toDate:yyyy-MM-dd}";
}
