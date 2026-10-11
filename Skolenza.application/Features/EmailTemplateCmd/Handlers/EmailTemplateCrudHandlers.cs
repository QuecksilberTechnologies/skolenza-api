// ================================================================
// Purpose : Handles Host email-template CRUD requests with validation, audit values, and delivery-history protection.
// ================================================================

using System.Net.Mail;
using AutoMapper;
using skolenza.application.Constants;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOs.EmailTemplate;
using skolenza.application.Exceptions;
using skolenza.application.Features.EmailTemplateCmd;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.EmailTemplateCmd.Handlers;

#region Requests

public sealed class CreateEmailTemplateCommand(CreateEmailTemplateRequestDTO? dto)
    : IRequest<ApiResponse<EmailTemplateResponseDTO>>, IEmailTemplatePermissionRequest
{
    public CreateEmailTemplateRequestDTO? DTO { get; } = dto;
    public PermissionRequestDTO? PermissionRequest => DTO?.PermissionRequest;
}

public sealed class UpdateEmailTemplateCommand(UpdateEmailTemplateRequestDTO? dto)
    : IRequest<ApiResponse<EmailTemplateResponseDTO>>, IEmailTemplatePermissionRequest
{
    public UpdateEmailTemplateRequestDTO? DTO { get; } = dto;
    public PermissionRequestDTO? PermissionRequest => DTO?.PermissionRequest;
}

public sealed class UpdateEmailTemplateStatusCommand(UpdateEmailTemplateStatusRequestDTO? dto)
    : IRequest<ApiResponse<EmailTemplateResponseDTO>>, IEmailTemplatePermissionRequest
{
    public UpdateEmailTemplateStatusRequestDTO? DTO { get; } = dto;
    public PermissionRequestDTO? PermissionRequest => DTO?.PermissionRequest;
}

public sealed class DeleteEmailTemplateCommand(int id, PermissionRequestDTO? permissionRequest)
    : IRequest<ApiResponse<bool>>, IEmailTemplatePermissionRequest
{
    public int Id { get; } = id;
    public PermissionRequestDTO? PermissionRequest { get; } = permissionRequest;
}

public sealed class GetEmailTemplateByIdQuery(int id, PermissionRequestDTO? permissionRequest)
    : IRequest<ApiResponse<EmailTemplateResponseDTO>>, IEmailTemplatePermissionRequest
{
    public int Id { get; } = id;
    public PermissionRequestDTO? PermissionRequest { get; } = permissionRequest;
}

public sealed class GetAllEmailTemplatesQuery(EmailTemplateListRequestDTO? filter)
    : IRequest<ApiResponse<List<EmailTemplateResponseDTO>>>, IEmailTemplatePermissionRequest
{
    public EmailTemplateListRequestDTO Filter { get; } = filter ?? new EmailTemplateListRequestDTO();
    public PermissionRequestDTO? PermissionRequest => Filter;
}

#endregion

#region Handlers

public sealed class CreateEmailTemplateCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    IMapper mapper,
    ILogger<CreateEmailTemplateCommandHandler> logger)
    : IRequestHandler<CreateEmailTemplateCommand, ApiResponse<EmailTemplateResponseDTO>>
{
    public async Task<ApiResponse<EmailTemplateResponseDTO>> Handle(
        CreateEmailTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.DTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        var values = EmailTemplateInput.Normalize(dto);
        if (await unitOfWork.EmailTemplateRepository.TemplateCodeExistsAsync(values.TemplateCode, cancellationToken: cancellationToken))
        {
            throw new ConflictException(AppConstants.ErrorMessages.DuplicateEmailTemplateCode);
        }

        var hostContext = await commonRequestService.ValidateHostUserPermissionRequestAsync();
        var entity = values.ToEntity(hostContext.HostUserId);
        await unitOfWork.EmailTemplateRepository.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created EmailTemplate {EmailTemplateId} by HostUser {HostUserId}.", entity.Id, hostContext.HostUserId);
        return ApiResponse<EmailTemplateResponseDTO>.Success(
            mapper.Map<EmailTemplateResponseDTO>(entity),
            AppConstants.SuccessMessages.EmailTemplateCreated);
    }
}

