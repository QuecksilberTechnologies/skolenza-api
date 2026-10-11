// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates Host-admin HTTP requests for Parent/Header Modules.
// ================================================================

using skolenza.application.DTOS.Module.ParentModule;
using skolenza.application.Features.ModuleCmd.Parent.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Module
{
    /// <summary>
    /// Provides Parent/Header Module management endpoints for authenticated Host Super Admins.
    /// </summary>
    [ApiController]
    [Route("api/ParentHeader")]
    [Route("api/ParentModule")]
    [Authorize]
    public class ParentModuleController : ControllerBase
    {
        #region Fields

        private readonly IMediator _mediator;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ParentModuleController"/> class.
        /// </summary>
        /// <param name="mediator">Dispatches Parent Module commands and queries.</param>
        public ParentModuleController(IMediator mediator)
        {
            _mediator = mediator;
        }

        #endregion

        #region Parent Module Commands

        /// <summary>
        /// Used-In-Angular: creates parent module.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: creates parent module.</para>
        /// <para>Handler flow: CreateParentModuleCommand is processed by CreateParentModuleCommandHandler; operation(s): AddParentModuleAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetParentModuleResponseDTO: Id (int), ModuleCode (string?), ModuleName (string?), DisplayName (string?), URLPath (string?), ParentModuleId (int?), IsLeafNode (bool?), IsModuleDisplayInUI (bool), IsCommonMenu (bool), ModuleScope (short), IsActive (bool), ImageIconWeb (string?), ImageIconMobile (string?), ItemPriority (int?), Remark (string?), AddedById (long?), AddedDateTime (DateTime?), UpdatedById (long?), UpdatedDateTime (DateTime?)</para>
        /// <para>Angular function(s): ParentModuleApi.addParentModule (app/core/services/parent-module-api.ts:45).</para>
        /// <para>Angular purpose: creates parent module.</para>
        /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
        /// <para>Angular UI component(s): ModuleForm (app/features/host/modules/module-form/module-form.ts)</para>
        /// </remarks>
        [HttpPost("add")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> AddModule(
            [FromBody] CreateParentModuleRequestDTO? createModuleRequestDTO,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new CreateParentModuleCommand(createModuleRequestDTO),
                cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: updates parent module.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates parent module.</para>
        /// <para>Handler flow: UpdateParentModuleCommand is processed by UpdateParentModuleCommandHandler; operation(s): GetParentModuleForUpdateAsync, UpdateParentModuleAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetParentModuleResponseDTO: Id (int), ModuleCode (string?), ModuleName (string?), DisplayName (string?), URLPath (string?), ParentModuleId (int?), IsLeafNode (bool?), IsModuleDisplayInUI (bool), IsCommonMenu (bool), ModuleScope (short), IsActive (bool), ImageIconWeb (string?), ImageIconMobile (string?), ItemPriority (int?), Remark (string?), AddedById (long?), AddedDateTime (DateTime?), UpdatedById (long?), UpdatedDateTime (DateTime?)</para>
        /// <para>Angular function(s): ParentModuleApi.updateParentModule (app/core/services/parent-module-api.ts:51).</para>
        /// <para>Angular purpose: updates parent module.</para>
        /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
        /// <para>Angular UI component(s): ModuleForm (app/features/host/modules/module-form/module-form.ts)</para>
        /// </remarks>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateModule(
            int id,
            [FromBody] UpdateParentModuleRequestDTO? updateModuleRequestDTO,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new UpdateParentModuleCommand(id, updateModuleRequestDTO),
                cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: updates parent module status.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates parent module status.</para>
        /// <para>Handler flow: UpdateParentModuleStatusCommand is processed by UpdateParentModuleStatusCommandHandler; operation(s): GetHeaderModuleForStatusUpdateAsync, GetDescendantModulesForStatusUpdateAsync, GetModuleOperationMappingsForStatusUpdateAsync, GetNonDeletedByModuleIdsAsync, SaveModuleStatusCascadeAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetParentModuleResponseDTO: Id (int), ModuleCode (string?), ModuleName (string?), DisplayName (string?), URLPath (string?), ParentModuleId (int?), IsLeafNode (bool?), IsModuleDisplayInUI (bool), IsCommonMenu (bool), ModuleScope (short), IsActive (bool), ImageIconWeb (string?), ImageIconMobile (string?), ItemPriority (int?), Remark (string?), AddedById (long?), AddedDateTime (DateTime?), UpdatedById (long?), UpdatedDateTime (DateTime?)</para>
        /// <para>Angular function(s): ParentModuleApi.setParentModuleStatus (app/core/services/parent-module-api.ts:57).</para>
        /// <para>Angular purpose: updates parent module status.</para>
        /// <para>Integrated UI page(s): /app/modules</para>
        /// <para>Angular UI component(s): ParentModulesStore (app/features/host/modules/parent-modules/parent-modules.store.ts); ParentModules (app/features/host/modules/parent-modules/parent-modules.ts)</para>
        /// </remarks>
        [HttpPatch("{id:int}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
                    public async Task<IActionResult> UpdateModuleStatus(
            int id,
            [FromBody] UpdateParentModuleStatusRequestDTO? statusRequestDTO,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new UpdateParentModuleStatusCommand(id, statusRequestDTO),
                cancellationToken);

            return Ok(result);
        }

        #endregion

        #region Parent Module Queries

        /// <summary>
        /// Used-In-Angular: retrieves parent module.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves parent module by id.</para>
        /// <para>Handler flow: GetParentModuleByIdQuery is processed by GetParentModuleByIdQueryHandler; operation(s): GetParentModuleByIdAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetParentModuleResponseDTO: Id (int), ModuleCode (string?), ModuleName (string?), DisplayName (string?), URLPath (string?), ParentModuleId (int?), IsLeafNode (bool?), IsModuleDisplayInUI (bool), IsCommonMenu (bool), ModuleScope (short), IsActive (bool), ImageIconWeb (string?), ImageIconMobile (string?), ItemPriority (int?), Remark (string?), AddedById (long?), AddedDateTime (DateTime?), UpdatedById (long?), UpdatedDateTime (DateTime?)</para>
        /// <para>Angular function(s): ParentModuleApi.getParentModule (app/core/services/parent-module-api.ts:39).</para>
        /// <para>Angular purpose: retrieves parent module.</para>
        /// <para>Integrated UI page(s): /app/modules/module-operations; /app/modules</para>
        /// <para>Angular UI component(s): ModuleTreeReader (app/core/services/module-tree-reader.ts); ModuleForm (app/features/host/modules/module-form/module-form.ts); ModuleOperationsStore (app/features/host/modules/module-operations/module-operations.store.ts); ParentModulesStore (app/features/host/modules/parent-modules/parent-modules.store.ts); ModuleOperations (app/features/host/modules/module-operations/module-operations.ts); ParentModules (app/features/host/modules/parent-modules/parent-modules.ts)</para>
        /// </remarks>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
                    public async Task<IActionResult> GetModuleById(
            int id,
            [FromQuery] short moduleScope,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetParentModuleByIdQuery(id, moduleScope),
                cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: retrieves module headers.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves module headers.</para>
        /// <para>Handler flow: GetModuleHeadersCommand is processed by GetModuleHeadersCommandHandler; operation(s): GetAllOnlyModuleTreeAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetModuleChildInversResponseDTO: Id (int), ModuleName (string), SubModuleUrl (string?), DisplayName (string?), IsLeafNode (bool?), URLPath (string?), ImageIconWeb (string?), ImageIconMobile (string?), ItemPriority (int?), Children (List&lt;GetModuleChildInversResponseDTO&gt;)</para>
        /// <para>Angular function(s): ParentModuleApi.getModuleHeaders (app/core/services/parent-module-api.ts:32).</para>
        /// <para>Angular purpose: retrieves module headers.</para>
        /// <para>Integrated UI page(s): /app/modules/sub-modules; /app/modules/module-operations; /app/modules</para>
        /// <para>Angular UI component(s): ModuleTreeReader (app/core/services/module-tree-reader.ts); ModuleForm (app/features/host/modules/module-form/module-form.ts); ModuleOperationForm (app/features/host/modules/module-operations/module-operation-form/module-operation-form.ts); SubModulesStore (app/features/host/modules/sub-modules/sub-modules.store.ts); ModuleOperationsStore (app/features/host/modules/module-operations/module-operations.store.ts); ParentModulesStore (app/features/host/modules/parent-modules/parent-modules.store.ts); SubModules (app/features/host/modules/sub-modules/sub-modules.ts); ModuleOperations (app/features/host/modules/module-operations/module-operations.ts)</para>
        /// </remarks>
        [HttpGet("get-module-headers")]
        [ProducesResponseType(StatusCodes.Status200OK)]
                public async Task<IActionResult> GetModuleHeaders(
            [FromQuery] GetParentModuleFilterRequestDTO requestDTO,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetModuleHeadersCommand(requestDTO),
                cancellationToken);

            return Ok(result);
        }

        #endregion
    }
}
