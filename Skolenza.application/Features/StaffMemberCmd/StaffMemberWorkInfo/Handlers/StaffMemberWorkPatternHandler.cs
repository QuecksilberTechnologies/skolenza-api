// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Validates and manages Institution staffMember work-pattern day configuration.
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
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.StaffMemberWorkInfo.Handlers;

#region Command

/// <summary>Creates an staffMember work-pattern day.</summary>
public sealed class CreateStaffMemberWorkPatternCommand(CreateStaffMemberWorkPatternRequestDTO dto) : IRequest<ApiResponse<StaffMemberWorkPatternResponseDTO>>
{
    /// <summary>Gets the client-editable work-pattern values.</summary>
    public CreateStaffMemberWorkPatternRequestDTO DTO { get; } = dto;
}

/// <summary>Updates an staffMember work-pattern day.</summary>
public sealed class UpdateStaffMemberWorkPatternCommand(UpdateStaffMemberWorkPatternRequestDTO dto) : IRequest<ApiResponse<StaffMemberWorkPatternResponseDTO>>
{
    /// <summary>Gets the client-editable work-pattern values.</summary>
    public UpdateStaffMemberWorkPatternRequestDTO DTO { get; } = dto;
}

/// <summary>Soft deletes an staffMember work-pattern day.</summary>
public sealed class DeleteStaffMemberWorkPatternCommand(long id, PermissionRequestDTO permissionRequest) : IRequest<ApiResponse<bool>>
{
    public long Id { get; } = id;
    /// <summary>Gets the module and operation required for institution-role authorization.</summary>
    public PermissionRequestDTO PermissionRequest { get; } = permissionRequest;
}

/// <summary>Changes an staffMember work-pattern active state.</summary>
public sealed class UpdateStaffMemberWorkPatternStatusCommand(UpdateStaffMemberWorkPatternStatusRequestDTO dto) : IRequest<ApiResponse<StaffMemberWorkPatternResponseDTO>> { public UpdateStaffMemberWorkPatternStatusRequestDTO DTO { get; } = dto; }

#endregion

#region Query

/// <summary>Retrieves one staffMember work-pattern day.</summary>
public sealed class GetStaffMemberWorkPatternByIdQuery(long id, PermissionRequestDTO permissionRequest) : IRequest<ApiResponse<StaffMemberWorkPatternResponseDTO>>
{
    public long Id { get; } = id;
    /// <summary>Gets the module and operation required for institution-role authorization.</summary>
    public PermissionRequestDTO PermissionRequest { get; } = permissionRequest;
}

/// <summary>Retrieves filtered staffMember work-pattern days.</summary>
public sealed class GetStaffMemberWorkPatternsQuery(StaffMemberWorkPatternFilterRequestDTO filter) : IRequest<ApiResponse<List<StaffMemberWorkPatternResponseDTO>>> { public StaffMemberWorkPatternFilterRequestDTO Filter { get; } = filter; }

#endregion

