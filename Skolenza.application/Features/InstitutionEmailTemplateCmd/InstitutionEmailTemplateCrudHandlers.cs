using System.Net.Mail;
using AutoMapper;
using skolenza.application.Constants;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOs.EmailTemplate;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.Features.InstitutionEmailTemplateCmd;

#region Requests

public sealed record CreateInstitutionEmailTemplateCommand(CreateInstitutionEmailTemplateRequestDTO? DTO)
    : IRequest<ApiResponse<InstitutionEmailTemplateResponseDTO>>, IInstitutionEmailTemplatePermissionRequest
{
    public PermissionRequestDTO? PermissionRequest => DTO?.PermissionRequest;
}

public sealed record UpdateInstitutionEmailTemplateCommand(UpdateInstitutionEmailTemplateRequestDTO? DTO)
    : IRequest<ApiResponse<InstitutionEmailTemplateResponseDTO>>, IInstitutionEmailTemplatePermissionRequest
{
    public PermissionRequestDTO? PermissionRequest => DTO?.PermissionRequest;
}

public sealed record UpdateInstitutionEmailTemplateStatusCommand(UpdateInstitutionEmailTemplateStatusRequestDTO? DTO)
    : IRequest<ApiResponse<InstitutionEmailTemplateResponseDTO>>, IInstitutionEmailTemplatePermissionRequest
{
    public PermissionRequestDTO? PermissionRequest => DTO?.PermissionRequest;
}

public sealed record SyncInstitutionEmailTemplatesCommand(SyncInstitutionEmailTemplatesRequestDTO? DTO)
    : IRequest<ApiResponse<SyncInstitutionEmailTemplatesResponseDTO>>, IInstitutionEmailTemplatePermissionRequest
{
    public PermissionRequestDTO? PermissionRequest => DTO?.PermissionRequest;
}

public sealed record DeleteInstitutionEmailTemplateCommand(int Id, PermissionRequestDTO? PermissionRequest)
    : IRequest<ApiResponse<bool>>, IInstitutionEmailTemplatePermissionRequest;

public sealed record GetInstitutionEmailTemplateByIdQuery(int Id, PermissionRequestDTO? PermissionRequest)
    : IRequest<ApiResponse<InstitutionEmailTemplateResponseDTO>>, IInstitutionEmailTemplatePermissionRequest;

public sealed record GetAllInstitutionEmailTemplatesQuery(InstitutionEmailTemplateListRequestDTO Filter)
    : IRequest<ApiResponse<List<InstitutionEmailTemplateResponseDTO>>>, IInstitutionEmailTemplatePermissionRequest
{
    public PermissionRequestDTO? PermissionRequest => Filter;
}

#endregion

#region Handlers

