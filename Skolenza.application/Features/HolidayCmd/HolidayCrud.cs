using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOs.Holiday;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.Features.HolidayCmd;

#region Requests

public sealed record ListHolidaysQuery(BasicRequestDTO DTO)
    : IRequest<ApiResponse<IReadOnlyList<HolidayDTO>>>;

public sealed record GetHolidayQuery(HolidayByIdRequestDTO DTO)
    : IRequest<ApiResponse<HolidayDTO>>;

public sealed record CreateHolidayCommand(SaveHolidayRequestDTO DTO)
    : IRequest<ApiResponse<HolidayDTO>>;

public sealed record UpdateHolidayCommand(UpdateHolidayRequestDTO DTO)
    : IRequest<ApiResponse<HolidayDTO>>;

public sealed record DeleteHolidayCommand(HolidayByIdRequestDTO DTO)
    : IRequest<ApiResponse<bool>>;

public sealed record ImportHolidaysCommand(ImportHolidayRequestDTO DTO)
    : IRequest<ApiResponse<HolidayImportResultDTO>>;

public sealed record ExportHolidaysQuery(BasicRequestDTO DTO)
    : IRequest<byte[]>;

public sealed record GetHolidayCalendarDisplayConstantsQuery
    : IRequest<ApiResponse<HolidayCalendarDisplayConstantsDTO>>;

#endregion

#region Permission pipeline

public sealed class HolidayPermissionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ListHolidaysQuery
            and not GetHolidayQuery
            and not CreateHolidayCommand
            and not UpdateHolidayCommand
            and not DeleteHolidayCommand
            and not ImportHolidaysCommand
            and not ExportHolidaysQuery)
        {
            return await next();
        }

        var dto = request switch
        {
            ListHolidaysQuery query => (PermissionRequestDTO)query.DTO,
            GetHolidayQuery query => query.DTO,
            CreateHolidayCommand command => command.DTO,
            UpdateHolidayCommand command => command.DTO,
            DeleteHolidayCommand command => command.DTO,
            ImportHolidaysCommand command => command.DTO,
            ExportHolidaysQuery query => query.DTO,
            _ => throw new ValidationErrorException("Holiday permission request is required.")
        };

        if (dto.ModuleId <= 0 || dto.OperationId <= 0)
        {
            throw new ValidationErrorException("ModuleId and OperationId are required.");
        }

        var moduleCode = await commonRequestService.GetModuleCodeAsync(dto.ModuleId);
        if (!string.Equals(moduleCode, "INSTITUTION_POLICY_HOLIDAY", StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenAccessException("The selected module is not valid for holidays.");
        }

        var expectedOperation = request switch
        {
            CreateHolidayCommand => "Add",
            UpdateHolidayCommand => "Update",
            DeleteHolidayCommand => "Delete",
            ImportHolidaysCommand => "Import",
            ExportHolidaysQuery => "Export",
            _ => "View"
        };

        var operation = await unitOfWork.OperationRepository.GetOperationByIdAsync(dto.OperationId);
        if (!string.Equals(operation?.OperationName, expectedOperation, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenAccessException("The selected operation is not valid for this holiday action.");
        }

        var actor = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!actor.Success || actor.InstitutionId <= 0 || actor.LoggedInStaffMemberId <= 0 || actor.RoleId <= 0)
        {
            throw new UnauthorizedAccessException(actor.ErrorMessage ?? "Unauthorized request.");
        }

        var permission = await unitOfWork.StoreProcedureRepository.CheckInstitutionStaffMemberPermissionAsync(
            actor.InstitutionId,
            actor.LoggedInStaffMemberId,
            actor.RoleId,
            dto.ModuleId,
            dto.OperationId,
            cancellationToken);

        InstitutionRuntimePermissionValidator.EnsureAllowed(permission);
        return await next();
    }
}

#endregion

#region Handlers