/// <summary>Applies the shared arrangement, policy, location, and weekday rules.</summary>
internal static class StaffMemberWorkPatternValidator
{
    public static async Task<StaffMemberWorkArrangement> ValidateAsync(
        IUnitOfWork unitOfWork,
        long institutionId,
        CreateStaffMemberWorkPatternRequestDTO dto,
        long? excludeId,
        CancellationToken cancellationToken)
    {
        var arrangement = await unitOfWork.StaffMemberWorkArrangementRepository.GetByIdAsync(
            institutionId,
            dto.StaffMemberWorkArrangementId,
            cancellationToken);
        if (arrangement is null || !arrangement.IsActive)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidInstitutionConfigurationReference);
        }

        await StaffMemberWorkArrangementReferenceValidator.ValidateStaffMemberAsync(
            unitOfWork.StaffMemberWorkArrangementRepository,
            institutionId,
            arrangement.StaffMemberId,
            cancellationToken);

        if (!dto.IsWorkingDay)
        {
            if (dto.CampusId.HasValue)
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.WorkPatternDayOffLocationNotAllowed);
            }
        }
        else
        {
            var arrangementWorkMode = (WorkMode)arrangement.WorkMode;
            if (!StaffMemberWorkConfigurationRules.SupportsWeekdayPattern(
                arrangementWorkMode,
                arrangement.HybridType))
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.FlexibleHybridPatternNotAllowed);
            }

            if (!StaffMemberWorkConfigurationRules.IsPatternModeAllowed(arrangementWorkMode, dto.WorkMode))
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.WorkPatternModeMustMatchArrangement);
            }

            if (StaffMemberWorkConfigurationRules.DisallowsLocation(dto.WorkMode)
                && dto.CampusId.HasValue)
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.WorkModeOverrideLocationNotAllowed);
            }

            if (StaffMemberWorkConfigurationRules.RequiresExecutionLocation(dto.WorkMode)
                && !dto.CampusId.HasValue)
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.WorkPatternLocationRequired);
            }

            if (dto.CampusId.HasValue)
            {
                var location = await unitOfWork.StaffMemberWorkArrangementRepository.GetLocationForValidationAsync(
                    institutionId,
                    dto.CampusId.Value,
                    cancellationToken);
                if (location is null || !location.IsActive
                    || !StaffMemberWorkConfigurationRules.IsLocationTypeCompatible(dto.WorkMode, (CampusType)location.LocationType))
                {
                    throw new ValidationErrorException(AppConstants.ErrorMessages.WorkArrangementLocationTypeMismatch);
                }

                var patternTo = arrangement.EffectiveTo ?? DateOnly.MaxValue;
                if (!await unitOfWork.StaffMemberWorkArrangementRepository.HasCoveringAttendanceLocationAssignmentAsync(
                    institutionId,
                    arrangement.StaffMemberId,
                    dto.CampusId.Value,
                    arrangement.EffectiveFrom,
                    patternTo,
                    cancellationToken))
                {
                    throw new ValidationErrorException(AppConstants.ErrorMessages.WorkPatternLocationAssignmentRequired);
                }
            }

            if (arrangement.PolicyVersionId is long policyVersionId)
            {
                var configuration = await unitOfWork.StaffMemberWorkArrangementRepository.GetAttendanceConfigurationAsync(institutionId, policyVersionId, cancellationToken)
                    ?? throw new ValidationErrorException(AppConstants.ErrorMessages.AttendancePolicyConfigurationRequired);
                if (!StaffMemberWorkConfigurationRules.IsAllowedByAttendancePolicy(dto.WorkMode, dto.CampusId, configuration))
                {
                    throw new ValidationErrorException(AppConstants.ErrorMessages.WorkPatternPolicyMismatch);
                }
            }
        }

        if (dto.IsActive && await unitOfWork.StaffMemberWorkPatternRepository.PatternDayExistsAsync(
            institutionId,
            dto.StaffMemberWorkArrangementId,
            (short)dto.DayOfWeek,
            excludeId,
            cancellationToken))
        {
            throw new ConflictException(AppConstants.ErrorMessages.DuplicateStaffMemberWorkPatternDay);
        }

        return arrangement;
    }
}

/// <summary>Maps a work pattern while preserving the public institution-salted staffMember identifier.</summary>
internal static class StaffMemberWorkPatternResponseMapper
{
    public static StaffMemberWorkPatternResponseDTO Map(
        IMapper mapper,
        StaffMemberWorkPattern entity,
        Func<long, string> encodeStaffMemberId)
    {
        var response = mapper.Map<StaffMemberWorkPatternResponseDTO>(entity);
        response.StaffMemberId = encodeStaffMemberId(entity.StaffMemberWorkArrangement.StaffMemberId);
        return response;
    }
}

#region Handler

