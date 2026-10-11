using skolenza.application.DTOs.Attendance;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.Features.AttendanceCmd;

public sealed record MarkAttendanceCommand(AttendanceRequestDTO Request)
    : IRequest<ApiResponse<AttendancePunchResponseDTO>>;

public sealed record GetTodayAttendanceQuery : IRequest<ApiResponse<AttendanceTodayResponseDTO>>;
public sealed record GetAttendanceDeviceTypesQuery
    : IRequest<ApiResponse<IReadOnlyList<AttendanceDeviceTypeOptionDTO>>>;

/// <summary>Marks attendance only for the staffMember represented by the validated Institution token.</summary>
public sealed class MarkAttendanceCommandHandler(IAttendanceRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<MarkAttendanceCommand, ApiResponse<AttendancePunchResponseDTO>>
{
    public async Task<ApiResponse<AttendancePunchResponseDTO>> Handle(MarkAttendanceCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request ?? throw new ValidationErrorException("Attendance request is required.");
        if (!Enum.IsDefined(request.Action) || request.AttendanceDeviceTypeId <= 0
            || request.IdempotencyKey == Guid.Empty)
            throw new ValidationErrorException("Action, AttendanceDeviceTypeId, and IdempotencyKey are required.");
        if (request.Latitude.HasValue != request.Longitude.HasValue)
            throw new ValidationErrorException("Latitude and Longitude must be supplied together.");

        var actor = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!actor.Success || actor.InstitutionId <= 0 || actor.LoggedInStaffMemberId <= 0)
            throw new UnauthorizedAccessException(actor.ErrorMessage ?? "Unauthorized request.");

        var result = await repository.MarkAsync(actor.InstitutionId, actor.LoggedInStaffMemberId,
            request, DateTime.UtcNow, cancellationToken);
        return ApiResponse<AttendancePunchResponseDTO>.Success(result, "Attendance punch recorded successfully.");
    }
}

/// <summary>Returns the active attendance source catalogue for authenticated Institution users.</summary>
public sealed class GetAttendanceDeviceTypesQueryHandler(IAttendanceRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<GetAttendanceDeviceTypesQuery,
        ApiResponse<IReadOnlyList<AttendanceDeviceTypeOptionDTO>>>
{
    public async Task<ApiResponse<IReadOnlyList<AttendanceDeviceTypeOptionDTO>>> Handle(
        GetAttendanceDeviceTypesQuery query, CancellationToken cancellationToken)
    {
        var actor = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!actor.Success || actor.InstitutionId <= 0 || actor.LoggedInStaffMemberId <= 0)
            throw new UnauthorizedAccessException(actor.ErrorMessage ?? "Unauthorized request.");

        var result = await repository.GetActiveDeviceTypesAsync(cancellationToken);
        return ApiResponse<IReadOnlyList<AttendanceDeviceTypeOptionDTO>>.Success(result,
            "Attendance device types retrieved successfully.");
    }
}

/// <summary>Returns today's immutable punch timeline for the authenticated staffMember.</summary>
public sealed class GetTodayAttendanceQueryHandler(IAttendanceRepository repository,
    ICommonRequestService commonRequestService)
    : IRequestHandler<GetTodayAttendanceQuery, ApiResponse<AttendanceTodayResponseDTO>>
{
    public async Task<ApiResponse<AttendanceTodayResponseDTO>> Handle(GetTodayAttendanceQuery query,
        CancellationToken cancellationToken)
    {
        var actor = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!actor.Success || actor.InstitutionId <= 0 || actor.LoggedInStaffMemberId <= 0)
            throw new UnauthorizedAccessException(actor.ErrorMessage ?? "Unauthorized request.");

        var result = await repository.GetTodayAsync(actor.InstitutionId, actor.LoggedInStaffMemberId,
            DateTime.UtcNow, cancellationToken);
        return ApiResponse<AttendanceTodayResponseDTO>.Success(result,
            "Today's attendance retrieved successfully.");
    }
}