public sealed class UpdateEmailTemplateCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    IMapper mapper,
    ILogger<UpdateEmailTemplateCommandHandler> logger)
    : IRequestHandler<UpdateEmailTemplateCommand, ApiResponse<EmailTemplateResponseDTO>>
{
    public async Task<ApiResponse<EmailTemplateResponseDTO>> Handle(
        UpdateEmailTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.DTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        if (dto.Id <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        var entity = await unitOfWork.EmailTemplateRepository.GetForUpdateAsync(dto.Id, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.EmailTemplateNotFound);
        var values = EmailTemplateInput.Normalize(dto);
        if (await unitOfWork.EmailTemplateRepository.TemplateCodeExistsAsync(values.TemplateCode, entity.Id, cancellationToken))
        {
            throw new ConflictException(AppConstants.ErrorMessages.DuplicateEmailTemplateCode);
        }

        var hostContext = await commonRequestService.ValidateHostUserPermissionRequestAsync();
        values.ApplyTo(entity, hostContext.HostUserId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Updated EmailTemplate {EmailTemplateId} by HostUser {HostUserId}.", entity.Id, hostContext.HostUserId);
        return ApiResponse<EmailTemplateResponseDTO>.Success(
            mapper.Map<EmailTemplateResponseDTO>(entity),
            AppConstants.SuccessMessages.EmailTemplateUpdated);
    }
}

public sealed class UpdateEmailTemplateStatusCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    IMapper mapper,
    ILogger<UpdateEmailTemplateStatusCommandHandler> logger)
    : IRequestHandler<UpdateEmailTemplateStatusCommand, ApiResponse<EmailTemplateResponseDTO>>
{
    public async Task<ApiResponse<EmailTemplateResponseDTO>> Handle(
        UpdateEmailTemplateStatusCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.DTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        if (dto.Id <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        var entity = await unitOfWork.EmailTemplateRepository.GetForUpdateAsync(dto.Id, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.EmailTemplateNotFound);
        var hostContext = await commonRequestService.ValidateHostUserPermissionRequestAsync();
        entity.IsActive = dto.IsActive;
        entity.UpdatedById = hostContext.HostUserId;
        entity.UpdatedDateTime = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Changed EmailTemplate {EmailTemplateId} active status to {IsActive} by HostUser {HostUserId}.",
            entity.Id,
            entity.IsActive,
            hostContext.HostUserId);
        return ApiResponse<EmailTemplateResponseDTO>.Success(
            mapper.Map<EmailTemplateResponseDTO>(entity),
            AppConstants.SuccessMessages.EmailTemplateStatusUpdated);
    }
}

public sealed class DeleteEmailTemplateCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService,
    ILogger<DeleteEmailTemplateCommandHandler> logger)
    : IRequestHandler<DeleteEmailTemplateCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(
        DeleteEmailTemplateCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Id <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        var entity = await unitOfWork.EmailTemplateRepository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.EmailTemplateNotFound);
        if (entity.IsActive)
        {
            throw new ConflictException(AppConstants.ErrorMessages.EmailTemplateMustBeInactiveToDelete);
        }

        if (await unitOfWork.EmailTemplateRepository.HasEmailQueueEntriesAsync(entity.Id, cancellationToken))
        {
            throw new ConflictException(AppConstants.ErrorMessages.EmailTemplateHasDeliveryHistory);
        }

        var hostContext = await commonRequestService.ValidateHostUserPermissionRequestAsync();
        unitOfWork.EmailTemplateRepository.Remove(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deleted EmailTemplate {EmailTemplateId} by HostUser {HostUserId}.", entity.Id, hostContext.HostUserId);
        return ApiResponse<bool>.Success(true, AppConstants.SuccessMessages.EmailTemplateDeleted);
    }
}

public sealed class GetEmailTemplateByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetEmailTemplateByIdQuery, ApiResponse<EmailTemplateResponseDTO>>
{
    public async Task<ApiResponse<EmailTemplateResponseDTO>> Handle(
        GetEmailTemplateByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Id <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        var entity = await unitOfWork.EmailTemplateRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.EmailTemplateNotFound);
        return ApiResponse<EmailTemplateResponseDTO>.Success(
            mapper.Map<EmailTemplateResponseDTO>(entity),
            AppConstants.SuccessMessages.EmailTemplateRetrieved);
    }
}