/// <summary>Handles staffMember work-pattern creation.</summary>
public sealed class CreateStaffMemberWorkPatternCommandHandler : InstitutionConfigurationHandlerBase, IRequestHandler<CreateStaffMemberWorkPatternCommand, ApiResponse<StaffMemberWorkPatternResponseDTO>>
{
    private readonly IMapper _mapper;
    public CreateStaffMemberWorkPatternCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, ILogger<InstitutionConfigurationHandlerBase> logger, IIdEncoderService idEncoderService) : base(unitOfWork, commonRequestService, logger, idEncoderService) => _mapper = mapper;
    public async Task<ApiResponse<StaffMemberWorkPatternResponseDTO>> Handle(CreateStaffMemberWorkPatternCommand request, CancellationToken cancellationToken)
    {
        var validation = await ValidateInstitutionDataAccessContextAsync(); var institutionId = validation.InstitutionId; var actorId = validation.LoggedInStaffMemberId; Validate(request.DTO); var arrangement = await StaffMemberWorkPatternValidator.ValidateAsync(UnitOfWork, institutionId, request.DTO, null, cancellationToken);
        await EnsureStaffMemberDataAccessAsync(validation, arrangement.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, cancellationToken);
        var entity = _mapper.Map<StaffMemberWorkPattern>(request.DTO); entity.InstitutionId = institutionId; entity.IsSoftDeleted = false; entity.AddedById = actorId; entity.AddedDateTime = DateTime.UtcNow;
        await UnitOfWork.StaffMemberWorkPatternRepository.AddAsync(entity, cancellationToken); await UnitOfWork.SaveChangesAsync(cancellationToken);
        var stored = await UnitOfWork.StaffMemberWorkPatternRepository.GetByIdAsync(institutionId, entity.Id, cancellationToken);
        return ApiResponse<StaffMemberWorkPatternResponseDTO>.Success(StaffMemberWorkPatternResponseMapper.Map(_mapper, stored!, id => EncodeStaffMemberId(id, validation)), AppConstants.SuccessMessages.StaffMemberWorkPatternCreated);
    }
    private static void Validate(CreateStaffMemberWorkPatternRequestDTO dto)
    {
        if (dto is null || dto.StaffMemberWorkArrangementId <= 0 || !Enum.IsDefined(dto.DayOfWeek) || !Enum.IsDefined(dto.WorkMode) || (dto.IsWorkingDay && dto.WorkMode == WorkMode.Hybrid)) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
    }
}

/// <summary>Handles staffMember work-pattern updates.</summary>
public sealed class UpdateStaffMemberWorkPatternCommandHandler : InstitutionConfigurationHandlerBase, IRequestHandler<UpdateStaffMemberWorkPatternCommand, ApiResponse<StaffMemberWorkPatternResponseDTO>>
{
    private readonly IMapper _mapper;
    public UpdateStaffMemberWorkPatternCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, ILogger<InstitutionConfigurationHandlerBase> logger, IIdEncoderService idEncoderService) : base(unitOfWork, commonRequestService, logger, idEncoderService) => _mapper = mapper;
    public async Task<ApiResponse<StaffMemberWorkPatternResponseDTO>> Handle(UpdateStaffMemberWorkPatternCommand request, CancellationToken cancellationToken)
    {
        var validation = await ValidateInstitutionDataAccessContextAsync(); var institutionId = validation.InstitutionId; var actorId = validation.LoggedInStaffMemberId; if (request.DTO is null || request.DTO.Id <= 0) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier); Validate(request.DTO);
        var entity = await UnitOfWork.StaffMemberWorkPatternRepository.GetForUpdateAsync(institutionId, request.DTO.Id, cancellationToken) ?? throw new NotFoundException(AppConstants.ErrorMessages.StaffMemberWorkPatternNotFound);
        await EnsureStaffMemberDataAccessAsync(validation, entity.StaffMemberWorkArrangement.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, cancellationToken);
        var requestedArrangement = await StaffMemberWorkPatternValidator.ValidateAsync(UnitOfWork, institutionId, request.DTO, entity.Id, cancellationToken);
        await EnsureStaffMemberDataAccessAsync(validation, requestedArrangement.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, cancellationToken);
        _mapper.Map(request.DTO, entity); entity.UpdatedById = actorId; entity.UpdatedDateTime = DateTime.UtcNow; await UnitOfWork.SaveChangesAsync(cancellationToken);
        var stored = await UnitOfWork.StaffMemberWorkPatternRepository.GetByIdAsync(institutionId, entity.Id, cancellationToken);
        return ApiResponse<StaffMemberWorkPatternResponseDTO>.Success(StaffMemberWorkPatternResponseMapper.Map(_mapper, stored!, id => EncodeStaffMemberId(id, validation)), AppConstants.SuccessMessages.StaffMemberWorkPatternUpdated);
    }
    private static void Validate(CreateStaffMemberWorkPatternRequestDTO dto)
    {
        if (dto.StaffMemberWorkArrangementId <= 0 || !Enum.IsDefined(dto.DayOfWeek) || !Enum.IsDefined(dto.WorkMode) || (dto.IsWorkingDay && dto.WorkMode == WorkMode.Hybrid)) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
    }
}

