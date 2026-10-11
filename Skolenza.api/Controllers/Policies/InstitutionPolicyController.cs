using System.Text;
using skolenza.api.Common;
using skolenza.application.Common.Enums;
using skolenza.application.Constants;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Policy;
using skolenza.application.Exceptions;
using skolenza.application.Features.GenericPolicyCmd;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Policies;

/// <summary>Exposes institution-isolated generic policy configuration and lifecycle operations.</summary>
[ApiController]
[Authorize]
[Route("api/InstitutionPolicy")]
public sealed class InstitutionPolicyController(IMediator mediator) : ControllerBase
{
    /// <summary>Gets the master lookup values required by the Institution Policy screens.</summary>
    /// <remarks>
    /// Use this endpoint when opening the Policy Type or Policy Editor screen. It returns policy
    /// categories, lifecycle statuses, rule types, document types and enum-backed Attendance
    /// location scopes and institution Leave Types for dropdowns. Send the
    /// dynamically resolved Policy Types module ID and View operation ID; do not hard-code the
    /// numeric examples from the UI handoff. This endpoint reads data only and creates no policy.
    /// </remarks>
    [HttpGet("lookups")]
    public async Task<IActionResult> GetLookups([FromQuery] PolicyAccessRequestDTO dto) => Ok(await mediator.Send(new GetPolicyLookupsQuery(dto)));

    /// <summary>Lists the institution's policy-type masters for selection and administration.</summary>
    /// <remarks>
    /// Call this after lookups to populate the Policy Type dropdown used by policy creation.
    /// The active filter can be used to request active or inactive rows. This is the supported
    /// Policy Type list for the new generic Institution Policy flow; do not use the legacy
    /// /api/PolicyType/get-ddl route. Requires Policy Types View permission.
    /// </remarks>
    [HttpGet("types")]
    public async Task<IActionResult> GetTypes([FromQuery] PolicyAccessRequestDTO dto) => Ok(await mediator.Send(new GetPolicyTypesQuery(dto)));

    /// <summary>Creates a institution Policy Type such as Leave, Attendance or Expense.</summary>
    /// <remarks>
    /// Policy Type is the master under which actual policies are created. Use the returned type
    /// ID as policyTypeId in POST /api/InstitutionPolicy. Code and category references are validated
    /// inside the authenticated institution. Requires Policy Types Add permission.
    /// </remarks>
    [HttpPost("types")]
    public async Task<IActionResult> CreateType([FromBody] CreateGenericPolicyTypeRequestDTO dto) => Ok(await mediator.Send(new CreatePolicyTypeCommand(dto)));

    /// <summary>Updates an existing institution Policy Type.</summary>
    /// <remarks>
    /// Changes the selected Policy Type's code, name, description, category, default currency and
    /// active state. The route ID is authoritative and is copied into the request by the server.
    /// Use the status endpoint when only activation is changing. Requires Policy Types Update
    /// permission.
    /// </remarks>
    /// <param name="id">Institution-owned Policy Type ID returned by the type list.</param>
    /// <param name="dto">Complete updated Policy Type values plus dynamic permission IDs.</param>
    [HttpPut("types/{id:int}")]
    public async Task<IActionResult> UpdateType(int id, [FromBody] UpdateGenericPolicyTypeRequestDTO dto)
    {
        dto.Id = id;
        return Ok(await mediator.Send(new UpdateGenericPolicyTypeCommand(dto)));
    }

    /// <summary>Activates or deactivates a institution Policy Type.</summary>
    /// <remarks>
    /// Use this endpoint for the Policy Type status toggle. It does not publish, archive or alter
    /// Policy Versions. Inactive types can be excluded from creation dropdowns by requesting only
    /// active types. Requires the matching Policy Types Active or Inactive operation permission.
    /// </remarks>
    /// <param name="id">Institution-owned Policy Type ID.</param>
    /// <param name="dto">Desired isActive value plus dynamic permission IDs.</param>
    [HttpPatch("types/{id:int}/status")]
    public async Task<IActionResult> ChangeTypeStatus(int id, [FromBody] ChangePolicyTypeStatusRequestDTO dto)
    {
        dto.Id = id;
        return Ok(await mediator.Send(new ChangePolicyTypeStatusCommand(dto)));
    }

