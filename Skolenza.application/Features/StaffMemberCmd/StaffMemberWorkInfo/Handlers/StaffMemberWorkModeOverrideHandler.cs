// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Validates and manages Institution temporary work-mode override configuration without approval workflow.
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
using System.Globalization;

namespace skolenza.application.Features.StaffMemberCmd.StaffMemberWorkInfo.Handlers;

#region Command

/// <summary>Creates a temporary staffMember work-mode override.</summary>
public sealed class CreateStaffMemberWorkModeOverrideCommand(CreateStaffMemberWorkModeOverrideRequestDTO dto) : IRequest<ApiResponse<StaffMemberWorkModeOverrideResponseDTO>> { public CreateStaffMemberWorkModeOverrideRequestDTO DTO { get; } = dto; }
/// <summary>Updates a temporary staffMember work-mode override without changing approval state.</summary>
public sealed class UpdateStaffMemberWorkModeOverrideCommand(UpdateStaffMemberWorkModeOverrideRequestDTO dto) : IRequest<ApiResponse<StaffMemberWorkModeOverrideResponseDTO>> { public UpdateStaffMemberWorkModeOverrideRequestDTO DTO { get; } = dto; }
/// <summary>Soft deletes temporary override configuration.</summary>
public sealed class DeleteStaffMemberWorkModeOverrideCommand(long id, PermissionRequestDTO permissionRequest) : IRequest<ApiResponse<bool>>
{
    public long Id { get; } = id;
    /// <summary>Gets the module and operation required for institution-role authorization.</summary>
    public PermissionRequestDTO PermissionRequest { get; } = permissionRequest;
}
/// <summary>Changes temporary override configuration state without changing approval state.</summary>
public sealed class UpdateStaffMemberWorkModeOverrideStatusCommand(UpdateStaffMemberWorkModeOverrideStatusRequestDTO dto) : IRequest<ApiResponse<StaffMemberWorkModeOverrideResponseDTO>> { public UpdateStaffMemberWorkModeOverrideStatusRequestDTO DTO { get; } = dto; }
/// <summary>Approves or rejects a pending temporary override.</summary>
public sealed class DecideStaffMemberWorkModeOverrideCommand(StaffMemberWorkModeOverrideDecisionRequestDTO dto, bool approve) : IRequest<ApiResponse<StaffMemberWorkModeOverrideResponseDTO>>
{
    public StaffMemberWorkModeOverrideDecisionRequestDTO DTO { get; } = dto;
    public bool Approve { get; } = approve;
}

#endregion

#region Query

/// <summary>Retrieves one temporary work-mode override.</summary>
public sealed class GetStaffMemberWorkModeOverrideByIdQuery(long id, PermissionRequestDTO permissionRequest) : IRequest<ApiResponse<StaffMemberWorkModeOverrideResponseDTO>>
{
    public long Id { get; } = id;
    /// <summary>Gets the module and operation required for institution-role authorization.</summary>
    public PermissionRequestDTO PermissionRequest { get; } = permissionRequest;
}
/// <summary>Retrieves filtered temporary work-mode overrides.</summary>
public sealed class GetStaffMemberWorkModeOverridesQuery(StaffMemberWorkModeOverrideFilterRequestDTO filter) : IRequest<ApiResponse<List<StaffMemberWorkModeOverrideResponseDTO>>> { public StaffMemberWorkModeOverrideFilterRequestDTO Filter { get; } = filter; }

#endregion

