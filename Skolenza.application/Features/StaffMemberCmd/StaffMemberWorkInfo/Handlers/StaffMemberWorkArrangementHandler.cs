// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Validates and manages Institution staffMember work arrangements and lifecycle dependencies.
// ================================================================

using AutoMapper;
using skolenza.application.Constants;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.InstitutionConfiguration;
using skolenza.application.Exceptions;
using skolenza.application.Features.InstitutionConfigurationCmd.Handlers;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace skolenza.application.Features.StaffMemberCmd.StaffMemberWorkInfo.Handlers;

#region Command
/// <summary>Creates an staffMember work arrangement.</summary>
public sealed class CreateStaffMemberWorkArrangementCommand(CreateStaffMemberWorkArrangementRequestDTO dto) : IRequest<ApiResponse<StaffMemberWorkArrangementResponseDTO>>
{
    /// <summary>Gets arrangement values.</summary>
    public CreateStaffMemberWorkArrangementRequestDTO DTO { get; } = dto;
}
/// <summary>Updates an staffMember work arrangement.</summary>
public sealed class UpdateStaffMemberWorkArrangementCommand(UpdateStaffMemberWorkArrangementRequestDTO dto) : IRequest<ApiResponse<StaffMemberWorkArrangementResponseDTO>>
{
    /// <summary>Gets arrangement values.</summary>
    public UpdateStaffMemberWorkArrangementRequestDTO DTO { get; } = dto;
}
/// <summary>Soft deletes an staffMember work arrangement.</summary>
public sealed class DeleteStaffMemberWorkArrangementCommand(long id, PermissionRequestDTO permissionRequest) : IRequest<ApiResponse<bool>>
{
    /// <summary>Gets identifier.</summary>
    public long Id { get; } = id;
    /// <summary>Gets the module and operation required for institution-role authorization.</summary>
    public PermissionRequestDTO PermissionRequest { get; } = permissionRequest;
}
/// <summary>Changes staffMember work arrangement active state.</summary>
public sealed class UpdateStaffMemberWorkArrangementStatusCommand(UpdateStaffMemberWorkArrangementStatusRequestDTO dto) : IRequest<ApiResponse<StaffMemberWorkArrangementResponseDTO>>
{
    /// <summary>Gets status values.</summary>
    public UpdateStaffMemberWorkArrangementStatusRequestDTO DTO { get; } = dto;
}
#endregion
#region Query
/// <summary>Retrieves one staffMember work arrangement.</summary>
public sealed class GetStaffMemberWorkArrangementByIdQuery(long id, PermissionRequestDTO permissionRequest) : IRequest<ApiResponse<StaffMemberWorkArrangementResponseDTO>>
{
    /// <summary>Gets identifier.</summary>
    public long Id { get; } = id;
    /// <summary>Gets the module and operation required for institution-role authorization.</summary>
    public PermissionRequestDTO PermissionRequest { get; } = permissionRequest;
}
/// <summary>Retrieves filtered staffMember work arrangements.</summary>
public sealed class GetStaffMemberWorkArrangementsQuery(StaffMemberWorkArrangementFilterRequestDTO filter) : IRequest<ApiResponse<List<StaffMemberWorkArrangementResponseDTO>>>
{
    /// <summary>Gets filters.</summary>
    public StaffMemberWorkArrangementFilterRequestDTO Filter { get; } = filter;
}
/// <summary>Retrieves effective Published Attendance policy versions for dropdown selection.</summary>
public sealed class GetAttendancePolicyOptionsQuery(AttendancePolicyOptionRequestDTO request) : IRequest<ApiResponse<List<AttendancePolicyOptionResponseDTO>>>
{
    /// <summary>Gets the effective-date and text-search values.</summary>
    public AttendancePolicyOptionRequestDTO Request { get; } = request;
}
#endregion

/// <summary>Maps a work arrangement while keeping the public staffMember identifier encoded.</summary>
internal static class StaffMemberWorkArrangementResponseMapper
{
    public static StaffMemberWorkArrangementResponseDTO Map(
        IMapper mapper,
        StaffMemberWorkArrangement entity,
        Func<long, string> encodeStaffMemberId)
    {
        var response = mapper.Map<StaffMemberWorkArrangementResponseDTO>(entity);
        response.StaffMemberId = encodeStaffMemberId(entity.StaffMemberId);
        return response;
    }
}

