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

public sealed record GetSuperAdminSummaryQuery(PermissionRequestDTO Permission) : IRequest<ApiResponse<DashboardSummaryDTO>>;
public sealed record GetSuperAdminStaffMemberOverviewQuery(PermissionRequestDTO Permission) : IRequest<ApiResponse<StaffMemberOverviewDTO>>;
public sealed record GetSuperAdminBirthdaysQuery(PermissionRequestDTO Permission, int Days = 30) : IRequest<ApiResponse<IReadOnlyList<DashboardStaffMemberDTO>>>;
public sealed record GetSuperAdminOnboardingQuery(PermissionRequestDTO Permission, int Limit = 10) : IRequest<ApiResponse<IReadOnlyList<DashboardStaffMemberDTO>>>;
public sealed record GetSuperAdminDepartmentHeadcountQuery(PermissionRequestDTO Permission) : IRequest<ApiResponse<IReadOnlyList<DepartmentHeadcountDTO>>>;
public sealed record GetSuperAdminLeaveQuery(PermissionRequestDTO Permission) : IRequest<ApiResponse<IReadOnlyList<DashboardLeaveDTO>>>;
public sealed record GetSuperAdminLocationsQuery(PermissionRequestDTO Permission, int Limit = 3) : IRequest<ApiResponse<LocationOverviewDTO>>;
public sealed record GetSuperAdminStorageQuery(PermissionRequestDTO Permission) : IRequest<ApiResponse<StorageStatusDTO>>;
public sealed record GetSuperAdminHiringPipelineQuery(PermissionRequestDTO Permission) : IRequest<ApiResponse<HiringPipelineDTO>>;

public abstract class SuperAdminDashboardHandlerBase(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    ILogger<InstitutionConfigurationHandlerBase> logger) : InstitutionConfigurationHandlerBase(unitOfWork, commonRequestService, logger)
{
    protected async Task<long> ValidateAsync(PermissionRequestDTO permission, CancellationToken cancellationToken)
    {
        var (institutionId, _) = await ValidateInstitutionPermissionAsync(permission, cancellationToken);
        var context = await ValidateInstitutionDataAccessContextAsync();
        if (context.RoleTypeId != ConstantValues.RoleTypeAdmin)
        {
            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
        }
        return institutionId;
    }
}