/// <summary>Applies the shared staffMember, arrangement, location, policy, and overlap rules.</summary>
internal static class StaffMemberWorkModeOverrideValidator
{
    public static async Task ValidateAsync(
        IUnitOfWork unitOfWork,
        long institutionId,
        long staffMemberId,
        CreateStaffMemberWorkModeOverrideRequestDTO dto,
        long? excludeId,
        CancellationToken cancellationToken)
    {
        if (!await unitOfWork.StaffMemberWorkModeOverrideRequestRepository.IsEligibleStaffMemberAsync(institutionId, staffMemberId, cancellationToken))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.WorkArrangementStaffMemberNotFound);
        }

        StaffMemberWorkArrangement? arrangement = null;
        if (dto.StaffMemberWorkArrangementId.HasValue)
        {
            arrangement = await unitOfWork.StaffMemberWorkArrangementRepository.GetByIdAsync(
                institutionId,
                dto.StaffMemberWorkArrangementId.Value,
                cancellationToken);
            if (arrangement is null || !arrangement.IsActive)
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidInstitutionConfigurationReference);
            }

            if (arrangement.StaffMemberId != staffMemberId)
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.WorkModeOverrideArrangementStaffMemberMismatch);
            }

            if (dto.FromDate < arrangement.EffectiveFrom
                || (arrangement.EffectiveTo.HasValue && dto.ToDate > arrangement.EffectiveTo.Value))
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.WorkModeOverrideArrangementDateMismatch);
            }
        }

        if (StaffMemberWorkConfigurationRules.DisallowsLocation(dto.RequestedWorkMode)
            && dto.CampusId.HasValue)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.WorkModeOverrideLocationNotAllowed);
        }

        if (StaffMemberWorkConfigurationRules.RequiresExecutionLocation(dto.RequestedWorkMode)
            && !dto.CampusId.HasValue)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.WorkModeOverrideLocationRequired);
        }

        if (dto.CampusId.HasValue)
        {
            var location = await unitOfWork.StaffMemberWorkArrangementRepository.GetLocationForValidationAsync(
                institutionId,
                dto.CampusId.Value,
                cancellationToken);
            if (location is null || !location.IsActive)
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.WorkArrangementLocationNotFound);
            }

            if (!StaffMemberWorkConfigurationRules.IsLocationTypeCompatible(dto.RequestedWorkMode, (CampusType)location.LocationType))
            {
                throw new ValidationErrorException(string.Format(
                    CultureInfo.InvariantCulture,
                    AppConstants.ErrorMessages.WorkArrangementLocationTypeDetails,
                    (CampusType)location.LocationType,
                    dto.RequestedWorkMode));
            }

            if (!await unitOfWork.StaffMemberWorkArrangementRepository.HasCoveringAttendanceLocationAssignmentAsync(
                institutionId,
                staffMemberId,
                dto.CampusId.Value,
                dto.FromDate,
                dto.ToDate,
                cancellationToken))
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.WorkModeOverrideLocationAssignmentRequired);
            }
        }

        if (arrangement?.PolicyVersionId is long policyVersionId)
        {
            var configuration = await unitOfWork.StaffMemberWorkArrangementRepository.GetAttendanceConfigurationAsync(
                institutionId,
                policyVersionId,
                cancellationToken)
                ?? throw new ValidationErrorException(AppConstants.ErrorMessages.AttendancePolicyConfigurationRequired);
            if (!StaffMemberWorkConfigurationRules.IsAllowedByAttendancePolicy(dto.RequestedWorkMode, dto.CampusId, configuration))
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.WorkModeOverridePolicyMismatch);
            }
        }

        if (dto.IsActive)
        {
            var conflict = await unitOfWork.StaffMemberWorkModeOverrideRequestRepository.GetOverlappingOverrideAsync(
                institutionId,
                staffMemberId,
                dto.FromDate,
                dto.ToDate,
                excludeId,
                cancellationToken);
            if (conflict is not null)
            {
                throw new ConflictException(string.Format(
                    CultureInfo.InvariantCulture,
                    AppConstants.ErrorMessages.WorkModeOverrideDateOverlap,
                    dto.FromDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    dto.ToDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    conflict.Id,
                    conflict.FromDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                    conflict.ToDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)));
            }
        }
    }
}

/// <summary>Maps an override while preserving the public institution-salted StaffMember identifier.</summary>
internal static class StaffMemberWorkModeOverrideResponseMapper
{
    public static StaffMemberWorkModeOverrideResponseDTO Map(IMapper mapper, StaffMemberWorkModeOverrideRequest entity, Func<long, string> encodeStaffMemberId)
    {
        var response = mapper.Map<StaffMemberWorkModeOverrideResponseDTO>(entity);
        response.StaffMemberId = encodeStaffMemberId(entity.StaffMemberId);
        return response;
    }
}

#region Handler

