// ================================================================
// Purpose : Manages Institution-specific SMTP configuration without exposing SMTP secrets.
// ================================================================

using System.Net.Mail;
using AutoMapper;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOS.Configruations;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.InstitutionEmailConfigCmd.Handlers;

#region Requests

public sealed class CreateInstitutionEmailConfigCommand(CreateInstitutionEmailConfigRequestDTO? dto)
    : IRequest<ApiResponse<InstitutionEmailConfigResponseDTO>>
{
    public CreateInstitutionEmailConfigRequestDTO? DTO { get; } = dto;
}

public sealed class UpdateInstitutionEmailConfigCommand(UpdateInstitutionEmailConfigRequestDTO? dto)
    : IRequest<ApiResponse<InstitutionEmailConfigResponseDTO>>
{
    public UpdateInstitutionEmailConfigRequestDTO? DTO { get; } = dto;
}

public sealed class DeleteInstitutionEmailConfigCommand(int id, InstitutionEmailConfigAccessRequestDTO? accessRequest)
    : IRequest<ApiResponse<bool>>
{
    public int Id { get; } = id;
    public InstitutionEmailConfigAccessRequestDTO? AccessRequest { get; } = accessRequest;
}

public sealed class GetInstitutionEmailConfigByIdQuery(int id, InstitutionEmailConfigAccessRequestDTO? accessRequest)
    : IRequest<ApiResponse<InstitutionEmailConfigResponseDTO>>
{
    public int Id { get; } = id;
    public InstitutionEmailConfigAccessRequestDTO? AccessRequest { get; } = accessRequest;
}

public sealed class GetAllInstitutionEmailConfigsQuery(InstitutionEmailConfigAccessRequestDTO? filter)
    : IRequest<ApiResponse<List<InstitutionEmailConfigResponseDTO>>>
{
    public InstitutionEmailConfigAccessRequestDTO? Filter { get; } = filter;
}

#endregion

#region Shared access

/// <summary>
/// Carries both the caller-facing key used to protect Institution identifiers and
/// the Institution-owned key used to encrypt SMTP secrets at rest. Host callers
/// have a distinct caller key, so the two must never be conflated.
/// </summary>
public sealed record InstitutionEmailConfigAccessScope(
    long InstitutionId,
    string InstitutionIdentifierProtectionKey,
    string SmtpPasswordEncryptionKey);

/// <summary>Resolves the Institution boundary from the authenticated Host or Institution principal.</summary>
public abstract class InstitutionEmailConfigAccessHandlerBase(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService)
{
    protected IUnitOfWork UnitOfWork { get; } = unitOfWork;
    protected ICommonRequestService CommonRequestService { get; } = commonRequestService;

    protected async Task<InstitutionEmailConfigAccessScope> ResolveInstitutionScopeAsync(
        InstitutionEmailConfigAccessRequestDTO? accessRequest,
        CancellationToken cancellationToken)
    {
        if (accessRequest is null)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        var principal = await CommonRequestService.ValidateAuthenticatedRequestAsync();
        return principal.UserType switch
        {
            LoginUserType.Host => await ResolveHostScopeAsync(accessRequest, cancellationToken),
            LoginUserType.InstitutionStaffMember => await ResolveInstitutionStaffMemberScopeAsync(cancellationToken),
            _ => throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized)
        };
    }

    protected InstitutionEmailConfigResponseDTO MapResponse(
        IMapper mapper,
        InstitutionEmailConfig configuration,
        InstitutionEmailConfigAccessScope scope)
    {
        var response = mapper.Map<InstitutionEmailConfigResponseDTO>(configuration);
        response.InstitutionId = HostInstitutionIdentifierProtector.Encrypt(
            configuration.InstitutionId,
            scope.InstitutionIdentifierProtectionKey,
            idEncoderService);
        return response;
    }

    private async Task<InstitutionEmailConfigAccessScope> ResolveHostScopeAsync(
        InstitutionEmailConfigAccessRequestDTO accessRequest,
        CancellationToken cancellationToken)
    {
        var hostContext = await CommonRequestService.ValidateHostUserPermissionRequestAsync();
        var institutionId = HostInstitutionIdentifierProtector.Decrypt(
            accessRequest.InstitutionId,
            hostContext.InstitutionEncryptionKey,
            idEncoderService);

        if (await UnitOfWork.InstitutionRepository.GetHostManagedInstitutionByIdAsync(institutionId, cancellationToken) is null)
        {
            throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);
        }

        var institutionEncryptionKey = await UnitOfWork.InstitutionEncryptionKeyRepository
            .GetActiveKeyByInstitutionIdAsync(institutionId, cancellationToken);
        if (institutionEncryptionKey is null || string.IsNullOrWhiteSpace(institutionEncryptionKey.EncryptionKey))
        {
            throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);
        }

        return new InstitutionEmailConfigAccessScope(
            institutionId,
            hostContext.InstitutionEncryptionKey,
            institutionEncryptionKey.EncryptionKey);
    }

    private async Task<InstitutionEmailConfigAccessScope> ResolveInstitutionStaffMemberScopeAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var institutionContext = await CommonRequestService.ValidateInstitutionUserRequestAsync();
        if (!institutionContext.Success ||
            institutionContext.InstitutionId <= 0 ||
            string.IsNullOrWhiteSpace(institutionContext.Claims?.InstitutionEncriptionKey))
        {
            throw new UnauthorizedAccessException(
                institutionContext.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
        }

        return new InstitutionEmailConfigAccessScope(
            institutionContext.InstitutionId,
            institutionContext.Claims.InstitutionEncriptionKey,
            institutionContext.Claims.InstitutionEncriptionKey);
    }
}

