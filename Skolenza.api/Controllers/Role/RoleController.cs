// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Exposes HTTP endpoints for Role operations.
// ================================================================

using skolenza.application.DTOs.Role;
using skolenza.application.DTOS.Role;
using skolenza.application.Features.CategoryCmd.Command;
using skolenza.application.Features.RoleCmd.Handlers;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Role
{
    /// <summary>
    /// Controller to manage Roles in the system.
    /// Provides endpoints for creating, updating, retrieving, and deleting roles.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RoleController : ControllerBase
    {
        #region Bulk Import Preview

        /// <summary>Validates Role Excel/CSV or pasted data and saves a durable draft; master data is unchanged.</summary>
        /// <remarks>
        /// <para>Angular usage status: Not integrated yet.</para>
        /// <para>Send multipart/form-data with File OR PastedText, ModuleId, OperationId,
        /// optional SheetName and ColumnMappingJson (target field to source header).</para>
        /// <para>Uses the existing Role permission pipeline and trusted login Institution scope.</para>
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
                new PreviewRoleImportQuery(dto),
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
            return Ok(await _mediator.Send(new ManageRoleImportCommand(
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
            return Ok(await _mediator.Send(new ManageRoleImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.Get), cancellationToken));
        }

        /// <summary>Lists the caller's institution-scoped imports, newest first; PageSize is limited to 100.</summary>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("bulk/jobs")]
        public async Task<IActionResult> ListBulkImports(
            [FromQuery] skolenza.application.DTOS.Common.BulkImportJobRequestDTO dto,
            CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new ManageRoleImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.List), cancellationToken));
        }

        /// <summary>Retries failed or incomplete rows; already created records are never recreated.</summary>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("bulk/retry")]
        public async Task<IActionResult> RetryBulkImport(
            [FromBody] skolenza.application.DTOS.Common.BulkImportJobRequestDTO dto,
            CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new ManageRoleImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.Retry), cancellationToken));
        }

        /// <summary>Cancels pending work at a batch boundary. Previously committed records remain.</summary>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("bulk/cancel")]
        public async Task<IActionResult> CancelBulkImport(
            [FromBody] skolenza.application.DTOS.Common.BulkImportJobRequestDTO dto,
            CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new ManageRoleImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.Cancel), cancellationToken));
        }

        /// <summary>Downloads the CSV column template. Reference values come from existing institution master APIs.</summary>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("bulk/template")]
        public async Task<IActionResult> DownloadBulkTemplate(
            [FromQuery] skolenza.application.DTOS.Common.BulkImportJobRequestDTO dto,
            CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(new ManageRoleImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.Template), cancellationToken);
            return File(System.Text.Encoding.UTF8.GetBytes((string)response.Data!),
                "text/csv; charset=utf-8", "Role-template.csv");
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
            var response = await _mediator.Send(new ManageRoleImportCommand(
                dto, skolenza.application.Common.Enums.BulkImportAction.Get), cancellationToken);
            return File(skolenza.api.Common.BulkImportReport.Create(
                (skolenza.application.DTOS.Common.BulkImportJobResponseDTO)response.Data!),
                "text/csv; charset=utf-8", "Role-import-" + jobId + ".csv");
        }

        #endregion

        private readonly IMediator _mediator;
        private readonly ILoggerService _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="RoleController"/>.
        /// </summary>
        /// <param name="mediator">Mediator service for handling commands and queries.</param>
        /// <param name="logger">Logger service for logging information and errors.</param>
        public RoleController(IMediator mediator, ILoggerService logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        #region Role Type Options

        /// <summary>
        /// Used-In-Angular: retrieves the centralized role-type options.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: supplies Role create, edit and filter controls from backend constants.</para>
        /// <para>Authentication: bearer authentication is required. ModuleId and OperationId are not accepted for this constant-backed lookup.</para>
        /// <para>Handler flow: GetRoleTypeOptionsQuery is processed by GetRoleTypeOptionsQueryHandler without a database write or institution-data read.</para>
        /// <para>Response DTO: Id (int), Code (stable string), Name (string), Description (string).</para>
        /// <para>Angular function: RolesApi.getRoleTypeOptions.</para>
        /// <para>Integrated UI page: /app/roles.</para>
        /// <para>Angular components: RolesList and RoleDialog.</para>
        /// </remarks>
        [Authorize]
        [HttpGet("type-options")]
        public async Task<IActionResult> GetRoleTypeOptions(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetRoleTypeOptionsQuery(),
                cancellationToken);

            return Ok(result);
        }

        #endregion

        /// <summary>
        /// Used-In-Angular: updates role.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates role.</para>
        /// <para>Handler flow: UpdateRoleCommand is processed by UpdateRoleCommandHandler; operation(s): GetByIdForInstitutionAsync, UpdateAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): RolesApi.updateRole (app/core/services/roles-api.ts:125).</para>
        /// <para>Angular purpose: updates role.</para>
        /// <para>Integrated UI page(s): /app/roles</para>
        /// <para>Angular UI component(s): RoleDialog (app/features/roles/role-dialog/role-dialog.ts); RolesList (app/features/roles/roles-list/roles-list.ts)</para>
        /// </remarks>
        [HttpPut("update")]
        // [Authorize]
        public async Task<IActionResult> UpdateRole([FromBody] UpdateRoleRequestDTO updateRoleDTO)
        {
            _logger.LogInfo("Received request to update a role: " + updateRoleDTO.ToString());
            var command = new UpdateRoleCommand(updateRoleDTO);
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        /// <summary>
        /// Used-In-Angular: retrieves role options.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves role option.</para>
        /// <para>Handler flow: GetRoleOptionQuery is processed by GetRoleOptionQueryHandler; operation(s): GetOptionAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetRoleOptionResponseDTO: Id (int), RoleType (int?), RoleName (string?), IsActive (bool)</para>
        /// <para>Angular function(s): RolesApi.getRoleOptions (app/core/services/roles-api.ts:119).</para>
        /// <para>Angular purpose: retrieves role options.</para>
        /// <para>Integrated UI page(s): /app/tickets/types; /app/staffMembers; /app/profile/basic-info</para>
        /// <para>Angular UI component(s): RolePopup (app/features/staffMembers/role-popup/role-popup.ts); TicketTypeManageDialog (app/features/tickets/ticket-type/ticket-type-manage-dialog/ticket-type-manage-dialog.ts); StaffMemberManageDialog (app/shared/components/staffMember/staffMember-manage-dialog/staffMember-manage-dialog.ts); StaffMemberRoleCell (app/features/staffMembers/staffMember-role-cell/staffMember-role-cell.ts); TicketTypeComponent (app/features/tickets/ticket-type/ticket-type.ts); StaffMembers (app/features/staffMembers/staffMembers.ts); StaffMemberBasicInfo (app/features/user-menu/staffMember-profile/staffMember-basic-info/staffMember-basic-info.ts)</para>
        /// </remarks>
        [HttpGet("option")]
        public async Task<IActionResult> getRole([FromQuery] GetRoleOptionRequestDTO requestDTO)
        {
            _logger.LogInfo("Received request to get role options.");

            var command = new GetRoleOptionQuery(requestDTO);
            var result = await _mediator.Send(command);

            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: creates role.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: creates role.</para>
        /// <para>Handler flow: CreateRoleCommand is processed by CreateRoleCommandHandler; operation(s): CreateAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetRoleResponseDTO: Id (int), RoleName (string), RoleType (int), RoleTypeName (string?), IsActive (bool), Remark (string?)</para>
        /// <para>Angular function(s): RolesApi.addRole (app/core/services/roles-api.ts:105).</para>
        /// <para>Angular purpose: creates role.</para>
        /// <para>Integrated UI page(s): /app/roles</para>
        /// <para>Angular UI component(s): RoleDialog (app/features/roles/role-dialog/role-dialog.ts); RolesList (app/features/roles/roles-list/roles-list.ts)</para>
        /// </remarks>
        [HttpPost("add")]
        // [Authorize]
        public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequestDTO createRoleDTO)
        {
            _logger.LogInfo("Received request to create a new role: " + createRoleDTO.ToString());
            var command = new CreateRoleCommand(createRoleDTO);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: retrieves roles.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves role.</para>
        /// <para>Handler flow: GetRoleQuery is processed by GetRoleQueryHandler; operation(s): GetAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetRoleResponseDTO: Id (int), RoleName (string), RoleType (int), RoleTypeName (string?), IsActive (bool), Remark (string?)</para>
        /// <para>Angular function(s): RolesApi.getRoles (app/core/services/roles-api.ts:112).</para>
        /// <para>Angular purpose: retrieves roles.</para>
        /// <para>Integrated UI page(s): /app/roles; /app/roles/permissions/:roleId; /app/staffMembers; /app/profile/basic-info</para>
        /// <para>Angular UI component(s): RolePermissionsStore (app/features/roles/role-permissions/role-permissions.store.ts); RolesList (app/features/roles/roles-list/roles-list.ts); StaffMemberManageDialog (app/shared/components/staffMember/staffMember-manage-dialog/staffMember-manage-dialog.ts); RolePermissions (app/features/roles/role-permissions/role-permissions.ts); StaffMembers (app/features/staffMembers/staffMembers.ts); StaffMemberBasicInfo (app/features/user-menu/staffMember-profile/staffMember-basic-info/staffMember-basic-info.ts)</para>
        /// </remarks>
        [HttpGet("get")]

        public async Task<IActionResult> GetAllRoles([FromQuery] GetRoleRequestDTO? roleRequestDTO)
        {

            var query = new GetRoleQuery(roleRequestDTO);
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: deletes role.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: deletes role.</para>
        /// <para>Handler flow: DeleteRoleQuery is processed by DeleteRoleQueryHandler; operation(s): DeleteAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): RolesApi.deleteRole (app/core/services/roles-api.ts:132).</para>
        /// <para>Angular purpose: deletes role.</para>
        /// <para>Integrated UI page(s): /app/roles</para>
        /// <para>Angular UI component(s): RolesList (app/features/roles/roles-list/roles-list.ts)</para>
        /// </remarks>
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteRole([FromQuery] DeleteRoleRequestDTO deleteRole)
        {

            var command = new DeleteRoleQuery(deleteRole);
            var result = await _mediator.Send(command);
            return Ok(result);
        }
    }
}