/// <summary>Handles temporary override creation.</summary>
public sealed class CreateStaffMemberWorkModeOverrideCommandHandler : InstitutionConfigurationHandlerBase, IRequestHandler<CreateStaffMemberWorkModeOverrideCommand, ApiResponse<StaffMemberWorkModeOverrideResponseDTO>>
{
    private readonly IMapper _mapper;
    public CreateStaffMemberWorkModeOverrideCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, ILogger<InstitutionConfigurationHandlerBase> logger, IIdEncoderService idEncoderService) : base(unitOfWork, commonRequestService, logger, idEncoderService) => _mapper = mapper;
    public async Task<ApiResponse<StaffMemberWorkModeOverrideResponseDTO>> Handle(CreateStaffMemberWorkModeOverrideCommand request, CancellationToken cancellationToken)
    {
        if (request.DTO is null) throw new ValidationErrorException(AppConstants.ErrorMessages.RequiredDataMissing);
        var (institutionId, actorId, staffMemberId) = await ValidateInstitutionAndDecodeStaffMemberIdAsync(request.DTO.StaffMemberId); Validate(request.DTO); await StaffMemberWorkModeOverrideValidator.ValidateAsync(UnitOfWork, institutionId, staffMemberId, request.DTO, null, cancellationToken);
        var entity = _mapper.Map<StaffMemberWorkModeOverrideRequest>(request.DTO); entity.StaffMemberId = staffMemberId; entity.InstitutionId = institutionId; entity.ApprovalStatus = (short)WorkModeOverrideApprovalStatus.Pending; entity.IsSoftDeleted = false; entity.AddedById = actorId; entity.AddedDateTime = DateTime.UtcNow;
        await UnitOfWork.StaffMemberWorkModeOverrideRequestRepository.AddAsync(entity, cancellationToken); await UnitOfWork.SaveChangesAsync(cancellationToken);
        var stored = await UnitOfWork.StaffMemberWorkModeOverrideRequestRepository.GetByIdAsync(institutionId, entity.Id, cancellationToken);
        var validation = await ValidateInstitutionDataAccessContextAsync();
        return ApiResponse<StaffMemberWorkModeOverrideResponseDTO>.Success(StaffMemberWorkModeOverrideResponseMapper.Map(_mapper, stored!, id => EncodeStaffMemberId(id, validation)), AppConstants.SuccessMessages.StaffMemberWorkModeOverrideCreated);
    }
    private static void Validate(CreateStaffMemberWorkModeOverrideRequestDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.StaffMemberId))
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        if (string.IsNullOrWhiteSpace(dto.Reason)
            || dto.Reason.Trim().Length > AppConstants.ValidationLimits.WorkModeOverrideTextMaxLength)
            throw new ValidationErrorException(AppConstants.ErrorMessages.WorkModeOverrideReasonRequired);
        if (dto.FromDate == default || dto.ToDate < dto.FromDate)
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidEffectiveDateRange);
        if (!StaffMemberWorkConfigurationRules.IsConcreteWorkMode(dto.RequestedWorkMode))
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidOverrideWorkMode);
    }
}

/// <summary>Handles temporary override updates without approval changes.</summary>
public sealed class UpdateStaffMemberWorkModeOverrideCommandHandler : InstitutionConfigurationHandlerBase, IRequestHandler<UpdateStaffMemberWorkModeOverrideCommand, ApiResponse<StaffMemberWorkModeOverrideResponseDTO>>
{
    private readonly IMapper _mapper;
    public UpdateStaffMemberWorkModeOverrideCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, ILogger<InstitutionConfigurationHandlerBase> logger, IIdEncoderService idEncoderService) : base(unitOfWork, commonRequestService, logger, idEncoderService) => _mapper = mapper;
    public async Task<ApiResponse<StaffMemberWorkModeOverrideResponseDTO>> Handle(UpdateStaffMemberWorkModeOverrideCommand request, CancellationToken cancellationToken)
    {
        if (request.DTO is null || request.DTO.Id <= 0) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        var (institutionId, actorId, staffMemberId) = await ValidateInstitutionAndDecodeStaffMemberIdAsync(request.DTO.StaffMemberId); Validate(request.DTO);
        var entity = await UnitOfWork.StaffMemberWorkModeOverrideRequestRepository.GetForUpdateAsync(institutionId, request.DTO.Id, cancellationToken) ?? throw new NotFoundException(AppConstants.ErrorMessages.StaffMemberWorkModeOverrideNotFound);
        if (entity.StaffMemberId != staffMemberId) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        if (entity.ApprovalStatus != (short)WorkModeOverrideApprovalStatus.Pending) throw new ConflictException(AppConstants.ErrorMessages.WorkModeOverrideDecisionFinal);
        await StaffMemberWorkModeOverrideValidator.ValidateAsync(UnitOfWork, institutionId, staffMemberId, request.DTO, entity.Id, cancellationToken); _mapper.Map(request.DTO, entity); entity.StaffMemberId = staffMemberId; entity.UpdatedById = actorId; entity.UpdatedDateTime = DateTime.UtcNow; await UnitOfWork.SaveChangesAsync(cancellationToken);
        var stored = await UnitOfWork.StaffMemberWorkModeOverrideRequestRepository.GetByIdAsync(institutionId, entity.Id, cancellationToken);
        var validation = await ValidateInstitutionDataAccessContextAsync();
        return ApiResponse<StaffMemberWorkModeOverrideResponseDTO>.Success(StaffMemberWorkModeOverrideResponseMapper.Map(_mapper, stored!, id => EncodeStaffMemberId(id, validation)), AppConstants.SuccessMessages.StaffMemberWorkModeOverrideUpdated);
    }
    private static void Validate(CreateStaffMemberWorkModeOverrideRequestDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.StaffMemberId))
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        if (string.IsNullOrWhiteSpace(dto.Reason)
            || dto.Reason.Trim().Length > AppConstants.ValidationLimits.WorkModeOverrideTextMaxLength)
            throw new ValidationErrorException(AppConstants.ErrorMessages.WorkModeOverrideReasonRequired);
        if (dto.FromDate == default || dto.ToDate < dto.FromDate)
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidEffectiveDateRange);
        if (!StaffMemberWorkConfigurationRules.IsConcreteWorkMode(dto.RequestedWorkMode))
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidOverrideWorkMode);
    }
}