/// <summary>Returns the token-authenticated, non-persistent staffMember-calendar display palette.</summary>
public sealed class GetHolidayCalendarDisplayConstantsQueryHandler(
    ICommonRequestService commonRequestService)
    : IRequestHandler<GetHolidayCalendarDisplayConstantsQuery, ApiResponse<HolidayCalendarDisplayConstantsDTO>>
{
    public async Task<ApiResponse<HolidayCalendarDisplayConstantsDTO>> Handle(
        GetHolidayCalendarDisplayConstantsQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!actor.Success || actor.InstitutionId <= 0 || actor.LoggedInStaffMemberId <= 0)
        {
            throw new UnauthorizedAccessException(actor.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
        }

        var styles = new List<HolidayCalendarStyleDTO>
        {
            new(
                AppConstants.CalendarStatusCodes.MandatoryHoliday,
                "Mandatory holiday",
                AppConstants.CalendarColors.MandatoryHoliday,
                AppConstants.CalendarColors.LightText,
                900),
            new(
                AppConstants.CalendarStatusCodes.OnLeave,
                "On leave",
                AppConstants.CalendarColors.OnLeave,
                AppConstants.CalendarColors.LightText,
                800),
            new(
                AppConstants.CalendarStatusCodes.Taken,
                "Taken",
                AppConstants.CalendarColors.Taken,
                AppConstants.CalendarColors.LightText,
                700),
            new(
                AppConstants.CalendarStatusCodes.Approved,
                "Approved",
                AppConstants.CalendarColors.Approved,
                AppConstants.CalendarColors.LightText,
                600),
            new(
                AppConstants.CalendarStatusCodes.Pending,
                "Pending",
                AppConstants.CalendarColors.Pending,
                AppConstants.CalendarColors.DarkText,
                500),
            new(
                AppConstants.CalendarStatusCodes.OptionalHoliday,
                "Optional holiday",
                AppConstants.CalendarColors.OptionalHoliday,
                AppConstants.CalendarColors.DarkText,
                400),
            new(
                AppConstants.CalendarStatusCodes.Rejected,
                "Rejected",
                AppConstants.CalendarColors.Rejected,
                AppConstants.CalendarColors.LightText,
                300),
            new(
                AppConstants.CalendarStatusCodes.Cancelled,
                "Cancelled",
                AppConstants.CalendarColors.Cancelled,
                AppConstants.CalendarColors.DarkText,
                200),
            new(
                AppConstants.CalendarStatusCodes.WeeklyOff,
                "Weekly off",
                AppConstants.CalendarColors.WeeklyOff,
                AppConstants.CalendarColors.DarkText,
                100)
        };

        return ApiResponse<HolidayCalendarDisplayConstantsDTO>.Success(
            new HolidayCalendarDisplayConstantsDTO(styles),
            "Holiday calendar display constants retrieved successfully.");
    }
}

internal static class HolidayValidation
{
    public static string DuplicateMessage(Holiday existing)
    {
        var status = existing.IsActive == true ? "active" : "inactive";
        return $"A holiday entry already exists for this location and date (Id {existing.Id}, {status}). Edit that entry or soft-delete it before creating another.";
    }

    public static void Validate(long campusId, string name, DateOnly date, string? description, string? icon)
    {
        if (campusId <= 0)
        {
            throw new ValidationErrorException("CampusId is required.");
        }

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
        {
            throw new ValidationErrorException("HolidayName must contain 1 to 100 characters.");
        }

        if (date == default)
        {
            throw new ValidationErrorException("HolidayDate is required.");
        }

        if (description?.Length > 255)
        {
            throw new ValidationErrorException("Description must not exceed 255 characters.");
        }

        if (icon?.Length > 100)
        {
            throw new ValidationErrorException("Icon must not exceed 100 characters.");
        }
    }

    public static HolidayDTO ToDTO(Holiday holiday)
    {
        return new HolidayDTO
        {
            Id = holiday.Id,
            InstitutionId = holiday.InstitutionId ?? 0,
            CampusId = holiday.CampusId,
            HolidayName = holiday.HolidayName,
            HolidayDate = holiday.HolidayDate,
            IsOptional = holiday.IsOptional,
            IsActive = holiday.IsActive == true,
            Description = holiday.Description,
            Icon = holiday.Icon
        };
    }
}

public sealed class ListHolidaysQueryHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService)
    : IRequestHandler<ListHolidaysQuery, ApiResponse<IReadOnlyList<HolidayDTO>>>
{
    public async Task<ApiResponse<IReadOnlyList<HolidayDTO>>> Handle(
        ListHolidaysQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (request.DTO.CampusId <= 0 || request.DTO.HolidayYear is < 1 or > 9999)
        {
            throw new ValidationErrorException("CampusId and HolidayYear must be valid when supplied.");
        }

        var holidays = await unitOfWork.HolidayRepository.GetInstitutionHolidaysAsync(
            actor.InstitutionId,
            request.DTO.CampusId,
            request.DTO.HolidayYear,
            cancellationToken);

        return ApiResponse<IReadOnlyList<HolidayDTO>>.Success(
            holidays.Select(HolidayValidation.ToDTO).ToList(),
            "Holidays retrieved successfully.");
    }
}

public sealed class GetHolidayQueryHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService)
    : IRequestHandler<GetHolidayQuery, ApiResponse<HolidayDTO>>
{
    public async Task<ApiResponse<HolidayDTO>> Handle(
        GetHolidayQuery request,
        CancellationToken cancellationToken)
    {
        var actor = await commonRequestService.ValidateInstitutionUserRequestAsync();
        var holiday = await unitOfWork.HolidayRepository.GetInstitutionHolidayForWriteAsync(
            actor.InstitutionId,
            request.DTO.Id,
            cancellationToken) ?? throw new NotFoundException("Holiday was not found.");

        return ApiResponse<HolidayDTO>.Success(
            HolidayValidation.ToDTO(holiday),
            "Holiday retrieved successfully.");
    }
}

