// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Handles Host and Institution administration of physical Institution devices and their configurations.
// ================================================================

using System.Text.Json;
using AutoMapper;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOS.Host;
using skolenza.application.Exceptions;
using skolenza.application.Features.InstitutionConfigurationCmd.Handlers;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IDeviceCommunication;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.HostDeviceCmd.Handlers;

#region Institution Device Commands

/// <summary>Creates a physical Institution device without creating its connection configuration.</summary>
public sealed class CreateInstitutionDeviceCommand(CreateInstitutionDeviceRequestDTO dto) : IRequest<ApiResponse<InstitutionDeviceResponseDTO>>
{
    public CreateInstitutionDeviceRequestDTO DTO { get; } = dto;
}

/// <summary>Updates a physical Institution device installation record.</summary>
public sealed class UpdateInstitutionDeviceCommand(UpdateInstitutionDeviceRequestDTO dto) : IRequest<ApiResponse<InstitutionDeviceResponseDTO>>
{
    public UpdateInstitutionDeviceRequestDTO DTO { get; } = dto;
}

/// <summary>Changes a physical Institution device active state.</summary>
public sealed class UpdateInstitutionDeviceStatusCommand(UpdateInstitutionDeviceStatusRequestDTO dto) : IRequest<ApiResponse<InstitutionDeviceResponseDTO>>
{
    public UpdateInstitutionDeviceStatusRequestDTO DTO { get; } = dto;
}

/// <summary>Soft deletes a physical Institution device after its dependent configuration has been hard deleted.</summary>
public sealed class DeleteInstitutionDeviceCommand(string encryptedId, InstitutionDeviceAccessRequestDTO accessRequest) : IRequest<ApiResponse<bool>>
{
    public string EncryptedId { get; } = encryptedId;
    public InstitutionDeviceAccessRequestDTO AccessRequest { get; } = accessRequest;
}

#endregion

#region Institution Device Queries

/// <summary>Retrieves one physical Institution device.</summary>
public sealed class GetInstitutionDeviceByIdQuery(string encryptedId, InstitutionDeviceAccessRequestDTO accessRequest) : IRequest<ApiResponse<InstitutionDeviceResponseDTO>>
{
    public string EncryptedId { get; } = encryptedId;
    public InstitutionDeviceAccessRequestDTO AccessRequest { get; } = accessRequest;
}

/// <summary>Retrieves a database-paged physical Institution device list.</summary>
public sealed class GetAllInstitutionDevicesQuery(GetInstitutionDeviceListRequestDTO filter) : IRequest<ApiResponse<List<InstitutionDeviceResponseDTO>>>
{
    public GetInstitutionDeviceListRequestDTO Filter { get; } = filter;
}

#endregion

#region Institution Device Configuration Commands

/// <summary>Creates the separate connection configuration for a Institution device.</summary>
public sealed class CreateInstitutionDeviceConfigurationCommand(CreateInstitutionDeviceConfigurationRequestDTO dto) : IRequest<ApiResponse<InstitutionDeviceConfigurationResponseDTO>>
{
    public CreateInstitutionDeviceConfigurationRequestDTO DTO { get; } = dto;
}

/// <summary>Updates the separate connection configuration for a Institution device.</summary>
public sealed class UpdateInstitutionDeviceConfigurationCommand(UpdateInstitutionDeviceConfigurationRequestDTO dto) : IRequest<ApiResponse<InstitutionDeviceConfigurationResponseDTO>>
{
    public UpdateInstitutionDeviceConfigurationRequestDTO DTO { get; } = dto;
}

/// <summary>Rotates the opaque HTTPS bearer URL for one configured Institution device.</summary>
public sealed class RotateInstitutionDeviceHttpsIngressTokenCommand(RotateInstitutionDeviceHttpsIngressTokenRequestDTO dto)
    : IRequest<ApiResponse<InstitutionDeviceHttpsIngressEndpointResponseDTO>>
{
    public RotateInstitutionDeviceHttpsIngressTokenRequestDTO DTO { get; } = dto;
}

/// <summary>Hard deletes the separate connection configuration for a Institution device.</summary>
public sealed class DeleteInstitutionDeviceConfigurationCommand(string encryptedId, InstitutionDeviceAccessRequestDTO accessRequest) : IRequest<ApiResponse<bool>>
{
    public string EncryptedId { get; } = encryptedId;
    public InstitutionDeviceAccessRequestDTO AccessRequest { get; } = accessRequest;
}

#endregion

#region Initial Device Provisioning and Runtime Commands

/// <summary>Creates a short-lived Host bootstrap URL for an unassigned HTTPS-capable device.</summary>
public sealed class IssueInitialDeviceBootstrapCommand(IssueInitialDeviceBootstrapRequestDTO dto)
    : IRequest<ApiResponse<InitialDeviceBootstrapResponseDTO>>
{
    /// <summary>Gets the Host provisioning request.</summary>
    public IssueInitialDeviceBootstrapRequestDTO DTO { get; } = dto;
}

/// <summary>Queues approved Institution-admin runtime device settings without accepting raw vendor JSON.</summary>
public sealed class ApplyInstitutionDeviceRuntimeConfigurationCommand(ApplyInstitutionDeviceRuntimeConfigurationRequestDTO dto)
    : IRequest<ApiResponse<InstitutionDeviceRuntimeConfigurationResponseDTO>>
{
    /// <summary>Gets the Institution runtime configuration request.</summary>
    public ApplyInstitutionDeviceRuntimeConfigurationRequestDTO DTO { get; } = dto;
}

/// <summary>Queues a Institution-admin-authorized device reboot through the configured HTTPS gateway.</summary>
public sealed class RebootInstitutionDeviceCommand(RebootInstitutionDeviceRequestDTO dto)
    : IRequest<ApiResponse<DeviceCommandSubmissionResponseDTO>>
{
    /// <summary>Gets the reboot request.</summary>
    public RebootInstitutionDeviceRequestDTO DTO { get; } = dto;
}

#endregion

#region Institution Device Configuration Queries

/// <summary>Retrieves one Institution device configuration.</summary>
public sealed class GetInstitutionDeviceConfigurationByIdQuery(string encryptedId, InstitutionDeviceAccessRequestDTO accessRequest) : IRequest<ApiResponse<InstitutionDeviceConfigurationResponseDTO>>
{
    public string EncryptedId { get; } = encryptedId;
    public InstitutionDeviceAccessRequestDTO AccessRequest { get; } = accessRequest;
}

/// <summary>Retrieves a database-paged Institution device configuration list.</summary>
public sealed class GetAllInstitutionDeviceConfigurationsQuery(GetInstitutionDeviceConfigurationListRequestDTO filter) : IRequest<ApiResponse<List<InstitutionDeviceConfigurationResponseDTO>>>
{
    public GetInstitutionDeviceConfigurationListRequestDTO Filter { get; } = filter;
}

#endregion

#region Shared Access

/// <summary>Represents the trusted Institution scope and identifier-protection key for a device request.</summary>
public sealed record InstitutionDeviceAccessScope(
    long InstitutionId,
    long ActorId,
    string InstitutionEncryptionKey,
    LoginUserType UserType);

/// <summary>Represents the trusted list scope and identifier-protection key for a device query.</summary>
public sealed record InstitutionDeviceListAccessScope(
    long? InstitutionId,
    string InstitutionEncryptionKey,
    LoginUserType UserType);

/// <summary>Resolves authoritative Host or Institution access for InstitutionDevice resources through the established permission flows.</summary>
public abstract class InstitutionDeviceAccessHandlerBase : InstitutionConfigurationHandlerBase
{
    private readonly IIdEncoderService _idEncoderService;

    protected InstitutionDeviceAccessHandlerBase(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService,
        IIdEncoderService idEncoderService,
        ILogger<InstitutionConfigurationHandlerBase> logger)
        : base(unitOfWork, commonRequestService, logger)
    {
        _idEncoderService = idEncoderService;
    }

    /// <summary>Validates the principal and resolves the authoritative Institution, audit actor, and encryption key.</summary>
    protected async Task<InstitutionDeviceAccessScope> ResolveInstitutionScopeAsync(InstitutionDeviceAccessRequestDTO accessRequest, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accessRequest);

