using System.Net.Mail;
using AutoMapper;
using skolenza.application.Constants;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOs.DefaultEmailConfig;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.DefaultEmailConfigCmd.Handlers;

#region Requests

public sealed class CreateDefaultEmailConfigCommand(CreateDefaultEmailConfigRequestDTO? dto)
    : IRequest<ApiResponse<DefaultEmailConfigResponseDTO>>
{
    public CreateDefaultEmailConfigRequestDTO? DTO { get; } = dto;
    public PermissionRequestDTO? PermissionRequest => DTO?.PermissionRequest;
}

public sealed class UpdateDefaultEmailConfigCommand(UpdateDefaultEmailConfigRequestDTO? dto)
    : IRequest<ApiResponse<DefaultEmailConfigResponseDTO>>
{
    public UpdateDefaultEmailConfigRequestDTO? DTO { get; } = dto;
    public PermissionRequestDTO? PermissionRequest => DTO?.PermissionRequest;
}

public sealed class DeleteDefaultEmailConfigCommand(int id, PermissionRequestDTO? permissionRequest)
    : IRequest<ApiResponse<bool>>
{
    public int Id { get; } = id;
    public PermissionRequestDTO? PermissionRequest { get; } = permissionRequest;
}

public sealed class GetDefaultEmailConfigByIdQuery(int id, PermissionRequestDTO? permissionRequest)
    : IRequest<ApiResponse<DefaultEmailConfigResponseDTO>>
{
    public int Id { get; } = id;
    public PermissionRequestDTO? PermissionRequest { get; } = permissionRequest;
}

public sealed class GetAllDefaultEmailConfigsQuery(PermissionRequestDTO? permissionRequest)
    : IRequest<ApiResponse<List<DefaultEmailConfigResponseDTO>>>
{
    public PermissionRequestDTO? PermissionRequest { get; } = permissionRequest;
}

#endregion

#region Handlers

/// <summary>Creates a centrally managed SMTP configuration for new Institution registration.</summary>
public sealed class CreateDefaultEmailConfigCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    IMapper mapper,
    ILogger<CreateDefaultEmailConfigCommandHandler> logger)
    : IRequestHandler<CreateDefaultEmailConfigCommand, ApiResponse<DefaultEmailConfigResponseDTO>>
{
    public async Task<ApiResponse<DefaultEmailConfigResponseDTO>> Handle(
        CreateDefaultEmailConfigCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.DTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        var values = DefaultEmailConfigInput.Normalize(dto);

        if (await unitOfWork.DefaultEmailConfigRepository
                .ConfigNameExistsAsync(values.ConfigName, cancellationToken: cancellationToken))
        {
            throw new ConflictException(AppConstants.ErrorMessages.DuplicateDefaultEmailConfigName);
        }

        if (values.IsDefault && !values.IsActive)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.DefaultEmailConfigMustBeActiveToSetDefault);
        }

        if (!values.IsDefault && await unitOfWork.DefaultEmailConfigRepository
                .GetActiveDefaultEmailConfigAsync(cancellationToken) is null)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.DefaultEmailConfigDefaultRequired);
        }

        await commonRequestService.ValidateHostUserPermissionRequestAsync();
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (values.IsDefault)
            {
                await unitOfWork.DefaultEmailConfigRepository
                    .ClearExistingDefaultAsync(null, cancellationToken);
            }

            var entity = values.ToEntity();
            await unitOfWork.DefaultEmailConfigRepository.AddAsync(entity, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            logger.LogInformation("Created default email configuration {DefaultEmailConfigId}.", entity.Id);
            return ApiResponse<DefaultEmailConfigResponseDTO>.Success(
                mapper.Map<DefaultEmailConfigResponseDTO>(entity),
                AppConstants.SuccessMessages.DefaultEmailConfigCreated);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}

