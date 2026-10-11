// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Asset operations.
// ================================================================

using skolenza.application.DTOS.AssetDTO.asset;
using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.Features.AssetFeatures.Assets.Handlers;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace skolenza.api.Controllers.Asset
{
    /// <summary>
    /// Controller to handle all Asset-related operations.
    /// Supports CRUD operations, filtering, and retrieval by institution.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AssetController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILoggerService _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetController"/> class.
        /// </summary>
        /// <param name="mediator">MediatR instance for handling commands/queries.</param>
        /// <param name="logger">Logger service for tracking requests and errors.</param>
        public AssetController(IMediator mediator, ILoggerService logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        #region CRUD - Asset by Institution User


        /// <summary>
        /// Used-In-Angular: retrieves assets.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves all asset.</para>
        /// <para>Handler flow: GetAllAssetCommand is processed by GetAllAssetCommandHandler; operation(s): GetAssetsByFilterAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetAssetResponseDTO: AssetId (long), AssetImageId (long ?), CategoryId (long?), CategoryName (string?), AssetName (string?), AssetTypeId (int?), TypeName (string?), Company (string?), ModelNo (string?), Size (string?), Weight (string?), Color (string?), IsRepairable (bool), IsActive (bool), HasMultipleUser (bool), IsAssigned (bool), Price (decimal?), SerialNumber (string?), Barcode (string?), QRCode (string?), AssetImagePath (string?), AssetImageType (int?), PurchaseDate (DateTime?), WarrantyExpiryDate (DateTime?), AssetStatusId (int?), ColorKey (string?), StatusName (string?)</para>
        /// <para>Angular function(s): AssetsApi.getAssets (app/core/services/assets-api.ts:36).</para>
        /// <para>Angular purpose: retrieves assets.</para>
        /// <para>Integrated UI page(s): /app/assets/list</para>
        /// <para>Angular UI component(s): AssetsManagementStore (app/features/assets-management/assets-management.store.ts); AssetsManagement (app/features/assets-management/assets-management.ts)</para>
        /// </remarks>
        [HttpGet("get")]
        public async Task<IActionResult> GetAllAssets([FromQuery] GetAssetRequestDTO assetRequestDTO)
        {
            var command = new GetAllAssetCommand(assetRequestDTO);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: creates asset.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: creates asset.</para>
        /// <para>Handler flow: AddAssetCommand is processed by AddAssetCommandHandler; operation(s): GetByIdForInstitutionAsync, AddAsync, UpdateQrCodeAsync, DeleteFileAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetAssetResponseDTO: AssetId (long), AssetImageId (long ?), CategoryId (long?), CategoryName (string?), AssetName (string?), AssetTypeId (int?), TypeName (string?), Company (string?), ModelNo (string?), Size (string?), Weight (string?), Color (string?), IsRepairable (bool), IsActive (bool), HasMultipleUser (bool), IsAssigned (bool), Price (decimal?), SerialNumber (string?), Barcode (string?), QRCode (string?), AssetImagePath (string?), AssetImageType (int?), PurchaseDate (DateTime?), WarrantyExpiryDate (DateTime?), AssetStatusId (int?), ColorKey (string?), StatusName (string?)</para>
        /// <para>Angular function(s): AssetsApi.createAsset (app/core/services/assets-api.ts:29).</para>
        /// <para>Angular purpose: creates asset.</para>
        /// <para>Integrated UI page(s): /app/assets/list</para>
        /// <para>Angular UI component(s): UpsertAssetDialogStore (app/shared/components/asset/upsert-asset-dialog/upsert-asset-dialog.store.ts); UpsertAssetDialog (app/shared/components/asset/upsert-asset-dialog/upsert-asset-dialog.ts); AssetsManagement (app/features/assets-management/assets-management.ts)</para>
        /// </remarks>
        [HttpPost("add")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AddAsset([FromForm] AddAssetRequestDTO  addAssetDTO)
        {
            _logger.LogInfo("Request: Add asset - " + addAssetDTO);
            var command = new AddAssetCommand(addAssetDTO);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: updates asset.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates asset.</para>
        /// <para>Handler flow: UpdateAssetCommand is processed by UpdateAssetCommandHandler; operation(s): GetSingleRecordForInstitutionAsync, UpdateAsync, DeleteFileAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetAssetResponseDTO: AssetId (long), AssetImageId (long ?), CategoryId (long?), CategoryName (string?), AssetName (string?), AssetTypeId (int?), TypeName (string?), Company (string?), ModelNo (string?), Size (string?), Weight (string?), Color (string?), IsRepairable (bool), IsActive (bool), HasMultipleUser (bool), IsAssigned (bool), Price (decimal?), SerialNumber (string?), Barcode (string?), QRCode (string?), AssetImagePath (string?), AssetImageType (int?), PurchaseDate (DateTime?), WarrantyExpiryDate (DateTime?), AssetStatusId (int?), ColorKey (string?), StatusName (string?)</para>
        /// <para>Angular function(s): AssetsApi.updateAsset (app/core/services/assets-api.ts:42).</para>
        /// <para>Angular purpose: updates asset.</para>
        /// <para>Integrated UI page(s): /app/assets/list</para>
        /// <para>Angular UI component(s): UpsertAssetDialogStore (app/shared/components/asset/upsert-asset-dialog/upsert-asset-dialog.store.ts); UpsertAssetDialog (app/shared/components/asset/upsert-asset-dialog/upsert-asset-dialog.ts); AssetsManagement (app/features/assets-management/assets-management.ts)</para>
        /// </remarks>
        [HttpPut("update")]

        public async Task<IActionResult> UpdateAsset([FromForm] UpdateAssetRequestDTO updateAssetDTO)
        {
            _logger.LogInfo("Request: Update asset - " + updateAssetDTO);
            var command = new UpdateAssetCommand(updateAssetDTO);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: deletes asset.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: deletes asset.</para>
        /// <para>Handler flow: DeleteAssetCommand is processed by DeleteAssetCommandHandler; operation(s): DeleteAssetAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): AssetsApi.deleteAsset (app/core/services/assets-api.ts:49).</para>
        /// <para>Angular purpose: deletes asset.</para>
        /// <para>Integrated UI page(s): /app/assets/list</para>
        /// <para>Angular UI component(s): AssetsManagementStore (app/features/assets-management/assets-management.store.ts); AssetsManagement (app/features/assets-management/assets-management.ts)</para>
        /// </remarks>
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteAsset([FromQuery] DeleteAssetReqestDTO deleteAssetDTO)
        {
            _logger.LogInfo("Request: Delete asset - " + deleteAssetDTO);
            var command = new DeleteAssetCommand(deleteAssetDTO);
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        #endregion
    }
}
