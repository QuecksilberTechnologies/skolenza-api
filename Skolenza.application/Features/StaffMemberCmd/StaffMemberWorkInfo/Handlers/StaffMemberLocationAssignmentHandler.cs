// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Validates and manages Institution staffMember-to-location assignments.
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
/// <summary>Creates an staffMember-location assignment.</summary>
public sealed class CreateStaffMemberLocationAssignmentCommand(CreateStaffMemberLocationAssignmentRequestDTO dto) : IRequest<ApiResponse<StaffMemberLocationAssignmentResponseDTO>>
{
    /// <summary>Gets assignment values.</summary>
    public CreateStaffMemberLocationAssignmentRequestDTO DTO { get; } = dto;
}
/// <summary>Updates an staffMember-location assignment.</summary>
public sealed class UpdateStaffMemberLocationAssignmentCommand(UpdateStaffMemberLocationAssignmentRequestDTO dto) : IRequest<ApiResponse<StaffMemberLocationAssignmentResponseDTO>>
{
    /// <summary>Gets assignment values.</summary>
    public UpdateStaffMemberLocationAssignmentRequestDTO DTO { get; } = dto;
}
/// <summary>Soft deletes an staffMember-location assignment.</summary>
public sealed class DeleteStaffMemberLocationAssignmentCommand(long id, PermissionRequestDTO permissionRequest) : IRequest<ApiResponse<bool>>
{
    /// <summary>Gets the assignment identifier.</summary>
    public long Id { get; } = id;
    /// <summary>Gets the module and operation required for institution-role authorization.</summary>
    public PermissionRequestDTO PermissionRequest { get; } = permissionRequest;
}
/// <summary>Changes an staffMember-location-assignment active state.</summary>
public sealed class UpdateStaffMemberLocationAssignmentStatusCommand(UpdateStaffMemberLocationAssignmentStatusRequestDTO dto) : IRequest<ApiResponse<StaffMemberLocationAssignmentResponseDTO>>
{
    /// <summary>Gets status values.</summary>
    public UpdateStaffMemberLocationAssignmentStatusRequestDTO DTO { get; } = dto;
}
#endregion
#region Query
/// <summary>Retrieves one staffMember-location assignment.</summary>
public sealed class GetStaffMemberLocationAssignmentByIdQuery(long id, PermissionRequestDTO permissionRequest) : IRequest<ApiResponse<StaffMemberLocationAssignmentResponseDTO>>
{
    /// <summary>Gets the identifier.</summary>
    public long Id { get; } = id;
    /// <summary>Gets the module and operation required for institution-role authorization.</summary>
    public PermissionRequestDTO PermissionRequest { get; } = permissionRequest;
}
/// <summary>Retrieves filtered staffMember-location assignments.</summary>
public sealed class GetStaffMemberLocationAssignmentsQuery(StaffMemberLocationAssignmentFilterRequestDTO filter) : IRequest<ApiResponse<List<StaffMemberLocationAssignmentResponseDTO>>>
{
    /// <summary>Gets filters.</summary>
    public StaffMemberLocationAssignmentFilterRequestDTO Filter { get; } = filter;
}
#endregion
#region Handler
/// <summary>Maps a location assignment while keeping the public staffMember identifier encoded.</summary>
internal static class StaffMemberLocationAssignmentResponseMapper
{
    public static StaffMemberLocationAssignmentResponseDTO Map(
        IMapper mapper,
        StaffMemberLocationAssignment entity,
        Func<long, string> encodeStaffMemberId)
    {
        var response = mapper.Map<StaffMemberLocationAssignmentResponseDTO>(entity);
        response.StaffMemberId = encodeStaffMemberId(entity.StaffMemberId);
        return response;
    }
}