/// <summary>Explains each staffMember, location, assignment-state, and date failure separately.</summary>
internal static class StaffMemberWorkArrangementReferenceValidator
{
    public static async Task ValidateStaffMemberAsync(
        IStaffMemberWorkArrangementRepository repository,
        long institutionId,
        long staffMemberId,
        CancellationToken cancellationToken)
    {
        var staffMember = await repository.GetStaffMemberForValidationAsync(institutionId, staffMemberId, cancellationToken);
        if (staffMember is null)
        {
            // Soft-deleted staffMembers are intentionally excluded by the repository and do not count.
            throw new ValidationErrorException(AppConstants.ErrorMessages.WorkArrangementStaffMemberNotFound);
        }

        if (!staffMember.IsActive)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.WorkArrangementStaffMemberInactive);
        }
    }

    public static async Task ValidateNoDateOverlapAsync(
        IStaffMemberWorkArrangementRepository repository,
        long institutionId,
        long staffMemberId,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        long? excludeId,
        CancellationToken cancellationToken)
    {
        var overlappingArrangement = await repository.GetOverlappingArrangementAsync(
            institutionId,
            staffMemberId,
            effectiveFrom,
            effectiveTo,
            excludeId,
            cancellationToken);
        if (overlappingArrangement is null)
        {
            return;
        }

        var message = overlappingArrangement.EffectiveTo.HasValue
            ? string.Format(
                CultureInfo.InvariantCulture,
                AppConstants.ErrorMessages.StaffMemberWorkArrangementDateOverlap,
                FormatDate(effectiveFrom),
                FormatDate(effectiveTo),
                FormatDate(overlappingArrangement.EffectiveFrom),
                FormatDate(overlappingArrangement.EffectiveTo))
            : string.Format(
                CultureInfo.InvariantCulture,
                AppConstants.ErrorMessages.StaffMemberWorkArrangementOpenEndedDateOverlap,
                FormatDate(effectiveFrom),
                FormatDate(effectiveTo),
                FormatDate(overlappingArrangement.EffectiveFrom));

        throw new ConflictException(message);
    }

    public static async Task ValidatePrimaryLocationAsync(
        IStaffMemberWorkArrangementRepository repository,
        long institutionId,
        long staffMemberId,
        CreateStaffMemberWorkArrangementRequestDTO dto,
        CancellationToken cancellationToken)
    {
        if (StaffMemberWorkConfigurationRules.DisallowsLocation(dto.WorkMode)
            && dto.PrimaryCampusId.HasValue)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.WorkFromHomePrimaryLocationNotAllowed);
        }

        if (StaffMemberWorkConfigurationRules.RequiresPrimaryLocation(dto.WorkMode)
            && !dto.PrimaryCampusId.HasValue)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.WorkArrangementPrimaryLocationRequired);
        }

        if (!dto.PrimaryCampusId.HasValue)
        {
            return;
        }

        var locationId = dto.PrimaryCampusId.Value;
        var location = await repository.GetLocationForValidationAsync(institutionId, locationId, cancellationToken);
        if (location is null)
        {
            // Soft-deleted locations are intentionally excluded by the repository and do not count.
            throw new ValidationErrorException(AppConstants.ErrorMessages.WorkArrangementLocationNotFound);
        }

        if (!location.IsActive)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.WorkArrangementLocationInactive);
        }

        var locationType = (CampusType)location.LocationType;
        if (!StaffMemberWorkConfigurationRules.IsLocationTypeCompatible(dto.WorkMode, locationType))
        {
            throw new ValidationErrorException(string.Format(
                CultureInfo.InvariantCulture,
                AppConstants.ErrorMessages.WorkArrangementLocationTypeDetails,
                locationType,
                dto.WorkMode));
        }

        if (await repository.HasCoveringPrimaryLocationAssignmentAsync(
            institutionId,
            staffMemberId,
            locationId,
            dto.EffectiveFrom,
            dto.EffectiveTo,
            cancellationToken))
        {
            return;
        }

        var assignments = await repository.GetLocationAssignmentsForValidationAsync(
            institutionId,
            staffMemberId,
            locationId,
            cancellationToken);
        if (assignments.Count == 0)
        {
            // Soft-deleted assignments are intentionally excluded and therefore behave as missing.
            throw new ValidationErrorException(AppConstants.ErrorMessages.WorkArrangementStaffMemberLocationMissing);
        }

        var activeAssignments = assignments.Where(x => x.IsActive).ToList();
        if (activeAssignments.Count == 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.WorkArrangementStaffMemberLocationInactive);
        }

        var primaryAssignments = activeAssignments.Where(x => x.IsPrimary).ToList();
        if (primaryAssignments.Count == 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.WorkArrangementStaffMemberLocationNotPrimary);
        }

        var attendanceAssignments = primaryAssignments.Where(x => x.IsAttendanceAllowed).ToList();
        if (attendanceAssignments.Count == 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.WorkArrangementStaffMemberLocationAttendanceDisabled);
        }

        var assignmentsStartedByArrangement = attendanceAssignments
            .Where(x => x.EffectiveFrom <= dto.EffectiveFrom)
            .ToList();
        if (assignmentsStartedByArrangement.Count == 0)
        {
            var assignmentStartingLate = attendanceAssignments.OrderBy(x => x.EffectiveFrom).First();
            throw new ValidationErrorException(string.Format(
                CultureInfo.InvariantCulture,
                AppConstants.ErrorMessages.WorkArrangementStaffMemberLocationStartsLate,
                FormatDate(assignmentStartingLate.EffectiveFrom),
                FormatDate(dto.EffectiveFrom)));
        }

        var assignment = assignmentsStartedByArrangement
            .OrderByDescending(x => !x.EffectiveTo.HasValue)
            .ThenByDescending(x => x.EffectiveTo)
            .First();
        if (!dto.EffectiveTo.HasValue && assignment.EffectiveTo.HasValue)
        {
            throw new ValidationErrorException(string.Format(
                CultureInfo.InvariantCulture,
                AppConstants.ErrorMessages.WorkArrangementStaffMemberLocationMustBeOpenEnded,
                FormatDate(assignment.EffectiveTo.Value)));
        }

        if (dto.EffectiveTo.HasValue
            && assignment.EffectiveTo.HasValue
            && assignment.EffectiveTo.Value < dto.EffectiveTo.Value)
        {
            throw new ValidationErrorException(string.Format(
                CultureInfo.InvariantCulture,
                AppConstants.ErrorMessages.WorkArrangementStaffMemberLocationEndsEarly,
                FormatDate(assignment.EffectiveTo.Value),
                FormatDate(dto.EffectiveTo.Value)));
        }

        throw new ValidationErrorException(AppConstants.ErrorMessages.WorkArrangementPrimaryLocationAssignmentRequired);
    }

    private static string FormatDate(DateOnly date) => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    private static string FormatDate(DateOnly? date) => date.HasValue
        ? FormatDate(date.Value)
        : "No end date";
}