        var principal = await CommonRequestService.ValidateAuthenticatedRequestAsync();
        return principal.UserType switch
        {
            LoginUserType.Host => await ResolveHostInstitutionScopeAsync(accessRequest, cancellationToken),
            LoginUserType.InstitutionStaffMember => await ResolveInstitutionStaffMemberScopeAsync(accessRequest, cancellationToken),
            _ => throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized)
        };
    }

    /// <summary>
    /// Resolves the Host-only inventory-assignment scope. Institution users may view
    /// assigned devices but cannot create, replace, disable, or remove a
    /// physical Institution-device assignment.
    /// </summary>
    protected async Task<InstitutionDeviceAccessScope> ResolveHostDeviceAssignmentScopeAsync(
        InstitutionDeviceAccessRequestDTO accessRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accessRequest);

        var principal = await CommonRequestService.ValidateAuthenticatedRequestAsync();
        if (principal.UserType != LoginUserType.Host)
        {
            throw new ForbiddenAccessException(AppConstants.ErrorMessages.PermissionDenied);
        }

        return await ResolveHostInstitutionScopeAsync(accessRequest, cancellationToken);
    }

    /// <summary>Maps a Institution device response and protects the Institution identifier.</summary>
    protected InstitutionDeviceResponseDTO MapDeviceResponse(IMapper mapper, InstitutionDevice entity, InstitutionDeviceAccessScope scope)
        => MapDeviceResponse(mapper, entity, scope.InstitutionEncryptionKey, scope.UserType == LoginUserType.Host);

    /// <summary>Maps a Institution device response while protecting its Institution identifier with the trusted request key.</summary>
    protected InstitutionDeviceResponseDTO MapDeviceResponse(
        IMapper mapper,
        InstitutionDevice entity,
        string institutionEncryptionKey,
        bool includeDeviceMasterMetadata)
    {
        var response = mapper.Map<InstitutionDeviceResponseDTO>(entity);
        response.InstitutionId = EncryptInstitutionId(entity.InstitutionId, institutionEncryptionKey);
        response.Id = EncryptIdentifier(entity.Id, institutionEncryptionKey);
        if (!includeDeviceMasterMetadata)
        {
            response.DeviceMasterId = null;
            response.DeviceMasterName = null;
            response.DeviceMasterModelNo = null;
        }

        return response;
    }

    /// <summary>Maps a configuration response and protects the parent device Institution identifier.</summary>
    protected InstitutionDeviceConfigurationResponseDTO MapConfigurationResponse(IMapper mapper, InstitutionDeviceConfiguration entity, InstitutionDeviceAccessScope scope)
        => MapConfigurationResponse(mapper, entity, scope.InstitutionEncryptionKey, scope.UserType == LoginUserType.Host);

    /// <summary>Maps a device configuration response while protecting its parent Institution identifier with the trusted request key.</summary>
    protected InstitutionDeviceConfigurationResponseDTO MapConfigurationResponse(
        IMapper mapper,
        InstitutionDeviceConfiguration entity,
        string institutionEncryptionKey,
        bool includeDeviceMasterMetadata)
    {
        var response = mapper.Map<InstitutionDeviceConfigurationResponseDTO>(entity);
        response.InstitutionId = EncryptInstitutionId(entity.InstitutionDevice.InstitutionId, institutionEncryptionKey);
        response.Id = EncryptIdentifier(entity.Id, institutionEncryptionKey);
        response.InstitutionDeviceId = EncryptIdentifier(entity.InstitutionDeviceId, institutionEncryptionKey);
        if (!includeDeviceMasterMetadata)
        {
            response.DeviceMasterName = null;
            response.DeviceMasterSNo = null;
        }

        return response;
    }

    /// <summary>
    /// Resolves a list scope. Any Host role that has the requested list
    /// permission may omit InstitutionId to list every live Institution record; Institution
    /// users remain restricted to their own Institution.
    /// </summary>
    protected async Task<InstitutionDeviceListAccessScope> ResolveInstitutionListScopeAsync(
        InstitutionDeviceAccessRequestDTO accessRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accessRequest);

        var principal = await CommonRequestService.ValidateAuthenticatedRequestAsync();
        if (principal.UserType == LoginUserType.Host)
        {
            var hostContext = await HostRuntimePermissionValidator.ValidateAsync(
                CommonRequestService,
                UnitOfWork.StoreProcedureRepository,
                accessRequest.ModuleId,
                accessRequest.OperationId,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(accessRequest.InstitutionId))
            {
                return new InstitutionDeviceListAccessScope(null, hostContext.InstitutionEncryptionKey, LoginUserType.Host);
            }

            var institutionId = HostInstitutionIdentifierProtector.Decrypt(
                accessRequest.InstitutionId,
                hostContext.InstitutionEncryptionKey,
                _idEncoderService);
            return new InstitutionDeviceListAccessScope(institutionId, hostContext.InstitutionEncryptionKey, LoginUserType.Host);
        }

        if (principal.UserType == LoginUserType.InstitutionStaffMember)
        {
            var institutionScope = await ResolveInstitutionStaffMemberScopeAsync(accessRequest, cancellationToken);
            return new InstitutionDeviceListAccessScope(institutionScope.InstitutionId, institutionScope.InstitutionEncryptionKey, LoginUserType.InstitutionStaffMember);
        }

        throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
    }

    private string EncryptInstitutionId(long institutionId, string institutionEncryptionKey) =>
        HostInstitutionIdentifierProtector.Encrypt(institutionId, institutionEncryptionKey, _idEncoderService);

    /// <summary>
    /// Encodes a Institution-device aggregate identifier for every external response.
    /// This follows the same IIdEncoderService pattern used by StaffMember handlers.
    /// </summary>
    protected string EncryptIdentifier(long identifier, string institutionEncryptionKey) =>
        _idEncoderService.EncodeId_long(identifier, institutionEncryptionKey);

    /// <summary>Decodes and validates an identifier only after the caller Institution scope is trusted.</summary>
    protected long DecodeIdentifier(string? encryptedIdentifier, InstitutionDeviceAccessScope scope, string identifierName)
        => DecodeIdentifier(encryptedIdentifier, scope.InstitutionEncryptionKey, identifierName);

    /// <summary>Decodes and validates an identifier using a trusted Institution encryption key.</summary>
    protected long DecodeIdentifier(string? encryptedIdentifier, string institutionEncryptionKey, string identifierName)
    {
        if (string.IsNullOrWhiteSpace(encryptedIdentifier))
        {
            throw new ValidationErrorException($"A valid {identifierName} is required.");
        }

        var token = encryptedIdentifier.Trim();
        var identifier = _idEncoderService.DecodeId_long(token, institutionEncryptionKey);
        if (identifier <= 0 ||
            !string.Equals(EncryptIdentifier(identifier, institutionEncryptionKey), token, StringComparison.Ordinal))
        {
            throw new ValidationErrorException($"A valid {identifierName} is required.");
        }

        return identifier;
    }

    private async Task<InstitutionDeviceAccessScope> ResolveHostInstitutionScopeAsync(InstitutionDeviceAccessRequestDTO accessRequest, CancellationToken cancellationToken)
    {
        var hostContext = await HostRuntimePermissionValidator.ValidateAsync(
            CommonRequestService,
            UnitOfWork.StoreProcedureRepository,
            accessRequest.ModuleId,
            accessRequest.OperationId,
            cancellationToken);
        var institutionId = HostInstitutionIdentifierProtector.Decrypt(accessRequest.InstitutionId, hostContext.InstitutionEncryptionKey, _idEncoderService);
        return new InstitutionDeviceAccessScope(institutionId, hostContext.HostUserId, hostContext.InstitutionEncryptionKey, LoginUserType.Host);
    }

    private async Task<InstitutionDeviceAccessScope> ResolveInstitutionStaffMemberScopeAsync(InstitutionDeviceAccessRequestDTO accessRequest, CancellationToken cancellationToken)
    {
        var institutionValidation = await CommonRequestService.ValidateInstitutionUserRequestAsync();
        if (!institutionValidation.Success || string.IsNullOrWhiteSpace(institutionValidation.Claims?.InstitutionEncriptionKey))
        {
            throw new UnauthorizedAccessException(institutionValidation.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
        }

        var (institutionId, actorId) = await ValidateInstitutionPermissionAsync(accessRequest, cancellationToken);
        if (institutionId != institutionValidation.InstitutionId)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        return new InstitutionDeviceAccessScope(institutionId, actorId, institutionValidation.Claims.InstitutionEncriptionKey, LoginUserType.InstitutionStaffMember);
    }

    /// <summary>Resolves Institution runtime scope after the pipeline behavior has already checked the module and operation permission.</summary>
    protected async Task<InstitutionDeviceAccessScope> ResolveInstitutionConfigurationScopeAsync(
        CancellationToken cancellationToken)
    {
        var institutionValidation = await ValidateInstitutionDataAccessContextAsync();
        if (string.IsNullOrWhiteSpace(institutionValidation.Claims.InstitutionEncriptionKey))
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        return new InstitutionDeviceAccessScope(
            institutionValidation.InstitutionId,
            institutionValidation.LoggedInStaffMemberId,
            institutionValidation.Claims.InstitutionEncriptionKey,
            LoginUserType.InstitutionStaffMember);
    }

    /// <summary>
    /// Resolves a configuration scope after the device-configuration permission behavior has authorized the request.
    /// A Host user acts on the encrypted Institution selected in the request; a Institution staffMember remains scoped to their token Institution.
    /// </summary>
    protected async Task<InstitutionDeviceAccessScope> ResolveAuthorizedInstitutionConfigurationScopeAsync(
        InstitutionDeviceAccessRequestDTO accessRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accessRequest);

        var principal = await CommonRequestService.ValidateAuthenticatedRequestAsync();
        if (principal.UserType == LoginUserType.Host)
        {
            var hostContext = await CommonRequestService.ValidateHostUserPermissionRequestAsync();
            var institutionId = HostInstitutionIdentifierProtector.Decrypt(
                accessRequest.InstitutionId,
                hostContext.InstitutionEncryptionKey,
                _idEncoderService);
            return new InstitutionDeviceAccessScope(
                institutionId,
                hostContext.HostUserId,
                hostContext.InstitutionEncryptionKey,
                LoginUserType.Host);
        }

        if (principal.UserType == LoginUserType.InstitutionStaffMember)
        {
            return await ResolveInstitutionConfigurationScopeAsync(cancellationToken);
        }

        throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
    }

    /// <summary>
    /// Resolves the configuration list scope after the device-configuration permission behavior has authorized the request.
    /// An authorized Host role may omit InstitutionId to list every Institution
    /// configuration; Institution users remain Institution-scoped.
    /// </summary>
    protected async Task<InstitutionDeviceListAccessScope> ResolveAuthorizedInstitutionConfigurationListScopeAsync(
        InstitutionDeviceAccessRequestDTO accessRequest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accessRequest);

        var principal = await CommonRequestService.ValidateAuthenticatedRequestAsync();
        if (principal.UserType == LoginUserType.Host)
        {
            var hostContext = await CommonRequestService.ValidateHostUserPermissionRequestAsync();
            if (string.IsNullOrWhiteSpace(accessRequest.InstitutionId))
            {
                return new InstitutionDeviceListAccessScope(null, hostContext.InstitutionEncryptionKey, LoginUserType.Host);
            }

            var institutionId = HostInstitutionIdentifierProtector.Decrypt(
                accessRequest.InstitutionId,
                hostContext.InstitutionEncryptionKey,
                _idEncoderService);
            return new InstitutionDeviceListAccessScope(institutionId, hostContext.InstitutionEncryptionKey, LoginUserType.Host);
        }

        if (principal.UserType == LoginUserType.InstitutionStaffMember)
        {
            var scope = await ResolveInstitutionConfigurationScopeAsync(cancellationToken);
            return new InstitutionDeviceListAccessScope(scope.InstitutionId, scope.InstitutionEncryptionKey, LoginUserType.InstitutionStaffMember);
        }

        throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
    }
}