    /// <summary>Gets the paged policy-definition list for the current institution.</summary>
    /// <remarks>
    /// Use this endpoint on the Policy List or dashboard. It supports paging and optional policy
    /// type, lifecycle status and text-search filters. Each row identifies the Policy and its
    /// current version. Requires Policy Definitions View permission.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PolicyListRequestDTO dto) => Ok(await mediator.Send(new GetPoliciesQuery(dto)));

    /// <summary>Gets one Policy with its selected/current version, rules and applicability.</summary>
    /// <remarks>
    /// Use this after create/update to verify saved data and on editor, detail and review screens.
    /// The response contains policyId, versionId, lifecycle status, rules and targeting criteria.
    /// Requires Policy Definitions View permission.
    /// </remarks>
    /// <param name="id">Institution-owned Policy ID, not a Policy Version ID.</param>
    /// <param name="dto">Dynamic Policy Definitions View permission IDs.</param>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, [FromQuery] PolicyByIdRequestDTO dto)
    {
        dto.Id = id;
        return Ok(await mediator.Send(new GetPolicyQuery(dto)));
    }

    /// <summary>Gets one exact Policy Version with its rules and applicability.</summary>
    /// <remarks>
    /// Use this endpoint in version workspaces and review screens. Unlike the Policy endpoint,
    /// this route never substitutes the current version for the version named in the URL.
    /// Requires Policy Definitions View permission.
    /// </remarks>
    [HttpGet("{policyId:long}/versions/{versionId:long}")]
    public async Task<IActionResult> GetVersion(
        long policyId,
        long versionId,
        [FromQuery] PolicyVersionByIdRequestDTO dto)
    {
        dto.PolicyId = policyId;
        dto.PolicyVersionId = versionId;
        return Ok(await mediator.Send(new GetPolicyVersionQuery(dto)));
    }

    /// <summary>Creates a Policy and its Version 1 Draft in one transaction.</summary>
    /// <remarks>
    /// This is the main starting point for a new policy. It atomically saves policy identity,
    /// effective dates, rules and applicability. Leave-category policies also require covered
    /// leaveTypeIds, and each rule/applicability row must target one or more of those IDs.
    /// ruleConfiguration values must be JSON-object
    /// strings; dates use yyyy-MM-dd; applicabilityMode is 1 Include or 2 Exclude. Preserve the
    /// returned policy ID and version ID. The result remains Draft and must later be submitted,
    /// approved and published. Requires Policy Definitions Add permission.
    /// </remarks>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePolicyRequestDTO dto) => Ok(await mediator.Send(new CreatePolicyCommand(dto)));

    /// <summary>Replaces the editable Draft version's details, rules and applicability.</summary>
    /// <remarks>
    /// Use this when saving corrections from the Policy Editor. The update is atomic and replaces
    /// the submitted Draft collections; it is not a partial patch. Published and Archived versions
    /// are immutable. To change a Published policy, clone it into the next Draft first. Requires
    /// Policy Definitions Update permission.
    /// </remarks>
    /// <param name="policyId">Policy identity ID returned by create/list.</param>
    /// <param name="versionId">Editable Draft version ID.</param>
    /// <param name="dto">Complete replacement Draft payload plus permission IDs.</param>
    [HttpPut("{policyId:long}/versions/{versionId:long}")]
    public async Task<IActionResult> UpdateDraft(long policyId, long versionId, [FromBody] UpdatePolicyDraftRequestDTO dto)
    {
        dto.PolicyId = policyId;
        dto.PolicyVersionId = versionId;
        return Ok(await mediator.Send(new UpdatePolicyDraftCommand(dto)));
    }

    /// <summary>Clones an existing Policy Version into the next editable Draft.</summary>
    /// <remarks>
    /// Use this to revise an immutable Published version. Rules, applicability and Attendance
    /// configuration are copied into the next Draft. Documents, assignments, acknowledgements,
    /// exceptions and approval history remain with the source version. The Draft must follow
    /// Submit, Approve and Publish again. Requires Policy Definitions Add permission.
    /// </remarks>
    /// <param name="policyId">Policy identity that owns the source version.</param>
    /// <param name="dto">Source version, new effective date, change summary and permission IDs.</param>
    [HttpPost("{policyId:long}/versions/clone")]
    public async Task<IActionResult> Clone(long policyId, [FromBody] ClonePolicyVersionRequestDTO dto)
    {
        dto.PolicyId = policyId;
        return Ok(await mediator.Send(new ClonePolicyVersionCommand(dto)));
    }