#region Handler
/// <summary>Handles staffMember work arrangement creation.</summary>
public sealed class CreateStaffMemberWorkArrangementCommandHandler : InstitutionConfigurationHandlerBase, IRequestHandler<CreateStaffMemberWorkArrangementCommand, ApiResponse<StaffMemberWorkArrangementResponseDTO>>
{
    #region Fields
    private readonly IMapper _mapper;
    #endregion
    #region Constructor
    /// <summary>Initializes handler.</summary>
    public CreateStaffMemberWorkArrangementCommandHandler(IUnitOfWork u, IMapper m, ICommonRequestService c, ILogger<InstitutionConfigurationHandlerBase> l, IIdEncoderService idEncoderService) : base(u, c, l, idEncoderService) => _mapper = m;
    #endregion
    #region Handle
    /// <inheritdoc />
    public async Task<ApiResponse<StaffMemberWorkArrangementResponseDTO>> Handle(CreateStaffMemberWorkArrangementCommand request, CancellationToken ct)
    {
        if (request.DTO is null) throw new ValidationErrorException(AppConstants.ErrorMessages.RequiredDataMissing);
        var (institutionId, actorId, staffMemberId) = await ValidateInstitutionAndDecodeStaffMemberIdAsync(request.DTO.StaffMemberId); Validate(request.DTO); await ValidateRefs(institutionId, staffMemberId, request.DTO, null, ct);
        var e = _mapper.Map<StaffMemberWorkArrangement>(request.DTO); e.StaffMemberId = staffMemberId; e.InstitutionId = institutionId; e.IsSoftDeleted = false; e.AddedById = actorId; e.AddedDateTime = DateTime.UtcNow; await UnitOfWork.StaffMemberWorkArrangementRepository.AddAsync(e, ct); await UnitOfWork.SaveChangesAsync(ct);
        Logger.LogInformation("Work arrangement {WorkArrangementId} created for StaffMember {StaffMemberId} and Institution {InstitutionId}.", e.Id, e.StaffMemberId, institutionId);
        var stored = (await UnitOfWork.StaffMemberWorkArrangementRepository.GetByIdAsync(institutionId, e.Id, ct))!;
        var validation = await ValidateInstitutionDataAccessContextAsync();
        var response = StaffMemberWorkArrangementResponseMapper.Map(
            _mapper,
            stored,
            id => EncodeStaffMemberId(id, validation));
        return ApiResponse<StaffMemberWorkArrangementResponseDTO>.Success(
            response,
            AppConstants.SuccessMessages.StaffMemberWorkArrangementCreated);
    }
    #endregion
    private async Task ValidateRefs(long institutionId, long staffMemberId, CreateStaffMemberWorkArrangementRequestDTO dto, long? excludeId, CancellationToken ct)
    {
        await StaffMemberWorkArrangementReferenceValidator.ValidateStaffMemberAsync(
            UnitOfWork.StaffMemberWorkArrangementRepository,
            institutionId,
            staffMemberId,
            ct);

        if (!await UnitOfWork.StaffMemberWorkArrangementRepository.IsEligibleAttendancePolicyVersionAsync(institutionId, dto.PolicyVersionId, dto.EffectiveFrom, ct))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidAttendancePolicyVersion);
        }

        var attendanceConfiguration = await UnitOfWork.StaffMemberWorkArrangementRepository
            .GetAttendanceConfigurationAsync(institutionId, dto.PolicyVersionId, ct)
            ?? throw new ValidationErrorException(AppConstants.ErrorMessages.AttendancePolicyConfigurationRequired);
        if (!StaffMemberWorkConfigurationRules.IsAllowedByAttendancePolicy(dto.WorkMode, dto.PrimaryCampusId, attendanceConfiguration))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.AttendancePolicyLocationScopeMismatch);
        }

        await ValidatePrimaryLocationAsync(institutionId, staffMemberId, dto, ct);

        if (dto.IsActive)
        {
            await StaffMemberWorkArrangementReferenceValidator.ValidateNoDateOverlapAsync(
                UnitOfWork.StaffMemberWorkArrangementRepository,
                institutionId,
                staffMemberId,
                dto.EffectiveFrom,
                dto.EffectiveTo,
                excludeId,
                ct);
        }
    }

    private async Task ValidatePrimaryLocationAsync(
        long institutionId,
        long staffMemberId,
        CreateStaffMemberWorkArrangementRequestDTO dto,
        CancellationToken cancellationToken)
    {
        await StaffMemberWorkArrangementReferenceValidator.ValidatePrimaryLocationAsync(
            UnitOfWork.StaffMemberWorkArrangementRepository,
            institutionId,
            staffMemberId,
            dto,
            cancellationToken);
    }

    private static void Validate(CreateStaffMemberWorkArrangementRequestDTO dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.StaffMemberId))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.RequiredDataMissing);
        }

        if (dto.PolicyVersionId <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidAttendancePolicyVersion);
        }

        if (dto.EffectiveFrom == default || (dto.EffectiveTo.HasValue && dto.EffectiveTo < dto.EffectiveFrom))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidEffectiveDateRange);
        }

        if (!Enum.IsDefined(dto.WorkMode))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        if ((dto.HybridType.HasValue && !Enum.IsDefined(dto.HybridType.Value))
            || (dto.WorkMode == WorkMode.Hybrid && !dto.HybridType.HasValue)
            || (dto.WorkMode != WorkMode.Hybrid && dto.HybridType.HasValue))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidHybridConfiguration);
        }

        if (!Within(dto.MinimumOfficeDaysPerWeek, 7)
            || !Within(dto.MinimumOfficeDaysPerMonth, 31)
            || !Within(dto.MaximumWFHDaysPerMonth, 31))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidWorkArrangementDayCounts);
        }
    }
    private static bool Within(short? value, short maximum) => !value.HasValue || (value.Value >= 0 && value.Value <= maximum);
}

