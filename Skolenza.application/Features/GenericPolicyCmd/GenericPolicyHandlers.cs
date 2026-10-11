using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.Common.Helpers.EncryptionHelper;
using skolenza.application.Constants;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Policy;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.Features.GenericPolicyCmd;

#region Requests

public sealed record GetPolicyLookupsQuery(PolicyAccessRequestDTO DTO) : IRequest<ApiResponse<object>>;
public sealed record GetPolicyTypesQuery(PolicyAccessRequestDTO DTO) : IRequest<ApiResponse<IReadOnlyList<PolicyTypeResponseDTO>>>;
public sealed record CreatePolicyTypeCommand(CreateGenericPolicyTypeRequestDTO DTO) : IRequest<ApiResponse<PolicyTypeResponseDTO>>;
public sealed record UpdateGenericPolicyTypeCommand(UpdateGenericPolicyTypeRequestDTO DTO) : IRequest<ApiResponse<PolicyTypeResponseDTO>>;
public sealed record ChangePolicyTypeStatusCommand(ChangePolicyTypeStatusRequestDTO DTO) : IRequest<ApiResponse<bool>>;
public sealed record GetPoliciesQuery(PolicyListRequestDTO DTO) : IRequest<ApiResponse<IReadOnlyList<PolicySummaryResponseDTO>>>;
public sealed record GetPolicyQuery(PolicyByIdRequestDTO DTO) : IRequest<ApiResponse<PolicyDetailResponseDTO>>;
public sealed record GetPolicyVersionQuery(PolicyVersionByIdRequestDTO DTO) : IRequest<ApiResponse<PolicyDetailResponseDTO>>;
public sealed record CreatePolicyCommand(CreatePolicyRequestDTO DTO) : IRequest<ApiResponse<PolicyDetailResponseDTO>>;
public sealed record UpdatePolicyDraftCommand(UpdatePolicyDraftRequestDTO DTO) : IRequest<ApiResponse<PolicyDetailResponseDTO>>;
public sealed record ClonePolicyVersionCommand(ClonePolicyVersionRequestDTO DTO) : IRequest<ApiResponse<PolicyDetailResponseDTO>>;
public sealed record TransitionPolicyCommand(PolicyTransitionRequestDTO DTO) : IRequest<ApiResponse<PolicyDetailResponseDTO>>;
public sealed record ResolveStaffMemberPoliciesQuery(ResolveStaffMemberPoliciesRequestDTO DTO) : IRequest<ApiResponse<IReadOnlyList<ResolvedPolicyResponseDTO>>>;
public sealed record AssignPolicyCommand(AssignPolicyRequestDTO DTO) : IRequest<ApiResponse<PolicyAssignmentResultDTO>>;
public sealed record RemovePolicyAssignmentCommand(RemovePolicyAssignmentRequestDTO DTO) : IRequest<ApiResponse<bool>>;
public sealed record CreatePolicyExceptionCommand(CreatePolicyExceptionRequestDTO DTO) : IRequest<ApiResponse<long>>;
public sealed record ApprovePolicyExceptionCommand(ApprovePolicyExceptionRequestDTO DTO) : IRequest<ApiResponse<bool>>;
public sealed record AcknowledgePolicyCommand(AcknowledgePolicyRequestDTO DTO) : IRequest<ApiResponse<bool>>;
public sealed record UploadPolicyDocumentCommand(UploadPolicyDocumentRequestDTO DTO) : IRequest<ApiResponse<PolicyDocumentResponseDTO>>;
public sealed record GetPolicyDocumentsQuery(PolicyDocumentsRequestDTO DTO) : IRequest<ApiResponse<IReadOnlyList<PolicyDocumentResponseDTO>>>;
public sealed record DeletePolicyDocumentCommand(DeletePolicyDocumentRequestDTO DTO) : IRequest<ApiResponse<bool>>;
public sealed record GetPolicyAuditQuery(PolicyByIdRequestDTO DTO) : IRequest<ApiResponse<IReadOnlyList<PolicyAuditResponseDTO>>>;
public sealed record GetPolicyApprovalStagesQuery(PolicyApprovalStageListRequestDTO DTO) : IRequest<ApiResponse<IReadOnlyList<PolicyApprovalStageResponseDTO>>>;
public sealed record CreatePolicyApprovalStageCommand(CreatePolicyApprovalStageRequestDTO DTO) : IRequest<ApiResponse<PolicyApprovalStageResponseDTO>>;
public sealed record UpdatePolicyApprovalStageCommand(UpdatePolicyApprovalStageRequestDTO DTO) : IRequest<ApiResponse<PolicyApprovalStageResponseDTO>>;
public sealed record DeletePolicyApprovalStageCommand(DeletePolicyApprovalStageRequestDTO DTO) : IRequest<ApiResponse<bool>>;
public sealed record GetPolicyApprovalProgressQuery(PolicyVersionAccessRequestDTO DTO) : IRequest<ApiResponse<IReadOnlyList<PolicyApprovalProgressResponseDTO>>>;
public sealed record GetPolicyAssignmentsQuery(PolicyVersionAccessRequestDTO DTO) : IRequest<ApiResponse<IReadOnlyList<PolicyAssignmentResponseDTO>>>;
public sealed record GetPolicyAssignmentCandidatesQuery(PolicyAssignmentCandidateRequestDTO DTO) : IRequest<ApiResponse<PolicyAssignmentCandidatePageDTO>>;
public sealed record ExportPolicyAssignmentsQuery(PolicyVersionAccessRequestDTO DTO) : IRequest<ApiResponse<string>>;
public sealed record GetPolicyExceptionsQuery(PolicyVersionAccessRequestDTO DTO) : IRequest<ApiResponse<IReadOnlyList<PolicyExceptionResponseDTO>>>;
public sealed record GetPolicyAcknowledgementsQuery(PolicyVersionAccessRequestDTO DTO) : IRequest<ApiResponse<IReadOnlyList<PolicyAcknowledgementResponseDTO>>>;
public sealed record PreviewPolicyBulkImportCommand(BulkImportMaster Master, BulkImportPreviewRequestDTO DTO) : IRequest<ApiResponse<BulkImportPreviewResponseDTO>>;
public sealed record ManagePolicyBulkImportCommand(BulkImportMaster Master, BulkImportAction Action, BulkImportJobRequestDTO DTO) : IRequest<ApiResponse<object>>;