/// <summary>Handles staffMember work-pattern soft deletion.</summary>
public sealed class DeleteStaffMemberWorkPatternCommandHandler : InstitutionConfigurationHandlerBase, IRequestHandler<DeleteStaffMemberWorkPatternCommand, ApiResponse<bool>>
{
    public DeleteStaffMemberWorkPatternCommandHandler(IUnitOfWork unitOfWork, ICommonRequestService commonRequestService, ILogger<InstitutionConfigurationHandlerBase> logger) : base(unitOfWork, commonRequestService, logger) { }
    public async Task<ApiResponse<bool>> Handle(DeleteStaffMemberWorkPatternCommand request, CancellationToken cancellationToken)
    { var validation = await ValidateInstitutionDataAccessContextAsync(); var entity = await UnitOfWork.StaffMemberWorkPatternRepository.GetForUpdateAsync(validation.InstitutionId, request.Id, cancellationToken) ?? throw new NotFoundException(AppConstants.ErrorMessages.StaffMemberWorkPatternNotFound); await EnsureStaffMemberDataAccessAsync(validation, entity.StaffMemberWorkArrangement.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, cancellationToken); entity.IsSoftDeleted = true; entity.IsActive = false; entity.SoftDeletedById = validation.LoggedInStaffMemberId; entity.SoftDeletedDateTime = DateTime.UtcNow; await UnitOfWork.SaveChangesAsync(cancellationToken); return ApiResponse<bool>.Success(true, AppConstants.SuccessMessages.StaffMemberWorkPatternDeleted); }
}

/// <summary>Handles staffMember work-pattern active-state changes.</summary>
public sealed class UpdateStaffMemberWorkPatternStatusCommandHandler : InstitutionConfigurationHandlerBase, IRequestHandler<UpdateStaffMemberWorkPatternStatusCommand, ApiResponse<StaffMemberWorkPatternResponseDTO>>
{
    private readonly IMapper _mapper;
    public UpdateStaffMemberWorkPatternStatusCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, ILogger<InstitutionConfigurationHandlerBase> logger, IIdEncoderService idEncoderService) : base(unitOfWork, commonRequestService, logger, idEncoderService) => _mapper = mapper;
    public async Task<ApiResponse<StaffMemberWorkPatternResponseDTO>> Handle(UpdateStaffMemberWorkPatternStatusCommand request, CancellationToken cancellationToken)
    { var validation = await ValidateInstitutionDataAccessContextAsync(); var institutionId = validation.InstitutionId; if (request.DTO is null || request.DTO.Id <= 0) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier); var entity = await UnitOfWork.StaffMemberWorkPatternRepository.GetForUpdateAsync(institutionId, request.DTO.Id, cancellationToken) ?? throw new NotFoundException(AppConstants.ErrorMessages.StaffMemberWorkPatternNotFound); await EnsureStaffMemberDataAccessAsync(validation, entity.StaffMemberWorkArrangement.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, cancellationToken); if (request.DTO.IsActive) await StaffMemberWorkPatternValidator.ValidateAsync(UnitOfWork, institutionId, new CreateStaffMemberWorkPatternRequestDTO { StaffMemberWorkArrangementId = entity.StaffMemberWorkArrangementId, DayOfWeek = (WorkPatternDay)entity.DayOfWeek, WorkMode = (WorkMode)entity.WorkMode, CampusId = entity.CampusId, IsWorkingDay = entity.IsWorkingDay, IsActive = true }, entity.Id, cancellationToken); entity.IsActive = request.DTO.IsActive; entity.UpdatedById = validation.LoggedInStaffMemberId; entity.UpdatedDateTime = DateTime.UtcNow; await UnitOfWork.SaveChangesAsync(cancellationToken); var stored = await UnitOfWork.StaffMemberWorkPatternRepository.GetByIdAsync(institutionId, entity.Id, cancellationToken); return ApiResponse<StaffMemberWorkPatternResponseDTO>.Success(StaffMemberWorkPatternResponseMapper.Map(_mapper, stored!, id => EncodeStaffMemberId(id, validation)), AppConstants.SuccessMessages.StaffMemberWorkPatternStatusUpdated); }
}