#endregion

#region Institution Device Handlers

/// <summary>Handles creation of a Institution device before configuration is supplied.</summary>
public sealed class CreateInstitutionDeviceCommandHandler : InstitutionDeviceAccessHandlerBase, IRequestHandler<CreateInstitutionDeviceCommand, ApiResponse<InstitutionDeviceResponseDTO>>
{
    private readonly IMapper _mapper;
    private readonly ILogger<CreateInstitutionDeviceCommandHandler> _logger;

    public CreateInstitutionDeviceCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, IIdEncoderService idEncoderService, ILogger<InstitutionConfigurationHandlerBase> institutionLogger, ILogger<CreateInstitutionDeviceCommandHandler> logger)
        : base(unitOfWork, commonRequestService, idEncoderService, institutionLogger)
    {
        _mapper = mapper;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ApiResponse<InstitutionDeviceResponseDTO>> Handle(CreateInstitutionDeviceCommand request, CancellationToken cancellationToken)
    {
        var scope = await ResolveHostDeviceAssignmentScopeAsync(request.DTO, cancellationToken);
        InstitutionDeviceValidation.Validate(request.DTO);
        await InstitutionDeviceValidation.ValidateReferencesAsync(UnitOfWork, scope.InstitutionId, request.DTO.CampusId, request.DTO.DeviceMasterId, cancellationToken);
        await InstitutionDeviceValidation.ValidateUniqueDeviceCodeAsync(UnitOfWork, scope.InstitutionId, request.DTO.DeviceCode, null, cancellationToken);

        await UnitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var master = await UnitOfWork.DeviceMasterRepository.GetForUpdateAsync(request.DTO.DeviceMasterId, cancellationToken)
                ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidDeviceMaster);
            if (!master.IsActive || master.IsSoftDeleted || master.IsOccupied)
            {
                throw new ConflictException(AppConstants.ErrorMessages.DeviceMasterAlreadyRegisteredWithInstitution);
            }

            var entity = _mapper.Map<InstitutionDevice>(request.DTO);
            InstitutionDeviceValidation.ApplyNormalizedValues(entity, request.DTO);
            entity.InstitutionId = scope.InstitutionId;
            entity.IsSoftDeleted = false;
            entity.AddedById = scope.ActorId;
            entity.AddedDateTime = DateTime.UtcNow;
            master.IsOccupied = true;
            master.UpdatedById = scope.ActorId;
            master.UpdatedDateTime = DateTime.UtcNow;

            await UnitOfWork.InstitutionDeviceRepository.AddAsync(entity, cancellationToken);
            await UnitOfWork.SaveChangesAsync(cancellationToken);
            await UnitOfWork.CommitTransactionAsync(cancellationToken);

            var stored = await UnitOfWork.InstitutionDeviceRepository.GetByIdAsync(scope.InstitutionId, entity.Id, cancellationToken)
                ?? throw new NotFoundException(AppConstants.ErrorMessages.InstitutionDeviceNotFound);
            _logger.LogInformation("Created InstitutionDevice {InstitutionDeviceId} for Institution {InstitutionId} by actor {ActorId}.", entity.Id, scope.InstitutionId, scope.ActorId);
            return ApiResponse<InstitutionDeviceResponseDTO>.Success(MapDeviceResponse(_mapper, stored, scope), AppConstants.SuccessMessages.InstitutionDeviceCreated);
        }
        catch
        {
            await UnitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}

/// <summary>Handles Institution device installation updates while preserving separate configuration data.</summary>
public sealed class UpdateInstitutionDeviceCommandHandler : InstitutionDeviceAccessHandlerBase, IRequestHandler<UpdateInstitutionDeviceCommand, ApiResponse<InstitutionDeviceResponseDTO>>
{
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateInstitutionDeviceCommandHandler> _logger;

    public UpdateInstitutionDeviceCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, IIdEncoderService idEncoderService, ILogger<InstitutionConfigurationHandlerBase> institutionLogger, ILogger<UpdateInstitutionDeviceCommandHandler> logger)
        : base(unitOfWork, commonRequestService, idEncoderService, institutionLogger)
    {
        _mapper = mapper;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ApiResponse<InstitutionDeviceResponseDTO>> Handle(UpdateInstitutionDeviceCommand request, CancellationToken cancellationToken)
    {
        var scope = await ResolveHostDeviceAssignmentScopeAsync(request.DTO, cancellationToken);
        if (request.DTO is null) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        InstitutionDeviceValidation.Validate(request.DTO);
        var institutionDeviceId = DecodeIdentifier(request.DTO.Id, scope, "InstitutionDeviceId");

        var entity = await UnitOfWork.InstitutionDeviceRepository.GetForUpdateAsync(scope.InstitutionId, institutionDeviceId, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.InstitutionDeviceNotFound);
        await InstitutionDeviceValidation.ValidateReferencesAsync(UnitOfWork, scope.InstitutionId, request.DTO.CampusId, request.DTO.DeviceMasterId, cancellationToken);
        await InstitutionDeviceValidation.ValidateUniqueDeviceCodeAsync(UnitOfWork, scope.InstitutionId, request.DTO.DeviceCode, entity.Id, cancellationToken);
        if (!request.DTO.IsActive && entity.IsActive && await UnitOfWork.InstitutionDeviceRepository.HasActiveEnrollmentsAsync(entity.Id, cancellationToken))
        {
            throw new ConflictException(AppConstants.ErrorMessages.InstitutionDeviceEnrollmentInUse);
        }

        await UnitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (entity.DeviceMasterId != request.DTO.DeviceMasterId)
            {
                var replacementMaster = await UnitOfWork.DeviceMasterRepository.GetForUpdateAsync(request.DTO.DeviceMasterId, cancellationToken)
                    ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidDeviceMaster);
                if (!replacementMaster.IsActive || replacementMaster.IsSoftDeleted || replacementMaster.IsOccupied)
                {
                    throw new ConflictException(AppConstants.ErrorMessages.DeviceMasterAlreadyRegisteredWithInstitution);
                }

                var previousMaster = await UnitOfWork.DeviceMasterRepository.GetForUpdateAsync(entity.DeviceMasterId, cancellationToken)
                    ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidDeviceMaster);
                previousMaster.IsOccupied = false;
                previousMaster.UpdatedById = scope.ActorId;
                previousMaster.UpdatedDateTime = DateTime.UtcNow;
                replacementMaster.IsOccupied = true;
                replacementMaster.UpdatedById = scope.ActorId;
                replacementMaster.UpdatedDateTime = DateTime.UtcNow;
            }

            _mapper.Map(request.DTO, entity);
            InstitutionDeviceValidation.ApplyNormalizedValues(entity, request.DTO);
            entity.UpdatedById = scope.ActorId;
            entity.UpdatedDateTime = DateTime.UtcNow;
            await UnitOfWork.SaveChangesAsync(cancellationToken);
            await UnitOfWork.CommitTransactionAsync(cancellationToken);

            var stored = await UnitOfWork.InstitutionDeviceRepository.GetByIdAsync(scope.InstitutionId, entity.Id, cancellationToken)
                ?? throw new NotFoundException(AppConstants.ErrorMessages.InstitutionDeviceNotFound);
            _logger.LogInformation("Updated InstitutionDevice {InstitutionDeviceId} for Institution {InstitutionId} by actor {ActorId}.", entity.Id, scope.InstitutionId, scope.ActorId);
            return ApiResponse<InstitutionDeviceResponseDTO>.Success(MapDeviceResponse(_mapper, stored, scope), AppConstants.SuccessMessages.InstitutionDeviceUpdated);
        }
        catch
        {
            await UnitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}