/// <summary>Handles staffMember work arrangement updates.</summary>
public sealed class UpdateStaffMemberWorkArrangementCommandHandler : InstitutionConfigurationHandlerBase, IRequestHandler<UpdateStaffMemberWorkArrangementCommand, ApiResponse<StaffMemberWorkArrangementResponseDTO>>
{
    #region Fields
    private readonly IMapper _mapper;
    #endregion
    #region Constructor
    /// <summary>Initializes handler.</summary>
    public UpdateStaffMemberWorkArrangementCommandHandler(IUnitOfWork u, IMapper m, ICommonRequestService c, ILogger<InstitutionConfigurationHandlerBase> l, IIdEncoderService idEncoderService) : base(u, c, l, idEncoderService) => _mapper = m;
    #endregion
    #region Handle
    /// <inheritdoc />
    public async Task<ApiResponse<StaffMemberWorkArrangementResponseDTO>> Handle(UpdateStaffMemberWorkArrangementCommand request, CancellationToken ct)
    {
        if (request.DTO is null || request.DTO.Id <= 0) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        var (institutionId, actorId, staffMemberId) = await ValidateInstitutionAndDecodeStaffMemberIdAsync(request.DTO.StaffMemberId); Validate(request.DTO);
        var e = await UnitOfWork.StaffMemberWorkArrangementRepository.GetForUpdateAsync(institutionId, request.DTO.Id, ct) ?? throw new NotFoundException(AppConstants.ErrorMessages.StaffMemberWorkArrangementNotFound);
        if (e.StaffMemberId != staffMemberId) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        if (!request.DTO.IsActive && e.IsActive && await UnitOfWork.StaffMemberWorkArrangementRepository.HasLiveActiveDependenciesAsync(institutionId, e.Id, ct)) throw new ConflictException(AppConstants.ErrorMessages.StaffMemberWorkArrangementInUse);
        await ValidateRefs(institutionId, staffMemberId, request.DTO, e.Id, ct);
        _mapper.Map(request.DTO, e); e.StaffMemberId = staffMemberId; e.UpdatedById = actorId; e.UpdatedDateTime = DateTime.UtcNow; await UnitOfWork.SaveChangesAsync(ct);
        var stored = (await UnitOfWork.StaffMemberWorkArrangementRepository.GetByIdAsync(institutionId, e.Id, ct))!;
        var validation = await ValidateInstitutionDataAccessContextAsync();
        var response = StaffMemberWorkArrangementResponseMapper.Map(
            _mapper,
            stored,
            id => EncodeStaffMemberId(id, validation));
        return ApiResponse<StaffMemberWorkArrangementResponseDTO>.Success(
            response,
            AppConstants.SuccessMessages.StaffMemberWorkArrangementUpdated);
    }
    #endregion
    private async Task ValidateRefs(long institutionId, long staffMemberId, CreateStaffMemberWorkArrangementRequestDTO dto, long? excludeId, CancellationToken ct)
    {
        await StaffMemberWorkArrangementReferenceValidator.ValidateStaffMemberAsync(
            UnitOfWork.StaffMemberWorkArrangementRepository,
            institutionId,
            staffMemberId,
            ct);

        if (!await UnitOfWork.StaffMemberWorkArrangementRepository.IsEligibleAttendancePolicyVersionAsync(institutionId, dto.PolicyVersionId, dto.EffectiveFrom, ct))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidAttendancePolicyVersion);
        }

        var attendanceConfiguration = await UnitOfWork.StaffMemberWorkArrangementRepository
            .GetAttendanceConfigurationAsync(institutionId, dto.PolicyVersionId, ct)
            ?? throw new ValidationErrorException(AppConstants.ErrorMessages.AttendancePolicyConfigurationRequired);
        if (!StaffMemberWorkConfigurationRules.IsAllowedByAttendancePolicy(dto.WorkMode, dto.PrimaryCampusId, attendanceConfiguration))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.AttendancePolicyLocationScopeMismatch);
        }

        await ValidatePrimaryLocationAsync(institutionId, staffMemberId, dto, ct);

        if (dto.IsActive)
        {
            await StaffMemberWorkArrangementReferenceValidator.ValidateNoDateOverlapAsync(
                UnitOfWork.StaffMemberWorkArrangementRepository,
                institutionId,
                staffMemberId,
                dto.EffectiveFrom,
                dto.EffectiveTo,
                excludeId,
                ct);
        }
    }

    private async Task ValidatePrimaryLocationAsync(
        long institutionId,
        long staffMemberId,
        CreateStaffMemberWorkArrangementRequestDTO dto,
        CancellationToken cancellationToken)
    {
        await StaffMemberWorkArrangementReferenceValidator.ValidatePrimaryLocationAsync(
            UnitOfWork.StaffMemberWorkArrangementRepository,
            institutionId,
            staffMemberId,
            dto,
            cancellationToken);
    }

    private static void Validate(CreateStaffMemberWorkArrangementRequestDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.StaffMemberId))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.RequiredDataMissing);
        }

        if (dto.PolicyVersionId <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidAttendancePolicyVersion);
        }

        if (dto.EffectiveFrom == default || (dto.EffectiveTo.HasValue && dto.EffectiveTo < dto.EffectiveFrom))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidEffectiveDateRange);
        }

        if (!Enum.IsDefined(dto.WorkMode))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        if ((dto.HybridType.HasValue && !Enum.IsDefined(dto.HybridType.Value))
            || (dto.WorkMode == WorkMode.Hybrid && !dto.HybridType.HasValue)
            || (dto.WorkMode != WorkMode.Hybrid && dto.HybridType.HasValue))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidHybridConfiguration);
        }

        if (!Within(dto.MinimumOfficeDaysPerWeek, 7)
            || !Within(dto.MinimumOfficeDaysPerMonth, 31)
            || !Within(dto.MaximumWFHDaysPerMonth, 31))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidWorkArrangementDayCounts);
        }
    }
    private static bool Within(short? value, short maximum) => !value.HasValue || (value.Value >= 0 && value.Value <= maximum);
}