/// <summary>Handles temporary override soft deletion.</summary>
public sealed class DeleteStaffMemberWorkModeOverrideCommandHandler : InstitutionConfigurationHandlerBase, IRequestHandler<DeleteStaffMemberWorkModeOverrideCommand, ApiResponse<bool>>
{
    public DeleteStaffMemberWorkModeOverrideCommandHandler(IUnitOfWork unitOfWork, ICommonRequestService commonRequestService, ILogger<InstitutionConfigurationHandlerBase> logger) : base(unitOfWork, commonRequestService, logger) { }
    public async Task<ApiResponse<bool>> Handle(DeleteStaffMemberWorkModeOverrideCommand request, CancellationToken cancellationToken)
    { var validation = await ValidateInstitutionDataAccessContextAsync(); var entity = await UnitOfWork.StaffMemberWorkModeOverrideRequestRepository.GetForUpdateAsync(validation.InstitutionId, request.Id, cancellationToken) ?? throw new NotFoundException(AppConstants.ErrorMessages.StaffMemberWorkModeOverrideNotFound); await EnsureStaffMemberDataAccessAsync(validation, entity.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, cancellationToken); entity.IsSoftDeleted = true; entity.IsActive = false; entity.SoftDeletedById = validation.LoggedInStaffMemberId; entity.SoftDeletedDateTime = DateTime.UtcNow; await UnitOfWork.SaveChangesAsync(cancellationToken); return ApiResponse<bool>.Success(true, AppConstants.SuccessMessages.StaffMemberWorkModeOverrideDeleted); }
}

/// <summary>Handles temporary override active-state changes without approval changes.</summary>
public sealed class UpdateStaffMemberWorkModeOverrideStatusCommandHandler : InstitutionConfigurationHandlerBase, IRequestHandler<UpdateStaffMemberWorkModeOverrideStatusCommand, ApiResponse<StaffMemberWorkModeOverrideResponseDTO>>
{
    private readonly IMapper _mapper;
    public UpdateStaffMemberWorkModeOverrideStatusCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, ILogger<InstitutionConfigurationHandlerBase> logger, IIdEncoderService idEncoderService) : base(unitOfWork, commonRequestService, logger, idEncoderService) => _mapper = mapper;
    public async Task<ApiResponse<StaffMemberWorkModeOverrideResponseDTO>> Handle(UpdateStaffMemberWorkModeOverrideStatusCommand request, CancellationToken cancellationToken)
    { var validation = await ValidateInstitutionDataAccessContextAsync(); var institutionId = validation.InstitutionId; if (request.DTO is null || request.DTO.Id <= 0) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier); var entity = await UnitOfWork.StaffMemberWorkModeOverrideRequestRepository.GetForUpdateAsync(institutionId, request.DTO.Id, cancellationToken) ?? throw new NotFoundException(AppConstants.ErrorMessages.StaffMemberWorkModeOverrideNotFound); await EnsureStaffMemberDataAccessAsync(validation, entity.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, cancellationToken); if (request.DTO.IsActive) await StaffMemberWorkModeOverrideValidator.ValidateAsync(UnitOfWork, institutionId, entity.StaffMemberId, ToValidationDto(entity, true), entity.Id, cancellationToken); entity.IsActive = request.DTO.IsActive; entity.UpdatedById = validation.LoggedInStaffMemberId; entity.UpdatedDateTime = DateTime.UtcNow; await UnitOfWork.SaveChangesAsync(cancellationToken); var stored = await UnitOfWork.StaffMemberWorkModeOverrideRequestRepository.GetByIdAsync(institutionId, entity.Id, cancellationToken); return ApiResponse<StaffMemberWorkModeOverrideResponseDTO>.Success(StaffMemberWorkModeOverrideResponseMapper.Map(_mapper, stored!, id => EncodeStaffMemberId(id, validation)), AppConstants.SuccessMessages.StaffMemberWorkModeOverrideStatusUpdated); }

    internal static CreateStaffMemberWorkModeOverrideRequestDTO ToValidationDto(StaffMemberWorkModeOverrideRequest entity, bool isActive) => new()
    {
        StaffMemberWorkArrangementId = entity.StaffMemberWorkArrangementId,
        RequestedWorkMode = (WorkMode)entity.RequestedWorkMode,
        FromDate = entity.FromDate,
        ToDate = entity.ToDate,
        CampusId = entity.CampusId,
        Reason = entity.Reason,
        IsActive = isActive
    };
}