/// <summary>Handles InstitutionDevice active-state changes.</summary>
public sealed class UpdateInstitutionDeviceStatusCommandHandler : InstitutionDeviceAccessHandlerBase, IRequestHandler<UpdateInstitutionDeviceStatusCommand, ApiResponse<InstitutionDeviceResponseDTO>>
{
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateInstitutionDeviceStatusCommandHandler> _logger;

    public UpdateInstitutionDeviceStatusCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, IIdEncoderService idEncoderService, ILogger<InstitutionConfigurationHandlerBase> institutionLogger, ILogger<UpdateInstitutionDeviceStatusCommandHandler> logger)
        : base(unitOfWork, commonRequestService, idEncoderService, institutionLogger)
    {
        _mapper = mapper;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ApiResponse<InstitutionDeviceResponseDTO>> Handle(UpdateInstitutionDeviceStatusCommand request, CancellationToken cancellationToken)
    {
        var scope = await ResolveHostDeviceAssignmentScopeAsync(request.DTO, cancellationToken);
        if (request.DTO is null) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        var institutionDeviceId = DecodeIdentifier(request.DTO.Id, scope, "InstitutionDeviceId");

        var entity = await UnitOfWork.InstitutionDeviceRepository.GetForUpdateAsync(scope.InstitutionId, institutionDeviceId, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.InstitutionDeviceNotFound);
        if (request.DTO.IsActive)
        {
            await InstitutionDeviceValidation.ValidateReferencesAsync(UnitOfWork, scope.InstitutionId, entity.CampusId, entity.DeviceMasterId, cancellationToken);
        }
        else if (await UnitOfWork.InstitutionDeviceRepository.HasActiveEnrollmentsAsync(entity.Id, cancellationToken))
        {
            throw new ConflictException(AppConstants.ErrorMessages.InstitutionDeviceEnrollmentInUse);
        }

        entity.IsActive = request.DTO.IsActive;
        entity.UpdatedById = scope.ActorId;
        entity.UpdatedDateTime = DateTime.UtcNow;
        await UnitOfWork.SaveChangesAsync(cancellationToken);

        var stored = await UnitOfWork.InstitutionDeviceRepository.GetByIdAsync(scope.InstitutionId, entity.Id, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.InstitutionDeviceNotFound);
        _logger.LogInformation("Changed InstitutionDevice {InstitutionDeviceId} status to {IsActive} for Institution {InstitutionId} by actor {ActorId}.", entity.Id, entity.IsActive, scope.InstitutionId, scope.ActorId);
        return ApiResponse<InstitutionDeviceResponseDTO>.Success(MapDeviceResponse(_mapper, stored, scope), AppConstants.SuccessMessages.InstitutionDeviceStatusUpdated);
    }
}

/// <summary>Handles InstitutionDevice soft deletion.</summary>
public sealed class DeleteInstitutionDeviceCommandHandler : InstitutionDeviceAccessHandlerBase, IRequestHandler<DeleteInstitutionDeviceCommand, ApiResponse<bool>>
{
    private readonly ILogger<DeleteInstitutionDeviceCommandHandler> _logger;

    public DeleteInstitutionDeviceCommandHandler(IUnitOfWork unitOfWork, ICommonRequestService commonRequestService, IIdEncoderService idEncoderService, ILogger<InstitutionConfigurationHandlerBase> institutionLogger, ILogger<DeleteInstitutionDeviceCommandHandler> logger)
        : base(unitOfWork, commonRequestService, idEncoderService, institutionLogger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ApiResponse<bool>> Handle(DeleteInstitutionDeviceCommand request, CancellationToken cancellationToken)
    {
        var scope = await ResolveHostDeviceAssignmentScopeAsync(request.AccessRequest, cancellationToken);
        var institutionDeviceId = DecodeIdentifier(request.EncryptedId, scope, "InstitutionDeviceId");
        var entity = await UnitOfWork.InstitutionDeviceRepository.GetForUpdateAsync(scope.InstitutionId, institutionDeviceId, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.InstitutionDeviceNotFound);
        if (await UnitOfWork.InstitutionDeviceRepository.HasEnrollmentsAsync(entity.Id, cancellationToken)) throw new ConflictException(AppConstants.ErrorMessages.InstitutionDeviceEnrollmentInUse);
        if (await UnitOfWork.InstitutionDeviceRepository.HasConfigurationAsync(scope.InstitutionId, entity.Id, cancellationToken)) throw new ConflictException(AppConstants.ErrorMessages.InstitutionDeviceConfigurationInUse);

        await UnitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var master = await UnitOfWork.DeviceMasterRepository.GetForUpdateAsync(entity.DeviceMasterId, cancellationToken);
            entity.IsSoftDeleted = true;
            entity.IsActive = false;
            entity.SoftDeletedById = scope.ActorId;
            entity.SoftDeletedDateTime = DateTime.UtcNow;
            if (master is not null)
            {
                master.IsOccupied = false;
                master.UpdatedById = scope.ActorId;
                master.UpdatedDateTime = DateTime.UtcNow;
            }

            await UnitOfWork.SaveChangesAsync(cancellationToken);
            await UnitOfWork.CommitTransactionAsync(cancellationToken);
            _logger.LogInformation("Soft deleted InstitutionDevice {InstitutionDeviceId} for Institution {InstitutionId} by actor {ActorId}.", entity.Id, scope.InstitutionId, scope.ActorId);
            return ApiResponse<bool>.Success(true, AppConstants.SuccessMessages.InstitutionDeviceDeleted);
        }
        catch
        {
            await UnitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}

/// <summary>Handles InstitutionDevice retrieval by identifier.</summary>
public sealed class GetInstitutionDeviceByIdQueryHandler : InstitutionDeviceAccessHandlerBase, IRequestHandler<GetInstitutionDeviceByIdQuery, ApiResponse<InstitutionDeviceResponseDTO>>
{
    private readonly IMapper _mapper;

    public GetInstitutionDeviceByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, IIdEncoderService idEncoderService, ILogger<InstitutionConfigurationHandlerBase> institutionLogger)
        : base(unitOfWork, commonRequestService, idEncoderService, institutionLogger) => _mapper = mapper;

    /// <inheritdoc />
    public async Task<ApiResponse<InstitutionDeviceResponseDTO>> Handle(GetInstitutionDeviceByIdQuery request, CancellationToken cancellationToken)
    {
        var scope = await ResolveInstitutionScopeAsync(request.AccessRequest, cancellationToken);
        var institutionDeviceId = DecodeIdentifier(request.EncryptedId, scope, "InstitutionDeviceId");
        var entity = await UnitOfWork.InstitutionDeviceRepository.GetByIdAsync(scope.InstitutionId, institutionDeviceId, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.InstitutionDeviceNotFound);
        return ApiResponse<InstitutionDeviceResponseDTO>.Success(MapDeviceResponse(_mapper, entity, scope), AppConstants.SuccessMessages.InstitutionDeviceRetrieved);
    }
}

/// <summary>Handles database-paged InstitutionDevice retrieval.</summary>
public sealed class GetAllInstitutionDevicesQueryHandler : InstitutionDeviceAccessHandlerBase, IRequestHandler<GetAllInstitutionDevicesQuery, ApiResponse<List<InstitutionDeviceResponseDTO>>>
{
    private readonly IMapper _mapper;

    public GetAllInstitutionDevicesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, IIdEncoderService idEncoderService, ILogger<InstitutionConfigurationHandlerBase> institutionLogger)
        : base(unitOfWork, commonRequestService, idEncoderService, institutionLogger) => _mapper = mapper;

    /// <inheritdoc />
    public async Task<ApiResponse<List<InstitutionDeviceResponseDTO>>> Handle(GetAllInstitutionDevicesQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter ?? new GetInstitutionDeviceListRequestDTO();
        var scope = await ResolveInstitutionListScopeAsync(filter, cancellationToken);
        var page = scope.InstitutionId.HasValue
            ? await UnitOfWork.InstitutionDeviceRepository.GetPagedAsync(scope.InstitutionId.Value, filter, cancellationToken)
            : await UnitOfWork.InstitutionDeviceRepository.GetHostPagedAsync(filter, cancellationToken);
        return ApiResponse<List<InstitutionDeviceResponseDTO>>.SuccessPaginated(
            page.Data.Select(entity => MapDeviceResponse(
                _mapper,
                entity,
                scope.InstitutionEncryptionKey,
                scope.UserType == LoginUserType.Host)).ToList(),
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            page.TotalPages,
            AppConstants.SuccessMessages.InstitutionDeviceRetrieved);
    }
}