public sealed class GetAllEmailTemplatesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    : IRequestHandler<GetAllEmailTemplatesQuery, ApiResponse<List<EmailTemplateResponseDTO>>>
{
    public async Task<ApiResponse<List<EmailTemplateResponseDTO>>> Handle(
        GetAllEmailTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var page = await unitOfWork.EmailTemplateRepository.GetPagedAsync(request.Filter, cancellationToken);
        return ApiResponse<List<EmailTemplateResponseDTO>>.SuccessPaginated(
            mapper.Map<List<EmailTemplateResponseDTO>>(page.Data),
            page.PageNumber,
            page.PageSize,
            page.TotalCount,
            page.TotalPages,
            AppConstants.SuccessMessages.EmailTemplateRetrieved);
    }
}

#endregion

internal sealed record EmailTemplateInput(
    string TemplateName,
    string TemplateCode,
    string Subject,
    string Body,
    string? FromEmail,
    string? FromName,
    string? CcEmail,
    string? BccEmail,
    string? Category,
    string? LanguageCode,
    bool IsActive)
{
    public static EmailTemplateInput Normalize(CreateEmailTemplateRequestDTO dto) => Create(
        dto.TemplateName,
        dto.TemplateCode,
        dto.Subject,
        dto.Body,
        dto.FromEmail,
        dto.FromName,
        dto.CcEmail,
        dto.BccEmail,
        dto.Category,
        dto.LanguageCode,
        dto.IsActive);

    public static EmailTemplateInput Normalize(UpdateEmailTemplateRequestDTO dto) => Create(
        dto.TemplateName,
        dto.TemplateCode,
        dto.Subject,
        dto.Body,
        dto.FromEmail,
        dto.FromName,
        dto.CcEmail,
        dto.BccEmail,
        dto.Category,
        dto.LanguageCode,
        dto.IsActive);

    public EmailTemplate ToEntity(long hostUserId) => new()
    {
        TemplateName = TemplateName,
        TemplateCode = TemplateCode,
        Subject = Subject,
        Body = Body,
        FromEmail = FromEmail,
        FromName = FromName,
        CcEmail = CcEmail,
        BccEmail = BccEmail,
        Category = Category,
        LanguageCode = LanguageCode,
        IsActive = IsActive,
        AddedById = hostUserId,
        AddedDateTime = DateTime.UtcNow
    };

    public void ApplyTo(EmailTemplate entity, long hostUserId)
    {
        entity.TemplateName = TemplateName;
        entity.TemplateCode = TemplateCode;
        entity.Subject = Subject;
        entity.Body = Body;
        entity.FromEmail = FromEmail;
        entity.FromName = FromName;
        entity.CcEmail = CcEmail;
        entity.BccEmail = BccEmail;
        entity.Category = Category;
        entity.LanguageCode = LanguageCode;
        entity.IsActive = IsActive;
        entity.UpdatedById = hostUserId;
        entity.UpdatedDateTime = DateTime.UtcNow;
    }

    private static EmailTemplateInput Create(
        string? templateName,
        string? templateCode,
        string? subject,
        string? body,
        string? fromEmail,
        string? fromName,
        string? ccEmail,
        string? bccEmail,
        string? category,
        string? languageCode,
        bool isActive)
    {
        var normalizedCode = RequireAndTrim(templateCode, 100).ToUpperInvariant();
        if (!ConstantValues.IsSupportedEmailTemplateCode(normalizedCode))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.UnsupportedEmailTemplateCode);
        }

        return new EmailTemplateInput(
            RequireAndTrim(templateName, 150),
            normalizedCode,
            RequireAndTrim(subject, 250),
            RequireAndTrim(body),
            NormalizeEmail(fromEmail, 150),
            NormalizeOptional(fromName, 100),
            NormalizeEmailList(ccEmail, 1000),
            NormalizeEmailList(bccEmail, 1000),
            NormalizeOptional(category, 100),
            NormalizeOptional(languageCode, 10),
            isActive);
    }

    private static string RequireAndTrim(string? value, int maximumLength = int.MaxValue)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maximumLength)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maximumLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (normalized.Length > maximumLength)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        return normalized;
    }

    private static string? NormalizeEmail(string? value, int maximumLength)
    {
        var normalized = NormalizeOptional(value, maximumLength);
        if (normalized is not null && !IsValidEmail(normalized))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        return normalized;
    }

    private static string? NormalizeEmailList(string? value, int maximumLength)
    {
        var normalized = NormalizeOptional(value, maximumLength);
        if (normalized is null)
        {
            return null;
        }

        var addresses = normalized.Split([';', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (addresses.Length == 0 || addresses.Any(address => !IsValidEmail(address)))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        return string.Join(';', addresses);
    }

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