public sealed class GetSuperAdminSummaryHandler(
    IUnitOfWork uow,
    ICommonRequestService common,
    ILogger<InstitutionConfigurationHandlerBase> logger,
    ISuperAdminDashboardRepository repository)
    : SuperAdminDashboardHandlerBase(uow, common, logger),
      IRequestHandler<GetSuperAdminSummaryQuery, ApiResponse<DashboardSummaryDTO>>
{
    public async Task<ApiResponse<DashboardSummaryDTO>> Handle(
        GetSuperAdminSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var institutionId = await ValidateAsync(request.Permission, cancellationToken);
        var summary = await repository.GetSummaryAsync(institutionId, DateTime.UtcNow, cancellationToken);
        return ApiResponse<DashboardSummaryDTO>.Success(summary);
    }
}
public sealed class GetSuperAdminStaffMemberOverviewHandler(
    IUnitOfWork uow,
    ICommonRequestService common,
    ILogger<InstitutionConfigurationHandlerBase> logger,
    ISuperAdminDashboardRepository repository)
    : SuperAdminDashboardHandlerBase(uow, common, logger),
      IRequestHandler<GetSuperAdminStaffMemberOverviewQuery, ApiResponse<StaffMemberOverviewDTO>>
{
    public async Task<ApiResponse<StaffMemberOverviewDTO>> Handle(
        GetSuperAdminStaffMemberOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var institutionId = await ValidateAsync(request.Permission, cancellationToken);
        var overview = await repository.GetStaffMemberOverviewAsync(
            institutionId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            cancellationToken);
        return ApiResponse<StaffMemberOverviewDTO>.Success(overview);
    }
}
public sealed class GetSuperAdminBirthdaysHandler(
    IUnitOfWork uow,
    ICommonRequestService common,
    ILogger<InstitutionConfigurationHandlerBase> logger,
    ISuperAdminDashboardRepository repository)
    : SuperAdminDashboardHandlerBase(uow, common, logger),
      IRequestHandler<GetSuperAdminBirthdaysQuery, ApiResponse<IReadOnlyList<DashboardStaffMemberDTO>>>
{
    public async Task<ApiResponse<IReadOnlyList<DashboardStaffMemberDTO>>> Handle(
        GetSuperAdminBirthdaysQuery request,
        CancellationToken cancellationToken)
    {
        var institutionId = await ValidateAsync(request.Permission, cancellationToken);
        var birthdays = await repository.GetUpcomingBirthdaysAsync(
            institutionId,
            DateTime.UtcNow,
            Math.Clamp(request.Days, 1, 366),
            cancellationToken);
        return ApiResponse<IReadOnlyList<DashboardStaffMemberDTO>>.Success(birthdays);
    }
}
public sealed class GetSuperAdminOnboardingHandler(
    IUnitOfWork uow,
    ICommonRequestService common,
    ILogger<InstitutionConfigurationHandlerBase> logger,
    ISuperAdminDashboardRepository repository)
    : SuperAdminDashboardHandlerBase(uow, common, logger),
      IRequestHandler<GetSuperAdminOnboardingQuery, ApiResponse<IReadOnlyList<DashboardStaffMemberDTO>>>
{
    public async Task<ApiResponse<IReadOnlyList<DashboardStaffMemberDTO>>> Handle(
        GetSuperAdminOnboardingQuery request,
        CancellationToken cancellationToken)
    {
        var institutionId = await ValidateAsync(request.Permission, cancellationToken);
        var staffMembers = await repository.GetRecentOnboardingAsync(
            institutionId,
            DateTime.UtcNow.Date.AddDays(-30),
            Math.Clamp(request.Limit, 1, 50),
            cancellationToken);
        return ApiResponse<IReadOnlyList<DashboardStaffMemberDTO>>.Success(staffMembers);
    }
}
public sealed class GetSuperAdminDepartmentHeadcountHandler(
    IUnitOfWork uow,
    ICommonRequestService common,
    ILogger<InstitutionConfigurationHandlerBase> logger,
    ISuperAdminDashboardRepository repository)
    : SuperAdminDashboardHandlerBase(uow, common, logger),
      IRequestHandler<GetSuperAdminDepartmentHeadcountQuery, ApiResponse<IReadOnlyList<DepartmentHeadcountDTO>>>
{
    public async Task<ApiResponse<IReadOnlyList<DepartmentHeadcountDTO>>> Handle(
        GetSuperAdminDepartmentHeadcountQuery request,
        CancellationToken cancellationToken)
    {
        var institutionId = await ValidateAsync(request.Permission, cancellationToken);
        var departments = await repository.GetDepartmentHeadcountAsync(institutionId, cancellationToken);
        return ApiResponse<IReadOnlyList<DepartmentHeadcountDTO>>.Success(departments);
    }
}
public sealed class GetSuperAdminLeaveHandler(
    IUnitOfWork uow,
    ICommonRequestService common,
    ILogger<InstitutionConfigurationHandlerBase> logger,
    ISuperAdminDashboardRepository repository)
    : SuperAdminDashboardHandlerBase(uow, common, logger),
      IRequestHandler<GetSuperAdminLeaveQuery, ApiResponse<IReadOnlyList<DashboardLeaveDTO>>>
{
    public async Task<ApiResponse<IReadOnlyList<DashboardLeaveDTO>>> Handle(
        GetSuperAdminLeaveQuery request,
        CancellationToken cancellationToken)
    {
        var institutionId = await ValidateAsync(request.Permission, cancellationToken);
        var staffMembers = await repository.GetCurrentlyOnLeaveAsync(
            institutionId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            cancellationToken);
        return ApiResponse<IReadOnlyList<DashboardLeaveDTO>>.Success(staffMembers);
    }
}
public sealed class GetSuperAdminLocationsHandler(
    IUnitOfWork uow,
    ICommonRequestService common,
    ILogger<InstitutionConfigurationHandlerBase> logger,
    ISuperAdminDashboardRepository repository)
    : SuperAdminDashboardHandlerBase(uow, common, logger),
      IRequestHandler<GetSuperAdminLocationsQuery, ApiResponse<LocationOverviewDTO>>
{
    public async Task<ApiResponse<LocationOverviewDTO>> Handle(
        GetSuperAdminLocationsQuery request,
        CancellationToken cancellationToken)
    {
        var institutionId = await ValidateAsync(request.Permission, cancellationToken);
        var locations = await repository.GetLocationsAsync(
            institutionId,
            Math.Clamp(request.Limit, 1, 50),
            cancellationToken);
        return ApiResponse<LocationOverviewDTO>.Success(locations);
    }
}
public sealed class GetSuperAdminStorageHandler(
    IUnitOfWork uow,
    ICommonRequestService common,
    ILogger<InstitutionConfigurationHandlerBase> logger)
    : SuperAdminDashboardHandlerBase(uow, common, logger),
      IRequestHandler<GetSuperAdminStorageQuery, ApiResponse<StorageStatusDTO>>
{
    public async Task<ApiResponse<StorageStatusDTO>> Handle(
        GetSuperAdminStorageQuery request,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(request.Permission, cancellationToken);
        var categories = new[]
        {
            new StorageCategoryDTO("Documents", 14.2m),
            new StorageCategoryDTO("Media", 9.8m),
            new StorageCategoryDTO("Videos", 5.6m),
            new StorageCategoryDTO("Other", 1.8m)
        };
        return ApiResponse<StorageStatusDTO>.Success(new StorageStatusDTO(50m, 31.4m, categories));
    }
}
public sealed class GetSuperAdminHiringPipelineHandler(
    IUnitOfWork uow,
    ICommonRequestService common,
    ILogger<InstitutionConfigurationHandlerBase> logger)
    : SuperAdminDashboardHandlerBase(uow, common, logger),
      IRequestHandler<GetSuperAdminHiringPipelineQuery, ApiResponse<HiringPipelineDTO>>
{
    public async Task<ApiResponse<HiringPipelineDTO>> Handle(
        GetSuperAdminHiringPipelineQuery request,
        CancellationToken cancellationToken)
    {
        await ValidateAsync(request.Permission, cancellationToken);
        var stages = new[]
        {
            new HiringStageDTO("Applied", 148),
            new HiringStageDTO("Screening", 89),
            new HiringStageDTO("Interview", 42),
            new HiringStageDTO("Offer", 14),
            new HiringStageDTO("Hired", 8)
        };
        return ApiResponse<HiringPipelineDTO>.Success(new HiringPipelineDTO(148, stages));
    }
}