#endregion

#region Handlers

public abstract class GenericPolicyHandlerBase(IGenericPolicyRepository repository, ICommonRequestService commonRequestService)
{
    protected IGenericPolicyRepository Repository { get; } = repository;
    protected async Task<(long InstitutionId, long StaffMemberId)> GetActorAsync()
    {
        var actor = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!actor.Success || actor.InstitutionId <= 0 || actor.LoggedInStaffMemberId <= 0) throw new UnauthorizedAccessException(actor.ErrorMessage ?? "Unauthorized request.");
        return (actor.InstitutionId, actor.LoggedInStaffMemberId);
    }
}

public sealed class GetPolicyLookupsQueryHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService) : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<GetPolicyLookupsQuery, ApiResponse<object>>
{
    public async Task<ApiResponse<object>> Handle(GetPolicyLookupsQuery request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        var data = new
        {
            categories = await Repository.GetCategoriesAsync(token),
            statuses = await Repository.GetStatusesAsync(token),
            ruleTypes = await Repository.GetRuleTypesAsync(token),
            ruleDefinitions = await Repository.GetRuleDefinitionsAsync(token),
            documentTypes = await Repository.GetDocumentTypesAsync(token),
            leaveTypes = await Repository.GetLeaveTypesAsync(actor.InstitutionId, token),
            // Attendance Policy forms consume these enum-derived values instead of duplicating
            // numeric enum mappings in the UI. Adding a domain enum member automatically publishes it.
            attendanceLocationScopes = Enum.GetValues<AttendanceLocationScope>()
                .Select(value => new PolicyEnumLookupResponseDTO(
                    (short)value,
                    ToUpperSnakeCase(value.ToString()),
                    ToDisplayName(value.ToString())))
                .ToArray()
        };
        return ApiResponse<object>.Success(data, "Policy lookups retrieved successfully.");
    }

    private static string ToUpperSnakeCase(string value) =>
        string.Concat(value.Select((character, index) =>
            index > 0 && char.IsUpper(character) ? $"_{character}" : character.ToString()))
            .ToUpperInvariant();

    private static string ToDisplayName(string value) =>
        string.Concat(value.Select((character, index) =>
            index > 0 && char.IsUpper(character) ? $" {character}" : character.ToString()));
}

public sealed class GetPolicyTypesQueryHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService) : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<GetPolicyTypesQuery, ApiResponse<IReadOnlyList<PolicyTypeResponseDTO>>>
{
    public async Task<ApiResponse<IReadOnlyList<PolicyTypeResponseDTO>>> Handle(GetPolicyTypesQuery request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        return ApiResponse<IReadOnlyList<PolicyTypeResponseDTO>>.Success(await Repository.GetPolicyTypesAsync(actor.InstitutionId, request.DTO.IsActive, token), "Policy types retrieved successfully.");
    }
}

public sealed class CreatePolicyTypeCommandHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService) : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<CreatePolicyTypeCommand, ApiResponse<PolicyTypeResponseDTO>>
{
    public async Task<ApiResponse<PolicyTypeResponseDTO>> Handle(CreatePolicyTypeCommand request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        return ApiResponse<PolicyTypeResponseDTO>.Success(await Repository.CreatePolicyTypeAsync(actor.InstitutionId, actor.StaffMemberId, request.DTO, token), "Policy type created successfully.");
    }
}

public sealed class UpdateGenericPolicyTypeCommandHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService) : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<UpdateGenericPolicyTypeCommand, ApiResponse<PolicyTypeResponseDTO>>
{
    public async Task<ApiResponse<PolicyTypeResponseDTO>> Handle(UpdateGenericPolicyTypeCommand request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        return ApiResponse<PolicyTypeResponseDTO>.Success(await Repository.UpdatePolicyTypeAsync(actor.InstitutionId, actor.StaffMemberId, request.DTO, token), "Policy type updated successfully.");
    }
}

public sealed class ChangePolicyTypeStatusCommandHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService) : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<ChangePolicyTypeStatusCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(ChangePolicyTypeStatusCommand request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        return ApiResponse<bool>.Success(await Repository.ChangePolicyTypeStatusAsync(actor.InstitutionId, actor.StaffMemberId, request.DTO, token), "Policy type status updated successfully.");
    }
}

public sealed class GetPoliciesQueryHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService) : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<GetPoliciesQuery, ApiResponse<IReadOnlyList<PolicySummaryResponseDTO>>>
{
    public async Task<ApiResponse<IReadOnlyList<PolicySummaryResponseDTO>>> Handle(GetPoliciesQuery request, CancellationToken token)
    {
        if (request.DTO.PageNumber < 1 || request.DTO.PageSize is < 1 or > 100) throw new ValidationErrorException("PageNumber must be positive and PageSize must be 1 to 100.");
        var actor = await GetActorAsync();
        var result = await Repository.GetPoliciesAsync(actor.InstitutionId, request.DTO, token);
        return ApiResponse<IReadOnlyList<PolicySummaryResponseDTO>>.SuccessPaginated(result.Items, request.DTO.PageNumber, request.DTO.PageSize, result.Total, (int)Math.Ceiling((double)result.Total / request.DTO.PageSize), "Policies retrieved successfully.");
    }
}

public sealed class GetPolicyQueryHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService) : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<GetPolicyQuery, ApiResponse<PolicyDetailResponseDTO>>
{
    public async Task<ApiResponse<PolicyDetailResponseDTO>> Handle(GetPolicyQuery request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        return ApiResponse<PolicyDetailResponseDTO>.Success(await Repository.GetPolicyAsync(actor.InstitutionId, request.DTO.Id, token), "Policy retrieved successfully.");
    }
}

public sealed class GetPolicyVersionQueryHandler(
    IGenericPolicyRepository repository,
    ICommonRequestService commonRequestService)
    : GenericPolicyHandlerBase(repository, commonRequestService),
      IRequestHandler<GetPolicyVersionQuery, ApiResponse<PolicyDetailResponseDTO>>
{
    public async Task<ApiResponse<PolicyDetailResponseDTO>> Handle(GetPolicyVersionQuery request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        return ApiResponse<PolicyDetailResponseDTO>.Success(
            await Repository.GetPolicyVersionAsync(
                actor.InstitutionId,
                request.DTO.PolicyId,
                request.DTO.PolicyVersionId,
                token),
            "Policy version retrieved successfully.");
    }
}