    /// <summary>Moves a Policy Version through Submit, Approve, Reject, Publish or Archive.</summary>
    /// <remarks>
    /// Supported action values are SUBMIT, APPROVE, REJECT, PUBLISH and ARCHIVE. Valid flow is
    /// Draft or Rejected to Under Review, Under Review to Approved or Rejected, Approved to
    /// Published, and Published to Archived. SUBMIT uses Policy Definitions Submit permission;
    /// every other action uses its matching Policy Approvals operation. Mandatory approval stages
    /// run in order, enforce approver roles and minimum approval counts, and prevent the same
    /// staffMember approving one stage twice. Submit and Publish require an staffMember-visible Policy
    /// Document. Approval fingerprints the version and document checksums; Publish rejects content
    /// that differs from the final approval. Invalid transitions return Conflict.
    /// </remarks>
    /// <param name="versionId">Policy Version being transitioned.</param>
    /// <param name="dto">Action, optional comments and dynamically resolved permission IDs.</param>
    [HttpPost("versions/{versionId:long}/transition")]
    public async Task<IActionResult> Transition(long versionId, [FromBody] PolicyTransitionRequestDTO dto)
    {
        dto.PolicyVersionId = versionId;
        return Ok(await mediator.Send(new TransitionPolicyCommand(dto)));
    }

    /// <summary>Resolves the effective Published policies for an staffMember on a date.</summary>
    /// <remarks>
    /// Use this for applicability preview and to display an staffMember's effective policy set. This
    /// endpoint calculates results only; it does not create assignments. Manual assignments have
    /// highest precedence, followed by the most specific applicability match. At equal specificity,
    /// the lowest numeric priority wins and Exclude wins a tie. Requires Policy Definitions View
    /// permission.
    /// </remarks>
    [HttpGet("resolve")]
    public async Task<IActionResult> Resolve([FromQuery] ResolveStaffMemberPoliciesRequestDTO dto) => Ok(await mediator.Send(new ResolveStaffMemberPoliciesQuery(dto)));

    /// <summary>Manually assigns a Published Policy Version to one or more staffMembers.</summary>
    /// <remarks>
    /// Use this only after the version is Published. The operation is idempotent for the same
    /// version, staffMember and start date: duplicates are reported as existing, while a matching
    /// removed assignment is reactivated. Missing acknowledgement rows are created as Assigned in
    /// the same transaction. Manual assignment takes precedence over applicability. Requires Policy
    /// Assignments Assign permission.
    /// </remarks>
    [HttpPost("assignments")]
    public async Task<IActionResult> Assign([FromBody] AssignPolicyRequestDTO dto) => Ok(await mediator.Send(new AssignPolicyCommand(dto)));

    /// <summary>Deactivates a manual Policy Assignment without deleting its history.</summary>
    /// <remarks>
    /// Use this from the assignment list when an staffMember should no longer have the explicit
    /// assignment. The Policy, Version and audit/history records remain intact. Requires Policy
    /// Assignments Remove permission.
    /// </remarks>
    /// <param name="assignmentId">Assignment row ID returned by the version assignment list.</param>
    /// <param name="dto">Dynamic Policy Assignments Remove permission IDs.</param>
    [HttpDelete("assignments/{assignmentId:long}")]
    public async Task<IActionResult> RemoveAssignment(long assignmentId, [FromQuery] RemovePolicyAssignmentRequestDTO dto)
    {
        dto.AssignmentId = assignmentId;
        return Ok(await mediator.Send(new RemovePolicyAssignmentCommand(dto)));
    }

    /// <summary>Submits a temporary staffMember-specific exception to a Policy Version.</summary>
    /// <remarks>
    /// Use this for an approved-period override such as a temporary accommodation. The
    /// overrideConfiguration value must be a JSON-object string, and effective dates define the
    /// exception window. Creating an exception does not approve it. Requires Policy Exceptions Add
    /// permission.
    /// </remarks>
    [HttpPost("exceptions")]
    public async Task<IActionResult> CreateException([FromBody] CreatePolicyExceptionRequestDTO dto) => Ok(await mediator.Send(new CreatePolicyExceptionCommand(dto)));

