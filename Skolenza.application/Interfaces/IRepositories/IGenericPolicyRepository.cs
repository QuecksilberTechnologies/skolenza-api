using skolenza.application.DTOS.Policy;
using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IRepositories;

public interface IGenericPolicyRepository
{
    Task<IReadOnlyList<PolicyLookupResponseDTO>> GetCategoriesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PolicyLookupResponseDTO>> GetStatusesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PolicyLookupResponseDTO>> GetRuleTypesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PolicyRuleDefinitionResponseDTO>> GetRuleDefinitionsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PolicyLookupResponseDTO>> GetDocumentTypesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PolicyLeaveTypeResponseDTO>> GetLeaveTypesAsync(long institutionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PolicyTypeResponseDTO>> GetPolicyTypesAsync(long institutionId, bool isActive, CancellationToken cancellationToken);
    Task<PolicyTypeResponseDTO> CreatePolicyTypeAsync(long institutionId, long actorId, CreateGenericPolicyTypeRequestDTO dto, CancellationToken cancellationToken);
    Task<PolicyTypeResponseDTO> UpdatePolicyTypeAsync(long institutionId, long actorId, UpdateGenericPolicyTypeRequestDTO dto, CancellationToken cancellationToken);
    Task<bool> ChangePolicyTypeStatusAsync(long institutionId, long actorId, ChangePolicyTypeStatusRequestDTO dto, CancellationToken cancellationToken);
    Task<(IReadOnlyList<PolicySummaryResponseDTO> Items, int Total)> GetPoliciesAsync(long institutionId, PolicyListRequestDTO dto, CancellationToken cancellationToken);
    Task<PolicyDetailResponseDTO> GetPolicyAsync(long institutionId, long policyId, CancellationToken cancellationToken);
    Task<PolicyDetailResponseDTO> GetPolicyVersionAsync(long institutionId, long policyId, long policyVersionId, CancellationToken cancellationToken);
    Task<PolicyDetailResponseDTO> CreatePolicyAsync(long institutionId, long actorId, CreatePolicyRequestDTO dto, CancellationToken cancellationToken);
    Task<PolicyDetailResponseDTO> UpdateDraftAsync(long institutionId, long actorId, UpdatePolicyDraftRequestDTO dto, CancellationToken cancellationToken);
    Task<PolicyDetailResponseDTO> CloneVersionAsync(long institutionId, long actorId, ClonePolicyVersionRequestDTO dto, CancellationToken cancellationToken);
    Task<PolicyDetailResponseDTO> TransitionAsync(long institutionId, long actorId, PolicyTransitionRequestDTO dto, CancellationToken cancellationToken);
    Task<PolicyAssignmentResultDTO> AssignAsync(long institutionId, long actorId, AssignPolicyRequestDTO dto, CancellationToken cancellationToken);
    Task<bool> RemoveAssignmentAsync(long institutionId, long actorId, long assignmentId, CancellationToken cancellationToken);
    Task<long> CreateExceptionAsync(long institutionId, long actorId, CreatePolicyExceptionRequestDTO dto, CancellationToken cancellationToken);
    Task<bool> ApproveExceptionAsync(long institutionId, long actorId, ApprovePolicyExceptionRequestDTO dto, CancellationToken cancellationToken);
    Task<bool> AcknowledgeAsync(long institutionId, long staffMemberId, AcknowledgePolicyRequestDTO dto, CancellationToken cancellationToken);
    Task<IReadOnlyList<ResolvedPolicyResponseDTO>> ResolveAsync(long institutionId, long staffMemberId, ResolveStaffMemberPoliciesRequestDTO dto, CancellationToken cancellationToken);
    Task<long> AddDocumentAsync(long institutionId, long actorId, UploadPolicyDocumentRequestDTO dto, string objectKey, string checksum, CancellationToken cancellationToken);
    Task<IReadOnlyList<PolicyDocument>> GetDocumentsAsync(long institutionId, long policyVersionId, CancellationToken cancellationToken);
    Task<string> DeleteDocumentAsync(long institutionId, long actorId, long documentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PolicyAuditResponseDTO>> GetAuditAsync(long institutionId, long policyId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PolicyApprovalStageResponseDTO>> GetApprovalStagesAsync(long institutionId, PolicyApprovalStageListRequestDTO dto, CancellationToken cancellationToken);
    Task<PolicyApprovalStageResponseDTO> CreateApprovalStageAsync(long institutionId, long actorId, CreatePolicyApprovalStageRequestDTO dto, CancellationToken cancellationToken);
    Task<PolicyApprovalStageResponseDTO> UpdateApprovalStageAsync(long institutionId, long actorId, UpdatePolicyApprovalStageRequestDTO dto, CancellationToken cancellationToken);
    Task<bool> DeleteApprovalStageAsync(long institutionId, long actorId, long id, CancellationToken cancellationToken);
    Task<IReadOnlyList<PolicyApprovalProgressResponseDTO>> GetApprovalProgressAsync(long institutionId, long policyVersionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PolicyAssignmentResponseDTO>> GetAssignmentsAsync(long institutionId, long policyVersionId, CancellationToken cancellationToken);
    Task<PolicyAssignmentCandidateDataPageDTO> GetAssignmentCandidatesAsync(long institutionId, PolicyAssignmentCandidateRequestDTO dto, CancellationToken cancellationToken);
    Task<IReadOnlyList<PolicyAssignmentExportRowDTO>> GetAssignmentExportAsync(long institutionId, long policyVersionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PolicyExceptionResponseDTO>> GetExceptionsAsync(long institutionId, long policyVersionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PolicyAcknowledgementResponseDTO>> GetAcknowledgementsAsync(long institutionId, long policyVersionId, CancellationToken cancellationToken);
}