public sealed class CreatePolicyCommandHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService, IIdEncoderService idEncoderService) : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<CreatePolicyCommand, ApiResponse<PolicyDetailResponseDTO>>
{
    public async Task<ApiResponse<PolicyDetailResponseDTO>> Handle(CreatePolicyCommand request, CancellationToken token)
    {
        var actor = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!actor.Success || actor.InstitutionId <= 0 || actor.LoggedInStaffMemberId <= 0) throw new UnauthorizedAccessException(actor.ErrorMessage ?? "Unauthorized request.");
        DecodeApplicabilityStaffMembers(request.DTO.Applicability, idEncoderService, actor.Claims.InstitutionEncriptionKey);
        return ApiResponse<PolicyDetailResponseDTO>.Success(await Repository.CreatePolicyAsync(actor.InstitutionId, actor.LoggedInStaffMemberId, request.DTO, token), "Policy draft created successfully.");
    }

    internal static void DecodeApplicabilityStaffMembers(IEnumerable<PolicyApplicabilityInputDTO> scopes, IIdEncoderService encoder, string institutionKey)
    {
        try
        {
            foreach (var scope in scopes)
            {
                if (string.IsNullOrWhiteSpace(scope.StaffMemberPublicId))
                {
                    scope.ResolvedStaffMemberId = scope.StaffMemberId;
                    continue;
                }

                scope.ResolvedStaffMemberId = encoder.DecodeId_long(
                    EncryptionSanitizer.CleanEncodedInput(scope.StaffMemberPublicId), institutionKey);
                if (scope.ResolvedStaffMemberId <= 0) throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
            }
        }
        catch (ValidationErrorException) { throw; }
        catch (Exception) { throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier); }
    }
}

public sealed class UpdatePolicyDraftCommandHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService, IIdEncoderService idEncoderService) : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<UpdatePolicyDraftCommand, ApiResponse<PolicyDetailResponseDTO>>
{
    public async Task<ApiResponse<PolicyDetailResponseDTO>> Handle(UpdatePolicyDraftCommand request, CancellationToken token)
    {
        var actor = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!actor.Success || actor.InstitutionId <= 0 || actor.LoggedInStaffMemberId <= 0) throw new UnauthorizedAccessException(actor.ErrorMessage ?? "Unauthorized request.");
        CreatePolicyCommandHandler.DecodeApplicabilityStaffMembers(request.DTO.Applicability, idEncoderService, actor.Claims.InstitutionEncriptionKey);
        return ApiResponse<PolicyDetailResponseDTO>.Success(await Repository.UpdateDraftAsync(actor.InstitutionId, actor.LoggedInStaffMemberId, request.DTO, token), "Policy draft updated successfully.");
    }
}

public sealed class ClonePolicyVersionCommandHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService) : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<ClonePolicyVersionCommand, ApiResponse<PolicyDetailResponseDTO>>
{
    public async Task<ApiResponse<PolicyDetailResponseDTO>> Handle(ClonePolicyVersionCommand request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        return ApiResponse<PolicyDetailResponseDTO>.Success(await Repository.CloneVersionAsync(actor.InstitutionId, actor.StaffMemberId, request.DTO, token), "Policy version cloned as draft.");
    }
}

public sealed class TransitionPolicyCommandHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService) : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<TransitionPolicyCommand, ApiResponse<PolicyDetailResponseDTO>>
{
    public async Task<ApiResponse<PolicyDetailResponseDTO>> Handle(TransitionPolicyCommand request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        return ApiResponse<PolicyDetailResponseDTO>.Success(await Repository.TransitionAsync(actor.InstitutionId, actor.StaffMemberId, request.DTO, token), "Policy lifecycle action completed successfully.");
    }
}

public sealed class ResolveStaffMemberPoliciesQueryHandler(
    IGenericPolicyRepository repository,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService)
    : GenericPolicyHandlerBase(repository, commonRequestService),
      IRequestHandler<ResolveStaffMemberPoliciesQuery, ApiResponse<IReadOnlyList<ResolvedPolicyResponseDTO>>>
{
    public async Task<ApiResponse<IReadOnlyList<ResolvedPolicyResponseDTO>>> Handle(ResolveStaffMemberPoliciesQuery request, CancellationToken token)
    {
        var actor = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!actor.Success || actor.InstitutionId <= 0 || actor.LoggedInStaffMemberId <= 0)
        {
            throw new UnauthorizedAccessException(actor.ErrorMessage ?? "Unauthorized request.");
        }

        if (string.IsNullOrWhiteSpace(request.DTO.StaffMemberId))
        {
            throw new ValidationErrorException("StaffMemberId is required.");
        }

        long staffMemberId;
        try
        {
            // Keep the StaffMember list/profile identifier flow consistent: sanitize the public
            // string and decode it through the shared salted ID encoder.
            staffMemberId = idEncoderService.DecodeId_long(
                EncryptionSanitizer.CleanEncodedInput(request.DTO.StaffMemberId),
                actor.Claims.InstitutionEncriptionKey);
        }
        catch (Exception)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        if (staffMemberId <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        return ApiResponse<IReadOnlyList<ResolvedPolicyResponseDTO>>.Success(
            await Repository.ResolveAsync(actor.InstitutionId, staffMemberId, request.DTO, token),
            "Effective policies resolved successfully.");
    }
}