/// <summary>Handles staffMember-location-assignment creation.</summary>
public sealed class CreateStaffMemberLocationAssignmentCommandHandler : InstitutionConfigurationHandlerBase, IRequestHandler<CreateStaffMemberLocationAssignmentCommand, ApiResponse<StaffMemberLocationAssignmentResponseDTO>>
{
    #region Fields
    private readonly IMapper _mapper;
    #endregion
    #region Constructor
    /// <summary>Initializes the handler.</summary>
    public CreateStaffMemberLocationAssignmentCommandHandler(IUnitOfWork u, IMapper mapper, ICommonRequestService c, ILogger<InstitutionConfigurationHandlerBase> l, IIdEncoderService idEncoderService) : base(u, c, l, idEncoderService) => _mapper = mapper;
    #endregion
    #region Handle
    /// <inheritdoc />
    public async Task<ApiResponse<StaffMemberLocationAssignmentResponseDTO>> Handle(CreateStaffMemberLocationAssignmentCommand request, CancellationToken ct)
    {
        if (request.DTO is null) throw new ValidationErrorException(AppConstants.ErrorMessages.RequiredDataMissing);
        var (institutionId, actorId, staffMemberId) = await ValidateInstitutionAndDecodeStaffMemberIdAsync(
            request.DTO.StaffMemberId,
            StaffMemberDataAccessRequirement.WorkLocation,
            ct); Validate(request.DTO);
        await ValidateReferences(institutionId, staffMemberId, request.DTO, null, ct);
        var entity = _mapper.Map<StaffMemberLocationAssignment>(request.DTO); entity.StaffMemberId = staffMemberId; entity.InstitutionId = institutionId; entity.IsSoftDeleted = false; entity.AddedById = actorId; entity.AddedDateTime = DateTime.UtcNow;
        await UnitOfWork.StaffMemberLocationAssignmentRepository.AddAsync(entity, ct); await UnitOfWork.SaveChangesAsync(ct);
        Logger.LogInformation("StaffMember location assignment {AssignmentId} created for Institution {InstitutionId}.", entity.Id, institutionId);
        var saved = (await UnitOfWork.StaffMemberLocationAssignmentRepository.GetByIdAsync(institutionId, entity.Id, ct))!;
        var validation = await ValidateInstitutionDataAccessContextAsync();
        var response = StaffMemberLocationAssignmentResponseMapper.Map(
            _mapper,
            saved,
            id => EncodeStaffMemberId(id, validation));
        return ApiResponse<StaffMemberLocationAssignmentResponseDTO>.Success(
            response,
            AppConstants.SuccessMessages.StaffMemberLocationAssignmentCreated);
    }
    #endregion
    private async Task ValidateReferences(long institutionId, long staffMemberId, CreateStaffMemberLocationAssignmentRequestDTO dto, long? excludeId, CancellationToken ct)
    {
        if (!await UnitOfWork.StaffMemberLocationAssignmentRepository.IsEligibleStaffMemberAsync(institutionId, staffMemberId, ct) || !await UnitOfWork.StaffMemberLocationAssignmentRepository.IsEligibleLocationAsync(institutionId, dto.CampusId, ct)) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidInstitutionConfigurationReference);
        if (dto.IsActive && await UnitOfWork.StaffMemberLocationAssignmentRepository.AssignmentExistsAsync(institutionId, staffMemberId, dto.CampusId, dto.EffectiveFrom, dto.EffectiveTo, excludeId, ct)) throw new ConflictException(AppConstants.ErrorMessages.DuplicateStaffMemberLocationAssignment);
        if (dto.IsActive && dto.IsPrimary && await UnitOfWork.StaffMemberLocationAssignmentRepository.PrimaryAssignmentExistsAsync(institutionId, staffMemberId, dto.EffectiveFrom, dto.EffectiveTo, excludeId, ct)) throw new ConflictException(AppConstants.ErrorMessages.StaffMemberAlreadyHasPrimaryLocation);
    }
    private static void Validate(CreateStaffMemberLocationAssignmentRequestDTO dto) { if (dto is null || string.IsNullOrWhiteSpace(dto.StaffMemberId) || dto.CampusId <= 0 || dto.EffectiveFrom == default || (dto.EffectiveTo.HasValue && dto.EffectiveTo < dto.EffectiveFrom)) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidEffectiveDateRange); }
}

