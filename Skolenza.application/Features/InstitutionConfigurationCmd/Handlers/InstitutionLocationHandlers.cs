// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Validates and manages Institution-owned work-location configuration.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOS.InstitutionConfiguration;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.InstitutionConfigurationCmd.Handlers;

#region Command

/// <summary>Creates a Institution-owned work location.</summary>
public sealed class CreateCampusCommand : IRequest<ApiResponse<CampusResponseDTO>>
{
    /// <summary>Initializes a command with client-editable location values.</summary>
    public CreateCampusCommand(CreateCampusRequestDTO dto) => DTO = dto;
    /// <summary>Gets the location values to create.</summary>
    public CreateCampusRequestDTO DTO { get; }
}

/// <summary>Updates a Institution-owned work location.</summary>
public sealed class UpdateCampusCommand : IRequest<ApiResponse<CampusResponseDTO>>
{
    /// <summary>Initializes a command with client-editable location values.</summary>
    public UpdateCampusCommand(UpdateCampusRequestDTO dto) => DTO = dto;
    /// <summary>Gets the location values to update.</summary>
    public UpdateCampusRequestDTO DTO { get; }
}

/// <summary>Soft deletes a Institution-owned work location.</summary>
public sealed class DeleteCampusCommand : IRequest<ApiResponse<bool>>
{
    /// <summary>Initializes a command with the target location identifier.</summary>
    public DeleteCampusCommand(long id, CampusAccessRequestDTO accessRequest)
    {
        Id = id;
        AccessRequest = accessRequest;
    }
    /// <summary>Gets the target location identifier.</summary>
    public long Id { get; }
    /// <summary>Gets the encrypted Host Institution scope and permission context.</summary>
    public CampusAccessRequestDTO AccessRequest { get; }
}

/// <summary>Changes the active state of a Institution-owned work location.</summary>
public sealed class UpdateCampusStatusCommand : IRequest<ApiResponse<CampusResponseDTO>>
{
    /// <summary>Initializes a command with the desired status.</summary>
    public UpdateCampusStatusCommand(UpdateCampusStatusRequestDTO dto) => DTO = dto;
    /// <summary>Gets the requested status change.</summary>
    public UpdateCampusStatusRequestDTO DTO { get; }
}

#endregion

#region Query

/// <summary>Retrieves one Institution-owned work location.</summary>
public sealed class GetCampusByIdQuery : IRequest<ApiResponse<CampusResponseDTO>>
{
    /// <summary>Initializes a query with the target location identifier.</summary>
    public GetCampusByIdQuery(long id, CampusAccessRequestDTO accessRequest)
    {
        Id = id;
        AccessRequest = accessRequest;
    }
    /// <summary>Gets the target location identifier.</summary>
    public long Id { get; }
    /// <summary>Gets the encrypted Host Institution scope and permission context.</summary>
    public CampusAccessRequestDTO AccessRequest { get; }
}

/// <summary>Retrieves filtered and paginated Institution-owned work locations.</summary>
public sealed class GetCampussQuery : IRequest<ApiResponse<List<CampusResponseDTO>>>
{
    /// <summary>Initializes a query with database-side filters.</summary>
    public GetCampussQuery(CampusFilterRequestDTO filter) => Filter = filter;
    /// <summary>Gets the location filters and paging request.</summary>
    public CampusFilterRequestDTO Filter { get; }
}

#endregion

#region Handler

/// <summary>
/// Resolves the trusted Institution scope for Campus endpoints. Host requests
/// must supply an encrypted Institution identifier, while Institution StaffMember requests
/// are constrained to the Institution in their authenticated token.
/// </summary>
public abstract class CampusAccessHandlerBase : InstitutionConfigurationHandlerBase
{
    private readonly IIdEncoderService _idEncoderService;

    protected CampusAccessHandlerBase(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService,
        IIdEncoderService idEncoderService,
        ILogger<InstitutionConfigurationHandlerBase> logger)
        : base(unitOfWork, commonRequestService, logger)
    {
        _idEncoderService = idEncoderService;
    }