#endregion

#region Institution Device Configuration and Runtime Handlers

/// <summary>Handles creation of a separate InstitutionDeviceConfiguration record.</summary>
public sealed class CreateInstitutionDeviceConfigurationCommandHandler : InstitutionDeviceAccessHandlerBase, IRequestHandler<CreateInstitutionDeviceConfigurationCommand, ApiResponse<InstitutionDeviceConfigurationResponseDTO>>
{
    private readonly IMapper _mapper;

    public CreateInstitutionDeviceConfigurationCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, IIdEncoderService idEncoderService, ILogger<InstitutionConfigurationHandlerBase> institutionLogger)
        : base(unitOfWork, commonRequestService, idEncoderService, institutionLogger) => _mapper = mapper;

    /// <inheritdoc />
    public async Task<ApiResponse<InstitutionDeviceConfigurationResponseDTO>> Handle(CreateInstitutionDeviceConfigurationCommand request, CancellationToken cancellationToken)
    {
        var scope = await ResolveAuthorizedInstitutionConfigurationScopeAsync(request.DTO, cancellationToken);
        InstitutionDeviceConfigurationValidation.Validate(request.DTO);
        var institutionDeviceId = DecodeIdentifier(request.DTO.InstitutionDeviceId, scope, "InstitutionDeviceId");
        var institutionDevice = await UnitOfWork.InstitutionDeviceRepository.GetForUpdateAsync(scope.InstitutionId, institutionDeviceId, cancellationToken)
            ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InstitutionDeviceNotFound);
        if (!institutionDevice.IsActive)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InstitutionDeviceNotFound);
        }

        var deviceMaster = await UnitOfWork.DeviceMasterRepository.GetForUpdateAsync(institutionDevice.DeviceMasterId, cancellationToken)
            ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InstitutionDeviceNotFound);
        InstitutionDeviceConfigurationValidation.ValidateDeviceTransport(deviceMaster, request.DTO);
        if (await UnitOfWork.InstitutionDeviceConfigurationRepository.ExistsForInstitutionDeviceAsync(scope.InstitutionId, institutionDeviceId, null, cancellationToken)) throw new ConflictException(AppConstants.ErrorMessages.InstitutionDeviceConfigurationAlreadyExists);

        var entity = _mapper.Map<InstitutionDeviceConfiguration>(request.DTO);
        entity.InstitutionDeviceId = institutionDeviceId;
        InstitutionDeviceConfigurationValidation.ApplyNormalizedValues(entity, request.DTO);
        entity.AddedById = scope.ActorId;
        entity.AddedDateTime = DateTime.UtcNow;
        await UnitOfWork.InstitutionDeviceConfigurationRepository.AddAsync(entity, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);

        var stored = await UnitOfWork.InstitutionDeviceConfigurationRepository.GetByIdAsync(scope.InstitutionId, entity.Id, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.InstitutionDeviceConfigurationNotFound);
        return ApiResponse<InstitutionDeviceConfigurationResponseDTO>.Success(MapConfigurationResponse(_mapper, stored, scope), AppConstants.SuccessMessages.InstitutionDeviceConfigurationCreated);
    }
}

/// <summary>Handles updates to a separate InstitutionDeviceConfiguration record.</summary>
public sealed class UpdateInstitutionDeviceConfigurationCommandHandler : InstitutionDeviceAccessHandlerBase, IRequestHandler<UpdateInstitutionDeviceConfigurationCommand, ApiResponse<InstitutionDeviceConfigurationResponseDTO>>
{
    private readonly IMapper _mapper;

    public UpdateInstitutionDeviceConfigurationCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, IIdEncoderService idEncoderService, ILogger<InstitutionConfigurationHandlerBase> institutionLogger)
        : base(unitOfWork, commonRequestService, idEncoderService, institutionLogger) => _mapper = mapper;

    /// <inheritdoc />
    public async Task<ApiResponse<InstitutionDeviceConfigurationResponseDTO>> Handle(UpdateInstitutionDeviceConfigurationCommand request, CancellationToken cancellationToken)
    {
        var scope = await ResolveAuthorizedInstitutionConfigurationScopeAsync(request.DTO, cancellationToken);
        if (request.DTO is null) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        InstitutionDeviceConfigurationValidation.Validate(request.DTO);
        var institutionDeviceConfigurationId = DecodeIdentifier(request.DTO.Id, scope, "InstitutionDeviceConfigurationId");
        var institutionDeviceId = DecodeIdentifier(request.DTO.InstitutionDeviceId, scope, "InstitutionDeviceId");

        var entity = await UnitOfWork.InstitutionDeviceConfigurationRepository.GetForUpdateAsync(scope.InstitutionId, institutionDeviceConfigurationId, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.InstitutionDeviceConfigurationNotFound);
        var institutionDevice = await UnitOfWork.InstitutionDeviceRepository.GetForUpdateAsync(scope.InstitutionId, institutionDeviceId, cancellationToken)
            ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InstitutionDeviceNotFound);
        if (!institutionDevice.IsActive)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InstitutionDeviceNotFound);
        }

        var deviceMaster = await UnitOfWork.DeviceMasterRepository.GetForUpdateAsync(institutionDevice.DeviceMasterId, cancellationToken)
            ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InstitutionDeviceNotFound);
        InstitutionDeviceConfigurationValidation.ValidateDeviceTransport(deviceMaster, request.DTO);
        if (await UnitOfWork.InstitutionDeviceConfigurationRepository.ExistsForInstitutionDeviceAsync(scope.InstitutionId, institutionDeviceId, entity.Id, cancellationToken)) throw new ConflictException(AppConstants.ErrorMessages.InstitutionDeviceConfigurationAlreadyExists);

        _mapper.Map(request.DTO, entity);
        entity.InstitutionDeviceId = institutionDeviceId;
        InstitutionDeviceConfigurationValidation.ApplyNormalizedValues(entity, request.DTO);
        if (entity.CommandTransport != (short)DeviceCommunicationProtocol.Https)
        {
            // A stale token must never remain usable after a transport change.
            entity.HttpsIngressTokenHash = null;
        }
        entity.UpdatedById = scope.ActorId;
        entity.UpdatedDateTime = DateTime.UtcNow;
        await UnitOfWork.SaveChangesAsync(cancellationToken);

        var stored = await UnitOfWork.InstitutionDeviceConfigurationRepository.GetByIdAsync(scope.InstitutionId, entity.Id, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.InstitutionDeviceConfigurationNotFound);
        return ApiResponse<InstitutionDeviceConfigurationResponseDTO>.Success(MapConfigurationResponse(_mapper, stored, scope), AppConstants.SuccessMessages.InstitutionDeviceConfigurationUpdated);
    }
}