/// <summary>Handles staffMember work-pattern retrieval.</summary>
public sealed class GetStaffMemberWorkPatternByIdQueryHandler : InstitutionConfigurationHandlerBase, IRequestHandler<GetStaffMemberWorkPatternByIdQuery, ApiResponse<StaffMemberWorkPatternResponseDTO>>
{
    private readonly IMapper _mapper;
    public GetStaffMemberWorkPatternByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, ILogger<InstitutionConfigurationHandlerBase> logger, IIdEncoderService idEncoderService) : base(unitOfWork, commonRequestService, logger, idEncoderService) => _mapper = mapper;
    public async Task<ApiResponse<StaffMemberWorkPatternResponseDTO>> Handle(GetStaffMemberWorkPatternByIdQuery request, CancellationToken cancellationToken)
    { var validation = await ValidateInstitutionDataAccessContextAsync(); var entity = await UnitOfWork.StaffMemberWorkPatternRepository.GetByIdAsync(validation.InstitutionId, request.Id, cancellationToken) ?? throw new NotFoundException(AppConstants.ErrorMessages.StaffMemberWorkPatternNotFound); await EnsureStaffMemberDataAccessAsync(validation, entity.StaffMemberWorkArrangement.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, cancellationToken); return ApiResponse<StaffMemberWorkPatternResponseDTO>.Success(StaffMemberWorkPatternResponseMapper.Map(_mapper, entity, id => EncodeStaffMemberId(id, validation))); }
}

/// <summary>Handles paged staffMember work-pattern retrieval.</summary>
public sealed class GetStaffMemberWorkPatternsQueryHandler : InstitutionConfigurationHandlerBase, IRequestHandler<GetStaffMemberWorkPatternsQuery, ApiResponse<List<StaffMemberWorkPatternResponseDTO>>>
{
    private readonly IMapper _mapper;
    public GetStaffMemberWorkPatternsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, ILogger<InstitutionConfigurationHandlerBase> logger, IIdEncoderService idEncoderService) : base(unitOfWork, commonRequestService, logger, idEncoderService) => _mapper = mapper;
    public async Task<ApiResponse<List<StaffMemberWorkPatternResponseDTO>>> Handle(GetStaffMemberWorkPatternsQuery request, CancellationToken cancellationToken)
    { var validation = await ValidateInstitutionDataAccessContextAsync(); var page = await UnitOfWork.StaffMemberWorkPatternRepository.GetPagedAsync(validation.InstitutionId, request.Filter ?? new StaffMemberWorkPatternFilterRequestDTO(), validation.LoggedInStaffMemberId, validation.RoleTypeId, cancellationToken); return Paged(page.Data.Select(entity => StaffMemberWorkPatternResponseMapper.Map(_mapper, entity, id => EncodeStaffMemberId(id, validation))).ToList(), page.PageNumber, page.PageSize, page.TotalCount, "StaffMember work patterns retrieved successfully."); }
}

#endregion