    /// <summary>Validates the principal and resolves its authoritative Institution and audit actor.</summary>
    protected async Task<(long InstitutionId, long ActorId)> ResolveInstitutionScopeAsync(
        CampusAccessRequestDTO accessRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accessRequest);

        var principal = await CommonRequestService.ValidateAuthenticatedRequestAsync();
        return principal.UserType switch
        {
            LoginUserType.Host => await ResolveHostInstitutionScopeAsync(accessRequest, cancellationToken),
            LoginUserType.InstitutionStaffMember => await ValidateInstitutionAsync(),
            _ => throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized)
        };
    }

    private async Task<(long InstitutionId, long ActorId)> ResolveHostInstitutionScopeAsync(
        CampusAccessRequestDTO accessRequest,
        CancellationToken cancellationToken)
    {
        var hostContext = await CommonRequestService.ValidateHostUserPermissionRequestAsync();
        var institutionId = HostInstitutionIdentifierProtector.Decrypt(
            accessRequest.InstitutionId,
            hostContext.InstitutionEncryptionKey,
            _idEncoderService);

        return (institutionId, hostContext.HostUserId);
    }

    /// <summary>
    /// Resolves the list scope. Any authorized Host role may retrieve all live
    /// Institution locations when InstitutionId is omitted; Institution users remain scoped to
    /// their own Institution.
    /// </summary>
    /// <param name="accessRequest">The request containing Host or Institution permission context.</param>
    /// <param name="cancellationToken">The token used to cancel authorization work.</param>
    /// <returns>The selected Institution identifier, or <see langword="null"/> for a Host Admin global list.</returns>
    protected async Task<long?> ResolveLocationListInstitutionScopeAsync(
        CampusAccessRequestDTO accessRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accessRequest);

        var principal = await CommonRequestService.ValidateAuthenticatedRequestAsync();
        if (principal.UserType == LoginUserType.Host)
        {
            var hostContext = await CommonRequestService.ValidateHostUserPermissionRequestAsync();

            if (string.IsNullOrWhiteSpace(accessRequest.InstitutionId))
            {
                return null;
            }

            return HostInstitutionIdentifierProtector.Decrypt(
                accessRequest.InstitutionId,
                hostContext.InstitutionEncryptionKey,
                _idEncoderService);
        }

        if (principal.UserType == LoginUserType.InstitutionStaffMember)
        {
            var (institutionId, _) = await ValidateInstitutionAsync();
            return institutionId;
        }

        throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
    }
}