/// <summary>Handles safe staffMember work arrangement soft deletion.</summary>
public sealed class DeleteStaffMemberWorkArrangementCommandHandler : InstitutionConfigurationHandlerBase, IRequestHandler<DeleteStaffMemberWorkArrangementCommand, ApiResponse<bool>>
{
    /// <summary>Initializes handler.</summary>
    public DeleteStaffMemberWorkArrangementCommandHandler(IUnitOfWork u, ICommonRequestService c, ILogger<InstitutionConfigurationHandlerBase> l) : base(u, c, l) { }
    /// <inheritdoc />
    public async Task<ApiResponse<bool>> Handle(DeleteStaffMemberWorkArrangementCommand request, CancellationToken ct) { var validation = await ValidateInstitutionDataAccessContextAsync(); var institutionId = validation.InstitutionId; var e = await UnitOfWork.StaffMemberWorkArrangementRepository.GetForUpdateAsync(institutionId, request.Id, ct) ?? throw new NotFoundException(AppConstants.ErrorMessages.StaffMemberWorkArrangementNotFound); await EnsureStaffMemberDataAccessAsync(validation, e.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, ct); if (await UnitOfWork.StaffMemberWorkArrangementRepository.HasAnyDependenciesAsync(institutionId, e.Id, ct)) throw new ConflictException(AppConstants.ErrorMessages.StaffMemberWorkArrangementInUse); e.IsSoftDeleted = true; e.IsActive = false; e.SoftDeletedById = validation.LoggedInStaffMemberId; e.SoftDeletedDateTime = DateTime.UtcNow; await UnitOfWork.SaveChangesAsync(ct); return ApiResponse<bool>.Success(true, AppConstants.SuccessMessages.StaffMemberWorkArrangementDeleted); }
}