/// <summary>
/// Generates a new opaque device-gateway URL exactly once. Only its SHA-256 hash
/// is retained, so a later configuration read cannot disclose the bearer token.
/// </summary>
public sealed class RotateInstitutionDeviceHttpsIngressTokenCommandHandler : InstitutionDeviceAccessHandlerBase,
    IRequestHandler<RotateInstitutionDeviceHttpsIngressTokenCommand, ApiResponse<InstitutionDeviceHttpsIngressEndpointResponseDTO>>
{
    public RotateInstitutionDeviceHttpsIngressTokenCommandHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService,
        IIdEncoderService idEncoderService,
        ILogger<InstitutionConfigurationHandlerBase> institutionLogger)
        : base(unitOfWork, commonRequestService, idEncoderService, institutionLogger) { }

    /// <inheritdoc />
    public async Task<ApiResponse<InstitutionDeviceHttpsIngressEndpointResponseDTO>> Handle(
        RotateInstitutionDeviceHttpsIngressTokenCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DTO is null)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        var scope = await ResolveAuthorizedInstitutionConfigurationScopeAsync(request.DTO, cancellationToken);
        var hasConfigurationId = !string.IsNullOrWhiteSpace(request.DTO.InstitutionDeviceConfigurationId);
        var hasInstitutionDeviceId = !string.IsNullOrWhiteSpace(request.DTO.InstitutionDeviceId);
        if (hasConfigurationId == hasInstitutionDeviceId)
        {
            throw new ValidationErrorException(
                "Provide exactly one encrypted InstitutionDeviceConfigurationId or InstitutionDeviceId.");
        }

        if (scope.UserType == LoginUserType.Host && !hasInstitutionDeviceId)
        {
            throw new ValidationErrorException("Host URL issuance requires an encrypted InstitutionDeviceId.");
        }

        if (scope.UserType == LoginUserType.InstitutionStaffMember && !hasConfigurationId)
        {
            throw new ValidationErrorException("Institution URL rotation requires an encrypted InstitutionDeviceConfigurationId.");
        }

        InstitutionDeviceConfiguration? configuration;
        if (hasConfigurationId)
        {
            var institutionDeviceConfigurationId = DecodeIdentifier(
                request.DTO.InstitutionDeviceConfigurationId,
                scope,
                "InstitutionDeviceConfigurationId");
            configuration = await UnitOfWork.InstitutionDeviceConfigurationRepository.GetForUpdateAsync(
                scope.InstitutionId,
                institutionDeviceConfigurationId,
                cancellationToken);
        }
        else
        {
            var institutionDeviceId = DecodeIdentifier(request.DTO.InstitutionDeviceId, scope, "InstitutionDeviceId");
            configuration = await UnitOfWork.InstitutionDeviceConfigurationRepository.GetForUpdateByInstitutionDeviceAsync(
                scope.InstitutionId,
                institutionDeviceId,
                cancellationToken);
        }

        if (configuration is null)
        {
            throw new NotFoundException(AppConstants.ErrorMessages.InstitutionDeviceConfigurationNotFound);
        }

        var transport = configuration.CommandTransport ?? configuration.MqttTransport;
        if (transport != (short)DeviceCommunicationProtocol.Https ||
            !DeviceHttpsGatewaySecurity.IsValidGatewayPath(configuration.ServerPath) ||
            !Uri.TryCreate(configuration.ServerUrl, UriKind.Absolute, out var serverUri) ||
            !string.Equals(serverUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationErrorException("Configure this device for HTTPS polling before generating its gateway URL.");
        }

        var deviceMaster = await UnitOfWork.DeviceMasterRepository.GetByIdAsync(
            configuration.InstitutionDevice.DeviceMasterId,
            cancellationToken);
        if (deviceMaster is null || !deviceMaster.IsActive || deviceMaster.IsSoftDeleted || !deviceMaster.SupportsHttps)
        {
            throw new ValidationErrorException("The selected device model does not support HTTPS polling.");
        }

        var gatewayUrl = InstitutionDeviceConfigurationValidation.IssueHttpsGatewayUrl(configuration);
        configuration.UpdatedById = scope.ActorId;
        configuration.UpdatedDateTime = DateTime.UtcNow;
        await UnitOfWork.SaveChangesAsync(cancellationToken);

        // Never log the generated URL or token. The caller receives it once and
        // must install it into the device's Server Domain Name field immediately.
        return ApiResponse<InstitutionDeviceHttpsIngressEndpointResponseDTO>.Success(
            new InstitutionDeviceHttpsIngressEndpointResponseDTO { GatewayUrl = gatewayUrl },
            "HTTPS device gateway URL generated. It will not be shown again.");
    }
}

/// <summary>Hard deletes a InstitutionDeviceConfiguration record.</summary>
public sealed class DeleteInstitutionDeviceConfigurationCommandHandler : InstitutionDeviceAccessHandlerBase, IRequestHandler<DeleteInstitutionDeviceConfigurationCommand, ApiResponse<bool>>
{
    public DeleteInstitutionDeviceConfigurationCommandHandler(IUnitOfWork unitOfWork, ICommonRequestService commonRequestService, IIdEncoderService idEncoderService, ILogger<InstitutionConfigurationHandlerBase> institutionLogger)
        : base(unitOfWork, commonRequestService, idEncoderService, institutionLogger) { }

    /// <inheritdoc />
    public async Task<ApiResponse<bool>> Handle(DeleteInstitutionDeviceConfigurationCommand request, CancellationToken cancellationToken)
    {
        var scope = await ResolveAuthorizedInstitutionConfigurationScopeAsync(request.AccessRequest, cancellationToken);
        var institutionDeviceConfigurationId = DecodeIdentifier(request.EncryptedId, scope, "InstitutionDeviceConfigurationId");
        var entity = await UnitOfWork.InstitutionDeviceConfigurationRepository.GetForUpdateAsync(scope.InstitutionId, institutionDeviceConfigurationId, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.InstitutionDeviceConfigurationNotFound);
        UnitOfWork.InstitutionDeviceConfigurationRepository.Remove(entity);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return ApiResponse<bool>.Success(true, AppConstants.SuccessMessages.InstitutionDeviceConfigurationDeleted);
    }
}

/// <summary>Handles InstitutionDeviceConfiguration retrieval by identifier.</summary>
public sealed class GetInstitutionDeviceConfigurationByIdQueryHandler : InstitutionDeviceAccessHandlerBase, IRequestHandler<GetInstitutionDeviceConfigurationByIdQuery, ApiResponse<InstitutionDeviceConfigurationResponseDTO>>
{
    private readonly IMapper _mapper;

    public GetInstitutionDeviceConfigurationByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, IIdEncoderService idEncoderService, ILogger<InstitutionConfigurationHandlerBase> institutionLogger)
        : base(unitOfWork, commonRequestService, idEncoderService, institutionLogger) => _mapper = mapper;

    /// <inheritdoc />
    public async Task<ApiResponse<InstitutionDeviceConfigurationResponseDTO>> Handle(GetInstitutionDeviceConfigurationByIdQuery request, CancellationToken cancellationToken)
    {
        var scope = await ResolveAuthorizedInstitutionConfigurationScopeAsync(request.AccessRequest, cancellationToken);
        var institutionDeviceConfigurationId = DecodeIdentifier(request.EncryptedId, scope, "InstitutionDeviceConfigurationId");
        var entity = await UnitOfWork.InstitutionDeviceConfigurationRepository.GetByIdAsync(scope.InstitutionId, institutionDeviceConfigurationId, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.InstitutionDeviceConfigurationNotFound);
        return ApiResponse<InstitutionDeviceConfigurationResponseDTO>.Success(MapConfigurationResponse(_mapper, entity, scope), AppConstants.SuccessMessages.InstitutionDeviceConfigurationRetrieved);
    }
}

/// <summary>Handles database-paged InstitutionDeviceConfiguration retrieval.</summary>
public sealed class GetAllInstitutionDeviceConfigurationsQueryHandler : InstitutionDeviceAccessHandlerBase, IRequestHandler<GetAllInstitutionDeviceConfigurationsQuery, ApiResponse<List<InstitutionDeviceConfigurationResponseDTO>>>
{
    private readonly IMapper _mapper;

    public GetAllInstitutionDeviceConfigurationsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, ICommonRequestService commonRequestService, IIdEncoderService idEncoderService, ILogger<InstitutionConfigurationHandlerBase> institutionLogger)
        : base(unitOfWork, commonRequestService, idEncoderService, institutionLogger) => _mapper = mapper;

    /// <inheritdoc />
    public async Task<ApiResponse<List<InstitutionDeviceConfigurationResponseDTO>>> Handle(GetAllInstitutionDeviceConfigurationsQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter ?? new GetInstitutionDeviceConfigurationListRequestDTO();
        var scope = await ResolveAuthorizedInstitutionConfigurationListScopeAsync(filter, cancellationToken);
        filter.ResolvedInstitutionDeviceId = string.IsNullOrWhiteSpace(filter.InstitutionDeviceId)
            ? null
            : DecodeIdentifier(filter.InstitutionDeviceId, scope.InstitutionEncryptionKey, "InstitutionDeviceId");
        var page = scope.InstitutionId.HasValue
            ? await UnitOfWork.InstitutionDeviceConfigurationRepository.GetPagedAsync(scope.InstitutionId.Value, filter, cancellationToken)
            : await UnitOfWork.InstitutionDeviceConfigurationRepository.GetHostPagedAsync(filter, cancellationToken);
        return ApiResponse<List<InstitutionDeviceConfigurationResponseDTO>>.SuccessPaginated(
            page.Data.Select(entity => MapConfigurationResponse(
                _mapper,
                entity,
                scope.InstitutionEncryptionKey,
                scope.UserType == LoginUserType.Host)).ToList(),
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            page.TotalPages,
            AppConstants.SuccessMessages.InstitutionDeviceConfigurationRetrieved);
    }
}

/// <summary>Handles Host issuance of an initial device bootstrap URL.</summary>
public sealed class IssueInitialDeviceBootstrapCommandHandler(
    ICommonRequestService commonRequestService,
    IDeviceInitialProvisioningService initialProvisioningService)
    : IRequestHandler<IssueInitialDeviceBootstrapCommand, ApiResponse<InitialDeviceBootstrapResponseDTO>>
{
    /// <inheritdoc />
    public async Task<ApiResponse<InitialDeviceBootstrapResponseDTO>> Handle(
        IssueInitialDeviceBootstrapCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.DTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        if (dto.DeviceMasterId <= 0)
        {
            throw new ValidationErrorException("A valid physical device is required.");
        }

        var hostContext = await commonRequestService.ValidateHostUserPermissionRequestAsync();
        var issue = await initialProvisioningService.IssueAsync(
            dto.DeviceMasterId,
            dto.LifetimeMinutes,
            hostContext.HostUserId,
            cancellationToken);

        return ApiResponse<InitialDeviceBootstrapResponseDTO>.Success(
            new InitialDeviceBootstrapResponseDTO
            {
                DeviceSerialNumber = issue.DeviceSerialNumber,
                InitialGatewayUrl = issue.InitialGatewayUrl,
                HeartbeatIntervalSeconds = issue.HeartbeatIntervalSeconds,
                ExpiresDateTime = issue.ExpiresDateTime
            },
            "Initial device gateway URL generated. It will not be shown again.");
    }
}