public sealed class AssignPolicyCommandHandler(
    IGenericPolicyRepository repository,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService)
    : GenericPolicyHandlerBase(repository, commonRequestService),
      IRequestHandler<AssignPolicyCommand, ApiResponse<PolicyAssignmentResultDTO>>
{
    public async Task<ApiResponse<PolicyAssignmentResultDTO>> Handle(AssignPolicyCommand request, CancellationToken token)
    {
        var actor = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!actor.Success || actor.InstitutionId <= 0 || actor.LoggedInStaffMemberId <= 0)
        {
            throw new UnauthorizedAccessException(actor.ErrorMessage ?? "Unauthorized request.");
        }

        try
        {
            request.DTO.ResolvedStaffMemberIds = request.DTO.StaffMemberIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => idEncoderService.DecodeId_long(
                    EncryptionSanitizer.CleanEncodedInput(id),
                    actor.Claims.InstitutionEncriptionKey))
                .Where(id => id > 0)
                .Distinct()
                .ToList();
        }
        catch (Exception)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        if (request.DTO.ResolvedStaffMemberIds.Count != request.DTO.StaffMemberIds.Distinct().Count())
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        return ApiResponse<PolicyAssignmentResultDTO>.Success(await Repository.AssignAsync(actor.InstitutionId, actor.LoggedInStaffMemberId, request.DTO, token), "Policy assignments processed successfully.");
    }
}

public sealed class RemovePolicyAssignmentCommandHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService) : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<RemovePolicyAssignmentCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(RemovePolicyAssignmentCommand request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        return ApiResponse<bool>.Success(await Repository.RemoveAssignmentAsync(actor.InstitutionId, actor.StaffMemberId, request.DTO.AssignmentId, token), "Policy assignment removed successfully.");
    }
}

public sealed class CreatePolicyExceptionCommandHandler(
    IGenericPolicyRepository repository,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService)
    : GenericPolicyHandlerBase(repository, commonRequestService),
      IRequestHandler<CreatePolicyExceptionCommand, ApiResponse<long>>
{
    public async Task<ApiResponse<long>> Handle(CreatePolicyExceptionCommand request, CancellationToken token)
    {
        var actor = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!actor.Success || actor.InstitutionId <= 0 || actor.LoggedInStaffMemberId <= 0)
        {
            throw new UnauthorizedAccessException(actor.ErrorMessage ?? "Unauthorized request.");
        }

        try
        {
            request.DTO.ResolvedStaffMemberId = idEncoderService.DecodeId_long(
                EncryptionSanitizer.CleanEncodedInput(request.DTO.StaffMemberId),
                actor.Claims.InstitutionEncriptionKey);
        }
        catch (Exception)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        if (request.DTO.ResolvedStaffMemberId <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        return ApiResponse<long>.Success(
            await Repository.CreateExceptionAsync(
                actor.InstitutionId,
                actor.LoggedInStaffMemberId,
                request.DTO,
                token),
            "Policy exception submitted successfully.");
    }
}

public sealed class ApprovePolicyExceptionCommandHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService) : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<ApprovePolicyExceptionCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(ApprovePolicyExceptionCommand request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        return ApiResponse<bool>.Success(await Repository.ApproveExceptionAsync(actor.InstitutionId, actor.StaffMemberId, request.DTO, token), "Policy exception decision saved successfully.");
    }
}

public sealed class AcknowledgePolicyCommandHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService) : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<AcknowledgePolicyCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(AcknowledgePolicyCommand request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        return ApiResponse<bool>.Success(await Repository.AcknowledgeAsync(actor.InstitutionId, actor.StaffMemberId, request.DTO, token), "Policy acknowledged successfully.");
    }
}

public sealed class UploadPolicyDocumentCommandHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService, IFileStorageService fileStorageService) : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<UploadPolicyDocumentCommand, ApiResponse<PolicyDocumentResponseDTO>>
{
    public async Task<ApiResponse<PolicyDocumentResponseDTO>> Handle(UploadPolicyDocumentCommand request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        var file = request.DTO.File;
        if (file.Length is <= 0 or > 10_485_760) throw new ValidationErrorException("Policy document must be between 1 byte and 10 MB.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".pdf" or ".doc" or ".docx")) throw new ValidationErrorException("Only PDF, DOC and DOCX policy documents are allowed.");
        string checksum;
        await using (var stream = file.OpenReadStream()) checksum = Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(stream, token)).ToLowerInvariant();
        // FileStorageService appends the validated source extension to this generated name.
        var storedName = $"{Guid.NewGuid():N}";
        var key = await fileStorageService.UploadFileAsync(file, $"institution-{actor.InstitutionId}/policies/{request.DTO.PolicyVersionId}", storedName);
        try
        {
            var id = await Repository.AddDocumentAsync(actor.InstitutionId, actor.StaffMemberId, request.DTO, key, checksum, token);
            var response = new PolicyDocumentResponseDTO(id, request.DTO.PolicyVersionId, request.DTO.PolicyDocumentTypeId, request.DTO.DocumentTitle.Trim(), Path.GetFileName(file.FileName), file.ContentType, file.Length, request.DTO.LanguageCode, request.DTO.IsStaffMemberVisible, fileStorageService.GetFileUrl(key));
            return ApiResponse<PolicyDocumentResponseDTO>.Success(response, "Policy document uploaded successfully.");
        }
        catch
        {
            await fileStorageService.DeleteFileAsync(key);
            throw;
        }
    }
}

public sealed class GetPolicyDocumentsQueryHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService, IFileStorageService fileStorageService) : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<GetPolicyDocumentsQuery, ApiResponse<IReadOnlyList<PolicyDocumentResponseDTO>>>
{
    public async Task<ApiResponse<IReadOnlyList<PolicyDocumentResponseDTO>>> Handle(GetPolicyDocumentsQuery request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        var documents = await Repository.GetDocumentsAsync(actor.InstitutionId, request.DTO.PolicyVersionId, token);
        var response = documents.Select(x => new PolicyDocumentResponseDTO(x.Id, x.PolicyVersionId, x.PolicyDocumentTypeId, x.DocumentTitle, x.OriginalFileName, x.ContentType, x.FileSizeBytes, x.LanguageCode, x.IsStaffMemberVisible, fileStorageService.GetFileUrl(x.ObjectKey))).ToList();
        return ApiResponse<IReadOnlyList<PolicyDocumentResponseDTO>>.Success(response, "Policy documents retrieved successfully.");
    }
}

public sealed class DeletePolicyDocumentCommandHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService, IFileStorageService fileStorageService) : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<DeletePolicyDocumentCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(DeletePolicyDocumentCommand request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        var key = await Repository.DeleteDocumentAsync(actor.InstitutionId, actor.StaffMemberId, request.DTO.DocumentId, token);
        await fileStorageService.DeleteFileAsync(key);
        return ApiResponse<bool>.Success(true, "Policy document deleted successfully.");
    }
}

public sealed class GetPolicyAuditQueryHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService) : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<GetPolicyAuditQuery, ApiResponse<IReadOnlyList<PolicyAuditResponseDTO>>>
{
    public async Task<ApiResponse<IReadOnlyList<PolicyAuditResponseDTO>>> Handle(GetPolicyAuditQuery request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        return ApiResponse<IReadOnlyList<PolicyAuditResponseDTO>>.Success(await Repository.GetAuditAsync(actor.InstitutionId, request.DTO.Id, token), "Policy audit retrieved successfully.");
    }
}

public sealed class GetPolicyApprovalStagesQueryHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService)
    : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<GetPolicyApprovalStagesQuery, ApiResponse<IReadOnlyList<PolicyApprovalStageResponseDTO>>>
{
    public async Task<ApiResponse<IReadOnlyList<PolicyApprovalStageResponseDTO>>> Handle(GetPolicyApprovalStagesQuery request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        var data = await Repository.GetApprovalStagesAsync(actor.InstitutionId, request.DTO, token);
        return ApiResponse<IReadOnlyList<PolicyApprovalStageResponseDTO>>.Success(data, "Policy approval stages retrieved successfully.");
    }
}

public sealed class CreatePolicyApprovalStageCommandHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService)
    : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<CreatePolicyApprovalStageCommand, ApiResponse<PolicyApprovalStageResponseDTO>>
{
    public async Task<ApiResponse<PolicyApprovalStageResponseDTO>> Handle(CreatePolicyApprovalStageCommand request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        var data = await Repository.CreateApprovalStageAsync(actor.InstitutionId, actor.StaffMemberId, request.DTO, token);
        return ApiResponse<PolicyApprovalStageResponseDTO>.Success(data, "Policy approval stage created successfully.");
    }
}