/// <summary>Handles staffMember-location-assignment updates.</summary>
public sealed class UpdateStaffMemberLocationAssignmentCommandHandler : InstitutionConfigurationHandlerBase, IRequestHandler<UpdateStaffMemberLocationAssignmentCommand, ApiResponse<StaffMemberLocationAssignmentResponseDTO>>
{
    #region Fields
    private readonly IMapper _mapper;
    #endregion
    #region Constructor
    /// <summary>Initializes the handler.</summary>
    public UpdateStaffMemberLocationAssignmentCommandHandler(IUnitOfWork u, IMapper mapper, ICommonRequestService c, ILogger<InstitutionConfigurationHandlerBase> l, IIdEncoderService idEncoderService) : base(u, c, l, idEncoderService) => _mapper = mapper;
    #endregion
    #region Handle
    /// <inheritdoc />
    public async Task<ApiResponse<StaffMemberLocationAssignmentResponseDTO>> Handle(UpdateStaffMemberLocationAssignmentCommand request, CancellationToken ct)
    {
        if (request.DTO is null || request.DTO.Id <= 0) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        var (institutionId, actorId, staffMemberId) = await ValidateInstitutionAndDecodeStaffMemberIdAsync(
            request.DTO.StaffMemberId,
            StaffMemberDataAccessRequirement.WorkLocation,
            ct); Validate(request.DTO);
        var entity = await UnitOfWork.StaffMemberLocationAssignmentRepository.GetForUpdateAsync(institutionId, request.DTO.Id, ct) ?? throw new NotFoundException(AppConstants.ErrorMessages.StaffMemberLocationAssignmentNotFound);
        if (entity.StaffMemberId != staffMemberId)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        // Moving an assignment to another location must not orphan an arrangement that uses the old location.
        if (entity.CampusId != request.DTO.CampusId
            && await UnitOfWork.StaffMemberLocationAssignmentRepository.WouldInvalidatePrimaryWorkArrangementAsync(
                institutionId,
                staffMemberId,
                entity.CampusId,
                false,
                false,
                false,
                entity.EffectiveFrom,
                entity.EffectiveTo,
                ct))
        {
            throw new ConflictException(AppConstants.ErrorMessages.StaffMemberLocationAssignmentInUse);
        }

        await ValidateReferences(institutionId, staffMemberId, request.DTO, entity.Id, ct);
        _mapper.Map(request.DTO, entity); entity.StaffMemberId = staffMemberId; entity.UpdatedById = actorId; entity.UpdatedDateTime = DateTime.UtcNow; await UnitOfWork.SaveChangesAsync(ct);
        var saved = (await UnitOfWork.StaffMemberLocationAssignmentRepository.GetByIdAsync(institutionId, entity.Id, ct))!;
        var validation = await ValidateInstitutionDataAccessContextAsync();
        var response = StaffMemberLocationAssignmentResponseMapper.Map(
            _mapper,
            saved,
            id => EncodeStaffMemberId(id, validation));
        return ApiResponse<StaffMemberLocationAssignmentResponseDTO>.Success(
            response,
            AppConstants.SuccessMessages.StaffMemberLocationAssignmentUpdated);
    }
    #endregion
    private async Task ValidateReferences(long institutionId, long staffMemberId, CreateStaffMemberLocationAssignmentRequestDTO dto, long? excludeId, CancellationToken ct)
    {
        if (!await UnitOfWork.StaffMemberLocationAssignmentRepository.IsEligibleStaffMemberAsync(institutionId, staffMemberId, ct) || !await UnitOfWork.StaffMemberLocationAssignmentRepository.IsEligibleLocationAsync(institutionId, dto.CampusId, ct)) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidInstitutionConfigurationReference);
        if (dto.IsActive && await UnitOfWork.StaffMemberLocationAssignmentRepository.AssignmentExistsAsync(institutionId, staffMemberId, dto.CampusId, dto.EffectiveFrom, dto.EffectiveTo, excludeId, ct)) throw new ConflictException(AppConstants.ErrorMessages.DuplicateStaffMemberLocationAssignment);
        if (dto.IsActive && dto.IsPrimary && await UnitOfWork.StaffMemberLocationAssignmentRepository.PrimaryAssignmentExistsAsync(institutionId, staffMemberId, dto.EffectiveFrom, dto.EffectiveTo, excludeId, ct)) throw new ConflictException(AppConstants.ErrorMessages.StaffMemberAlreadyHasPrimaryLocation);

        // Keep the assignment and every work arrangement that consumes it consistent in both directions.
        if (await UnitOfWork.StaffMemberLocationAssignmentRepository.WouldInvalidatePrimaryWorkArrangementAsync(
            institutionId,
            staffMemberId,
            dto.CampusId,
            dto.IsPrimary,
            dto.IsAttendanceAllowed,
            dto.IsActive,
            dto.EffectiveFrom,
            dto.EffectiveTo,
            ct))
        {
            throw new ConflictException(AppConstants.ErrorMessages.StaffMemberLocationAssignmentInUse);
        }
    }
    private static void Validate(CreateStaffMemberLocationAssignmentRequestDTO dto) { if (string.IsNullOrWhiteSpace(dto.StaffMemberId) || dto.CampusId <= 0 || dto.EffectiveFrom == default || (dto.EffectiveTo.HasValue && dto.EffectiveTo < dto.EffectiveFrom)) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidEffectiveDateRange); }
}

