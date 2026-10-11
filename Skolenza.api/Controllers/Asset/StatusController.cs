// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Status operations.
// ================================================================

using skolenza.application.DTOS.AssetDTO.status;
using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.Features.AssetFeatures.Status.Handlers;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace skolenza.api.Controllers.Asset
{
    /// <summary>
    /// Controller to manage all Asset Status related operations
    /// for Institution Admins (Add, Update, Delete, GetAll).
    /// </summary>
    [ApiController]
    [Route("api/Asset/Status")]
    public class StatusController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILoggerService _logger;

        public StatusController(IMediator mediator, ILoggerService logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        #region Institution Admin - Asset Status CRUD


        /// <summary>
        /// Used-In-Angular: retrieves assets status.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves all asset status.</para>
        /// <para>Handler flow: GetAllAssetStatusCommand is processed by GetAllStatusCommandHandler; operation(s): GetAllAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetStatusResponseDTO: Id (long), InstitutionId (long?), IsActive (bool?), StatusName (string?), ColorKey (string?), Description (string?), AddedById (long?), UpdateById (long?), UpdatedDateTime (DateTime?), AddedDateTime (DateTime?)</para>
        /// <para>Angular function(s): AssetStatusApi.fetchAssetsStatus (app/core/services/asset-status-api.ts:35).</para>
        /// <para>Angular purpose: retrieves assets status.</para>
        /// <para>Integrated UI page(s): /app/assets/asset-status; /app/assets/list</para>
        /// <para>Angular UI component(s): AssetStatusStore (app/features/assets-management/asset-status/asset-status.store.ts); AssetsManagementStore (app/features/assets-management/assets-management.store.ts); AssetFilter (app/shared/components/asset/asset-filter/asset-filter.ts); UpsertAssetDialogStore (app/shared/components/asset/upsert-asset-dialog/upsert-asset-dialog.store.ts); AssetStatusComponent (app/features/assets-management/asset-status/asset-status.ts); AssetsManagement (app/features/assets-management/assets-management.ts); UpsertAssetDialog (app/shared/components/asset/upsert-asset-dialog/upsert-asset-dialog.ts)</para>
        /// </remarks>
        [HttpGet("get")]
        public async Task<IActionResult> GetByIdAssetStatus([FromQuery] GetStatusRequestDTO request)
        {
                _logger.LogInfo("Fetching all asset statuses for institution...");
                var query = new GetAllAssetStatusCommand(request);
                var result = await _mediator.Send(query);
                return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: creates asset status.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: creates status.</para>
        /// <para>Handler flow: AddStatusCommand is processed by AddStatusCommandHandler; operation(s): CreateAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetStatusResponseDTO: Id (long), InstitutionId (long?), IsActive (bool?), StatusName (string?), ColorKey (string?), Description (string?), AddedById (long?), UpdateById (long?), UpdatedDateTime (DateTime?), AddedDateTime (DateTime?)</para>
        /// <para>Angular function(s): AssetStatusApi.createAssetStatus (app/core/services/asset-status-api.ts:28).</para>
        /// <para>Angular purpose: creates asset status.</para>
        /// <para>Integrated UI page(s): /app/assets/asset-status</para>
        /// <para>Angular UI component(s): AssetStatusManageDialog (app/shared/components/asset/asset-status-manage-dialog/asset-status-manage-dialog.ts); AssetStatusComponent (app/features/assets-management/asset-status/asset-status.ts)</para>
        /// </remarks>
        [HttpPost("add")]
        public async Task<IActionResult> AddAssetStatus([FromBody] CreateStatusRequestDTO request)
        {

            _logger.LogInfo("Add Asset Status request received.");
            var command = new AddStatusCommand(request);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: updates asset status.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates status.</para>
        /// <para>Handler flow: UpdateStatusCommand is processed by UpdateStatusCommandHandler; operation(s): GetByIdForInstitutionAsync, Map, UpdateAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): AssetStatusApi.updateAssetStatus (app/core/services/asset-status-api.ts:41).</para>
        /// <para>Angular purpose: updates asset status.</para>
        /// <para>Integrated UI page(s): /app/assets/asset-status</para>
        /// <para>Angular UI component(s): AssetStatusManageDialog (app/shared/components/asset/asset-status-manage-dialog/asset-status-manage-dialog.ts); AssetStatusComponent (app/features/assets-management/asset-status/asset-status.ts)</para>
        /// </remarks>
        [HttpPut("update")]
        public async Task<IActionResult> UpdateAssetStatus( [FromBody] UpdateStatusRequestDTO request)
        {

                _logger.LogInfo($"Update request received for Asset Status ID: {request.Id}");
                var command = new UpdateStatusCommand(request);
                var result = await _mediator.Send(command);
                return Ok(result);

        }

        /// <summary>
        /// Used-In-Angular: deletes asset status.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: deletes status.</para>
        /// <para>Handler flow: DeleteStatusCommand is processed by DeleteStatusCommandHandler; operation(s): DeleteAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): AssetStatusApi.deleteAssetStatus (app/core/services/asset-status-api.ts:48).</para>
        /// <para>Angular purpose: deletes asset status.</para>
        /// <para>Integrated UI page(s): /app/assets/asset-status</para>
        /// <para>Angular UI component(s): AssetStatusStore (app/features/assets-management/asset-status/asset-status.store.ts); AssetStatusComponent (app/features/assets-management/asset-status/asset-status.ts)</para>
        /// </remarks>
        [HttpDelete("delete")]

        public async Task<IActionResult> DeleteAssetStatus([FromQuery] DeleteStatusReqestDTO request)
        {

                _logger.LogInfo($"Delete Asset Status request received for ID: {request.Id}");
                var command = new DeleteStatusCommand(request);
                var result = await _mediator.Send(command);
                return Ok(result);

        }

        #endregion
    }
}