/// <summary>Handles strongly typed Institution-admin runtime configuration for an HTTPS device.</summary>
public sealed class ApplyInstitutionDeviceRuntimeConfigurationCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService,
    IDeviceCommandSubmissionService deviceCommandSubmissionService,
    ILogger<InstitutionConfigurationHandlerBase> institutionLogger)
    : InstitutionDeviceAccessHandlerBase(unitOfWork, commonRequestService, idEncoderService, institutionLogger),
        IRequestHandler<ApplyInstitutionDeviceRuntimeConfigurationCommand, ApiResponse<InstitutionDeviceRuntimeConfigurationResponseDTO>>
{
    /// <inheritdoc />
    public async Task<ApiResponse<InstitutionDeviceRuntimeConfigurationResponseDTO>> Handle(
        ApplyInstitutionDeviceRuntimeConfigurationCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.DTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        var scope = await ResolveAuthorizedInstitutionConfigurationScopeAsync(request.DTO, cancellationToken);
        InstitutionDeviceConfigurationValidation.ValidateRuntimeConfiguration(dto);
        var institutionDeviceId = DecodeIdentifier(dto.InstitutionDeviceId, scope, "InstitutionDeviceId");

        var configuration = await UnitOfWork.InstitutionDeviceConfigurationRepository.GetForUpdateByInstitutionDeviceAsync(
                scope.InstitutionId,
                institutionDeviceId,
                cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.InstitutionDeviceConfigurationNotFound);

        InstitutionDeviceConfigurationValidation.EnsureHttpsGateway(configuration);
        var deviceMaster = await UnitOfWork.DeviceMasterRepository.GetByIdAsync(
            configuration.InstitutionDevice.DeviceMasterId,
            cancellationToken);
        if (deviceMaster is null || !deviceMaster.IsActive || deviceMaster.IsSoftDeleted || !deviceMaster.SupportsHttps ||
            !configuration.InstitutionDevice.IsActive || configuration.InstitutionDevice.IsSoftDeleted)
        {
            throw new ValidationErrorException("The Institution device must be active and support HTTPS before runtime settings can be queued.");
        }

        await UnitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            string? initialNormalGatewayUrl = null;
            if (string.IsNullOrWhiteSpace(configuration.HttpsIngressTokenHash))
            {
                initialNormalGatewayUrl = InstitutionDeviceConfigurationValidation.IssueHttpsGatewayUrl(configuration);
            }

            configuration.HeartbeatIntervalSeconds = dto.HeartbeatIntervalSeconds;
            configuration.Configuration = JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                heartbeatIntervalSeconds = dto.HeartbeatIntervalSeconds,
                volume = dto.Volume,
                localWebServerEnabled = false,
                managedBy = "InstitutionAdmin"
            });
            configuration.UpdatedById = scope.ActorId;
            configuration.UpdatedDateTime = DateTime.UtcNow;
            await UnitOfWork.SaveChangesAsync(cancellationToken);

            var configurationCommand = await deviceCommandSubmissionService.SubmitAsync(
                new DeviceCommandSubmission(
                    scope.InstitutionId,
                    institutionDeviceId,
                    DeviceCommands.SetDeviceInfo,
                    InstitutionDeviceConfigurationValidation.BuildSetDeviceInfoPayload(dto, initialNormalGatewayUrl),
                    scope.ActorId,
                    ProtectPayload: true),
                cancellationToken);

            DeviceCommandSubmissionResult? rebootCommand = null;
            if (dto.RebootAfterApply)
            {
                rebootCommand = await deviceCommandSubmissionService.SubmitAsync(
                    new DeviceCommandSubmission(
                        scope.InstitutionId,
                        institutionDeviceId,
                        DeviceCommands.Reboot,
                        JsonSerializer.Serialize(new { cmd = DeviceCommands.Reboot }),
                        scope.ActorId),
                    cancellationToken);
            }

            await UnitOfWork.CommitTransactionAsync(cancellationToken);
            return ApiResponse<InstitutionDeviceRuntimeConfigurationResponseDTO>.Success(
                new InstitutionDeviceRuntimeConfigurationResponseDTO
                {
                    ConfigurationCommandId = configurationCommand.DeviceCommandId,
                    ConfigurationTrackingId = configurationCommand.InternalTrackingId,
                    RebootCommandId = rebootCommand?.DeviceCommandId,
                    RebootTrackingId = rebootCommand?.InternalTrackingId,
                    Status = configurationCommand.Status.ToString()
                },
                "Institution device configuration has been queued securely.");
        }
        catch
        {
            await UnitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}

/// <summary>Handles a Institution-admin reboot without the generic raw command endpoint.</summary>
public sealed class RebootInstitutionDeviceCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService,
    IDeviceCommandSubmissionService deviceCommandSubmissionService,
    ILogger<InstitutionConfigurationHandlerBase> institutionLogger)
    : InstitutionDeviceAccessHandlerBase(unitOfWork, commonRequestService, idEncoderService, institutionLogger),
        IRequestHandler<RebootInstitutionDeviceCommand, ApiResponse<DeviceCommandSubmissionResponseDTO>>
{
    /// <inheritdoc />
    public async Task<ApiResponse<DeviceCommandSubmissionResponseDTO>> Handle(
        RebootInstitutionDeviceCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.DTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        var scope = await ResolveAuthorizedInstitutionConfigurationScopeAsync(request.DTO, cancellationToken);
        var institutionDeviceId = DecodeIdentifier(dto.InstitutionDeviceId, scope, "InstitutionDeviceId");
        var result = await deviceCommandSubmissionService.SubmitAsync(
            new DeviceCommandSubmission(
                scope.InstitutionId,
                institutionDeviceId,
                DeviceCommands.Reboot,
                JsonSerializer.Serialize(new { cmd = DeviceCommands.Reboot }),
                scope.ActorId),
            cancellationToken);

        return ApiResponse<DeviceCommandSubmissionResponseDTO>.Success(
            new DeviceCommandSubmissionResponseDTO
            {
                DeviceCommandId = result.DeviceCommandId,
                InternalTrackingId = result.InternalTrackingId,
                Status = result.Status.ToString()
            },
            "Institution device reboot has been queued securely.");
    }
}

#endregion

#region Validation

internal static class InstitutionDeviceValidation
{
    internal static void Validate(InstitutionDeviceRequestDTO? dto)
    {
        if (dto is null || dto.CampusId <= 0 || dto.DeviceMasterId <= 0 || string.IsNullOrWhiteSpace(dto.DeviceCode))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }
    }

    internal static async Task ValidateReferencesAsync(IUnitOfWork unitOfWork, long institutionId, long campusId, long deviceMasterId, CancellationToken cancellationToken)
    {
        if (!await unitOfWork.InstitutionDeviceRepository.IsEligibleInstitutionAsync(institutionId, cancellationToken)) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidDeviceManagementInstitution);
        if (!await unitOfWork.InstitutionDeviceRepository.IsActiveCampusAsync(campusId, cancellationToken)) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidDeviceManagementCampus);
        if (!await unitOfWork.InstitutionDeviceRepository.CampusBelongsToInstitutionAsync(institutionId, campusId, cancellationToken)) throw new ValidationErrorException(AppConstants.ErrorMessages.CampusDoesNotBelongToInstitution);
        if (!await unitOfWork.InstitutionDeviceRepository.IsEligibleDeviceMasterAsync(deviceMasterId, cancellationToken)) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidDeviceMaster);
    }

    internal static async Task ValidateUniqueDeviceCodeAsync(IUnitOfWork unitOfWork, long institutionId, string deviceCode, long? excludeId, CancellationToken cancellationToken)
    {
        if (await unitOfWork.InstitutionDeviceRepository.DeviceCodeExistsAsync(institutionId, deviceCode, excludeId, cancellationToken)) throw new ConflictException(AppConstants.ErrorMessages.DuplicateInstitutionDeviceCode);
    }

    internal static void ApplyNormalizedValues(InstitutionDevice entity, InstitutionDeviceRequestDTO dto)
    {
        entity.DeviceCode = dto.DeviceCode.Trim();
        entity.DeviceName = Normalize(dto.DeviceName);
        entity.InstallationRemark = Normalize(dto.InstallationRemark);
        entity.Description = Normalize(dto.Description);
        entity.Remark = Normalize(dto.Remark);
    }

    internal static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

