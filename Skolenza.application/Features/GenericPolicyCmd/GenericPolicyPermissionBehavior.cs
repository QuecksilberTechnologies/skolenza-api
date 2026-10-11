using System.Reflection;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using MediatR;

namespace skolenza.application.Features.GenericPolicyCmd;

public sealed class GenericPolicyPermissionBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork, ICommonRequestService commonRequestService)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (typeof(TRequest).Namespace?.StartsWith("skolenza.application.Features.GenericPolicyCmd", StringComparison.Ordinal) != true)
        {
            return await next();
        }
        var dto = typeof(TRequest).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(x => x.GetValue(request)).OfType<PermissionRequestDTO>().FirstOrDefault()
            ?? throw new ValidationErrorException("Permission request is required.");
        if (dto.ModuleId <= 0 || dto.OperationId <= 0)
        {
            throw new ValidationErrorException("ModuleId and OperationId are required.");
        }
        var moduleCode = await commonRequestService.GetModuleCodeAsync(dto.ModuleId);
        var expectedModuleCode = ExpectedModuleCode(request);
        var isPolicyModule = moduleCode != null && (moduleCode.Equals("INSTITUTION_POLICIES", StringComparison.OrdinalIgnoreCase)
            || moduleCode.StartsWith("INSTITUTION_POLICY_", StringComparison.OrdinalIgnoreCase));
        if (!isPolicyModule || expectedModuleCode != null
            && !moduleCode!.Equals(expectedModuleCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenAccessException("The selected module is not valid for this policy action.");
        }
        var actor = await commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!actor.Success || actor.InstitutionId <= 0 || actor.LoggedInStaffMemberId <= 0 || actor.RoleId <= 0)
        {
            throw new UnauthorizedAccessException(actor.ErrorMessage ?? "Unauthorized request.");
        }
        var permission = await unitOfWork.StoreProcedureRepository.CheckInstitutionStaffMemberPermissionAsync(actor.InstitutionId, actor.LoggedInStaffMemberId, actor.RoleId, dto.ModuleId, dto.OperationId, cancellationToken);
        InstitutionRuntimePermissionValidator.EnsureAllowed(permission);
        return await next();
    }

    private static string? ExpectedModuleCode(TRequest request)
    {
        return request switch
        {
            GetPolicyTypesQuery or CreatePolicyTypeCommand or UpdateGenericPolicyTypeCommand
                or ChangePolicyTypeStatusCommand => "INSTITUTION_POLICY_TYPES",
            GetPoliciesQuery or GetPolicyQuery or GetPolicyVersionQuery or CreatePolicyCommand or UpdatePolicyDraftCommand
                or ClonePolicyVersionCommand or ResolveStaffMemberPoliciesQuery or UploadPolicyDocumentCommand
                or GetPolicyDocumentsQuery or DeletePolicyDocumentCommand => "INSTITUTION_POLICY_DEFINITIONS",
            TransitionPolicyCommand transition when transition.DTO.Action.Trim().Equals("SUBMIT", StringComparison.OrdinalIgnoreCase)
                => "INSTITUTION_POLICY_DEFINITIONS",
            TransitionPolicyCommand => "INSTITUTION_POLICY_APPROVALS",
            AssignPolicyCommand or RemovePolicyAssignmentCommand or GetPolicyAssignmentsQuery
                or GetPolicyAssignmentCandidatesQuery or ExportPolicyAssignmentsQuery
                => "INSTITUTION_POLICY_ASSIGNMENTS",
            CreatePolicyExceptionCommand or ApprovePolicyExceptionCommand or GetPolicyExceptionsQuery
                => "INSTITUTION_POLICY_EXCEPTIONS",
            AcknowledgePolicyCommand or GetPolicyAcknowledgementsQuery
                => "INSTITUTION_POLICY_ACKNOWLEDGEMENTS",
            GetPolicyAuditQuery => "INSTITUTION_POLICY_AUDIT",
            GetPolicyApprovalStagesQuery or CreatePolicyApprovalStageCommand or UpdatePolicyApprovalStageCommand
                or DeletePolicyApprovalStageCommand or GetPolicyApprovalProgressQuery
                => "INSTITUTION_POLICY_APPROVALS",
            PreviewPolicyBulkImportCommand bulk => BulkModuleCode(bulk.Master),
            ManagePolicyBulkImportCommand bulk => BulkModuleCode(bulk.Master),
            _ => null
        };
    }

    private static string BulkModuleCode(BulkImportMaster master)
    {
        return master switch
        {
            BulkImportMaster.PolicyType => "INSTITUTION_POLICY_TYPES",
            BulkImportMaster.PolicyDefinition => "INSTITUTION_POLICY_DEFINITIONS",
            BulkImportMaster.PolicyAssignment => "INSTITUTION_POLICY_ASSIGNMENTS",
            _ => throw new ValidationErrorException("Unsupported policy import target.")
        };
    }
}
