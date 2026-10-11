// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Category operations.
// ================================================================

using skolenza.application.DTOS.AssetDTO.category;
using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.Features.AssetFeatures.Category.Handlers;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace skolenza.api.Controllers.Asset
{
    /// <summary>
    /// Controller to manage all Asset Category related operations
    /// for Institution Admins (Add, Update, Delete, GetAll).
    /// </summary>
    [ApiController]
    [Route("api/Asset/Category")]
    public class CategoryController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILoggerService _logger;

        public CategoryController(IMediator mediator, ILoggerService logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        #region Institution Admin - Asset Category CRUD

        /// <summary>
        /// Used-In-Angular: retrieves all asset category.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves all category.</para>
        /// <para>Handler flow: GetAllCategoryCommand is processed by GetAllCategoryCommandHandler; operation(s): GetAllAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetCategoryResponseDTO: Id (long), CategoryName (string?), Remark (string?), IsActive (bool), HasMultipleUser (bool)</para>
        /// <para>Angular function(s): AssetCategoriesApi.getAllAssetCategory (app/core/services/asset-categories-api.ts:37).</para>
        /// <para>Angular purpose: retrieves all asset category.</para>
        /// <para>Integrated UI page(s): /app/assets/asset-categories; /app/assets/asset-types; /app/assets/list</para>
        /// <para>Angular UI component(s): AssetCategoryStore (app/features/assets-management/asset-category/asset-category.store.ts); AssetTypesStore (app/features/assets-management/asset-types/asset-types.store.ts); AssetsManagementStore (app/features/assets-management/assets-management.store.ts); AssetTypeManageDialog (app/shared/components/asset/asset-type-manage-dialog/asset-type-manage-dialog.ts); UpsertAssetDialogStore (app/shared/components/asset/upsert-asset-dialog/upsert-asset-dialog.store.ts); AssetCategory (app/features/assets-management/asset-category/asset-category.ts); AssetTypes (app/features/assets-management/asset-types/asset-types.ts); AssetsManagement (app/features/assets-management/assets-management.ts)</para>
        /// </remarks>
        [HttpGet("get")]
        public async Task<IActionResult> GetAllAssetCategory([FromQuery] GetCategoryReqestDTO request)
        {

                _logger.LogInfo("Fetching all asset categories for institution...");
                var query = new GetAllCategoryCommand(request);
                var result = await _mediator.Send(query);
                return Ok(result);

        }


        /// <summary>
        /// Used-In-Angular: creates asset category.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: creates category.</para>
        /// <para>Handler flow: AddCategoryCommand is processed by CreateCategoryCommandHandler; operation(s): CreateAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetCategoryResponseDTO: Id (long), CategoryName (string?), Remark (string?), IsActive (bool), HasMultipleUser (bool)</para>
        /// <para>Angular function(s): AssetCategoriesApi.createAssetCategory (app/core/services/asset-categories-api.ts:30).</para>
        /// <para>Angular purpose: creates asset category.</para>
        /// <para>Integrated UI page(s): /app/assets/asset-categories</para>
        /// <para>Angular UI component(s): AssetCategoryManageDialog (app/shared/components/asset/asset-category-manage-dialog/asset-category-manage-dialog.ts); AssetCategory (app/features/assets-management/asset-category/asset-category.ts)</para>
        /// </remarks>
        [HttpPost("add")]
        public async Task<IActionResult> AddAssetCategory([FromBody] AddCategoryReqestDTO request)
        {
                _logger.LogInfo("Add Asset Category request received.");
                var command = new AddCategoryCommand(request);
                var result = await _mediator.Send(command);
                return Ok(result);

        }

        /// <summary>
        /// Used-In-Angular: updates asset category.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates category.</para>
        /// <para>Handler flow: UpdateCategoryCommand is processed by UpdateCategoryCommandHandler; operation(s): GetByIdForInstitutionAsync, Map, UpdateAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): AssetCategoriesApi.updateAssetCategory (app/core/services/asset-categories-api.ts:44).</para>
        /// <para>Angular purpose: updates asset category.</para>
        /// <para>Integrated UI page(s): /app/assets/asset-categories</para>
        /// <para>Angular UI component(s): AssetCategoryManageDialog (app/shared/components/asset/asset-category-manage-dialog/asset-category-manage-dialog.ts); AssetCategory (app/features/assets-management/asset-category/asset-category.ts)</para>
        /// </remarks>
        [HttpPut("update")]
        public async Task<IActionResult> UpdateAssetCategory([FromBody] UpdateCategoryReqestDTO request)
        {

                _logger.LogInfo($"Update Asset Category request received for ID: {request.Id}");
                var command = new UpdateCategoryCommand(request);
                var result = await _mediator.Send(command);
                return Ok(result);

        }

        /// <summary>
        /// Used-In-Angular: deletes asset category.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: deletes category.</para>
        /// <para>Handler flow: DeleteCategoryCommand is processed by DeleteCategoryCommandHandler; operation(s): DeleteAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): AssetCategoriesApi.deleteAssetCategory (app/core/services/asset-categories-api.ts:51).</para>
        /// <para>Angular purpose: deletes asset category.</para>
        /// <para>Integrated UI page(s): /app/assets/asset-categories</para>
        /// <para>Angular UI component(s): AssetCategoryStore (app/features/assets-management/asset-category/asset-category.store.ts); AssetCategory (app/features/assets-management/asset-category/asset-category.ts)</para>
        /// </remarks>
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteAssetCategory([FromQuery] DeleteCategoryReqestDTO request)
        {

                _logger.LogInfo($"Delete Asset Category request received for ID: {request.Id}");
                var command = new DeleteCategoryCommand(request);
                var result = await _mediator.Send(command);
                return Ok(result);

        }

        #endregion
    }
}