/// <summary>Handles staffMember work arrangement status changes.</summary>
public sealed class UpdateStaffMemberWorkArrangementStatusCommandHandler : InstitutionConfigurationHandlerBase, IRequestHandler<UpdateStaffMemberWorkArrangementStatusCommand, ApiResponse<StaffMemberWorkArrangementResponseDTO>>
{
    private readonly IMapper _mapper;
    /// <summary>Initializes handler.</summary>
    public UpdateStaffMemberWorkArrangementStatusCommandHandler(IUnitOfWork u, IMapper mapper, ICommonRequestService c, ILogger<InstitutionConfigurationHandlerBase> l, IIdEncoderService idEncoderService) : base(u, c, l, idEncoderService) => _mapper = mapper;
    /// <inheritdoc />
    public async Task<ApiResponse<StaffMemberWorkArrangementResponseDTO>> Handle(UpdateStaffMemberWorkArrangementStatusCommand request, CancellationToken ct)
    {
        var validation = await ValidateInstitutionDataAccessContextAsync();
        var institutionId = validation.InstitutionId;
        if (request.DTO is null || request.DTO.Id <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        var entity = await UnitOfWork.StaffMemberWorkArrangementRepository.GetForUpdateAsync(institutionId, request.DTO.Id, ct)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.StaffMemberWorkArrangementNotFound);
        await EnsureStaffMemberDataAccessAsync(validation, entity.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, ct);

        if (!request.DTO.IsActive && entity.IsActive && await UnitOfWork.StaffMemberWorkArrangementRepository.HasLiveActiveDependenciesAsync(institutionId, entity.Id, ct))
        {
            throw new ConflictException(AppConstants.ErrorMessages.StaffMemberWorkArrangementInUse);
        }

        if (request.DTO.IsActive)
        {
            await StaffMemberWorkArrangementReferenceValidator.ValidateStaffMemberAsync(
                UnitOfWork.StaffMemberWorkArrangementRepository,
                institutionId,
                entity.StaffMemberId,
                ct);

            await StaffMemberWorkArrangementReferenceValidator.ValidateNoDateOverlapAsync(
                UnitOfWork.StaffMemberWorkArrangementRepository,
                institutionId,
                entity.StaffMemberId,
                entity.EffectiveFrom,
                entity.EffectiveTo,
                entity.Id,
                ct);

            if (!entity.PolicyVersionId.HasValue
                || !await UnitOfWork.StaffMemberWorkArrangementRepository.IsEligibleAttendancePolicyVersionAsync(institutionId, entity.PolicyVersionId.Value, entity.EffectiveFrom, ct))
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidAttendancePolicyVersion);
            }