/// <summary>Handles the mandatory approval decision for a pending override.</summary>
public sealed class DecideStaffMemberWorkModeOverrideCommandHandler : InstitutionConfigurationHandlerBase, IRequestHandler<DecideStaffMemberWorkModeOverrideCommand, ApiResponse<StaffMemberWorkModeOverrideResponseDTO>>
{
    private readonly IMapper _mapper;
    public DecideStaffMemberWorkModeOverrideCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, ILogger<InstitutionConfigurationHandlerBase> logger, IIdEncoderService idEncoderService) : base(unitOfWork, commonRequestService, logger, idEncoderService) => _mapper = mapper;
    public async Task<ApiResponse<StaffMemberWorkModeOverrideResponseDTO>> Handle(DecideStaffMemberWorkModeOverrideCommand request, CancellationToken cancellationToken)
    {
        if (request.DTO is null || request.DTO.Id <= 0) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        if (!request.Approve
            && (string.IsNullOrWhiteSpace(request.DTO.Remark)
                || request.DTO.Remark.Trim().Length > AppConstants.ValidationLimits.WorkModeOverrideTextMaxLength))
            throw new ValidationErrorException(AppConstants.ErrorMessages.WorkModeOverrideRejectedRemarkRequired);
        var expectedOperation = request.Approve ? "Approve" : "Reject";
        var operationName = await CommonRequestService.GetActiveOperationNameAsync(request.DTO.OperationId);
        if (!string.Equals(operationName, expectedOperation, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"The {expectedOperation} operation permission is required for this decision.");
        }
        var validation = await ValidateInstitutionDataAccessContextAsync();
        var entity = await UnitOfWork.StaffMemberWorkModeOverrideRequestRepository.GetForUpdateAsync(validation.InstitutionId, request.DTO.Id, cancellationToken) ?? throw new NotFoundException(AppConstants.ErrorMessages.StaffMemberWorkModeOverrideNotFound);
        await EnsureStaffMemberDataAccessAsync(validation, entity.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, cancellationToken);
        if (entity.ApprovalStatus != (short)WorkModeOverrideApprovalStatus.Pending) throw new ConflictException(AppConstants.ErrorMessages.WorkModeOverrideDecisionFinal);
        if (request.Approve && !entity.IsActive) throw new ConflictException(AppConstants.ErrorMessages.WorkModeOverrideInactiveApproval);
        if (request.Approve) await StaffMemberWorkModeOverrideValidator.ValidateAsync(UnitOfWork, validation.InstitutionId, entity.StaffMemberId, UpdateStaffMemberWorkModeOverrideStatusCommandHandler.ToValidationDto(entity, true), entity.Id, cancellationToken);
        entity.ApprovalStatus = (short)(request.Approve ? WorkModeOverrideApprovalStatus.Approved : WorkModeOverrideApprovalStatus.Rejected);
        entity.ApprovedById = request.Approve ? validation.LoggedInStaffMemberId : null;
        entity.ApprovedDateTime = request.Approve ? DateTime.UtcNow : null;
        entity.ApprovalRemark = request.Approve ? request.DTO.Remark?.Trim() : null;
        entity.RejectedById = request.Approve ? null : validation.LoggedInStaffMemberId;
        entity.RejectedDateTime = request.Approve ? null : DateTime.UtcNow;
        entity.RejectionRemark = request.Approve ? null : request.DTO.Remark!.Trim();
        entity.UpdatedById = validation.LoggedInStaffMemberId;
        entity.UpdatedDateTime = DateTime.UtcNow;
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        var stored = (await UnitOfWork.StaffMemberWorkModeOverrideRequestRepository.GetByIdAsync(validation.InstitutionId, entity.Id, cancellationToken))!;
        return ApiResponse<StaffMemberWorkModeOverrideResponseDTO>.Success(StaffMemberWorkModeOverrideResponseMapper.Map(_mapper, stored, id => EncodeStaffMemberId(id, validation)), request.Approve ? AppConstants.SuccessMessages.StaffMemberWorkModeOverrideApproved : AppConstants.SuccessMessages.StaffMemberWorkModeOverrideRejected);
    }
}