#endregion

#region Handlers

public sealed class CreateInstitutionEmailConfigCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService,
    IEncryptionService encryptionService,
    IMapper mapper,
    ILogger<CreateInstitutionEmailConfigCommandHandler> logger)
    : InstitutionEmailConfigAccessHandlerBase(unitOfWork, commonRequestService, idEncoderService),
        IRequestHandler<CreateInstitutionEmailConfigCommand, ApiResponse<InstitutionEmailConfigResponseDTO>>
{
    public async Task<ApiResponse<InstitutionEmailConfigResponseDTO>> Handle(
        CreateInstitutionEmailConfigCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.DTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        var scope = await ResolveInstitutionScopeAsync(dto, cancellationToken);
        var values = InstitutionEmailConfigInput.Normalize(dto);

        await UnitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (values.IsActive)
            {
                await UnitOfWork.InstitutionEmailConfigRepository
                    .DeactivateOtherActiveAsync(scope.InstitutionId, null, cancellationToken);
            }

            var configuration = values.ToEntity(
                scope.InstitutionId,
                encryptionService,
                scope.SmtpPasswordEncryptionKey);
            await UnitOfWork.InstitutionEmailConfigRepository.AddAsync(configuration, cancellationToken);
            await UnitOfWork.SaveChangesAsync(cancellationToken);
            await UnitOfWork.CommitTransactionAsync(cancellationToken);

            logger.LogInformation("Created Institution SMTP configuration {InstitutionEmailConfigId} for Institution {InstitutionId}.", configuration.Id, scope.InstitutionId);
            return ApiResponse<InstitutionEmailConfigResponseDTO>.Success(
                MapResponse(mapper, configuration, scope),
                AppConstants.SuccessMessages.InstitutionEmailConfigCreated);
        }
        catch
        {
            await UnitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}

public sealed class UpdateInstitutionEmailConfigCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService,
    IEncryptionService encryptionService,
    IMapper mapper,
    ILogger<UpdateInstitutionEmailConfigCommandHandler> logger)
    : InstitutionEmailConfigAccessHandlerBase(unitOfWork, commonRequestService, idEncoderService),
        IRequestHandler<UpdateInstitutionEmailConfigCommand, ApiResponse<InstitutionEmailConfigResponseDTO>>
{
    public async Task<ApiResponse<InstitutionEmailConfigResponseDTO>> Handle(
        UpdateInstitutionEmailConfigCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.DTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        if (dto.Id <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        var scope = await ResolveInstitutionScopeAsync(dto, cancellationToken);
        var configuration = await UnitOfWork.InstitutionEmailConfigRepository
            .GetForUpdateAsync(scope.InstitutionId, dto.Id, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.InstitutionEmailConfigNotFound);
        var values = InstitutionEmailConfigInput.Normalize(dto);

        await UnitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (values.IsActive)
            {
                await UnitOfWork.InstitutionEmailConfigRepository
                    .DeactivateOtherActiveAsync(scope.InstitutionId, configuration.Id, cancellationToken);
            }

            values.ApplyTo(
                configuration,
                encryptionService,
                scope.SmtpPasswordEncryptionKey);
            await UnitOfWork.SaveChangesAsync(cancellationToken);
            await UnitOfWork.CommitTransactionAsync(cancellationToken);

            logger.LogInformation("Updated Institution SMTP configuration {InstitutionEmailConfigId} for Institution {InstitutionId}.", configuration.Id, scope.InstitutionId);
            return ApiResponse<InstitutionEmailConfigResponseDTO>.Success(
                MapResponse(mapper, configuration, scope),
                AppConstants.SuccessMessages.InstitutionEmailConfigUpdated);
        }
        catch
        {
            await UnitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}

public sealed class DeleteInstitutionEmailConfigCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService,
    ILogger<DeleteInstitutionEmailConfigCommandHandler> logger)
    : InstitutionEmailConfigAccessHandlerBase(unitOfWork, commonRequestService, idEncoderService),
        IRequestHandler<DeleteInstitutionEmailConfigCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        DeleteInstitutionEmailConfigCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Id <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        var scope = await ResolveInstitutionScopeAsync(request.AccessRequest, cancellationToken);
        var configuration = await UnitOfWork.InstitutionEmailConfigRepository
            .GetForUpdateAsync(scope.InstitutionId, request.Id, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.InstitutionEmailConfigNotFound);
        if (configuration.IsActive)
        {
            throw new ConflictException(AppConstants.ErrorMessages.InstitutionEmailConfigMustBeInactiveToDelete);
        }

        UnitOfWork.InstitutionEmailConfigRepository.Remove(configuration);
        await UnitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deleted inactive Institution SMTP configuration {InstitutionEmailConfigId} for Institution {InstitutionId}.", configuration.Id, scope.InstitutionId);
        return ApiResponse<bool>.Success(true, AppConstants.SuccessMessages.InstitutionEmailConfigDeleted);
    }
}