/// <summary>Handles staffMember-location-assignment soft deletion.</summary>
public sealed class DeleteStaffMemberLocationAssignmentCommandHandler : InstitutionConfigurationHandlerBase, IRequestHandler<DeleteStaffMemberLocationAssignmentCommand, ApiResponse<bool>>
{
    #region Constructor
    /// <summary>Initializes the handler.</summary>
    public DeleteStaffMemberLocationAssignmentCommandHandler(IUnitOfWork u, ICommonRequestService c, ILogger<InstitutionConfigurationHandlerBase> l) : base(u, c, l) { }
    #endregion
    #region Handle
    /// <inheritdoc />
    public async Task<ApiResponse<bool>> Handle(DeleteStaffMemberLocationAssignmentCommand request, CancellationToken ct)
    {
        var validation = await ValidateInstitutionDataAccessContextAsync();
        var entity = await UnitOfWork.StaffMemberLocationAssignmentRepository.GetForUpdateAsync(validation.InstitutionId, request.Id, ct)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.StaffMemberLocationAssignmentNotFound);
        await EnsureStaffMemberDataAccessAsync(validation, entity.StaffMemberId, StaffMemberDataAccessRequirement.WorkLocation, ct);

        // Deleting a primary assignment must not leave a live arrangement pointing at an unusable location.
        if (await UnitOfWork.StaffMemberLocationAssignmentRepository.WouldInvalidatePrimaryWorkArrangementAsync(
            validation.InstitutionId,
            entity.StaffMemberId,
            entity.CampusId,
            false,
            false,
            false,
            entity.EffectiveFrom,
            entity.EffectiveTo,
            ct))
        {
            throw new ConflictException(AppConstants.ErrorMessages.StaffMemberLocationAssignmentInUse);
        }

        entity.IsSoftDeleted = true;
        entity.IsActive = false;
        entity.SoftDeletedById = validation.LoggedInStaffMemberId;
        entity.SoftDeletedDateTime = DateTime.UtcNow;
        await UnitOfWork.SaveChangesAsync(ct);
        return ApiResponse<bool>.Success(true, AppConstants.SuccessMessages.StaffMemberLocationAssignmentDeleted);
    }
    #endregion
}

/// <summary>Handles staffMember-location-assignment status changes.</summary>
public sealed class UpdateStaffMemberLocationAssignmentStatusCommandHandler : InstitutionConfigurationHandlerBase, IRequestHandler<UpdateStaffMemberLocationAssignmentStatusCommand, ApiResponse<StaffMemberLocationAssignmentResponseDTO>>
{
    private readonly IMapper _mapper;
    #region Constructor
    /// <summary>Initializes the handler.</summary>
    public UpdateStaffMemberLocationAssignmentStatusCommandHandler(IUnitOfWork u, IMapper mapper, ICommonRequestService c, ILogger<InstitutionConfigurationHandlerBase> l, IIdEncoderService idEncoderService) : base(u, c, l, idEncoderService) => _mapper = mapper;
    #endregion
    #region Handle
    /// <inheritdoc />
    public async Task<ApiResponse<StaffMemberLocationAssignmentResponseDTO>> Handle(UpdateStaffMemberLocationAssignmentStatusCommand request, CancellationToken ct)
    {
        var validation = await ValidateInstitutionDataAccessContextAsync(); var institutionId = validation.InstitutionId; if (request.DTO is null || request.DTO.Id <= 0) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier); var entity = await UnitOfWork.StaffMemberLocationAssignmentRepository.GetForUpdateAsync(institutionId, request.DTO.Id, ct) ?? throw new NotFoundException(AppConstants.ErrorMessages.StaffMemberLocationAssignmentNotFound); await EnsureStaffMemberDataAccessAsync(validation, entity.StaffMemberId, StaffMemberDataAccessRequirement.WorkLocation, ct);
        if (request.DTO.IsActive && entity.IsPrimary && await UnitOfWork.StaffMemberLocationAssignmentRepository.PrimaryAssignmentExistsAsync(institutionId, entity.StaffMemberId, entity.EffectiveFrom, entity.EffectiveTo, entity.Id, ct)) throw new ConflictException(AppConstants.ErrorMessages.StaffMemberAlreadyHasPrimaryLocation);
        if (request.DTO.IsActive && await UnitOfWork.StaffMemberLocationAssignmentRepository.AssignmentExistsAsync(institutionId, entity.StaffMemberId, entity.CampusId, entity.EffectiveFrom, entity.EffectiveTo, entity.Id, ct)) throw new ConflictException(AppConstants.ErrorMessages.DuplicateStaffMemberLocationAssignment);
        if (!request.DTO.IsActive && await UnitOfWork.StaffMemberLocationAssignmentRepository.WouldInvalidatePrimaryWorkArrangementAsync(institutionId, entity.StaffMemberId, entity.CampusId, entity.IsPrimary, entity.IsAttendanceAllowed, false, entity.EffectiveFrom, entity.EffectiveTo, ct)) throw new ConflictException(AppConstants.ErrorMessages.StaffMemberLocationAssignmentInUse);
        entity.IsActive = request.DTO.IsActive;
        entity.UpdatedById = validation.LoggedInStaffMemberId;
        entity.UpdatedDateTime = DateTime.UtcNow;
        await UnitOfWork.SaveChangesAsync(ct);
        var saved = (await UnitOfWork.StaffMemberLocationAssignmentRepository.GetByIdAsync(institutionId, entity.Id, ct))!;
        var response = StaffMemberLocationAssignmentResponseMapper.Map(
            _mapper,
            saved,
            id => EncodeStaffMemberId(id, validation));
        return ApiResponse<StaffMemberLocationAssignmentResponseDTO>.Success(
            response,
            AppConstants.SuccessMessages.StaffMemberLocationAssignmentStatusUpdated);
    }
    #endregion
}

