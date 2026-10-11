// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Exposes department endpoints and delegates application errors to middleware.
// ================================================================

using skolenza.application.DTOs.Department;
using skolenza.application.DTOS.Department;
using skolenza.application.Features.DepartmentCmd.Handlers;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Department
{
    [ApiController]
    [Route("api/[controller]")]

    public class DepartmentController : ControllerBase    {
        #region Bulk Import Preview

        /// <summary>Validates Department Excel/CSV or pasted data and saves a durable draft; master data is unchanged.</summary>
        /// <remarks>
        /// <para>Angular usage status: Not integrated yet.</para>
        /// <para>Send multipart/form-data with File OR PastedText, ModuleId, OperationId,
        /// optional SheetName and ColumnMappingJson (target field to source header).</para>
        /// <para>Uses the existing Department permission pipeline and trusted login Institution scope.</para>
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
                new PreviewDepartmentImportQuery(dto),
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
            return Ok(await _mediator.Send(new ManageDepartmentImportCommand(
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
            return Ok(await _mediator.Send(new ManageDepartmentImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.Get), cancellationToken));
        }

        /// <summary>Lists the caller's institution-scoped imports, newest first; PageSize is limited to 100.</summary>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("bulk/jobs")]
        public async Task<IActionResult> ListBulkImports(
            [FromQuery] skolenza.application.DTOS.Common.BulkImportJobRequestDTO dto,
            CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new ManageDepartmentImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.List), cancellationToken));
        }

        /// <summary>Retries failed or incomplete rows; already created records are never recreated.</summary>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("bulk/retry")]
        public async Task<IActionResult> RetryBulkImport(
            [FromBody] skolenza.application.DTOS.Common.BulkImportJobRequestDTO dto,
            CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new ManageDepartmentImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.Retry), cancellationToken));
        }

        /// <summary>Cancels pending work at a batch boundary. Previously committed records remain.</summary>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("bulk/cancel")]
        public async Task<IActionResult> CancelBulkImport(
            [FromBody] skolenza.application.DTOS.Common.BulkImportJobRequestDTO dto,
            CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new ManageDepartmentImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.Cancel), cancellationToken));
        }

        /// <summary>Downloads the CSV column template. Reference values come from existing institution master APIs.</summary>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("bulk/template")]
        public async Task<IActionResult> DownloadBulkTemplate(
            [FromQuery] skolenza.application.DTOS.Common.BulkImportJobRequestDTO dto,
            CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(new ManageDepartmentImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.Template), cancellationToken);
            return File(System.Text.Encoding.UTF8.GetBytes((string)response.Data!),
                "text/csv; charset=utf-8", "Department-template.csv");
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
            var response = await _mediator.Send(new ManageDepartmentImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.Get), cancellationToken);
            return File(skolenza.api.Common.BulkImportReport.Create(
                (skolenza.application.DTOS.Common.BulkImportJobResponseDTO)response.Data!),
                "text/csv; charset=utf-8", "Department-import-" + jobId + ".csv");
        }

        #endregion

        private readonly IMediator _mediator;
        private readonly ILoggerService _logger;

        public DepartmentController(IMediator mediator, ILoggerService logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // -------------------------------------------------------
        // ✅ GET : Filtered list of departments
        // -------------------------------------------------------

        /// <summary>
        /// Used-In-Angular: retrieves departments.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves department.</para>
        /// <para>Handler flow: GetDepartmentQuery is processed by GetDepartmentQueryHandler; operation(s): GetAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetDepartmentResponseDTO: Id (int), DepartmentName (string), IsActive (bool), Description (string?), Remark (string?)</para>
        /// <para>Angular function(s): DepartmentsApi.getDepartments (app/core/services/departments-api.ts:34).</para>
        /// <para>Angular purpose: retrieves departments.</para>
        /// <para>Integrated UI page(s): /app/departments</para>
        /// <para>Angular UI component(s): DepartmentsStore (app/features/departments/departments.store.ts); Departments (app/features/departments/departments.ts)</para>
        /// </remarks>
        [HttpGet("get")]



        public async Task<IActionResult> GetAllDepartmentsAsync([FromQuery] GetDepartmentRequestDTO departmentRequestDTO)
        {
            _logger.LogInfo($"Request received to get departments for InstitutionId: {departmentRequestDTO.UserStaffMemberId}");

            var query = new GetDepartmentQuery(departmentRequestDTO);
            var result = await _mediator.Send(query);

            return Ok(result);
        }

        // -------------------------------------------------------
        // ✅ POST : Create new department
        // -------------------------------------------------------

        /// <summary>
        /// Used-In-Angular: creates department.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: creates department.</para>
        /// <para>Handler flow: CreateDepartmentCommand is processed by CreateDepartmentCommandHandler; operation(s): CreateAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetDepartmentResponseDTO: Id (int), DepartmentName (string), IsActive (bool), Description (string?), Remark (string?)</para>
        /// <para>Angular function(s): DepartmentsApi.addDepartment (app/core/services/departments-api.ts:27).</para>
        /// <para>Angular purpose: creates department.</para>
        /// <para>Integrated UI page(s): /app/departments</para>
        /// <para>Angular UI component(s): DepartmentManageDialog (app/shared/components/department/department-manage-dialog/department-manage-dialog.ts); Departments (app/features/departments/departments.ts)</para>
        /// </remarks>
        [HttpPost("add")]
        public async Task<IActionResult> CreateDepartmentAsync([FromBody] CreateDepartmentRequestDTO createDto)
        {
            if (createDto == null)
            {
                _logger.LogInfo("CreateDepartmentAsync received a null DTO.");
                throw new skolenza.application.Exceptions.ValidationErrorException(
                    skolenza.application.Constants.AppConstants.ErrorMessages.InvalidRequest);
            }

            _logger.LogInfo($"Creating new department: {createDto.DepartmentName}");
            var command = new  CreateDepartmentCommand(createDto);
            var result = await _mediator.Send(command);

            return Ok(result);
        }

        // -------------------------------------------------------
        // ✅ PUT : Update existing department
        // -------------------------------------------------------

        /// <summary>
        /// Used-In-Angular: updates department.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates department async.</para>
        /// <para>Handler flow: No application request/handler class was statically resolved from the controller action.</para>
        /// <para>Response DTO property analysis: No concrete response DTO properties were statically resolved from the request/handler declaration.</para>
        /// <para>Angular function(s): DepartmentsApi.updateDepartment (app/core/services/departments-api.ts:47).</para>
        /// <para>Angular purpose: updates department.</para>
        /// <para>Integrated UI page(s): /app/departments</para>
        /// <para>Angular UI component(s): DepartmentsStore (app/features/departments/departments.store.ts); DepartmentManageDialog (app/shared/components/department/department-manage-dialog/department-manage-dialog.ts); Departments (app/features/departments/departments.ts)</para>
        /// </remarks>
        [HttpPut("update")]
        public async Task<IActionResult> UpdateDepartmentAsync([FromBody] UpdateDepartmentRequestDTO updateDto)
        {
            if (updateDto == null)
            {
                _logger.LogInfo("UpdateDepartmentAsync received a null DTO.");
                throw new skolenza.application.Exceptions.ValidationErrorException(
                    skolenza.application.Constants.AppConstants.ErrorMessages.InvalidRequest);
            }

            _logger.LogInfo($"Updating department Id: {updateDto.Id}");
            var command = new UpdateDepartmentCommad(updateDto);
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        /// <summary>
        /// Used-In-Angular: retrieves departments options.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves department option.</para>
        /// <para>Handler flow: GetDepartmentOptionQuery is processed by GetDepartmentOptionQueryHandler; operation(s): GetOptionAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetDepartmentOptionResponse: Id (int), DepartmentName (string)</para>
        /// <para>Angular function(s): DepartmentsApi.getDepartmentsOptions (app/core/services/departments-api.ts:41).</para>
        /// <para>Angular purpose: retrieves departments options.</para>
        /// <para>Integrated UI page(s): /app/designations; /app/staffMembers; /app/institutions/new; /app/institutions/:institutionId/edit; /app/institutions; /app/campuss/new; /app/campuss/:campusId/edit; /app/campuss</para>
        /// <para>Angular UI component(s): LookupStore (app/core/stores/lookup.store.ts); DashboardHostStore (app/features/dashboard/dashboard-host/dashboard-host.store.ts); DepartmentsStore (app/features/departments/departments.store.ts); Designations (app/features/designations/designations.ts); StaffMembers (app/features/staffMembers/staffMembers.ts); InstitutionDetail (app/features/host/institutions/institution-detail/institution-detail.ts); InstitutionForm (app/features/host/institutions/institution-form/institution-form.ts); Institutions (app/features/host/institutions/institutions.ts)</para>
        /// </remarks>
        [HttpGet("option")]
        public async Task<IActionResult> getDepartment([FromQuery] GetDepartmentOptionRequestDTO requestDTO)
        {
            _logger.LogInfo($"Received request to get Department : {requestDTO.UserStaffMemberId}");

            var command = new GetDepartmentOptionQuery(requestDTO);
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        // -------------------------------------------------------
        // ✅ DELETE : Soft delete department
        // -------------------------------------------------------

        /// <summary>
        /// Used-In-Angular: deletes department.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: deletes department.</para>
        /// <para>Handler flow: DeleteDepartmentQuery is processed by DeleteDepartmentQueryHandler; operation(s): DeleteAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): DepartmentsApi.deleteDepartment (app/core/services/departments-api.ts:54).</para>
        /// <para>Angular purpose: deletes department.</para>
        /// <para>Integrated UI page(s): /app/departments</para>
        /// <para>Angular UI component(s): DepartmentsStore (app/features/departments/departments.store.ts); Departments (app/features/departments/departments.ts)</para>
        /// </remarks>
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteDepartmentAsync([FromQuery] DeleteDepartmentRequestDTO deleteDto)
        {
            if (deleteDto == null)
            {
                _logger.LogInfo("DeleteDepartmentAsync received a null DTO.");
                throw new skolenza.application.Exceptions.ValidationErrorException(
                    skolenza.application.Constants.AppConstants.ErrorMessages.InvalidRequest);
            }

            _logger.LogInfo($"Deleting department Id: {deleteDto.Id}");
            var command = new DeleteDepartmentQuery(deleteDto);
            var result = await _mediator.Send(command);
            return Ok(result);
        }
    }
}