public sealed class CreateInstitutionEmailTemplateCommandHandler(IUnitOfWork unitOfWork, ICommonRequestService commonRequestService, IMapper mapper)
    : IRequestHandler<CreateInstitutionEmailTemplateCommand, ApiResponse<InstitutionEmailTemplateResponseDTO>>
{
    public async Task<ApiResponse<InstitutionEmailTemplateResponseDTO>> Handle(CreateInstitutionEmailTemplateCommand request, CancellationToken cancellationToken)
    {
        var dto = request.DTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        var actor = await InstitutionEmailTemplateActor.GetAsync(commonRequestService);
        var input = InstitutionEmailTemplateInput.From(dto);
        if (await unitOfWork.InstitutionEmailTemplateRepository.TemplateCodeExistsAsync(actor.InstitutionId, input.TemplateCode, cancellationToken: cancellationToken))
        {
            throw new ConflictException(AppConstants.ErrorMessages.DuplicateEmailTemplateCode);
        }
        var entity = input.ToEntity(actor.InstitutionId, actor.StaffMemberId);
        await unitOfWork.InstitutionEmailTemplateRepository.AddAsync(entity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApiResponse<InstitutionEmailTemplateResponseDTO>.Success(mapper.Map<InstitutionEmailTemplateResponseDTO>(entity), AppConstants.SuccessMessages.EmailTemplateCreated);
    }
}

public sealed class UpdateInstitutionEmailTemplateCommandHandler(IUnitOfWork unitOfWork, ICommonRequestService commonRequestService, IMapper mapper)
    : IRequestHandler<UpdateInstitutionEmailTemplateCommand, ApiResponse<InstitutionEmailTemplateResponseDTO>>
{
    public async Task<ApiResponse<InstitutionEmailTemplateResponseDTO>> Handle(UpdateInstitutionEmailTemplateCommand request, CancellationToken cancellationToken)
    {
        var dto = request.DTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        if (dto.Id <= 0) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        var actor = await InstitutionEmailTemplateActor.GetAsync(commonRequestService);
        var entity = await unitOfWork.InstitutionEmailTemplateRepository.GetForUpdateAsync(actor.InstitutionId, dto.Id, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.EmailTemplateNotFound);
        var input = InstitutionEmailTemplateInput.From(dto);
        if (await unitOfWork.InstitutionEmailTemplateRepository.TemplateCodeExistsAsync(actor.InstitutionId, input.TemplateCode, entity.Id, cancellationToken))
        {
            throw new ConflictException(AppConstants.ErrorMessages.DuplicateEmailTemplateCode);
        }
        input.ApplyTo(entity, actor.StaffMemberId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApiResponse<InstitutionEmailTemplateResponseDTO>.Success(mapper.Map<InstitutionEmailTemplateResponseDTO>(entity), AppConstants.SuccessMessages.EmailTemplateUpdated);
    }
}

public sealed class UpdateInstitutionEmailTemplateStatusCommandHandler(IUnitOfWork unitOfWork, ICommonRequestService commonRequestService, IMapper mapper)
    : IRequestHandler<UpdateInstitutionEmailTemplateStatusCommand, ApiResponse<InstitutionEmailTemplateResponseDTO>>
{
    public async Task<ApiResponse<InstitutionEmailTemplateResponseDTO>> Handle(UpdateInstitutionEmailTemplateStatusCommand request, CancellationToken cancellationToken)
    {
        var dto = request.DTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        if (dto.Id <= 0) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        var actor = await InstitutionEmailTemplateActor.GetAsync(commonRequestService);
        var entity = await unitOfWork.InstitutionEmailTemplateRepository.GetForUpdateAsync(actor.InstitutionId, dto.Id, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.EmailTemplateNotFound);
        entity.IsActive = dto.IsActive;
        entity.UpdatedById = actor.StaffMemberId;
        entity.UpdatedDateTime = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApiResponse<InstitutionEmailTemplateResponseDTO>.Success(mapper.Map<InstitutionEmailTemplateResponseDTO>(entity), AppConstants.SuccessMessages.EmailTemplateStatusUpdated);
    }
}

public sealed class DeleteInstitutionEmailTemplateCommandHandler(IUnitOfWork unitOfWork, ICommonRequestService commonRequestService)
    : IRequestHandler<DeleteInstitutionEmailTemplateCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(DeleteInstitutionEmailTemplateCommand request, CancellationToken cancellationToken)
    {
        if (request.Id <= 0) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        var actor = await InstitutionEmailTemplateActor.GetAsync(commonRequestService);
        var entity = await unitOfWork.InstitutionEmailTemplateRepository.GetForUpdateAsync(actor.InstitutionId, request.Id, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.EmailTemplateNotFound);
        if (entity.IsActive) throw new ConflictException(AppConstants.ErrorMessages.EmailTemplateMustBeInactiveToDelete);
        unitOfWork.InstitutionEmailTemplateRepository.Remove(entity);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApiResponse<bool>.Success(true, AppConstants.SuccessMessages.EmailTemplateDeleted);
    }
}

public sealed class SyncInstitutionEmailTemplatesCommandHandler(
    IUnitOfWork unitOfWork,
    ICommonRequestService commonRequestService)
    : IRequestHandler<SyncInstitutionEmailTemplatesCommand, ApiResponse<SyncInstitutionEmailTemplatesResponseDTO>>
{
    public async Task<ApiResponse<SyncInstitutionEmailTemplatesResponseDTO>> Handle(
        SyncInstitutionEmailTemplatesCommand request,
        CancellationToken cancellationToken)
    {
        _ = request.DTO ?? throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        var actor = await InstitutionEmailTemplateActor.GetAsync(commonRequestService);
        var defaultTemplates = await unitOfWork.EmailTemplateRepository.GetActiveTemplatesAsync(cancellationToken);
        var existingCodes = await unitOfWork.InstitutionEmailTemplateRepository.GetTemplateCodesAsync(
            actor.InstitutionId,
            cancellationToken);
        var existingCodeSet = existingCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingTemplates = defaultTemplates
            .Where(template =>
                ConstantValues.IsSupportedEmailTemplateCode(template.TemplateCode) &&
                !existingCodeSet.Contains(template.TemplateCode!.Trim()))
            .GroupBy(template => template.TemplateCode!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(template => template.AddedDateTime)
                .ThenByDescending(template => template.Id)
                .First())
            .Select(template => CopyDefaultTemplate(template, actor.InstitutionId, actor.StaffMemberId))
            .ToList();

        if (missingTemplates.Count > 0)
        {
            await unitOfWork.InstitutionEmailTemplateRepository.AddRangeAsync(missingTemplates, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var response = new SyncInstitutionEmailTemplatesResponseDTO
        {
            ActiveDefaultTemplateCount = defaultTemplates.Count,
            ExistingInstitutionTemplateCount = existingCodeSet.Count,
            InsertedTemplateCount = missingTemplates.Count,
            InsertedTemplateCodes = missingTemplates
                .Select(template => template.TemplateCode!)
                .OrderBy(code => code)
                .ToList()
        };

        return ApiResponse<SyncInstitutionEmailTemplatesResponseDTO>.Success(
            response,
            AppConstants.SuccessMessages.InstitutionEmailTemplatesSynchronized);
    }

    private static InstitutionEmailTemplate CopyDefaultTemplate(
        EmailTemplate source,
        long institutionId,
        long staffMemberId) => new()
    {
        InstitutionId = institutionId,
        TemplateName = source.TemplateName,
        TemplateCode = source.TemplateCode!.Trim().ToUpperInvariant(),
        Subject = source.Subject,
        Body = source.Body,
        FromEmail = source.FromEmail,
        FromName = source.FromName,
        CcEmail = source.CcEmail,
        BccEmail = source.BccEmail,
        Category = source.Category,
        LanguageCode = source.LanguageCode,
        IsActive = true,
        AddedById = staffMemberId,
        AddedDateTime = DateTime.UtcNow
    };
}

public sealed class GetInstitutionEmailTemplateByIdQueryHandler(IUnitOfWork unitOfWork, ICommonRequestService commonRequestService, IMapper mapper)
    : IRequestHandler<GetInstitutionEmailTemplateByIdQuery, ApiResponse<InstitutionEmailTemplateResponseDTO>>
{
    public async Task<ApiResponse<InstitutionEmailTemplateResponseDTO>> Handle(GetInstitutionEmailTemplateByIdQuery request, CancellationToken cancellationToken)
    {
        if (request.Id <= 0) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        var actor = await InstitutionEmailTemplateActor.GetAsync(commonRequestService);
        var entity = await unitOfWork.InstitutionEmailTemplateRepository.GetByIdAsync(actor.InstitutionId, request.Id, cancellationToken)
            ?? throw new NotFoundException(AppConstants.ErrorMessages.EmailTemplateNotFound);
        return ApiResponse<InstitutionEmailTemplateResponseDTO>.Success(mapper.Map<InstitutionEmailTemplateResponseDTO>(entity), AppConstants.SuccessMessages.EmailTemplateRetrieved);
    }
}

public sealed class GetAllInstitutionEmailTemplatesQueryHandler(IUnitOfWork unitOfWork, ICommonRequestService commonRequestService, IMapper mapper)
    : IRequestHandler<GetAllInstitutionEmailTemplatesQuery, ApiResponse<List<InstitutionEmailTemplateResponseDTO>>>
{
    public async Task<ApiResponse<List<InstitutionEmailTemplateResponseDTO>>> Handle(GetAllInstitutionEmailTemplatesQuery request, CancellationToken cancellationToken)
    {
        var actor = await InstitutionEmailTemplateActor.GetAsync(commonRequestService);
        var page = await unitOfWork.InstitutionEmailTemplateRepository.GetPagedAsync(actor.InstitutionId, request.Filter, cancellationToken);
        return ApiResponse<List<InstitutionEmailTemplateResponseDTO>>.SuccessPaginated(mapper.Map<List<InstitutionEmailTemplateResponseDTO>>(page.Data),
            page.PageNumber, page.PageSize, page.TotalCount, page.TotalPages, AppConstants.SuccessMessages.EmailTemplateRetrieved);
    }
}

#endregion

internal static class InstitutionEmailTemplateActor
{
    public static async Task<(long InstitutionId, long StaffMemberId)> GetAsync(ICommonRequestService service)
    {
        var actor = await service.ValidateInstitutionUserRequestAsync();
        if (!actor.Success || actor.InstitutionId <= 0 || actor.LoggedInStaffMemberId <= 0)
        {
            throw new UnauthorizedAccessException(actor.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
        }
        return (actor.InstitutionId, actor.LoggedInStaffMemberId);
    }
}

internal sealed record InstitutionEmailTemplateInput(string TemplateName, string TemplateCode, string Subject, string Body,
    string? FromEmail, string? FromName, string? CcEmail, string? BccEmail, string? Category, string? LanguageCode, bool IsActive)
{
    public static InstitutionEmailTemplateInput From(CreateInstitutionEmailTemplateRequestDTO dto) => Create(dto.TemplateName, dto.TemplateCode, dto.Subject, dto.Body,
        dto.FromEmail, dto.FromName, dto.CcEmail, dto.BccEmail, dto.Category, dto.LanguageCode, dto.IsActive);

    public InstitutionEmailTemplate ToEntity(long institutionId, long actorId) => new()
    {
        InstitutionId = institutionId, TemplateName = TemplateName, TemplateCode = TemplateCode, Subject = Subject, Body = Body,
        FromEmail = FromEmail, FromName = FromName, CcEmail = CcEmail, BccEmail = BccEmail, Category = Category,
        LanguageCode = LanguageCode, IsActive = IsActive, AddedById = actorId, AddedDateTime = DateTime.UtcNow
    };

    public void ApplyTo(InstitutionEmailTemplate entity, long actorId)
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
        entity.UpdatedById = actorId;
        entity.UpdatedDateTime = DateTime.UtcNow;
    }

    private static InstitutionEmailTemplateInput Create(string? name, string? code, string? subject, string? body,
        string? fromEmail, string? fromName, string? cc, string? bcc, string? category, string? language, bool active)
    {
        var normalizedCode = Required(code, 100).ToUpperInvariant();
        if (!ConstantValues.IsSupportedEmailTemplateCode(normalizedCode))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.UnsupportedEmailTemplateCode);
        }
        return new(Required(name, 150), normalizedCode, Required(subject, 250), Required(body), Email(fromEmail, 150),
            Optional(fromName, 100), EmailList(cc, 1000), EmailList(bcc, 1000), Optional(category, 100), Optional(language, 10), active);
    }

    private static string Required(string? value, int max = int.MaxValue)
    {
        var result = value?.Trim();
        if (string.IsNullOrWhiteSpace(result) || result.Length > max)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }
        return result;
    }
    private static string? Optional(string? value, int max)
    {
        var result = value?.Trim();
        if (string.IsNullOrWhiteSpace(result))
        {
            return null;
        }
        if (result.Length > max)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }
        return result;
    }
    private static string? Email(string? value, int max)
    {
        var result = Optional(value, max);
        if (result is not null && !ValidEmail(result))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }
        return result;
    }
    private static string? EmailList(string? value, int max)
    {
        var result = Optional(value, max);
        if (result is null)
        {
            return null;
        }
        var values = result.Split([';', ','], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (values.Length == 0 || values.Any(x => !ValidEmail(x)))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }
        return string.Join(';', values);
    }
    private static bool ValidEmail(string value)
    {
        try
        {
            return string.Equals(new MailAddress(value).Address, value, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