/// <summary>Handles retrieval of staffMember-location assignments.</summary>
public sealed class GetStaffMemberLocationAssignmentByIdQueryHandler : InstitutionConfigurationHandlerBase, IRequestHandler<GetStaffMemberLocationAssignmentByIdQuery, ApiResponse<StaffMemberLocationAssignmentResponseDTO>>
{
    private readonly IMapper _mapper;
    /// <summary>Initializes the handler.</summary>
    public GetStaffMemberLocationAssignmentByIdQueryHandler(IUnitOfWork u, IMapper mapper, ICommonRequestService c, ILogger<InstitutionConfigurationHandlerBase> l, IIdEncoderService idEncoderService) : base(u, c, l, idEncoderService) => _mapper = mapper;
    /// <inheritdoc />
    public async Task<ApiResponse<StaffMemberLocationAssignmentResponseDTO>> Handle(GetStaffMemberLocationAssignmentByIdQuery request, CancellationToken ct)
    {
        var validation = await ValidateInstitutionDataAccessContextAsync();
        var entity = await UnitOfWork.StaffMemberLocationAssignmentRepository.GetByIdAsync(validation.InstitutionId, request.Id, ct)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.StaffMemberLocationAssignmentNotFound);
        await EnsureStaffMemberDataAccessAsync(validation, entity.StaffMemberId, StaffMemberDataAccessRequirement.WorkLocation, ct);
        var response = StaffMemberLocationAssignmentResponseMapper.Map(
            _mapper,
            entity,
            id => EncodeStaffMemberId(id, validation));
        return ApiResponse<StaffMemberLocationAssignmentResponseDTO>.Success(response);
    }
}

/// <summary>Handles filtered retrieval of staffMember-location assignments.</summary>
public sealed class GetStaffMemberLocationAssignmentsQueryHandler : InstitutionConfigurationHandlerBase, IRequestHandler<GetStaffMemberLocationAssignmentsQuery, ApiResponse<List<StaffMemberLocationAssignmentResponseDTO>>>
{
    private readonly IMapper _mapper;
    /// <summary>Initializes the handler.</summary>
    public GetStaffMemberLocationAssignmentsQueryHandler(IUnitOfWork u, IMapper mapper, ICommonRequestService c, ILogger<InstitutionConfigurationHandlerBase> l, IIdEncoderService idEncoderService) : base(u, c, l, idEncoderService) => _mapper = mapper;
    /// <inheritdoc />
    public async Task<ApiResponse<List<StaffMemberLocationAssignmentResponseDTO>>> Handle(GetStaffMemberLocationAssignmentsQuery request, CancellationToken ct)
    {
        var filter = request.Filter ?? new StaffMemberLocationAssignmentFilterRequestDTO();
        var validation = await ValidateInstitutionDataAccessContextAsync();
        long? staffMemberId = null;
        if (!string.IsNullOrWhiteSpace(filter.StaffMemberId))
        {
            var context = await ValidateInstitutionAndDecodeOptionalStaffMemberIdAsync(
                filter.StaffMemberId,
                StaffMemberDataAccessRequirement.WorkLocation,
                ct);
            staffMemberId = context.StaffMemberId;
        }

        filter.ResolvedStaffMemberId = staffMemberId;
        var page = await UnitOfWork.StaffMemberLocationAssignmentRepository.GetPagedAsync(
            validation.InstitutionId,
            filter,
            validation.LoggedInStaffMemberId,
            validation.RoleTypeId,
            ct);
        var responses = page.Data
            .Select(entity => StaffMemberLocationAssignmentResponseMapper.Map(
                _mapper,
                entity,
                id => EncodeStaffMemberId(id, validation)))
            .ToList();
        return Paged(
            responses,
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            "StaffMember location assignments retrieved successfully.");
    }
}
#endregion
