// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Exposes HostUser-controlled CRUD endpoints for ModuleOperation mappings.
// ================================================================

using skolenza.application.DTOs.Module;
using skolenza.application.DTOs.ModuleOperation;
using skolenza.application.Features.ModuleCmd.Parent.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Module;

/// <summary>
/// Coordinates HostUser HTTP requests for module-operation mappings.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ModuleOperationController : ControllerBase
{
    #region Fields

    private readonly IMediator _mediator;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ModuleOperationController"/> class.
    /// </summary>
    /// <param name="mediator">Dispatches module-operation commands and queries.</param>
    public ModuleOperationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    #endregion

    #region ModuleOperation CRUD

    /// <summary>
    /// Used-In-Angular: creates module operation.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: creates module operation.</para>
    /// <para>Handler flow: CreateModuleOperationCommand is processed by CreateModuleOperationCommandHandler; operation(s): CreateModuleOperationMappingAsync, GetModuleOperationMappingByIdAsync, GetModuleHierarchyForOperationActivationAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); ModuleOperationMappingByProductOwnerResponseDTO: Id (int), ModuleId (int), OperationId (int), DataViewStructureId (int?), PageTypeId (int?), ModuleName (string?), OperationName (string?), DataViewStructureDisplayOn (string?), PageTypeName (string?), PageURL (string?), IconURL (string?), IsCommonItem (bool?), IsOperational (bool?), Priority (int?), Remark (string?), IsActive (bool?), AddedById (long), AddedDateTime (DateTime), UpdatedById (long?), UpdatedDateTime (DateTime?)</para>
    /// <para>Angular function(s): ModuleOperationApi.addModuleOperation (app/core/services/module-operation-api.ts:38).</para>
    /// <para>Angular purpose: creates module operation.</para>
    /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
    /// <para>Angular UI component(s): ModuleOperationForm (app/features/host/modules/module-operations/module-operation-form/module-operation-form.ts)</para>
    /// </remarks>
    [HttpPost("create")]
    public async Task<IActionResult> CreateModuleOperation(
        [FromBody] CreateModuleOperationRequestDTO? dto,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateModuleOperationCommand(dto),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Used-In-Angular: updates module operation.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: updates module operation mapping by product owner.</para>
    /// <para>Handler flow: UpdateModuleOperationMappingByProductOwnerCommand is processed by UpdateModuleOperationMappingByProductOwnerCommandHandler; operation(s): GetModuleOperationMappingByIdAsync, Map, UpdateModuleOperationMappingAsync, GetValueOrDefault, GetModuleHierarchyForOperationActivationAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); ModuleOperationMappingByProductOwnerResponseDTO: Id (int), ModuleId (int), OperationId (int), DataViewStructureId (int?), PageTypeId (int?), ModuleName (string?), OperationName (string?), DataViewStructureDisplayOn (string?), PageTypeName (string?), PageURL (string?), IconURL (string?), IsCommonItem (bool?), IsOperational (bool?), Priority (int?), Remark (string?), IsActive (bool?), AddedById (long), AddedDateTime (DateTime), UpdatedById (long?), UpdatedDateTime (DateTime?)</para>
    /// <para>Angular function(s): ModuleOperationApi.updateModuleOperation (app/core/services/module-operation-api.ts:45).</para>
    /// <para>Angular purpose: updates module operation.</para>
    /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
    /// <para>Angular UI component(s): ModuleOperationForm (app/features/host/modules/module-operations/module-operation-form/module-operation-form.ts)</para>
    /// </remarks>
    [HttpPost("update")]
    public async Task<IActionResult> UpdateModuleOperation(
        [FromBody] UpdateModuleOperationMappingByProductOwnerRequestDTO? dto,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateModuleOperationMappingByProductOwnerCommand(dto),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Used-In-Angular: deletes module operation.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: deletes module operation.</para>
    /// <para>Handler flow: DeleteModuleOperationCommand is processed by DeleteModuleOperationCommandHandler.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
    /// <para>Angular function(s): ModuleOperationApi.deleteModuleOperation (app/core/services/module-operation-api.ts:51).</para>
    /// <para>Angular purpose: deletes module operation.</para>
    /// <para>Integrated UI page(s): /app/modules/module-operations</para>
    /// <para>Angular UI component(s): ModuleOperationsStore (app/features/host/modules/module-operations/module-operations.store.ts); ModuleOperations (app/features/host/modules/module-operations/module-operations.ts)</para>
    /// </remarks>
    [HttpDelete("delete/{id:int}")]
    public async Task<IActionResult> DeleteModuleOperation(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new DeleteModuleOperationCommand(id),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Used-In-Angular: retrieves module operation.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves module operation by id.</para>
    /// <para>Handler flow: GetModuleOperationByIdQuery is processed by GetModuleOperationByIdQueryHandler; operation(s): GetModuleOperationMappingByIdAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); ModuleOperationMappingByProductOwnerResponseDTO: Id (int), ModuleId (int), OperationId (int), DataViewStructureId (int?), PageTypeId (int?), ModuleName (string?), OperationName (string?), DataViewStructureDisplayOn (string?), PageTypeName (string?), PageURL (string?), IconURL (string?), IsCommonItem (bool?), IsOperational (bool?), Priority (int?), Remark (string?), IsActive (bool?), AddedById (long), AddedDateTime (DateTime), UpdatedById (long?), UpdatedDateTime (DateTime?)</para>
    /// <para>Angular function(s): ModuleOperationApi.getModuleOperation (app/core/services/module-operation-api.ts:32).</para>
    /// <para>Angular purpose: retrieves module operation.</para>
    /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
    /// <para>Angular UI component(s): ModuleOperationForm (app/features/host/modules/module-operations/module-operation-form/module-operation-form.ts)</para>
    /// </remarks>
    [HttpGet("get-by-id/{id:int}")]
    public async Task<IActionResult> GetModuleOperationById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetModuleOperationByIdQuery(id),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Used-In-Angular: retrieves module operations.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves all module operations.</para>
    /// <para>Handler flow: GetAllModuleOperationsQuery is processed by GetAllModuleOperationsQueryHandler; operation(s): GetAllModuleOperationMappingsAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); ModuleOperationMappingByProductOwnerResponseDTO: Id (int), ModuleId (int), OperationId (int), DataViewStructureId (int?), PageTypeId (int?), ModuleName (string?), OperationName (string?), DataViewStructureDisplayOn (string?), PageTypeName (string?), PageURL (string?), IconURL (string?), IsCommonItem (bool?), IsOperational (bool?), Priority (int?), Remark (string?), IsActive (bool?), AddedById (long), AddedDateTime (DateTime), UpdatedById (long?), UpdatedDateTime (DateTime?)</para>
    /// <para>Angular function(s): ModuleOperationApi.getModuleOperations (app/core/services/module-operation-api.ts:26).</para>
    /// <para>Angular purpose: retrieves module operations.</para>
    /// <para>Integrated UI page(s): /app/modules/module-operations</para>
    /// <para>Angular UI component(s): ModuleOperationsStore (app/features/host/modules/module-operations/module-operations.store.ts); ModuleOperations (app/features/host/modules/module-operations/module-operations.ts)</para>
    /// </remarks>
    [HttpGet("get-all")]
    public async Task<IActionResult> GetAllModuleOperations(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetAllModuleOperationsQuery(),
            cancellationToken);

        return Ok(result);
    }

    #endregion
}