/// <summary>Updates a centrally managed SMTP configuration without exposing the SMTP secret in responses.</summary>
public sealed class UpdateDefaultEmailConfigCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    IMapper mapper,
    ILogger<UpdateDefaultEmailConfigCommandHandler> logger)
    : IRequestHandler<UpdateDefaultEmailConfigCommand, ApiResponse<DefaultEmailConfigResponseDTO>>
{
    public async Task<ApiResponse<DefaultEmailConfigResponseDTO>> Handle(
        UpdateDefaultEmailConfigCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.DTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        if (dto.Id <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        var entity = await unitOfWork.DefaultEmailConfigRepository
            .GetForUpdateAsync(dto.Id, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.DefaultEmailConfigNotFound);
        var values = DefaultEmailConfigInput.Normalize(dto, entity);

        if (await unitOfWork.DefaultEmailConfigRepository
                .ConfigNameExistsAsync(values.ConfigName, entity.Id, cancellationToken))
        {
            throw new ConflictException(AppConstants.ErrorMessages.DuplicateDefaultEmailConfigName);
        }

        if (values.IsDefault && !values.IsActive)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.DefaultEmailConfigMustBeActiveToSetDefault);
        }

        if (entity.IsDefault && !values.IsDefault)
        {
            throw new ConflictException(AppConstants.ErrorMessages.DefaultEmailConfigCannotClearDefault);
        }

        if (!values.IsActive && entity.IsActive && !await unitOfWork.DefaultEmailConfigRepository
                .HasAnotherActiveConfigAsync(entity.Id, cancellationToken))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.DefaultEmailConfigMustRemainActive);
        }

        await commonRequestService.ValidateHostUserPermissionRequestAsync();
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (values.IsDefault)
            {
                await unitOfWork.DefaultEmailConfigRepository
                    .ClearExistingDefaultAsync(entity.Id, cancellationToken);
            }

            values.ApplyTo(entity);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            logger.LogInformation("Updated default email configuration {DefaultEmailConfigId}.", entity.Id);
            return ApiResponse<DefaultEmailConfigResponseDTO>.Success(
                mapper.Map<DefaultEmailConfigResponseDTO>(entity),
                AppConstants.SuccessMessages.DefaultEmailConfigUpdated);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}

/// <summary>Deletes an inactive default SMTP configuration. The active configuration cannot be deleted.</summary>
public sealed class DeleteDefaultEmailConfigCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    ILogger<DeleteDefaultEmailConfigCommandHandler> logger)
    : IRequestHandler<DeleteDefaultEmailConfigCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        DeleteDefaultEmailConfigCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Id <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        var entity = await unitOfWork.DefaultEmailConfigRepository
            .GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.DefaultEmailConfigNotFound);
        if (entity.IsDefault)
        {
            throw new ConflictException(AppConstants.ErrorMessages.ActiveDefaultEmailConfigCannotBeDeleted);
        }

        if (entity.IsActive)
        {
            throw new ConflictException(AppConstants.ErrorMessages.DefaultEmailConfigMustBeInactiveToDelete);
        }

        await commonRequestService.ValidateHostUserPermissionRequestAsync();
        unitOfWork.DefaultEmailConfigRepository.Remove(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deleted inactive default email configuration {DefaultEmailConfigId}.", entity.Id);
        return ApiResponse<bool>.Success(true, AppConstants.SuccessMessages.DefaultEmailConfigDeleted);
    }
}

/// <summary>Returns one safe, credential-free Host-facing default SMTP configuration.</summary>
public sealed class GetDefaultEmailConfigByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetDefaultEmailConfigByIdQuery, ApiResponse<DefaultEmailConfigResponseDTO>>
{
    public async Task<ApiResponse<DefaultEmailConfigResponseDTO>> Handle(
        GetDefaultEmailConfigByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Id <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        var entity = await unitOfWork.DefaultEmailConfigRepository
            .GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.DefaultEmailConfigNotFound);
        return ApiResponse<DefaultEmailConfigResponseDTO>.Success(
            mapper.Map<DefaultEmailConfigResponseDTO>(entity),
            AppConstants.SuccessMessages.DefaultEmailConfigRetrieved);
    }
}

/// <summary>Returns all safe, credential-free Host-facing default SMTP configurations.</summary>
public sealed class GetAllDefaultEmailConfigsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetAllDefaultEmailConfigsQuery, ApiResponse<List<DefaultEmailConfigResponseDTO>>>
{
    public async Task<ApiResponse<List<DefaultEmailConfigResponseDTO>>> Handle(
        GetAllDefaultEmailConfigsQuery request,
        CancellationToken cancellationToken)
    {
        var configurations = await unitOfWork.DefaultEmailConfigRepository.GetAllAsync(cancellationToken);
        return ApiResponse<List<DefaultEmailConfigResponseDTO>>.Success(
            mapper.Map<List<DefaultEmailConfigResponseDTO>>(configurations),
            AppConstants.SuccessMessages.DefaultEmailConfigRetrieved);
    }
}