public sealed class GetInstitutionEmailConfigByIdQueryHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService,
    IMapper mapper)
    : InstitutionEmailConfigAccessHandlerBase(unitOfWork, commonRequestService, idEncoderService),
        IRequestHandler<GetInstitutionEmailConfigByIdQuery, ApiResponse<InstitutionEmailConfigResponseDTO>>
{
    public async Task<ApiResponse<InstitutionEmailConfigResponseDTO>> Handle(
        GetInstitutionEmailConfigByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Id <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        var scope = await ResolveInstitutionScopeAsync(request.AccessRequest, cancellationToken);
        var configuration = await UnitOfWork.InstitutionEmailConfigRepository
            .GetByIdAsync(scope.InstitutionId, request.Id, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.InstitutionEmailConfigNotFound);
        return ApiResponse<InstitutionEmailConfigResponseDTO>.Success(
            MapResponse(mapper, configuration, scope),
            AppConstants.SuccessMessages.InstitutionEmailConfigRetrieved);
    }
}

public sealed class GetAllInstitutionEmailConfigsQueryHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService,
    IMapper mapper)
    : InstitutionEmailConfigAccessHandlerBase(unitOfWork, commonRequestService, idEncoderService),
        IRequestHandler<GetAllInstitutionEmailConfigsQuery, ApiResponse<List<InstitutionEmailConfigResponseDTO>>>
{
    public async Task<ApiResponse<List<InstitutionEmailConfigResponseDTO>>> Handle(
        GetAllInstitutionEmailConfigsQuery request,
        CancellationToken cancellationToken)
    {
        var scope = await ResolveInstitutionScopeAsync(request.Filter, cancellationToken);
        var configurations = await UnitOfWork.InstitutionEmailConfigRepository
            .GetAllAsync(scope.InstitutionId, cancellationToken);
        return ApiResponse<List<InstitutionEmailConfigResponseDTO>>.Success(
            configurations.Select(configuration => MapResponse(mapper, configuration, scope)).ToList(),
            AppConstants.SuccessMessages.InstitutionEmailConfigRetrieved);
    }
}

#endregion