/// <summary>Handles creation of Institution-owned work locations.</summary>
public sealed class CreateCampusCommandHandler : CampusAccessHandlerBase, IRequestHandler<CreateCampusCommand, ApiResponse<CampusResponseDTO>>
{
    #region Fields
    private readonly IMapper _mapper;
    #endregion
    #region Constructor
    /// <summary>Initializes the handler.</summary>
    public CreateCampusCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, IIdEncoderService idEncoderService, ILogger<InstitutionConfigurationHandlerBase> logger) : base(unitOfWork, commonRequestService, idEncoderService, logger) => _mapper = mapper;
    #endregion
    #region Handle
    /// <summary>Creates the validated Institution location.</summary>
    /// <param name="request">The creation command.</param><param name="cancellationToken">Cancellation token.</param><returns>The created location.</returns>
    public async Task<ApiResponse<CampusResponseDTO>> Handle(CreateCampusCommand request, CancellationToken cancellationToken)
    {
        var (institutionId, actorId) = await ResolveInstitutionScopeAsync(request.DTO, cancellationToken);
        Validate(request.DTO);
        if (!await UnitOfWork.CampusRepository.IsValidGeographyAsync(request.DTO.CountryId, request.DTO.StateId, request.DTO.DistrictId, request.DTO.LocalityId, cancellationToken)) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidInstitutionConfigurationReference);
        if (await UnitOfWork.CampusRepository.LocationCodeExistsAsync(institutionId, request.DTO.LocationCode.Trim(), null, cancellationToken)) throw new ConflictException(AppConstants.ErrorMessages.DuplicateCampusCode);
        var entity = _mapper.Map<Campus>(request.DTO);
        entity.LocationCode = request.DTO.LocationCode.Trim(); entity.LocationName = request.DTO.LocationName.Trim(); entity.TimeZoneId = request.DTO.TimeZoneId.Trim();
        entity.InstitutionId = institutionId; entity.IsSoftDeleted = false; entity.AddedById = actorId; entity.AddedDateTime = DateTime.UtcNow;
        await UnitOfWork.CampusRepository.AddAsync(entity, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        Logger.LogInformation("Institution location {CampusId} created for Institution {InstitutionId} by StaffMember {StaffMemberId}.", entity.Id, institutionId, actorId);
        return ApiResponse<CampusResponseDTO>.Success(_mapper.Map<CampusResponseDTO>((await UnitOfWork.CampusRepository.GetByIdAsync(institutionId, entity.Id, cancellationToken))!), AppConstants.SuccessMessages.CampusCreated);
    }
    #endregion
    private static void Validate(CreateCampusRequestDTO dto)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.LocationCode) || string.IsNullOrWhiteSpace(dto.LocationName) || string.IsNullOrWhiteSpace(dto.TimeZoneId)) throw new ValidationErrorException(AppConstants.ErrorMessages.RequiredDataMissing);
        if (!Enum.IsDefined(dto.LocationType)) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
    }
}

/// <summary>Handles updates to Institution-owned work locations.</summary>
public sealed class UpdateCampusCommandHandler : CampusAccessHandlerBase, IRequestHandler<UpdateCampusCommand, ApiResponse<CampusResponseDTO>>
{
    #region Fields
    private readonly IMapper _mapper;
    #endregion
    #region Constructor
    /// <summary>Initializes the handler.</summary>
    public UpdateCampusCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, IIdEncoderService idEncoderService, ILogger<InstitutionConfigurationHandlerBase> logger) : base(unitOfWork, commonRequestService, idEncoderService, logger) => _mapper = mapper;
    #endregion
    #region Handle
    /// <summary>Updates the validated Institution location.</summary>
    /// <param name="request">The update command.</param><param name="cancellationToken">Cancellation token.</param><returns>The updated location.</returns>
    public async Task<ApiResponse<CampusResponseDTO>> Handle(UpdateCampusCommand request, CancellationToken cancellationToken)
    {
        var (institutionId, actorId) = await ResolveInstitutionScopeAsync(request.DTO, cancellationToken);
        if (request.DTO is null || request.DTO.Id <= 0) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        Validate(request.DTO);
        var entity = await UnitOfWork.CampusRepository.GetForUpdateAsync(institutionId, request.DTO.Id, cancellationToken) ?? throw new NotFoundException(AppConstants.ErrorMessages.CampusNotFound);
        if (!request.DTO.IsActive && entity.IsActive && await UnitOfWork.CampusRepository.HasLiveActiveDependenciesAsync(institutionId, entity.Id, cancellationToken)) throw new ConflictException(AppConstants.ErrorMessages.CampusInUse);
        if (!await UnitOfWork.CampusRepository.IsValidGeographyAsync(request.DTO.CountryId, request.DTO.StateId, request.DTO.DistrictId, request.DTO.LocalityId, cancellationToken)) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidInstitutionConfigurationReference);
        if (await UnitOfWork.CampusRepository.LocationCodeExistsAsync(institutionId, request.DTO.LocationCode.Trim(), entity.Id, cancellationToken)) throw new ConflictException(AppConstants.ErrorMessages.DuplicateCampusCode);
        _mapper.Map(request.DTO, entity); entity.LocationCode = request.DTO.LocationCode.Trim(); entity.LocationName = request.DTO.LocationName.Trim(); entity.TimeZoneId = request.DTO.TimeZoneId.Trim(); entity.UpdatedById = actorId; entity.UpdatedDateTime = DateTime.UtcNow;
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        Logger.LogInformation("Institution location {CampusId} updated for Institution {InstitutionId} by StaffMember {StaffMemberId}.", entity.Id, institutionId, actorId);
        return ApiResponse<CampusResponseDTO>.Success(_mapper.Map<CampusResponseDTO>((await UnitOfWork.CampusRepository.GetByIdAsync(institutionId, entity.Id, cancellationToken))!), AppConstants.SuccessMessages.CampusUpdated);
    }
    #endregion
    private static void Validate(CreateCampusRequestDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.LocationCode) || string.IsNullOrWhiteSpace(dto.LocationName) || string.IsNullOrWhiteSpace(dto.TimeZoneId) || !Enum.IsDefined(dto.LocationType)) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
    }
}

