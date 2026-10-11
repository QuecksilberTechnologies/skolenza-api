// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Exposes department endpoints and delegates application errors to middleware.
// ================================================================

using skolenza.application.DTOS.Staff.Type;

using skolenza.application.Features.StaffEmploymentTypeCmd.Handlers;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.StaffEmploymentType
{
    [ApiController]
    [Route("api/StaffEmploymentType")]

    public class StaffEmploymentTypeController : ControllerBase    {
        #region Bulk Import Preview

        /// <summary>Validates StaffEmploymentType Excel/CSV or pasted data and saves a durable draft; master data is unchanged.</summary>
        /// <remarks>
        /// <para>Angular usage status: Not integrated yet.</para>
        /// <para>Send multipart/form-data with File OR PastedText, ModuleId, OperationId,
        /// optional SheetName and ColumnMappingJson (target field to source header).</para>
        /// <para>Uses the existing StaffEmploymentType permission pipeline and trusted login Institution scope.</para>
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
                new PreviewStaffEmploymentTypeImportQuery(dto),
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
            return Ok(await _mediator.Send(new ManageStaffEmploymentTypeImportCommand(
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
            return Ok(await _mediator.Send(new ManageStaffEmploymentTypeImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.Get), cancellationToken));
        }

        /// <summary>Lists the caller's institution-scoped imports, newest first; PageSize is limited to 100.</summary>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("bulk/jobs")]
        public async Task<IActionResult> ListBulkImports(
            [FromQuery] skolenza.application.DTOS.Common.BulkImportJobRequestDTO dto,
            CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new ManageStaffEmploymentTypeImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.List), cancellationToken));
        }

        /// <summary>Retries failed or incomplete rows; already created records are never recreated.</summary>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("bulk/retry")]
        public async Task<IActionResult> RetryBulkImport(
            [FromBody] skolenza.application.DTOS.Common.BulkImportJobRequestDTO dto,
            CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new ManageStaffEmploymentTypeImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.Retry), cancellationToken));
        }

        /// <summary>Cancels pending work at a batch boundary. Previously committed records remain.</summary>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("bulk/cancel")]
        public async Task<IActionResult> CancelBulkImport(
            [FromBody] skolenza.application.DTOS.Common.BulkImportJobRequestDTO dto,
            CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new ManageStaffEmploymentTypeImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.Cancel), cancellationToken));
        }

        /// <summary>Downloads the CSV column template. Reference values come from existing institution master APIs.</summary>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("bulk/template")]
        public async Task<IActionResult> DownloadBulkTemplate(
            [FromQuery] skolenza.application.DTOS.Common.BulkImportJobRequestDTO dto,
            CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(new ManageStaffEmploymentTypeImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.Template), cancellationToken);
            return File(System.Text.Encoding.UTF8.GetBytes((string)response.Data!),
                "text/csv; charset=utf-8", "StaffEmploymentType-template.csv");
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
            var response = await _mediator.Send(new ManageStaffEmploymentTypeImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.Get), cancellationToken);
            return File(skolenza.api.Common.BulkImportReport.Create(
                (skolenza.application.DTOS.Common.BulkImportJobResponseDTO)response.Data!),
                "text/csv; charset=utf-8", "StaffEmploymentType-import-" + jobId + ".csv");
        }

        #endregion

        private readonly IMediator _mediator;
        public StaffEmploymentTypeController(IMediator mediator)
        {
            _mediator = mediator;
        }

        #region Constant options

        /// <summary>
        /// Returns the platform-defined staff employment arrangements used by staff forms.
        /// </summary>
        /// <remarks>
        /// Values are stable short identifiers backed by application constants. This lookup
        /// is separate from institution-owned Staff Categories such as Teaching or Technical.
        /// </remarks>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("options")]
        public async Task<IActionResult> GetEmploymentTypeOptions(CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(
                new GetStaffEmploymentTypeOptionsQuery(),
                cancellationToken));
        }

        #endregion

        #region Institution master operations
        /// <summary>Creates one institution-owned type. Requires Add on STAFF_EMPLOYMENT_TYPES.</summary>
        /// <remarks>Angular integration pending. Body: TypeName, Description, Remark, IsActive, ModuleId, OperationId.
        /// Returns ApiResponse of the created type; ownership/audit IDs are server assigned.</remarks>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("add")]
        public async Task<IActionResult> Create([FromBody] CreateStaffEmploymentTypeDTO dto, CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new CreateStaffEmploymentTypeCommand(dto), cancellationToken));
        }

        /// <summary>Returns paged database types for the authenticated institution, including inactive types.</summary>
        /// <remarks>Replaces the old hard-coded list. Query: ModuleId, OperationId, PageNumber=1, PageSize=20.</remarks>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("get")]
        public async Task<IActionResult> Get([FromQuery] GetStaffEmploymentTypeRequestDTO dto, CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new GetStaffEmploymentTypesQuery(dto), cancellationToken));
        }

        /// <summary>Returns active, non-deleted StaffEmploymentType options for this institution only.</summary>
        /// <remarks>Query now requires ModuleId and OperationId from the existing menu/permission context.</remarks>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("option")]
        public async Task<IActionResult> Options([FromQuery] GetStaffEmploymentTypeRequestDTO dto, CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new GetStaffEmploymentTypeOptionQuery(dto), cancellationToken));
        }

        /// <summary>Updates one institution-owned StaffEmploymentType.</summary>
        /// <remarks>
        /// Requires Update permission on STAFF_EMPLOYMENT_TYPES. The trusted request context supplies
        /// the Institution and audit actor; the body must contain Id, TypeName, ModuleId, and OperationId.
        /// </remarks>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPut("update")]
        public async Task<IActionResult> Update(
            [FromBody] UpdateStaffEmploymentTypeRequestDTO dto,
            CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new UpdateStaffEmploymentTypeCommand(dto), cancellationToken));
        }

        /// <summary>Soft deletes one unused institution-owned StaffEmploymentType.</summary>
        /// <remarks>
        /// Requires Delete permission on STAFF_EMPLOYMENT_TYPES. Deletion returns 409 Conflict when
        /// a non-soft-deleted business dependency or any StaffEmploymentTypeBasicMenu mapping exists.
        /// </remarks>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpDelete("delete")]
        public async Task<IActionResult> Delete(
            [FromQuery] DeleteStaffEmploymentTypeRequestDTO dto,
            CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new DeleteStaffEmploymentTypeCommand(dto), cancellationToken));
        }
        #endregion
    }
}