/// <summary>Handles temporary override retrieval.</summary>
public sealed class GetStaffMemberWorkModeOverrideByIdQueryHandler : InstitutionConfigurationHandlerBase, IRequestHandler<GetStaffMemberWorkModeOverrideByIdQuery, ApiResponse<StaffMemberWorkModeOverrideResponseDTO>>
{
    private readonly IMapper _mapper;
    public GetStaffMemberWorkModeOverrideByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, ILogger<InstitutionConfigurationHandlerBase> logger, IIdEncoderService idEncoderService) : base(unitOfWork, commonRequestService, logger, idEncoderService) => _mapper = mapper;
    public async Task<ApiResponse<StaffMemberWorkModeOverrideResponseDTO>> Handle(GetStaffMemberWorkModeOverrideByIdQuery request, CancellationToken cancellationToken)
    { var validation = await ValidateInstitutionDataAccessContextAsync(); var entity = await UnitOfWork.StaffMemberWorkModeOverrideRequestRepository.GetByIdAsync(validation.InstitutionId, request.Id, cancellationToken) ?? throw new NotFoundException(AppConstants.ErrorMessages.StaffMemberWorkModeOverrideNotFound); await EnsureStaffMemberDataAccessAsync(validation, entity.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, cancellationToken); return ApiResponse<StaffMemberWorkModeOverrideResponseDTO>.Success(StaffMemberWorkModeOverrideResponseMapper.Map(_mapper, entity, id => EncodeStaffMemberId(id, validation))); }
}

/// <summary>Handles paged temporary override retrieval.</summary>
public sealed class GetStaffMemberWorkModeOverridesQueryHandler : InstitutionConfigurationHandlerBase, IRequestHandler<GetStaffMemberWorkModeOverridesQuery, ApiResponse<List<StaffMemberWorkModeOverrideResponseDTO>>>
{
    private readonly IMapper _mapper;
    public GetStaffMemberWorkModeOverridesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, ILogger<InstitutionConfigurationHandlerBase> logger, IIdEncoderService idEncoderService) : base(unitOfWork, commonRequestService, logger, idEncoderService) => _mapper = mapper;
    public async Task<ApiResponse<List<StaffMemberWorkModeOverrideResponseDTO>>> Handle(GetStaffMemberWorkModeOverridesQuery request, CancellationToken cancellationToken)
    { var filter = request.Filter ?? new StaffMemberWorkModeOverrideFilterRequestDTO(); var validation = await ValidateInstitutionDataAccessContextAsync(); long? staffMemberId = null; if (!string.IsNullOrWhiteSpace(filter.StaffMemberId)) { var context = await ValidateInstitutionAndDecodeOptionalStaffMemberIdAsync(filter.StaffMemberId, StaffMemberDataAccessRequirement.PersonalDetails, cancellationToken); staffMemberId = context.StaffMemberId; } filter.ResolvedStaffMemberId = staffMemberId; var page = await UnitOfWork.StaffMemberWorkModeOverrideRequestRepository.GetPagedAsync(validation.InstitutionId, filter, validation.LoggedInStaffMemberId, validation.RoleTypeId, cancellationToken); return Paged(page.Data.Select(entity => StaffMemberWorkModeOverrideResponseMapper.Map(_mapper, entity, id => EncodeStaffMemberId(id, validation))).ToList(), page.PageNumber, page.PageSize, page.TotalCount, "Work mode override requests retrieved successfully."); }
}

#endregion