            var workMode = (WorkMode)entity.WorkMode;
            var attendanceConfiguration = await UnitOfWork.StaffMemberWorkArrangementRepository
                .GetAttendanceConfigurationAsync(institutionId, entity.PolicyVersionId.Value, ct)
                ?? throw new ValidationErrorException(AppConstants.ErrorMessages.AttendancePolicyConfigurationRequired);
            if (!StaffMemberWorkConfigurationRules.IsAllowedByAttendancePolicy(workMode, entity.PrimaryCampusId, attendanceConfiguration))
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.AttendancePolicyLocationScopeMismatch);
            }

            await StaffMemberWorkArrangementReferenceValidator.ValidatePrimaryLocationAsync(
                UnitOfWork.StaffMemberWorkArrangementRepository,
                institutionId,
                entity.StaffMemberId,
                new CreateStaffMemberWorkArrangementRequestDTO
                {
                    WorkMode = workMode,
                    PrimaryCampusId = entity.PrimaryCampusId,
                    EffectiveFrom = entity.EffectiveFrom,
                    EffectiveTo = entity.EffectiveTo
                },
                ct);
        }

        entity.IsActive = request.DTO.IsActive;
        entity.UpdatedById = validation.LoggedInStaffMemberId;
        entity.UpdatedDateTime = DateTime.UtcNow;
        await UnitOfWork.SaveChangesAsync(ct);
        var stored = (await UnitOfWork.StaffMemberWorkArrangementRepository.GetByIdAsync(institutionId, entity.Id, ct))!;
        var response = StaffMemberWorkArrangementResponseMapper.Map(
            _mapper,
            stored,
            id => EncodeStaffMemberId(id, validation));
        return ApiResponse<StaffMemberWorkArrangementResponseDTO>.Success(
            response,
            AppConstants.SuccessMessages.StaffMemberWorkArrangementStatusUpdated);
    }
}

