// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates Host-admin requests for direct SubModule management.
// ================================================================

using skolenza.application.DTOS.Module.SubModule;
using skolenza.application.Features.ModuleCmd.SubModule.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Module
{
    /// <summary>
    /// Coordinates Host-admin HTTP requests for direct SubModules.
    /// </summary>
    [ApiController]
    [Route("api/ChildModule")]
    [Route("api/SubModule")]
    public class SubModuleController : ControllerBase
    {
        #region Fields

        private readonly IMediator _mediator;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SubModuleController"/> class.
        /// </summary>
        /// <param name="mediator">Dispatches SubModule commands and queries.</param>
        public SubModuleController(IMediator mediator)
        {
            _mediator = mediator;
        }

        #endregion

        #region SubModule CRUD

        /// <summary>
        /// Used-In-Angular: creates sub module.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: creates sub module.</para>
        /// <para>Handler flow: CreateSubModuleCommand is processed by CreateSubModuleCommandHandler; operation(s): GetParentModuleForSubModuleAsync, AddSubModuleAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetSubModuleResponseDTO: Id (int), ModuleCode (string?), ModuleName (string?), DisplayName (string?), URLPath (string?), ParentModuleId (int?), IsLeafNode (bool?), IsModuleDisplayInUI (bool), IsCommonMenu (bool), ModuleScope (short), IsActive (bool), ImageIconWeb (string?), ImageIconMobile (string?), ItemPriority (int?), Remark (string?), AddedById (long?), AddedDateTime (DateTime?), UpdatedById (long?), UpdatedDateTime (DateTime?), ParentModule (ParentModuleSummaryDTO?)</para>
        /// <para>Angular function(s): SubModuleApi.addSubModule (app/core/services/sub-module-api.ts:41).</para>
        /// <para>Angular purpose: creates sub module.</para>
        /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
        /// <para>Angular UI component(s): ModuleForm (app/features/host/modules/module-form/module-form.ts)</para>
        /// </remarks>
        [HttpPost("add")]
        public async Task<IActionResult> AddModule(
            [FromBody] CreateSubModuleRequestDTO? createSubModuleRequestDTO,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new CreateSubModuleCommand(createSubModuleRequestDTO),
                cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: updates sub module.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates sub module.</para>
        /// <para>Handler flow: UpdateSubModuleCommand is processed by UpdateSubModuleCommandHandler; operation(s): GetSubModuleForUpdateAsync, GetParentModuleForSubModuleAsync, UpdateSubModuleAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetSubModuleResponseDTO: Id (int), ModuleCode (string?), ModuleName (string?), DisplayName (string?), URLPath (string?), ParentModuleId (int?), IsLeafNode (bool?), IsModuleDisplayInUI (bool), IsCommonMenu (bool), ModuleScope (short), IsActive (bool), ImageIconWeb (string?), ImageIconMobile (string?), ItemPriority (int?), Remark (string?), AddedById (long?), AddedDateTime (DateTime?), UpdatedById (long?), UpdatedDateTime (DateTime?), ParentModule (ParentModuleSummaryDTO?)</para>
        /// <para>Angular function(s): SubModuleApi.updateSubModule (app/core/services/sub-module-api.ts:47).</para>
        /// <para>Angular purpose: updates sub module.</para>
        /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
        /// <para>Angular UI component(s): ModuleForm (app/features/host/modules/module-form/module-form.ts)</para>
        /// </remarks>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateModule(
            int id,
            [FromBody] UpdateSubModuleRequestDTO? updateSubModuleRequestDTO,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new UpdateSubModuleCommand(id, updateSubModuleRequestDTO),
                cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: retrieves sub module.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves sub module by id.</para>
        /// <para>Handler flow: GetSubModuleByIdQuery is processed by GetSubModuleByIdQueryHandler; operation(s): GetSubModuleByIdAsync, FindFirst.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetSubModuleResponseDTO: Id (int), ModuleCode (string?), ModuleName (string?), DisplayName (string?), URLPath (string?), ParentModuleId (int?), IsLeafNode (bool?), IsModuleDisplayInUI (bool), IsCommonMenu (bool), ModuleScope (short), IsActive (bool), ImageIconWeb (string?), ImageIconMobile (string?), ItemPriority (int?), Remark (string?), AddedById (long?), AddedDateTime (DateTime?), UpdatedById (long?), UpdatedDateTime (DateTime?), ParentModule (ParentModuleSummaryDTO?)</para>
        /// <para>Angular function(s): SubModuleApi.getSubModule (app/core/services/sub-module-api.ts:35).</para>
        /// <para>Angular purpose: retrieves sub module.</para>
        /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
        /// <para>Angular UI component(s): ModuleForm (app/features/host/modules/module-form/module-form.ts)</para>
        /// </remarks>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetModuleById(
            int id,
            [FromQuery] short moduleScope,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetSubModuleByIdQuery(id, moduleScope),
                cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: retrieves sub modules.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves sub modules.</para>
        /// <para>Handler flow: GetSubModulesQuery is processed by GetSubModulesQueryHandler; operation(s): GetSubModulesAsync, FindFirst.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetSubModuleResponseDTO: Id (int), ModuleCode (string?), ModuleName (string?), DisplayName (string?), URLPath (string?), ParentModuleId (int?), IsLeafNode (bool?), IsModuleDisplayInUI (bool), IsCommonMenu (bool), ModuleScope (short), IsActive (bool), ImageIconWeb (string?), ImageIconMobile (string?), ItemPriority (int?), Remark (string?), AddedById (long?), AddedDateTime (DateTime?), UpdatedById (long?), UpdatedDateTime (DateTime?), ParentModule (ParentModuleSummaryDTO?)</para>
        /// <para>Angular function(s): SubModuleApi.getSubModules (app/core/services/sub-module-api.ts:28).</para>
        /// <para>Angular purpose: retrieves sub modules.</para>
        /// <para>Integrated UI page(s): /app/modules; /app/modules/sub-modules; /app/modules/module-operations</para>
        /// <para>Angular UI component(s): ModuleTreeReader (app/core/services/module-tree-reader.ts); ModuleOperationForm (app/features/host/modules/module-operations/module-operation-form/module-operation-form.ts); ParentSubModulesDetail (app/features/host/modules/parent-modules/parent-sub-modules-detail/parent-sub-modules-detail.ts); SubModulesStore (app/features/host/modules/sub-modules/sub-modules.store.ts); ModuleOperationsStore (app/features/host/modules/module-operations/module-operations.store.ts); ParentModulesStore (app/features/host/modules/parent-modules/parent-modules.store.ts); ParentModules (app/features/host/modules/parent-modules/parent-modules.ts); SubModules (app/features/host/modules/sub-modules/sub-modules.ts)</para>
        /// </remarks>
        [HttpGet("list")]
        public async Task<IActionResult> GetModules(
            [FromQuery] short moduleScope,
            [FromQuery] int? parentModuleId,
            [FromQuery] bool? isActive,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetSubModulesQuery(moduleScope, parentModuleId, isActive),
                cancellationToken);

            return Ok(result);
        }
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: retrieves sub modules by parent.</para>
                /// <para>Handler flow: GetSubModulesByParentQuery is processed by GetSubModulesByParentQueryHandler; operation(s): GetParentModuleForSubModuleAsync, GetSubModulesAsync, FindFirst.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetSubModuleResponseDTO: Id (int), ModuleCode (string?), ModuleName (string?), DisplayName (string?), URLPath (string?), ParentModuleId (int?), IsLeafNode (bool?), IsModuleDisplayInUI (bool), IsCommonMenu (bool), ModuleScope (short), IsActive (bool), ImageIconWeb (string?), ImageIconMobile (string?), ItemPriority (int?), Remark (string?), AddedById (long?), AddedDateTime (DateTime?), UpdatedById (long?), UpdatedDateTime (DateTime?), ParentModule (ParentModuleSummaryDTO?)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: GET /api/submodule/parent/{}.</para>
                /// </remarks>

                [HttpGet("parent/{parentModuleId:int}")]
                public async Task<IActionResult> GetModulesByParent(
                    int parentModuleId,
                    [FromQuery] short moduleScope,
                    [FromQuery] bool? isActive,
                    CancellationToken cancellationToken)
                {
                    var result = await _mediator.Send(
                        new GetSubModulesByParentQuery(parentModuleId, moduleScope, isActive),
                        cancellationToken);

                    return Ok(result);
                }

        /// <summary>
        /// Used-In-Angular: updates sub module status.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates sub module status.</para>
        /// <para>Handler flow: UpdateSubModuleStatusCommand is processed by UpdateSubModuleStatusCommandHandler; operation(s): GetSubModuleForUpdateAsync, GetModuleHierarchyForOperationActivationAsync, GetParentModuleForSubModuleAsync, GetModuleOperationMappingsForStatusUpdateAsync, GetNonDeletedByModuleIdsAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetSubModuleResponseDTO: Id (int), ModuleCode (string?), ModuleName (string?), DisplayName (string?), URLPath (string?), ParentModuleId (int?), IsLeafNode (bool?), IsModuleDisplayInUI (bool), IsCommonMenu (bool), ModuleScope (short), IsActive (bool), ImageIconWeb (string?), ImageIconMobile (string?), ItemPriority (int?), Remark (string?), AddedById (long?), AddedDateTime (DateTime?), UpdatedById (long?), UpdatedDateTime (DateTime?), ParentModule (ParentModuleSummaryDTO?)</para>
        /// <para>Angular function(s): SubModuleApi.setSubModuleStatus (app/core/services/sub-module-api.ts:53).</para>
        /// <para>Angular purpose: updates sub module status.</para>
        /// <para>Integrated UI page(s): /app/modules/sub-modules</para>
        /// <para>Angular UI component(s): SubModulesStore (app/features/host/modules/sub-modules/sub-modules.store.ts); SubModules (app/features/host/modules/sub-modules/sub-modules.ts)</para>
        /// </remarks>
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateModuleStatus(
            int id,
            [FromBody] UpdateSubModuleStatusRequestDTO? statusRequestDTO,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new UpdateSubModuleStatusCommand(id, statusRequestDTO),
                cancellationToken);

            return Ok(result);
        }

        #endregion
    }
}