internal static class InstitutionDeviceConfigurationValidation
{
    internal static void Validate(InstitutionDeviceConfigurationRequestDTO? dto)
    {
        var transport = ResolveTransport(dto);
        if (dto is null ||
            string.IsNullOrWhiteSpace(dto.InstitutionDeviceId) ||
            !transport.HasValue ||
            !Enum.IsDefined(transport.Value) ||
            (dto.DevicePort.HasValue && dto.DevicePort <= 0) ||
            (dto.ServerPort.HasValue && dto.ServerPort <= 0) ||
            (dto.HeartbeatIntervalSeconds.HasValue && dto.HeartbeatIntervalSeconds <= 0))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        if (!string.IsNullOrWhiteSpace(dto.Configuration))
        {
            try
            {
                using var document = JsonDocument.Parse(dto.Configuration);
                EnsureNoPlainCredentialMaterial(document.RootElement);
            }
            catch (JsonException)
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
            }
        }

        if (transport == DeviceCommunicationProtocol.Https)
        {
            if (!Uri.TryCreate(dto.ServerUrl, UriKind.Absolute, out var serverUri) ||
                !string.Equals(serverUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                !DeviceHttpsGatewaySecurity.IsValidGatewayPath(dto.ServerPath) ||
                dto.ServerPort is not null and not 443 ||
                dto.HeartbeatIntervalSeconds is < 10 or > 3600)
            {
                throw new ValidationErrorException(
                    "HTTPS polling requires an absolute HTTPS ServerUrl, the /device-gateway path, port 443, and a 10–3600 second heartbeat.");
            }
        }
    }

    /// <summary>
    /// Ensures a selected transport is both catalog-confirmed for the physical
    /// device and backed by an enabled Skolenza device adapter.
    /// </summary>
    internal static void ValidateDeviceTransport(DeviceMaster deviceMaster, InstitutionDeviceConfigurationRequestDTO dto)
    {
        ArgumentNullException.ThrowIfNull(deviceMaster);
        var transport = ResolveTransport(dto)
            ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);

        var deviceSupportsTransport = transport switch
        {
            DeviceCommunicationProtocol.Mqtt => deviceMaster.SupportsMqtt,
            DeviceCommunicationProtocol.Mqtts => deviceMaster.SupportsMqtts,
            DeviceCommunicationProtocol.Https => deviceMaster.SupportsHttps,
            DeviceCommunicationProtocol.Http => deviceMaster.SupportsHttp,
            DeviceCommunicationProtocol.WebSocket or DeviceCommunicationProtocol.WebSocketSecure => deviceMaster.SupportsWebSocket,
            _ => false
        };
        if (!deviceMaster.IsActive || deviceMaster.IsSoftDeleted || !deviceSupportsTransport)
        {
            throw new ValidationErrorException("The selected physical device does not support the requested command transport.");
        }

        if (transport is not (DeviceCommunicationProtocol.Mqtt or DeviceCommunicationProtocol.Mqtts or DeviceCommunicationProtocol.Https))
        {
            throw new ValidationErrorException("The requested device transport does not yet have an enabled Skolenza transport adapter.");
        }

        if (transport is DeviceCommunicationProtocol.Mqtt or DeviceCommunicationProtocol.Mqtts &&
            (string.IsNullOrWhiteSpace(dto.ServerHost) || dto.ServerPort is null or < 1 or > 65535))
        {
            throw new ValidationErrorException("MQTT and MQTTS configurations require the broker host and a valid broker port.");
        }
    }

    private static DeviceCommunicationProtocol? ResolveTransport(InstitutionDeviceConfigurationRequestDTO? dto)
    {
        if (dto is null)
        {
            return null;
        }

        if (dto.CommandTransport.HasValue && dto.MqttTransport.HasValue &&
            dto.CommandTransport.Value != dto.MqttTransport.Value)
        {
            throw new ValidationErrorException("Send either CommandTransport or the legacy MqttTransport value, not conflicting values.");
        }

        if (dto.MqttTransport.HasValue &&
            dto.MqttTransport.Value is not DeviceCommunicationProtocol.Mqtt and not DeviceCommunicationProtocol.Mqtts)
        {
            throw new ValidationErrorException("The legacy MqttTransport field accepts only MQTT or MQTTS. Use CommandTransport for HTTPS or WebSocket.");
        }

        return dto.CommandTransport ?? dto.MqttTransport;
    }

    private static void EnsureNoPlainCredentialMaterial(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Contains("credential", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Contains("apikey", StringComparison.OrdinalIgnoreCase) ||
                    property.Name.Contains("token", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ValidationErrorException("Device credentials must be stored only in the encrypted DeviceCredential record.");
                }

                EnsureNoPlainCredentialMaterial(property.Value);
            }

            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                EnsureNoPlainCredentialMaterial(item);
            }
        }
    }

    internal static void ApplyNormalizedValues(InstitutionDeviceConfiguration entity, InstitutionDeviceConfigurationRequestDTO dto)
    {
        entity.IpAddress = InstitutionDeviceValidation.Normalize(dto.IpAddress);
        entity.MacAddress = InstitutionDeviceValidation.Normalize(dto.MacAddress);
        entity.ServerHost = InstitutionDeviceValidation.Normalize(dto.ServerHost);
        entity.ServerPath = InstitutionDeviceValidation.Normalize(dto.ServerPath);
        entity.ServerUrl = InstitutionDeviceValidation.Normalize(dto.ServerUrl);
        entity.PushMode = InstitutionDeviceValidation.Normalize(dto.PushMode);
        entity.TimeZoneId = InstitutionDeviceValidation.Normalize(dto.TimeZoneId);
        entity.Configuration = InstitutionDeviceValidation.Normalize(dto.Configuration);
    }

    /// <summary>Validates the approved runtime fields before a protected vendor command is queued.</summary>
    internal static void ValidateRuntimeConfiguration(ApplyInstitutionDeviceRuntimeConfigurationRequestDTO dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.NewWebServerPassword))
        {
            throw new ValidationErrorException(
                "Use the typed settings/web-access endpoint to change the local Web UI/API password.");
        }

        if (string.IsNullOrWhiteSpace(dto.InstitutionDeviceId) ||
            string.IsNullOrWhiteSpace(dto.CurrentWebServerPassword) ||
            dto.CurrentWebServerPassword.Length is < 4 or > 128 ||
            dto.HeartbeatIntervalSeconds is < 10 or > 3600 ||
            dto.Volume is < 0 or > 15)
        {
            throw new ValidationErrorException(
                "A valid device password, 10–3600 second heartbeat, and volume 0–15 are required.");
        }
    }

    /// <summary>Ensures the stored connection is an HTTPS device gateway before runtime commands are allowed.</summary>
    internal static void EnsureHttpsGateway(InstitutionDeviceConfiguration configuration)
    {
        var transport = configuration.CommandTransport ?? configuration.MqttTransport;
        if (transport != (short)DeviceCommunicationProtocol.Https ||
            !Uri.TryCreate(configuration.ServerUrl, UriKind.Absolute, out var serverUri) ||
            !string.Equals(serverUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !DeviceHttpsGatewaySecurity.IsValidGatewayPath(configuration.ServerPath) ||
            configuration.ServerPort is not null and not 443)
        {
            throw new ValidationErrorException(
                "Configure this Institution device for HTTPS, port 443, and the /device-gateway server path before applying runtime settings.");
        }
    }

    /// <summary>
    /// Issues a one-time normal HTTPS gateway URL and persists only its token hash
    /// on the supplied configuration. The caller returns the raw URL exactly once.
    /// </summary>
    internal static string IssueHttpsGatewayUrl(InstitutionDeviceConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        EnsureHttpsGateway(configuration);

        var ingressToken = DeviceHttpsGatewaySecurity.GenerateIngressToken();
        configuration.HttpsIngressTokenHash = DeviceHttpsGatewaySecurity.HashIngressToken(ingressToken);
        return $"{configuration.ServerUrl!.TrimEnd('/')}{DeviceHttpsGatewaySecurity.RoutePrefix}/{ingressToken}";
    }

    /// <summary>Builds the approved vendor request without persisting plaintext credentials.</summary>
    internal static string BuildSetDeviceInfoPayload(
        ApplyInstitutionDeviceRuntimeConfigurationRequestDTO dto,
        string? initialNormalGatewayUrl)
    {
        var payload = new Dictionary<string, object?>
        {
            ["cmd"] = DeviceCommands.SetDeviceInfo,
            ["password"] = dto.CurrentWebServerPassword,
            ["nowtime"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ["server_response_time"] = dto.HeartbeatIntervalSeconds
        };

        if (dto.Volume.HasValue)
        {
            payload["volume"] = dto.Volume.Value;
        }

        if (!string.IsNullOrWhiteSpace(initialNormalGatewayUrl))
        {
            payload["use_bs"] = 1;
            payload["use_domain_name"] = 1;
            payload["bs_domain_name"] = initialNormalGatewayUrl;
            payload["serverport"] = 443;
        }

        return JsonSerializer.Serialize(payload);
    }
}

#endregion