/// <summary>Handles safe soft deletion of Institution-owned work locations.</summary>
public sealed class DeleteCampusCommandHandler : CampusAccessHandlerBase, IRequestHandler<DeleteCampusCommand, ApiResponse<bool>>
{
    #region Constructor
    /// <summary>Initializes the handler.</summary>
    public DeleteCampusCommandHandler(IUnitOfWork unitOfWork, ICommonRequestService commonRequestService, IIdEncoderService idEncoderService, ILogger<InstitutionConfigurationHandlerBase> logger) : base(unitOfWork, commonRequestService, idEncoderService, logger) { }
    #endregion
    #region Handle
    /// <summary>Soft deletes an unused Institution location.</summary>
    /// <param name="request">The deletion command.</param><param name="cancellationToken">Cancellation token.</param><returns>A successful deletion acknowledgement.</returns>
    public async Task<ApiResponse<bool>> Handle(DeleteCampusCommand request, CancellationToken cancellationToken)
    {
        var (institutionId, actorId) = await ResolveInstitutionScopeAsync(request.AccessRequest, cancellationToken);
        var entity = await UnitOfWork.CampusRepository.GetForUpdateAsync(institutionId, request.Id, cancellationToken) ?? throw new NotFoundException(AppConstants.ErrorMessages.CampusNotFound);
        if (await UnitOfWork.CampusRepository.HasAnyDependenciesAsync(institutionId, entity.Id, cancellationToken)) throw new ConflictException(AppConstants.ErrorMessages.CampusInUse);
        entity.IsSoftDeleted = true; entity.IsActive = false; entity.SoftDeletedById = actorId; entity.SoftDeletedDateTime = DateTime.UtcNow;
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        Logger.LogInformation("Institution location {CampusId} soft deleted for Institution {InstitutionId} by StaffMember {StaffMemberId}.", entity.Id, institutionId, actorId);
        return ApiResponse<bool>.Success(true, AppConstants.SuccessMessages.CampusDeleted);
    }
    #endregion
}

/// <summary>Handles active-state changes to Institution-owned work locations.</summary>
public sealed class UpdateCampusStatusCommandHandler : CampusAccessHandlerBase, IRequestHandler<UpdateCampusStatusCommand, ApiResponse<CampusResponseDTO>>
{
    private readonly IMapper _mapper;
    #region Constructor
    /// <summary>Initializes the handler.</summary>
    public UpdateCampusStatusCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, IIdEncoderService idEncoderService, ILogger<InstitutionConfigurationHandlerBase> logger) : base(unitOfWork, commonRequestService, idEncoderService, logger) => _mapper = mapper;
    #endregion
    #region Handle
    /// <summary>Changes the active state after dependency validation.</summary>
    /// <param name="request">The status command.</param><param name="cancellationToken">Cancellation token.</param><returns>The location after the status change.</returns>
    public async Task<ApiResponse<CampusResponseDTO>> Handle(UpdateCampusStatusCommand request, CancellationToken cancellationToken)
    {
        var (institutionId, actorId) = await ResolveInstitutionScopeAsync(request.DTO, cancellationToken);
        if (request.DTO is null || request.DTO.Id <= 0) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        var entity = await UnitOfWork.CampusRepository.GetForUpdateAsync(institutionId, request.DTO.Id, cancellationToken) ?? throw new NotFoundException(AppConstants.ErrorMessages.CampusNotFound);
        if (!request.DTO.IsActive && entity.IsActive && await UnitOfWork.CampusRepository.HasLiveActiveDependenciesAsync(institutionId, entity.Id, cancellationToken)) throw new ConflictException(AppConstants.ErrorMessages.CampusInUse);
        entity.IsActive = request.DTO.IsActive; entity.UpdatedById = actorId; entity.UpdatedDateTime = DateTime.UtcNow;
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return ApiResponse<CampusResponseDTO>.Success(_mapper.Map<CampusResponseDTO>((await UnitOfWork.CampusRepository.GetByIdAsync(institutionId, entity.Id, cancellationToken))!), AppConstants.SuccessMessages.CampusStatusUpdated);
    }
    #endregion
}