/// <summary>Handles staffMember work arrangement retrieval.</summary>
public sealed class GetStaffMemberWorkArrangementByIdQueryHandler : InstitutionConfigurationHandlerBase, IRequestHandler<GetStaffMemberWorkArrangementByIdQuery, ApiResponse<StaffMemberWorkArrangementResponseDTO>>
{
    private readonly IMapper _mapper;
    /// <summary>Initializes handler.</summary>
    public GetStaffMemberWorkArrangementByIdQueryHandler(IUnitOfWork u, IMapper mapper, ICommonRequestService c, ILogger<InstitutionConfigurationHandlerBase> l, IIdEncoderService idEncoderService) : base(u, c, l, idEncoderService) => _mapper = mapper;
    /// <inheritdoc />
    public async Task<ApiResponse<StaffMemberWorkArrangementResponseDTO>> Handle(GetStaffMemberWorkArrangementByIdQuery request, CancellationToken ct) { var validation = await ValidateInstitutionDataAccessContextAsync(); var e = await UnitOfWork.StaffMemberWorkArrangementRepository.GetByIdAsync(validation.InstitutionId, request.Id, ct) ?? throw new NotFoundException(AppConstants.ErrorMessages.StaffMemberWorkArrangementNotFound); await EnsureStaffMemberDataAccessAsync(validation, e.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, ct); return ApiResponse<StaffMemberWorkArrangementResponseDTO>.Success(StaffMemberWorkArrangementResponseMapper.Map(_mapper, e, id => EncodeStaffMemberId(id, validation))); }
}

/// <summary>Handles filtered staffMember work arrangement retrieval.</summary>
public sealed class GetStaffMemberWorkArrangementsQueryHandler : InstitutionConfigurationHandlerBase, IRequestHandler<GetStaffMemberWorkArrangementsQuery, ApiResponse<List<StaffMemberWorkArrangementResponseDTO>>>
{
    private readonly IMapper _mapper;
    /// <summary>Initializes handler.</summary>
    public GetStaffMemberWorkArrangementsQueryHandler(IUnitOfWork u, IMapper mapper, ICommonRequestService c, ILogger<InstitutionConfigurationHandlerBase> l, IIdEncoderService idEncoderService) : base(u, c, l, idEncoderService) => _mapper = mapper;
    /// <inheritdoc />
    public async Task<ApiResponse<List<StaffMemberWorkArrangementResponseDTO>>> Handle(GetStaffMemberWorkArrangementsQuery request, CancellationToken ct) { var filter = request.Filter ?? new StaffMemberWorkArrangementFilterRequestDTO(); var validation = await ValidateInstitutionDataAccessContextAsync(); long? staffMemberId = null; if (!string.IsNullOrWhiteSpace(filter.StaffMemberId)) { var context = await ValidateInstitutionAndDecodeOptionalStaffMemberIdAsync(filter.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, ct); staffMemberId = context.StaffMemberId; } filter.ResolvedStaffMemberId = staffMemberId; var p = await UnitOfWork.StaffMemberWorkArrangementRepository.GetPagedAsync(validation.InstitutionId, filter, validation.LoggedInStaffMemberId, validation.RoleTypeId, ct); return Paged(p.Data.Select(entity => StaffMemberWorkArrangementResponseMapper.Map(_mapper, entity, id => EncodeStaffMemberId(id, validation))).ToList(), p.PageNumber, p.PageSize, p.TotalCount, "StaffMember work arrangements retrieved successfully."); }
}

/// <summary>Handles the token-authenticated Attendance policy dropdown query.</summary>
public sealed class GetAttendancePolicyOptionsQueryHandler : InstitutionConfigurationHandlerBase, IRequestHandler<GetAttendancePolicyOptionsQuery, ApiResponse<List<AttendancePolicyOptionResponseDTO>>>
{
    /// <summary>Initializes the Attendance policy dropdown handler.</summary>
    public GetAttendancePolicyOptionsQueryHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService,
        ILogger<InstitutionConfigurationHandlerBase> logger)
        : base(unitOfWork, commonRequestService, logger)
    {
    }

    /// <inheritdoc />
    public async Task<ApiResponse<List<AttendancePolicyOptionResponseDTO>>> Handle(
        GetAttendancePolicyOptionsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Request is null)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        var (institutionId, _) = await ValidateInstitutionAsync();
        var effectiveOn = request.Request.EffectiveOn
            ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var options = await UnitOfWork.StaffMemberWorkArrangementRepository
            .GetAttendancePolicyOptionsAsync(
                institutionId,
                effectiveOn,
                request.Request.Search,
                cancellationToken);

        var message = options.Count == 0
            ? "No published attendance policy is available for the selected effective date. Please create and publish an Attendance policy first."
            : "Attendance policy options retrieved successfully.";

        return ApiResponse<List<AttendancePolicyOptionResponseDTO>>.Success(options, message);
    }
}
#endregion