#endregion

internal sealed record DefaultEmailConfigInput(
    string ConfigName,
    string SmtpHost,
    int SmtpPort,
    string SmtpUsername,
    string SmtpPassword,
    string FromEmail,
    string FromName,
    bool IsActive,
    bool IsDefault)
{
    public static DefaultEmailConfigInput Normalize(CreateDefaultEmailConfigRequestDTO dto) =>
        Create(
            dto.ConfigName,
            dto.SmtpHost,
            dto.SmtpPort,
            dto.SmtpUsername,
            dto.SmtpPassword,
            dto.FromEmail,
            dto.FromName,
            dto.IsActive,
            dto.IsDefault);

    public static DefaultEmailConfigInput Normalize(
        UpdateDefaultEmailConfigRequestDTO dto,
        DefaultEmailConfig existing) =>
        Create(
            dto.ConfigName,
            dto.SmtpHost,
            dto.SmtpPort,
            dto.SmtpUsername,
            string.IsNullOrWhiteSpace(dto.SmtpPassword)
                ? !string.IsNullOrWhiteSpace(existing.SecrateKey)
                    ? existing.SecrateKey
                    : existing.SmtpPasswordEncrypted
                : dto.SmtpPassword,
            dto.FromEmail,
            dto.FromName,
            dto.IsActive,
            dto.IsDefault);

    public DefaultEmailConfig ToEntity() => new()
    {
        ConfigName = ConfigName,
        SmtpHost = SmtpHost,
        SmtpPort = SmtpPort,
        SmtpUsername = SmtpUsername,
        SmtpPasswordEncrypted = SmtpPassword,
        SecrateKey = SmtpPassword,
        FromEmail = FromEmail,
        FromName = FromName,
        IsActive = IsActive,
        IsDefault = IsDefault,
        CreatedDateTime = DateTime.UtcNow
    };

    public void ApplyTo(DefaultEmailConfig entity)
    {
        entity.ConfigName = ConfigName;
        entity.SmtpHost = SmtpHost;
        entity.SmtpPort = SmtpPort;
        entity.SmtpUsername = SmtpUsername;
        entity.SmtpPasswordEncrypted = SmtpPassword;
        entity.SecrateKey = SmtpPassword;
        entity.FromEmail = FromEmail;
        entity.FromName = FromName;
        entity.IsActive = IsActive;
        entity.IsDefault = IsDefault;
        entity.UpdatedDateTime = DateTime.UtcNow;
    }

    private static DefaultEmailConfigInput Create(
        string? configName,
        string? smtpHost,
        int smtpPort,
        string? smtpUsername,
        string? smtpPassword,
        string? fromEmail,
        string? fromName,
        bool isActive,
        bool isDefault)
    {
        var normalizedConfigName = RequireAndTrim(configName, 100);
        var normalizedSmtpHost = RequireAndTrim(smtpHost, 200);
        var normalizedSmtpUsername = RequireAndTrim(smtpUsername, 200);
        // InstitutionEmailConfig.SecrateKey is limited to 100 characters. Enforce
        // that same boundary here so a saved default can always be copied to a
        // newly registered Institution without a later database failure.
        var normalizedSmtpPassword = RequireAndTrim(smtpPassword, 100);
        var normalizedFromEmail = RequireAndTrim(fromEmail, 200);
        var normalizedFromName = RequireAndTrim(fromName, 100);

        if (smtpPort is < 1 or > 65535 ||
            normalizedSmtpHost.Any(char.IsWhiteSpace) ||
            normalizedSmtpUsername.Any(char.IsWhiteSpace) ||
            !IsValidHost(normalizedSmtpHost) ||
            !IsValidEmail(normalizedFromEmail))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        return new DefaultEmailConfigInput(
            normalizedConfigName,
            normalizedSmtpHost,
            smtpPort,
            normalizedSmtpUsername,
            normalizedSmtpPassword,
            normalizedFromEmail,
            normalizedFromName,
            isActive,
            isDefault);
    }

    private static string RequireAndTrim(string? value, int maximumLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maximumLength)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        return normalized;
    }

    private static bool IsValidHost(string host) => Uri.CheckHostName(host) is
        UriHostNameType.Dns or UriHostNameType.IPv4 or UriHostNameType.IPv6;

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