internal sealed record InstitutionEmailConfigInput(
    string SmtpHost,
    int SmtpPort,
    string SmtpUsername,
    string? SmtpPassword,
    string FromEmail,
    string FromName,
    bool IsActive)
{
    public static InstitutionEmailConfigInput Normalize(CreateInstitutionEmailConfigRequestDTO dto) =>
        Create(
            dto.SmtpHost,
            dto.SmtpPort,
            dto.SmtpUsername,
            dto.SmtpPassword,
            dto.FromEmail,
            dto.FromName,
            dto.IsActive,
            requireSmtpPassword: true);

    public static InstitutionEmailConfigInput Normalize(UpdateInstitutionEmailConfigRequestDTO dto) =>
        Create(
            dto.SmtpHost,
            dto.SmtpPort,
            dto.SmtpUsername,
            dto.SmtpPassword,
            dto.FromEmail,
            dto.FromName,
            dto.IsActive,
            requireSmtpPassword: false);

    public InstitutionEmailConfig ToEntity(
        long institutionId,
        IEncryptionService encryptionService,
        string institutionEncryptionKey) => new()
    {
        InstitutionId = institutionId,
        SmtpHost = SmtpHost,
        SmtpPort = SmtpPort,
        SmtpUsername = SmtpUsername,
        SmtpPasswordEncrypted = EncryptSmtpPassword(encryptionService, institutionEncryptionKey, SmtpPassword!),
        SecrateKey = null,
        FromEmail = FromEmail,
        FromName = FromName,
        IsActive = IsActive
    };

    public void ApplyTo(
        InstitutionEmailConfig entity,
        IEncryptionService encryptionService,
        string institutionEncryptionKey)
    {
        entity.SmtpHost = SmtpHost;
        entity.SmtpPort = SmtpPort;
        entity.SmtpUsername = SmtpUsername;
        if (!string.IsNullOrWhiteSpace(SmtpPassword))
        {
            entity.SmtpPasswordEncrypted = EncryptSmtpPassword(
                encryptionService,
                institutionEncryptionKey,
                SmtpPassword);
            entity.SecrateKey = null;
        }
        else if (!string.IsNullOrWhiteSpace(entity.SecrateKey))
        {
            // Upgrade a legacy record on its next edit without ever returning the
            // plaintext password to the caller.
            entity.SmtpPasswordEncrypted = EncryptSmtpPassword(
                encryptionService,
                institutionEncryptionKey,
                entity.SecrateKey);
            entity.SecrateKey = null;
        }
        entity.FromEmail = FromEmail;
        entity.FromName = FromName;
        entity.IsActive = IsActive;
    }

    private static InstitutionEmailConfigInput Create(
        string? smtpHost,
        int smtpPort,
        string? smtpUsername,
        string? smtpPassword,
        string? fromEmail,
        string? fromName,
        bool isActive,
        bool requireSmtpPassword)
    {
        var normalizedHost = RequireAndTrim(smtpHost, 200);
        var normalizedUsername = RequireAndTrim(smtpUsername, 200);
        var normalizedPassword = requireSmtpPassword
            ? RequireAndTrim(smtpPassword, 100)
            : NormalizeOptional(smtpPassword, 100);
        var normalizedFromEmail = RequireAndTrim(fromEmail, 200);
        var normalizedFromName = RequireAndTrim(fromName, 100);

        if (smtpPort is < 1 or > 65535 ||
            normalizedHost.Any(char.IsWhiteSpace) ||
            normalizedUsername.Any(char.IsWhiteSpace) ||
            Uri.CheckHostName(normalizedHost) is UriHostNameType.Unknown ||
            !IsValidEmail(normalizedFromEmail))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        return new InstitutionEmailConfigInput(
            normalizedHost,
            smtpPort,
            normalizedUsername,
            normalizedPassword,
            normalizedFromEmail,
            normalizedFromName,
            isActive);
    }

    private static string EncryptSmtpPassword(
        IEncryptionService encryptionService,
        string institutionEncryptionKey,
        string smtpPassword)
    {
        if (string.IsNullOrWhiteSpace(institutionEncryptionKey))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        return encryptionService.Encrypt(smtpPassword, institutionEncryptionKey);
    }

    private static string RequireAndTrim(string? value, int maximumLength)
    {
        var normalized = value?.Trim().Trim('\u200B', '\uFEFF');
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maximumLength)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maximumLength) =>
        string.IsNullOrWhiteSpace(value) ? null : RequireAndTrim(value, maximumLength);

    private static bool IsValidEmail(string email)
    {
        try
        {
            var parsed = new MailAddress(email);
            return string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
