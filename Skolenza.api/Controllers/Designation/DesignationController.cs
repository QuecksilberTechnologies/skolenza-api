// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Exposes institution-scoped department and designation endpoints.
// ================================================================

using skolenza.application.DTOs.Department;
using skolenza.application.DTOs.Designation;
using skolenza.application.DTOS.Designation;
using skolenza.application.Features.DepartmentCmd.Handlers;
using skolenza.application.Features.DesignationCmd.Handlers;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Designation
{
    /// <summary>
    /// Controller responsible for managing Designation operations such as
    /// create, update, delete, and fetch with filtering options.
    /// Uses MediatR for CQRS and custom ILoggerService for logging.
    /// Every designation belongs to a department in the same institution. The same
    /// designation name may exist in different departments; duplicates within
    /// a institution department are rejected on create and update.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DesignationController : ControllerBase
    {
        #region Bulk Import Preview

        /// <summary>Validates Designation Excel/CSV or pasted data and saves a durable draft; master data is unchanged.</summary>
        /// <remarks>
        /// <para>Angular usage status: Not integrated yet.</para>
        /// <para>Send multipart/form-data with File OR PastedText, ModuleId, OperationId,
        /// optional SheetName and ColumnMappingJson (target field to source header).</para>
        /// <para>Uses the existing Designation permission pipeline and trusted login Institution scope.</para>
        /// <para>Returns JobId, source row numbers, mappings, matches and errors.
        /// Confirm only when CanCommit is true. RequestId supports safe retries of the same upload.</para>
        /// </remarks>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("bulk/preview")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(skolenza.application.Constants.BulkImportConstants.MaxFileBytes + 65536)]
        public async Task<IActionResult> PreviewBulkImport(
            [FromForm] skolenza.application.DTOS.Common.BulkImportPreviewRequestDTO dto,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new PreviewDesignationImportQuery(dto),
                cancellationToken);
            return Ok(result);
        }

        #endregion


        #region Durable Bulk Import

        /// <summary>Confirms the saved preview and queues it now or at ScheduledAtUtc. Repeated confirmation is idempotent.</summary>
        /// <remarks>Requires Add or Import operation on this module. No replacement row data is accepted.</remarks>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("bulk/confirm")]
        public async Task<IActionResult> ConfirmBulkImport(
            [FromBody] skolenza.application.DTOS.Common.BulkImportJobRequestDTO dto,
            CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new ManageDesignationImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.Confirm), cancellationToken));
        }

        /// <summary>Gets the caller's saved import including source rows, progress and errors.</summary>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("bulk/jobs/{jobId:guid}")]
        public async Task<IActionResult> GetBulkImport(
            Guid jobId,
            [FromQuery] skolenza.application.DTOS.Common.BulkImportJobRequestDTO dto,
            CancellationToken cancellationToken)
        {
            dto.JobId = jobId;
            return Ok(await _mediator.Send(new ManageDesignationImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.Get), cancellationToken));
        }

        /// <summary>Lists the caller's institution-scoped imports, newest first; PageSize is limited to 100.</summary>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("bulk/jobs")]
        public async Task<IActionResult> ListBulkImports(
            [FromQuery] skolenza.application.DTOS.Common.BulkImportJobRequestDTO dto,
            CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new ManageDesignationImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.List), cancellationToken));
        }

        /// <summary>Retries failed or incomplete rows; already created records are never recreated.</summary>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("bulk/retry")]
        public async Task<IActionResult> RetryBulkImport(
            [FromBody] skolenza.application.DTOS.Common.BulkImportJobRequestDTO dto,
            CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new ManageDesignationImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.Retry), cancellationToken));
        }

        /// <summary>Cancels pending work at a batch boundary. Previously committed records remain.</summary>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("bulk/cancel")]
        public async Task<IActionResult> CancelBulkImport(
            [FromBody] skolenza.application.DTOS.Common.BulkImportJobRequestDTO dto,
            CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new ManageDesignationImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.Cancel), cancellationToken));
        }

        /// <summary>Downloads the CSV column template. Reference values come from existing institution master APIs.</summary>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("bulk/template")]
        public async Task<IActionResult> DownloadBulkTemplate(
            [FromQuery] skolenza.application.DTOS.Common.BulkImportJobRequestDTO dto,
            CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(new ManageDesignationImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.Template), cancellationToken);
            return File(System.Text.Encoding.UTF8.GetBytes((string)response.Data!),
                "text/csv; charset=utf-8", "Designation-template.csv");
        }

        /// <summary>Downloads source row numbers, results and corrections for the caller's import.</summary>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("bulk/jobs/{jobId:guid}/report")]
        public async Task<IActionResult> DownloadBulkReport(
            Guid jobId,
            [FromQuery] skolenza.application.DTOS.Common.BulkImportJobRequestDTO dto,
            CancellationToken cancellationToken)
        {
            dto.JobId = jobId;
            var response = await _mediator.Send(new ManageDesignationImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.Get), cancellationToken);
            return File(skolenza.api.Common.BulkImportReport.Create(
                (skolenza.application.DTOS.Common.BulkImportJobResponseDTO)response.Data!),
                "text/csv; charset=utf-8", "Designation-import-" + jobId + ".csv");
        }

        #endregion

        private readonly IMediator _mediator;
        private readonly ILoggerService _logger;

        public DesignationController(IMediator mediator, ILoggerService logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Used-In-Angular: retrieves designations.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves designation.</para>
        /// <para>Handler flow: GetDesignationQuery is processed by GetDesignationQueryHandler; operation(s): GetAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetDesignationResponseDTO: Id (int), DepartmentId (int), DepartmentName (string?), DesignationName (string?), Description (string?), IsActive (bool)</para>
        /// <para>Angular function(s): DesignationsApi.getDesignations (app/core/services/designations-api.ts:65).</para>
        /// <para>Angular purpose: retrieves designations.</para>
        /// <para>Integrated UI page(s): /app/designations; /app/departments; /app/staffMembers; /app/profile/basic-info</para>
        /// <para>Angular UI component(s): DesignationsStore (app/features/designations/designations.store.ts); DepartmentFilter (app/shared/components/department/department-filter/department-filter.ts); StaffMemberFilter (app/shared/components/staffMember/staffMember-filter/staffMember-filter.ts); StaffMemberManageDialog (app/shared/components/staffMember/staffMember-manage-dialog/staffMember-manage-dialog.ts); Designations (app/features/designations/designations.ts); Departments (app/features/departments/departments.ts); StaffMembers (app/features/staffMembers/staffMembers.ts); StaffMemberBasicInfo (app/features/user-menu/staffMember-profile/staffMember-basic-info/staffMember-basic-info.ts)</para>
        /// </remarks>
        [HttpGet("get")]
        public async Task<IActionResult> GetAllDesignationAsyc([FromQuery] GetDesignationRequestDTO designationRequestDTO)
        {
         //   _logger.LogInfo($"Received request to get designation from userId: {designationRequestDTO.Id}");

            var command = new GetDesignationQuery(designationRequestDTO);
            var result = await _mediator.Send(command);

            return Ok(result);
        }
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: retrieves department.</para>
                /// <para>Handler flow: GetDepartmentQuery is processed by GetDepartmentQueryHandler; operation(s): GetAsync.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetDepartmentResponseDTO: Id (int), DepartmentName (string), IsActive (bool), Description (string?), Remark (string?)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: POST /api/designation/department/group/get.</para>
                /// </remarks>
                [HttpPost("Department/Group/get")]



                public async Task<IActionResult> GetAllDepartmentAsyc([FromBody] GetDepartmentRequestDTO designationRequestDTO)
                {
                    _logger.LogInfo($"Received request to get institution from institutionId: {designationRequestDTO.Id}");

                    var command = new GetDepartmentQuery(designationRequestDTO);
                    var result = await _mediator.Send(command);

                    return Ok(result);
                }

        /// <summary>
        /// Used-In-Angular: retrieves designation options.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves designation option.</para>
        /// <para>Handler flow: GetDesignationOptionQuery is processed by GetDesignationOptionQueryHandler; operation(s): GetOptionAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetDesignationOptionResponseDTO: Id (int), DepartmentId (int), DesignationName (string?)</para>
        /// <para>Angular function(s): DesignationsApi.getDesignationOptions (app/core/services/designations-api.ts:72).</para>
        /// <para>Angular purpose: retrieves designation options.</para>
        /// <para>Integrated UI page(s): /app/staffMembers; /app/departments; /app/profile/basic-info</para>
        /// <para>Angular UI component(s): StaffMembersStore (app/features/staffMembers/staffMembers.store.ts); DepartmentFilter (app/shared/components/department/department-filter/department-filter.ts); StaffMemberFilter (app/shared/components/staffMember/staffMember-filter/staffMember-filter.ts); StaffMemberManageDialog (app/shared/components/staffMember/staffMember-manage-dialog/staffMember-manage-dialog.ts); StaffMembers (app/features/staffMembers/staffMembers.ts); Departments (app/features/departments/departments.ts); StaffMemberBasicInfo (app/features/user-menu/staffMember-profile/staffMember-basic-info/staffMember-basic-info.ts)</para>
        /// </remarks>
        [HttpGet("option")]

        public async Task<IActionResult> getDesignation([FromQuery] GetDesignationOptionRequestDTO requestDTO)
        {
            _logger.LogInfo($"Received request to get designation options for DepartmentId: {requestDTO.DepartmentId}");

            var command = new GetDesignationOptionQuery(requestDTO);
            var result = await _mediator.Send(command);

            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: creates designation.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: creates designation.</para>
        /// <para>Handler flow: CreateDesignationCommand is processed by CreateDesignationCommandHandler; operation(s): CreateAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetDesignationResponseDTO: Id (int), DepartmentId (int), DepartmentName (string?), DesignationName (string?), Description (string?), IsActive (bool)</para>
        /// <para>Angular function(s): DesignationsApi.addDesignation (app/core/services/designations-api.ts:58).</para>
        /// <para>Angular purpose: creates designation.</para>
        /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
        /// <para>Angular UI component(s): DesignationManageDialog (app/features/designations/designation-manage-dialog/designation-manage-dialog.ts)</para>
        /// </remarks>
        [HttpPost("add")]

        public async Task<IActionResult> CreateDesignation([FromBody] CreateDesignationRequestDTO dTO)
        {
            if (dTO == null)
            {
                _logger.LogInfo("Received null request for creating designation.");
                throw new skolenza.application.Exceptions.ValidationErrorException(
                    skolenza.application.Constants.AppConstants.ErrorMessages.InvalidRequest);
            }

            _logger.LogInfo($"Received request to create a new designation: {dTO.DesignationName}");

            var command = new CreateDesignationCommand(dTO);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: deletes designation.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: deletes designation.</para>
        /// <para>Handler flow: DeleteDesignationQuery is processed by DeleteDesignationQueryHandler; operation(s): DeleteDesignationAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): DesignationsApi.deleteDesignation (app/core/services/designations-api.ts:85).</para>
        /// <para>Angular purpose: deletes designation.</para>
        /// <para>Integrated UI page(s): /app/designations</para>
        /// <para>Angular UI component(s): DesignationsStore (app/features/designations/designations.store.ts); Designations (app/features/designations/designations.ts)</para>
        /// </remarks>
        [HttpDelete("delete")]
        public async Task<IActionResult> Delete([FromQuery] DeleteDesignationRequestDTO dTO)
        {
            if (dTO == null)
            {
                _logger.LogInfo("Received null request.");
                throw new skolenza.application.Exceptions.ValidationErrorException(
                    skolenza.application.Constants.AppConstants.ErrorMessages.InvalidRequest);
            }

            _logger.LogInfo($"Received request to delete designation");

            var command = new DeleteDesignationQuery(dTO);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: updates designation.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates designation.</para>
        /// <para>Handler flow: UpdateDesignationCommand is processed by UpdateDesignationCommandHandler; operation(s): GetByIdForInstitutionAsync, UpdateDesignationAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): DesignationsApi.updateDesignation (app/core/services/designations-api.ts:78).</para>
        /// <para>Angular purpose: updates designation.</para>
        /// <para>Integrated UI page(s): /app/designations</para>
        /// <para>Angular UI component(s): DesignationManageDialog (app/features/designations/designation-manage-dialog/designation-manage-dialog.ts); DesignationsStore (app/features/designations/designations.store.ts); Designations (app/features/designations/designations.ts)</para>
        /// </remarks>
        [HttpPut("update")]
        public async Task<IActionResult> UpdateDesignation([FromBody] UpdateDesignationRequestDTO updateDesignationDTO)
        {
            _logger.LogInfo("Received request for update designation: " + updateDesignationDTO.ToString());
            var command = new UpdateDesignationCommand(updateDesignationDTO);
            var result = await _mediator.Send(command);

            return Ok(result);
        }
    }
}