public sealed class CreateHolidayCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService)
    : IRequestHandler<CreateHolidayCommand, ApiResponse<HolidayDTO>>
{
    public async Task<ApiResponse<HolidayDTO>> Handle(
        CreateHolidayCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await commonRequestService.ValidateInstitutionUserRequestAsync();
        var dto = request.DTO;
        HolidayValidation.Validate(dto.CampusId, dto.HolidayName, dto.HolidayDate, dto.Description, dto.Icon);
        if (!await unitOfWork.HolidayRepository.CampusExistsAsync(
            actor.InstitutionId, dto.CampusId, cancellationToken))
        {
            throw new ValidationErrorException("Institution location is not active or does not belong to this institution.");
        }

        var name = dto.HolidayName.Trim();
        var conflict = await unitOfWork.HolidayRepository.FindConflictingHolidayAsync(
            actor.InstitutionId, dto.CampusId, dto.HolidayDate, null, cancellationToken);
        if (conflict is not null)
        {
            throw new ValidationErrorException(HolidayValidation.DuplicateMessage(conflict));
        }

        var holiday = new Holiday
        {
            InstitutionId = actor.InstitutionId,
            CampusId = dto.CampusId,
            HolidayName = name,
            HolidayDate = dto.HolidayDate,
            IsOptional = dto.IsOptional,
            Description = dto.Description?.Trim(),
            Icon = dto.Icon?.Trim(),
            IsActive = true,
            IsSoftDeleted = false,
            AddedById = actor.LoggedInStaffMemberId,
            AddedDateTime = DateTime.UtcNow
        };

        await unitOfWork.HolidayRepository.SaveHolidayAsync(holiday, cancellationToken);
        return ApiResponse<HolidayDTO>.Success(
            HolidayValidation.ToDTO(holiday),
            "Holiday created successfully.");
    }
}

public sealed class UpdateHolidayCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService)
    : IRequestHandler<UpdateHolidayCommand, ApiResponse<HolidayDTO>>
{
    public async Task<ApiResponse<HolidayDTO>> Handle(
        UpdateHolidayCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await commonRequestService.ValidateInstitutionUserRequestAsync();
        var dto = request.DTO;
        HolidayValidation.Validate(dto.CampusId, dto.HolidayName, dto.HolidayDate, dto.Description, dto.Icon);
        var holiday = await unitOfWork.HolidayRepository.GetInstitutionHolidayForWriteAsync(
            actor.InstitutionId, dto.Id, cancellationToken) ?? throw new NotFoundException("Holiday was not found.");

        if (!await unitOfWork.HolidayRepository.CampusExistsAsync(
            actor.InstitutionId, dto.CampusId, cancellationToken))
        {
            throw new ValidationErrorException("Institution location is not active or does not belong to this institution.");
        }

        var name = dto.HolidayName.Trim();
        var conflict = await unitOfWork.HolidayRepository.FindConflictingHolidayAsync(
            actor.InstitutionId, dto.CampusId, dto.HolidayDate, dto.Id, cancellationToken);
        if (conflict is not null)
        {
            throw new ValidationErrorException(HolidayValidation.DuplicateMessage(conflict));
        }

        holiday.CampusId = dto.CampusId;
        holiday.HolidayName = name;
        holiday.HolidayDate = dto.HolidayDate;
        holiday.IsOptional = dto.IsOptional;
        holiday.Description = dto.Description?.Trim();
        holiday.Icon = dto.Icon?.Trim();
        holiday.UpdatedById = actor.LoggedInStaffMemberId;
        holiday.UpdatedDateTime = DateTime.UtcNow;

        await unitOfWork.HolidayRepository.SaveHolidayAsync(holiday, cancellationToken);
        return ApiResponse<HolidayDTO>.Success(
            HolidayValidation.ToDTO(holiday),
            "Holiday updated successfully.");
    }
}

public sealed class DeleteHolidayCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService)
    : IRequestHandler<DeleteHolidayCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        DeleteHolidayCommand request,
        CancellationToken cancellationToken)
    {
        var actor = await commonRequestService.ValidateInstitutionUserRequestAsync();
        var holiday = await unitOfWork.HolidayRepository.GetInstitutionHolidayForWriteAsync(
            actor.InstitutionId,
            request.DTO.Id,
            cancellationToken) ?? throw new NotFoundException("Holiday was not found.");

        holiday.IsActive = false;
        holiday.IsSoftDeleted = true;
        holiday.SoftDeletedById = actor.LoggedInStaffMemberId;
        holiday.DeletedDateTime = DateTime.UtcNow;
        await unitOfWork.HolidayRepository.SaveHolidayAsync(holiday, cancellationToken);

        return ApiResponse<bool>.Success(true, "Holiday deleted successfully.");
    }
}

#endregion
