// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Exposes CRUD endpoints for OperationsMaster records.
// ================================================================

using skolenza.application.DTOs.Operation;
using skolenza.application.Features.OperationsMasterCmd.Handlers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.OperationsMaster;

/// <summary>
/// Provides CRUD endpoints for operation master records.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OperationsMasterController : ControllerBase
{
    #region Fields

    private readonly IMediator _mediator;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationsMasterController"/> class.
    /// </summary>
    /// <param name="mediator">The mediator used to dispatch commands and queries.</param>
    public OperationsMasterController(IMediator mediator)
    {
        _mediator = mediator;
    }

    #endregion

    #region Create

    /// <summary>
    /// Used-In-Angular: creates operation.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: creates operation.</para>
    /// <para>Handler flow: CreateOperationCommand is processed by CreateOperationCommandHandler; operation(s): CreateOperationAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetOperationResponseDTO: Id (int?), OperationName (string?), Remark (string?), IsActive (bool), AddedById (long?), AddedDateTime (DateTime), UpdatedById (long?), UpdatedDateTime (DateTime?)</para>
    /// <para>Angular function(s): OperationsMasterApi.addOperation (app/core/services/operations-master-api.ts:38).</para>
    /// <para>Angular purpose: creates operation.</para>
    /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
    /// <para>Angular UI component(s): OperationForm (app/features/host/modules/operations/operation-form/operation-form.ts)</para>
    /// </remarks>
    [HttpPost("create-operation")]
    public async Task<IActionResult> CreateOperation(
        [FromBody] CreateOperationRequestDTO requestDTO,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateOperationCommand(requestDTO),
            cancellationToken);

        return Ok(result);
    }

    #endregion

    #region Update

    /// <summary>
    /// Used-In-Angular: updates operation.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: updates operation.</para>
    /// <para>Handler flow: UpdateOperationCommand is processed by UpdateOperationCommandHandler; operation(s): GetOperationByIdAsync, UpdateOperationAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetOperationResponseDTO: Id (int?), OperationName (string?), Remark (string?), IsActive (bool), AddedById (long?), AddedDateTime (DateTime), UpdatedById (long?), UpdatedDateTime (DateTime?)</para>
    /// <para>Angular function(s): OperationsMasterApi.updateOperation (app/core/services/operations-master-api.ts:45).</para>
    /// <para>Angular purpose: updates operation.</para>
    /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
    /// <para>Angular UI component(s): OperationForm (app/features/host/modules/operations/operation-form/operation-form.ts)</para>
    /// </remarks>
    [HttpPost("update-operation")]
    public async Task<IActionResult> UpdateOperation(
        [FromBody] UpdateOperationRequestDTO requestDTO,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateOperationCommand(requestDTO),
            cancellationToken);

        return Ok(result);
    }

    #endregion

    #region Delete

    /// <summary>
    /// Used-In-Angular: deletes operation.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: deletes operation.</para>
    /// <para>Handler flow: DeleteOperationCommand is processed by DeleteOperationCommandHandler; operation(s): GetOperationByIdAsync, DeleteOperationAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
    /// <para>Angular function(s): OperationsMasterApi.deleteOperation (app/core/services/operations-master-api.ts:51).</para>
    /// <para>Angular purpose: deletes operation.</para>
    /// <para>Integrated UI page(s): /app/modules/operations</para>
    /// <para>Angular UI component(s): OperationsStore (app/features/host/modules/operations/operations.store.ts); Operations (app/features/host/modules/operations/operations.ts)</para>
    /// </remarks>
    [HttpDelete("delete-operation/{operationId:int}")]
    public async Task<IActionResult> DeleteOperation(
        int operationId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new DeleteOperationCommand(operationId),
            cancellationToken);

        return Ok(result);
    }

    #endregion

    #region GetById

    /// <summary>
    /// Used-In-Angular: retrieves operation.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves operation by id.</para>
    /// <para>Handler flow: GetOperationByIdQuery is processed by GetOperationByIdQueryHandler; operation(s): GetOperationByIdAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetOperationResponseDTO: Id (int?), OperationName (string?), Remark (string?), IsActive (bool), AddedById (long?), AddedDateTime (DateTime), UpdatedById (long?), UpdatedDateTime (DateTime?)</para>
    /// <para>Angular function(s): OperationsMasterApi.getOperation (app/core/services/operations-master-api.ts:32).</para>
    /// <para>Angular purpose: retrieves operation.</para>
    /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
    /// <para>Angular UI component(s): OperationForm (app/features/host/modules/operations/operation-form/operation-form.ts)</para>
    /// </remarks>
    [HttpGet("get-operation/{operationId:int}")]
    public async Task<IActionResult> GetOperationById(
        int operationId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetOperationByIdQuery(operationId),
            cancellationToken);

        return Ok(result);
    }

    #endregion

    #region GetAll

    /// <summary>
    /// Used-In-Angular: retrieves operations.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves all operations.</para>
    /// <para>Handler flow: GetAllOperationsQuery is processed by GetAllOperationsQueryHandler; operation(s): GetAllOperationAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetOperationResponseDTO: Id (int?), OperationName (string?), Remark (string?), IsActive (bool), AddedById (long?), AddedDateTime (DateTime), UpdatedById (long?), UpdatedDateTime (DateTime?)</para>
    /// <para>Angular function(s): OperationsMasterApi.getOperations (app/core/services/operations-master-api.ts:26).</para>
    /// <para>Angular purpose: retrieves operations.</para>
    /// <para>Integrated UI page(s): /app/modules/operations</para>
    /// <para>Angular UI component(s): ModuleOperationForm (app/features/host/modules/module-operations/module-operation-form/module-operation-form.ts); OperationsStore (app/features/host/modules/operations/operations.store.ts); Operations (app/features/host/modules/operations/operations.ts)</para>
    /// </remarks>
    [HttpGet("get-all-operations")]
    public async Task<IActionResult> GetAllOperations(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetAllOperationsQuery(),
            cancellationToken);

        return Ok(result);
    }

    #endregion
}