    /// <summary>Approves or rejects a submitted Policy Exception.</summary>
    /// <remarks>
    /// Set approve to true to approve the temporary override or false to reject it. Use the
    /// exception ID returned by create/list. Requires the matching Policy Exceptions Approve or
    /// Reject operation permission.
    /// </remarks>
    /// <param name="exceptionId">Institution-owned Policy Exception ID.</param>
    /// <param name="dto">Decision flag and dynamically resolved permission IDs.</param>
    [HttpPost("exceptions/{exceptionId:long}/decision")]
    public async Task<IActionResult> DecideException(long exceptionId, [FromBody] ApprovePolicyExceptionRequestDTO dto)
    {
        dto.ExceptionId = exceptionId;
        return Ok(await mediator.Send(new ApprovePolicyExceptionCommand(dto)));
    }

    /// <summary>Records the authenticated staffMember's acknowledgement of an assigned policy.</summary>
    /// <remarks>
    /// The caller can acknowledge only for themselves and only while an active assignment exists.
    /// evidenceJson may record UI source or similar evidence as a JSON-object string. Requires
    /// Policy Acknowledgements Acknowledge permission.
    /// </remarks>
    [HttpPost("acknowledgements")]
    public async Task<IActionResult> Acknowledge([FromBody] AcknowledgePolicyRequestDTO dto) => Ok(await mediator.Send(new AcknowledgePolicyCommand(dto)));