public sealed class UpdatePolicyApprovalStageCommandHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService)
    : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<UpdatePolicyApprovalStageCommand, ApiResponse<PolicyApprovalStageResponseDTO>>
{
    public async Task<ApiResponse<PolicyApprovalStageResponseDTO>> Handle(UpdatePolicyApprovalStageCommand request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        var data = await Repository.UpdateApprovalStageAsync(actor.InstitutionId, actor.StaffMemberId, request.DTO, token);
        return ApiResponse<PolicyApprovalStageResponseDTO>.Success(data, "Policy approval stage updated successfully.");
    }
}

public sealed class DeletePolicyApprovalStageCommandHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService)
    : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<DeletePolicyApprovalStageCommand, ApiResponse<bool>>
{
    public async Task<ApiResponse<bool>> Handle(DeletePolicyApprovalStageCommand request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        var data = await Repository.DeleteApprovalStageAsync(actor.InstitutionId, actor.StaffMemberId, request.DTO.Id, token);
        return ApiResponse<bool>.Success(data, "Policy approval stage disabled successfully.");
    }
}

public sealed class GetPolicyApprovalProgressQueryHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService)
    : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<GetPolicyApprovalProgressQuery, ApiResponse<IReadOnlyList<PolicyApprovalProgressResponseDTO>>>
{
    public async Task<ApiResponse<IReadOnlyList<PolicyApprovalProgressResponseDTO>>> Handle(GetPolicyApprovalProgressQuery request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        var data = await Repository.GetApprovalProgressAsync(actor.InstitutionId, request.DTO.PolicyVersionId, token);
        return ApiResponse<IReadOnlyList<PolicyApprovalProgressResponseDTO>>.Success(data, "Policy approval progress retrieved successfully.");
    }
}

public sealed class GetPolicyAssignmentsQueryHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService)
    : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<GetPolicyAssignmentsQuery, ApiResponse<IReadOnlyList<PolicyAssignmentResponseDTO>>>
{
    public async Task<ApiResponse<IReadOnlyList<PolicyAssignmentResponseDTO>>> Handle(GetPolicyAssignmentsQuery request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        var data = await Repository.GetAssignmentsAsync(actor.InstitutionId, request.DTO.PolicyVersionId, token);
        return ApiResponse<IReadOnlyList<PolicyAssignmentResponseDTO>>.Success(data, "Policy assignments retrieved successfully.");
    }
}

public sealed class GetPolicyExceptionsQueryHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService)
    : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<GetPolicyExceptionsQuery, ApiResponse<IReadOnlyList<PolicyExceptionResponseDTO>>>
{
    public async Task<ApiResponse<IReadOnlyList<PolicyExceptionResponseDTO>>> Handle(GetPolicyExceptionsQuery request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        var data = await Repository.GetExceptionsAsync(actor.InstitutionId, request.DTO.PolicyVersionId, token);
        return ApiResponse<IReadOnlyList<PolicyExceptionResponseDTO>>.Success(data, "Policy exceptions retrieved successfully.");
    }
}

public sealed class GetPolicyAcknowledgementsQueryHandler(IGenericPolicyRepository repository, ICommonRequestService commonRequestService)
    : GenericPolicyHandlerBase(repository, commonRequestService), IRequestHandler<GetPolicyAcknowledgementsQuery, ApiResponse<IReadOnlyList<PolicyAcknowledgementResponseDTO>>>
{
    public async Task<ApiResponse<IReadOnlyList<PolicyAcknowledgementResponseDTO>>> Handle(GetPolicyAcknowledgementsQuery request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        var data = await Repository.GetAcknowledgementsAsync(actor.InstitutionId, request.DTO.PolicyVersionId, token);
        return ApiResponse<IReadOnlyList<PolicyAcknowledgementResponseDTO>>.Success(data, "Policy acknowledgements retrieved successfully.");
    }
}