/// <summary>Handles retrieval of a Institution-owned work location.</summary>
public sealed class GetCampusByIdQueryHandler : CampusAccessHandlerBase, IRequestHandler<GetCampusByIdQuery, ApiResponse<CampusResponseDTO>>
{
    private readonly IMapper _mapper;
    #region Constructor
    /// <summary>Initializes the handler.</summary>
    public GetCampusByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, IIdEncoderService idEncoderService, ILogger<InstitutionConfigurationHandlerBase> logger) : base(unitOfWork, commonRequestService, idEncoderService, logger) => _mapper = mapper;
    #endregion
    #region Handle
    /// <summary>Retrieves one Institution-owned location.</summary>
    /// <param name="request">The identifier query.</param><param name="cancellationToken">Cancellation token.</param><returns>The requested location.</returns>
    public async Task<ApiResponse<CampusResponseDTO>> Handle(GetCampusByIdQuery request, CancellationToken cancellationToken)
    {
        var (institutionId, _) = await ResolveInstitutionScopeAsync(request.AccessRequest, cancellationToken);
        var entity = await UnitOfWork.CampusRepository.GetByIdAsync(institutionId, request.Id, cancellationToken) ?? throw new NotFoundException(AppConstants.ErrorMessages.CampusNotFound);
        return ApiResponse<CampusResponseDTO>.Success(_mapper.Map<CampusResponseDTO>(entity), AppConstants.SuccessMessages.CampusRetrieved);
    }
    #endregion
}

/// <summary>Handles filtered retrieval of Institution-owned work locations.</summary>
public sealed class GetCampussQueryHandler : CampusAccessHandlerBase, IRequestHandler<GetCampussQuery, ApiResponse<List<CampusResponseDTO>>>
{
    private readonly IMapper _mapper;
    #region Constructor
    /// <summary>Initializes the handler.</summary>
    public GetCampussQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, IIdEncoderService idEncoderService, ILogger<InstitutionConfigurationHandlerBase> logger) : base(unitOfWork, commonRequestService, idEncoderService, logger) => _mapper = mapper;
    #endregion
    #region Handle
    /// <summary>Retrieves a database-paged Institution location list.</summary>
    /// <param name="request">The filter query.</param><param name="cancellationToken">Cancellation token.</param><returns>A flattened paginated location response.</returns>
    public async Task<ApiResponse<List<CampusResponseDTO>>> Handle(GetCampussQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter ?? new CampusFilterRequestDTO();
        var institutionId = await ResolveLocationListInstitutionScopeAsync(filter, cancellationToken);
        var page = institutionId.HasValue
            ? await UnitOfWork.CampusRepository.GetPagedAsync(institutionId.Value, filter, cancellationToken)
            : await UnitOfWork.CampusRepository.GetHostPagedAsync(filter, cancellationToken);

        return Paged(page.Data.Select(entity => _mapper.Map<CampusResponseDTO>(entity)).ToList(), page.PageNumber, page.PageSize, page.TotalCount, AppConstants.SuccessMessages.CampusRetrieved);
    }
    #endregion
}

#endregion