    /// <summary>Uploads a PDF, DOC or DOCX document for an editable Policy Version.</summary>
    /// <remarks>
    /// Send multipart/form-data with policyVersionId, policyDocumentTypeId, documentTitle,
    /// languageCode, isStaffMemberVisible, file, moduleId and operationId. File size must be from one
    /// byte through 10 MB. The API calculates SHA-256, stores the object and persists its key.
    /// Documents can change only while the version is Draft or Rejected. Review, Published and
    /// Archived versions are immutable. Requires Policy Definitions Upload permission.
    /// </remarks>
    [HttpPost("documents")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadDocument([FromForm] UploadPolicyDocumentRequestDTO dto) => Ok(await mediator.Send(new UploadPolicyDocumentCommand(dto)));

    /// <summary>Lists the active documents for a Policy Version with temporary URLs.</summary>
    /// <remarks>
    /// Use this in the Policy Editor or Version Viewer after upload and whenever documents must be
    /// displayed or downloaded. Temporary URLs should not be stored by the UI. Requires Policy
    /// Definitions View permission.
    /// </remarks>
    /// <param name="versionId">Policy Version whose documents are requested.</param>
    /// <param name="dto">Dynamic Policy Definitions View permission IDs.</param>
    [HttpGet("versions/{versionId:long}/documents")]
    public async Task<IActionResult> GetDocuments(long versionId, [FromQuery] PolicyDocumentsRequestDTO dto)
    {
        dto.PolicyVersionId = versionId;
        return Ok(await mediator.Send(new GetPolicyDocumentsQuery(dto)));
    }

    /// <summary>Soft-deletes Policy Document metadata and removes its stored object.</summary>
    /// <remarks>
    /// Use this only while the owning Policy Version is editable. Documents belonging to Published
    /// or Archived versions cannot be changed. Requires Policy Definitions Delete permission.
    /// </remarks>
    /// <param name="documentId">Document ID returned by upload or document list.</param>
    /// <param name="dto">Dynamic Policy Definitions Delete permission IDs.</param>
    [HttpDelete("documents/{documentId:long}")]
    public async Task<IActionResult> DeleteDocument(long documentId, [FromQuery] DeletePolicyDocumentRequestDTO dto)
    {
        dto.DocumentId = documentId;
        return Ok(await mediator.Send(new DeletePolicyDocumentCommand(dto)));
    }

    /// <summary>Gets newest-first immutable audit evidence for a Policy.</summary>
    /// <remarks>
    /// Use this on the Policy Audit screen to show entity, action, before/after data, actor,
    /// timestamp, version and correlation evidence. Policy Audit is a separate permission leaf;
    /// another policy module's View permission cannot be reused.
    /// </remarks>
    /// <param name="policyId">Policy identity ID, not a Policy Version ID.</param>
    /// <param name="dto">Dynamic Policy Audit View permission IDs.</param>
    [HttpGet("{policyId:long}/audit")]
    public async Task<IActionResult> GetAudit(long policyId, [FromQuery] PolicyByIdRequestDTO dto)
    {
        dto.Id = policyId;
        return Ok(await mediator.Send(new GetPolicyAuditQuery(dto)));
    }

    /// <summary>Lists global or category-specific Policy Approval Stages.</summary>
    /// <remarks>
    /// Use this on approval setup screens and before submission to explain the expected review
    /// route. Filter by policyCategoryId and active state as needed. A null category represents a
    /// institution-wide stage. Requires Policy Approvals View permission.
    /// </remarks>
    [HttpGet("approval-stages")]
    public async Task<IActionResult> GetApprovalStages([FromQuery] PolicyApprovalStageListRequestDTO dto) =>
        Ok(await mediator.Send(new GetPolicyApprovalStagesQuery(dto)));

    /// <summary>Creates an ordered Policy Approval Stage.</summary>
    /// <remarks>
    /// Configure stage name, unique order within the category, optional approver role, minimum
    /// approval count and mandatory flag. A null policyCategoryId creates a institution-wide stage; a
    /// null approverRoleId allows any caller who already holds endpoint Approve permission.
    /// Category and role references are institution-validated. Requires Policy Approvals Add permission.
    /// </remarks>
    [HttpPost("approval-stages")]
    public async Task<IActionResult> CreateApprovalStage([FromBody] CreatePolicyApprovalStageRequestDTO dto) =>
        Ok(await mediator.Send(new CreatePolicyApprovalStageCommand(dto)));

    /// <summary>Updates an existing Policy Approval Stage.</summary>
    /// <remarks>
    /// Use this to change stage name, order, approver role, minimum approvals, mandatory state or
    /// active state. The route ID is authoritative and stage/category references are validated.
    /// Historical approval evidence is not rewritten. Requires Policy Approvals Update permission.
    /// </remarks>
    /// <param name="id">Approval Stage ID returned by the stage list.</param>
    /// <param name="dto">Complete updated stage values plus dynamic permission IDs.</param>
    [HttpPut("approval-stages/{id:long}")]
    public async Task<IActionResult> UpdateApprovalStage(long id, [FromBody] UpdatePolicyApprovalStageRequestDTO dto)
    {
        dto.Id = id;
        return Ok(await mediator.Send(new UpdatePolicyApprovalStageCommand(dto)));
    }

    /// <summary>Disables a Policy Approval Stage while preserving historical references.</summary>
    /// <remarks>
    /// This is a logical disable, not destructive history deletion. Use it when a stage should no
    /// longer participate in future approval cycles. Requires Policy Approvals Delete permission.
    /// </remarks>
    /// <param name="id">Approval Stage ID to disable.</param>
    /// <param name="dto">Dynamic Policy Approvals Delete permission IDs.</param>
    [HttpDelete("approval-stages/{id:long}")]
    public async Task<IActionResult> DeleteApprovalStage(long id, [FromQuery] DeletePolicyApprovalStageRequestDTO dto)
    {
        dto.Id = id;
        return Ok(await mediator.Send(new DeletePolicyApprovalStageCommand(dto)));
    }

    /// <summary>Gets stage-by-stage approval progress for a Policy Version.</summary>
    /// <remarks>
    /// Use this in the Approval Inbox and refresh it after every Approve or Reject action. Each row
    /// reports stage order, minimum required approvals, current approval count and completion state.
    /// Publish should be offered only after all mandatory stages have completed and the version is
    /// Approved. Requires Policy Approvals View permission.
    /// </remarks>
    /// <param name="versionId">Policy Version currently in the approval lifecycle.</param>
    /// <param name="dto">Dynamic Policy Approvals View permission IDs.</param>
    [HttpGet("versions/{versionId:long}/approval-progress")]
    public async Task<IActionResult> GetApprovalProgress(long versionId, [FromQuery] PolicyVersionAccessRequestDTO dto)
    {
        dto.PolicyVersionId = versionId;
        return Ok(await mediator.Send(new GetPolicyApprovalProgressQuery(dto)));
    }

    /// <summary>Lists active and removed staffMember assignments for a Policy Version.</summary>
    /// <remarks>
    /// Use this after manual assignment, removal or assignment import to verify persistence and to
    /// obtain assignment IDs for removal. The response includes staffMember, source, effective dates,
    /// mandatory flag, persisted active state, effective state and state reason. An Archived
    /// version preserves rows but reports active rows as ineffective. Requires Policy Assignments
    /// View permission.
    /// </remarks>
    /// <param name="versionId">Published Policy Version whose assignments are requested.</param>
    /// <param name="dto">Dynamic Policy Assignments View permission IDs.</param>
    [HttpGet("versions/{versionId:long}/assignments")]
    public async Task<IActionResult> GetAssignments(long versionId, [FromQuery] PolicyVersionAccessRequestDTO dto)
    {
        dto.PolicyVersionId = versionId;
        return Ok(await mediator.Send(new GetPolicyAssignmentsQuery(dto)));
    }

    /// <summary>Returns only staffMembers eligible under the selected Policy Version applicability.</summary>
    /// <remarks>
    /// Use this when opening the Map Policies popup. StaffMember code, staffMember type, department and
    /// designation are returned with every row to disambiguate duplicate names. The same
    /// applicability resolution used by assignment validation is applied before paging. Filter
    /// options are calculated from the eligible population. Requires Policy Assignments View
    /// permission.
    /// </remarks>
    [HttpGet("versions/{versionId:long}/assignment-candidates")]
    public async Task<IActionResult> GetAssignmentCandidates(
        long versionId,
        [FromQuery] PolicyAssignmentCandidateRequestDTO dto)
    {
        dto.PolicyVersionId = versionId;
        return Ok(await mediator.Send(new GetPolicyAssignmentCandidatesQuery(dto)));
    }

    /// <summary>Exports the complete staffMember mapping list for a Policy Version.</summary>
    /// <remarks>
    /// The CSV includes staffMember code, staffMember name, staffMember type, department, designation,
    /// effective dates and active state. It is not paged. Requires Policy Assignments Export
    /// permission resolved through the existing dynamic permission pipeline.
    /// </remarks>
    [HttpGet("versions/{versionId:long}/assignments/export")]
    public async Task<IActionResult> ExportAssignments(
        long versionId,
        [FromQuery] PolicyVersionAccessRequestDTO dto,
        CancellationToken cancellationToken)
    {
        dto.PolicyVersionId = versionId;
        var response = await mediator.Send(new ExportPolicyAssignmentsQuery(dto), cancellationToken);
        return File(Encoding.UTF8.GetBytes(response.Data ?? string.Empty), "text/csv; charset=utf-8",
            $"Policy-version-{versionId}-assignments.csv");
    }

    /// <summary>Lists staffMember exceptions and their decisions for a Policy Version.</summary>
    /// <remarks>
    /// Use this on the Exceptions screen to review override configuration, reason, effective window,
    /// approval status and active state. Requires Policy Exceptions View permission.
    /// </remarks>
    /// <param name="versionId">Policy Version whose exception records are requested.</param>
    /// <param name="dto">Dynamic Policy Exceptions View permission IDs.</param>
    [HttpGet("versions/{versionId:long}/exceptions")]
    public async Task<IActionResult> GetExceptions(long versionId, [FromQuery] PolicyVersionAccessRequestDTO dto)
    {
        dto.PolicyVersionId = versionId;
        return Ok(await mediator.Send(new GetPolicyExceptionsQuery(dto)));
    }

    /// <summary>Lists assignment, viewing and acknowledgement evidence for a Policy Version.</summary>
    /// <remarks>
    /// Use this on the Acknowledgements screen to show which staffMembers were assigned, viewed and
    /// accepted the policy and at what time. Requires Policy Acknowledgements View permission.
    /// </remarks>
    /// <param name="versionId">Policy Version whose acknowledgement evidence is requested.</param>
    /// <param name="dto">Dynamic Policy Acknowledgements View permission IDs.</param>
    [HttpGet("versions/{versionId:long}/acknowledgements")]
    public async Task<IActionResult> GetAcknowledgements(long versionId, [FromQuery] PolicyVersionAccessRequestDTO dto)
    {
        dto.PolicyVersionId = versionId;
        return Ok(await mediator.Send(new GetPolicyAcknowledgementsQuery(dto)));
    }

    /// <summary>Uploads and validates a CSV/XLSX policy import without writing final target rows.</summary>
    /// <remarks>
    /// First step of the durable bulk flow. target must be types, definitions or assignments.
    /// Send multipart/form-data with file or pastedText, optional columnMappingJson and sheetName,
    /// plus the target leaf module ID and Import operation ID. The response contains jobId, detected
    /// columns, mapping and row validation results. Preview creates an unconfirmed Draft job only;
    /// inspect errors before calling confirm.
    /// </remarks>
    /// <param name="target">Exactly types, definitions or assignments.</param>
    /// <param name="dto">Multipart source, optional mapping/sheet and dynamic Import permission IDs.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpPost("bulk/{target}/preview")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(BulkImportConstants.MaxFileBytes + 65536)]
    public async Task<IActionResult> PreviewBulk(string target, [FromForm] BulkImportPreviewRequestDTO dto,
        CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new PreviewPolicyBulkImportCommand(ParseBulkTarget(target), dto), cancellationToken));
    }

    /// <summary>Confirms a valid bulk preview and queues it for background processing.</summary>
    /// <remarks>
    /// Second step of the durable bulk flow. Send the jobId returned by preview with the same target
    /// leaf module and Import operation. A successful response normally means Queued, not that final
    /// rows already exist. Poll the job endpoint until a terminal status, then download the report.
    /// </remarks>
    /// <param name="target">Same target used by preview: types, definitions or assignments.</param>
    /// <param name="dto">Preview jobId and dynamic Import permission IDs.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpPost("bulk/{target}/confirm")]
    public async Task<IActionResult> ConfirmBulk(string target, [FromBody] BulkImportJobRequestDTO dto,
        CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new ManagePolicyBulkImportCommand(ParseBulkTarget(target),
            BulkImportAction.Confirm, dto), cancellationToken));
    }

    /// <summary>Gets current progress and row results for one policy bulk-import job.</summary>
    /// <remarks>
    /// Poll this endpoint after confirm. Status can be Draft, Queued, Running, Completed,
    /// CompletedWithErrors, Failed, CancelRequested or Cancelled. Stop polling on a terminal status
    /// and download the final report; confirm alone must not be treated as persistence success.
    /// Requires the target leaf's View permission.
    /// </remarks>
    /// <param name="target">Job target: types, definitions or assignments.</param>
    /// <param name="jobId">Job ID returned by preview/confirm.</param>
    /// <param name="dto">Dynamic target-leaf View permission IDs.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet("bulk/{target}/jobs/{jobId:guid}")]
    public async Task<IActionResult> GetBulkJob(string target, Guid jobId, [FromQuery] BulkImportJobRequestDTO dto,
        CancellationToken cancellationToken)
    {
        dto.JobId = jobId;
        return Ok(await mediator.Send(new ManagePolicyBulkImportCommand(ParseBulkTarget(target),
            BulkImportAction.Get, dto), cancellationToken));
    }

    /// <summary>Lists the current institution's bulk-import jobs for one policy target.</summary>
    /// <remarks>
    /// Use this for import history, recovery after navigation and selecting jobs for progress/report
    /// views. Results are isolated by authenticated institution and target and support paging. Requires
    /// the selected target leaf's View permission.
    /// </remarks>
    /// <param name="target">Job target: types, definitions or assignments.</param>
    /// <param name="dto">Paging/filter values and dynamic target-leaf View permission IDs.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet("bulk/{target}/jobs")]
    public async Task<IActionResult> ListBulkJobs(string target, [FromQuery] BulkImportJobRequestDTO dto,
        CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new ManagePolicyBulkImportCommand(ParseBulkTarget(target),
            BulkImportAction.List, dto), cancellationToken));
    }

    /// <summary>Creates a fresh queued job for retryable failed import rows.</summary>
    /// <remarks>
    /// Use this only after a job has genuine retryable failed worker rows. Retry never edits the
    /// original job or report; it creates a new job that must be polled independently. Existing or
    /// successful rows are not duplicated. Requires the target leaf's Import permission.
    /// </remarks>
    /// <param name="target">Original job target: types, definitions or assignments.</param>
    /// <param name="dto">Original jobId and dynamic Import permission IDs.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpPost("bulk/{target}/retry")]
    public async Task<IActionResult> RetryBulk(string target, [FromBody] BulkImportJobRequestDTO dto,
        CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new ManagePolicyBulkImportCommand(ParseBulkTarget(target),
            BulkImportAction.Retry, dto), cancellationToken));
    }

    /// <summary>Cancels or requests cancellation of a policy bulk-import job.</summary>
    /// <remarks>
    /// Draft and Queued jobs cancel immediately. Running jobs become CancelRequested and stop at the
    /// worker's next safe boundary. Completed, CompletedWithErrors, Failed and Cancelled jobs are
    /// terminal and cannot be cancelled. Requires the target leaf's Import permission.
    /// </remarks>
    /// <param name="target">Job target: types, definitions or assignments.</param>
    /// <param name="dto">JobId and dynamic Import permission IDs.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpPost("bulk/{target}/cancel")]
    public async Task<IActionResult> CancelBulk(string target, [FromBody] BulkImportJobRequestDTO dto,
        CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new ManagePolicyBulkImportCommand(ParseBulkTarget(target),
            BulkImportAction.Cancel, dto), cancellationToken));
    }

    /// <summary>Downloads the exact CSV header template for a policy bulk target.</summary>
    /// <remarks>
    /// Call this before preparing an import file. target types returns Policy Type columns;
    /// definitions returns Policy/Version/Rules/Applicability columns; assignments returns Published
    /// version and staffMember columns. The response is a text/csv file. Requires the target leaf's
    /// View permission.
    /// </remarks>
    /// <param name="target">Template target: types, definitions or assignments.</param>
    /// <param name="dto">Dynamic target-leaf View permission IDs.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet("bulk/{target}/template")]
    public async Task<IActionResult> GetBulkTemplate(string target, [FromQuery] BulkImportJobRequestDTO dto,
        CancellationToken cancellationToken)
    {
        var master = ParseBulkTarget(target);
        var response = await mediator.Send(new ManagePolicyBulkImportCommand(master,
            BulkImportAction.Template, dto), cancellationToken);
        return File(Encoding.UTF8.GetBytes((string)response.Data!), "text/csv; charset=utf-8",
            $"Policy-{target}-template.csv");
    }

    /// <summary>Downloads the final row-by-row CSV report for a policy bulk-import job.</summary>
    /// <remarks>
    /// Use this after the job reaches a terminal status. Each row reports Created, Existing, Failed
    /// or reactivated outcome and its errors. This report is the final persistence evidence; a
    /// successful confirm response is not. Requires the target leaf's View permission.
    /// </remarks>
    /// <param name="target">Job target: types, definitions or assignments.</param>
    /// <param name="jobId">Terminal job ID.</param>
    /// <param name="dto">Dynamic target-leaf View permission IDs.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet("bulk/{target}/jobs/{jobId:guid}/report")]
    public async Task<IActionResult> GetBulkReport(string target, Guid jobId, [FromQuery] BulkImportJobRequestDTO dto,
        CancellationToken cancellationToken)
    {
        dto.JobId = jobId;
        var response = await mediator.Send(new ManagePolicyBulkImportCommand(ParseBulkTarget(target),
            BulkImportAction.Get, dto), cancellationToken);
        return File(BulkImportReport.Create((BulkImportJobResponseDTO)response.Data!),
            "text/csv; charset=utf-8", $"Policy-{target}-import-{jobId}.csv");
    }

    private static BulkImportMaster ParseBulkTarget(string target)
    {
        return target.Trim().ToLowerInvariant() switch
        {
            "types" => BulkImportMaster.PolicyType,
            "definitions" => BulkImportMaster.PolicyDefinition,
            "assignments" => BulkImportMaster.PolicyAssignment,
            _ => throw new ValidationErrorException("Policy bulk target must be types, definitions or assignments.")
        };
    }
}