public sealed class GetPolicyAssignmentCandidatesQueryHandler(
    IGenericPolicyRepository repository,
    ICommonRequestService commonRequestService,
    IIdEncoderService idEncoderService)
    : GenericPolicyHandlerBase(repository, commonRequestService),
      IRequestHandler<GetPolicyAssignmentCandidatesQuery, ApiResponse<PolicyAssignmentCandidatePageDTO>>
{
    public async Task<ApiResponse<PolicyAssignmentCandidatePageDTO>> Handle(
        GetPolicyAssignmentCandidatesQuery request,
        CancellationToken token)
    {
        var actor = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!actor.Success || actor.InstitutionId <= 0 || actor.LoggedInStaffMemberId <= 0)
        {
            throw new UnauthorizedAccessException(actor.ErrorMessage ?? "Unauthorized request.");
        }

        var data = await repository.GetAssignmentCandidatesAsync(actor.InstitutionId, request.DTO, token);
        var staffMembers = data.StaffMembers.Select(staffMember => new PolicyAssignmentCandidateResponseDTO(
            idEncoderService.EncodeId_long(staffMember.StaffMemberId, actor.Claims.InstitutionEncriptionKey),
            staffMember.StaffMemberCode,
            staffMember.StaffMemberName,
            staffMember.StaffEmploymentTypeId,
            staffMember.StaffEmploymentTypeName,
            staffMember.DepartmentId,
            staffMember.DepartmentName,
            staffMember.DesignationId,
            staffMember.DesignationName,
            staffMember.IsAssigned)).ToList();
        var response = new PolicyAssignmentCandidatePageDTO(data.PolicyId, data.PolicyVersionId,
            data.PolicyCode, data.PolicyName, data.VersionNumber, data.EffectiveDate, data.TotalRecords,
            staffMembers, data.StaffEmploymentTypes, data.Departments, data.Designations);
        return ApiResponse<PolicyAssignmentCandidatePageDTO>.Success(response,
            "Eligible policy-assignment staffMembers retrieved successfully.");
    }
}

public sealed class ExportPolicyAssignmentsQueryHandler(
    IGenericPolicyRepository repository,
    ICommonRequestService commonRequestService)
    : GenericPolicyHandlerBase(repository, commonRequestService),
      IRequestHandler<ExportPolicyAssignmentsQuery, ApiResponse<string>>
{
    public async Task<ApiResponse<string>> Handle(ExportPolicyAssignmentsQuery request, CancellationToken token)
    {
        var actor = await GetActorAsync();
        var rows = await repository.GetAssignmentExportAsync(actor.InstitutionId, request.DTO.PolicyVersionId, token);
        static string Csv(string? value)
        {
            var safe = value ?? string.Empty;
            if (safe.Length > 0 && "=+-@\t\r\n".Contains(safe[0]))
            {
                safe = "'" + safe;
            }
            return $"\"{safe.Replace("\"", "\"\"")}\"";
        }
        var output = new System.Text.StringBuilder();
        output.AppendLine("AssignmentId,StaffMemberCode,StaffMemberName,StaffEmploymentType,Department,Designation,AssignmentSource,EffectiveFrom,EffectiveTo,IsMandatory,IsActive,IsEffective,StateReason");
        foreach (var row in rows)
        {
            output.Append(row.AssignmentId).Append(',').Append(Csv(row.StaffMemberCode)).Append(',')
                .Append(Csv(row.StaffMemberName)).Append(',').Append(Csv(row.StaffEmploymentTypeName)).Append(',')
                .Append(Csv(row.DepartmentName)).Append(',').Append(Csv(row.DesignationName)).Append(',')
                .Append(row.AssignmentSource).Append(',').Append(row.EffectiveFrom.ToString("yyyy-MM-dd")).Append(',')
                .Append(row.EffectiveTo?.ToString("yyyy-MM-dd") ?? string.Empty).Append(',')
                .Append(row.IsMandatory).Append(',').Append(row.IsActive).Append(',')
                .Append(row.IsEffective).Append(',').Append(Csv(row.StateReason)).AppendLine();
        }
        return ApiResponse<string>.Success(output.ToString(), "Policy assignments exported successfully.");
    }
}

public sealed class PreviewPolicyBulkImportCommandHandler(BulkImportWorkflowService workflow)
    : IRequestHandler<PreviewPolicyBulkImportCommand, ApiResponse<BulkImportPreviewResponseDTO>>
{
    public async Task<ApiResponse<BulkImportPreviewResponseDTO>> Handle(
        PreviewPolicyBulkImportCommand request,
        CancellationToken token)
    {
        var data = await workflow.PreviewAsync(request.Master, request.DTO, token);
        return ApiResponse<BulkImportPreviewResponseDTO>.Success(data, "Policy import preview saved successfully.");
    }
}

public sealed class ManagePolicyBulkImportCommandHandler(BulkImportWorkflowService workflow)
    : IRequestHandler<ManagePolicyBulkImportCommand, ApiResponse<object>>
{
    public async Task<ApiResponse<object>> Handle(ManagePolicyBulkImportCommand request, CancellationToken token)
    {
        var data = await workflow.ActAsync(request.Master, request.Action, request.DTO, token);
        return ApiResponse<object>.Success(data, "Policy import action completed successfully.");
    }
}

#endregion
