// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Ticket Classification operations.
// ================================================================

using skolenza.application.DTOS.TicketDTO.Classification;
using skolenza.application.Features.TickeAllCmd.Classification;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace skolenza.api.Controllers.Ticket;

/// <summary>
/// Controller responsible for managing Ticket Classification operations.
/// Handles all Create, Read, Update, and Delete (CRUD) APIs for Ticket Classifications.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TicketClassificationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TicketClassificationController> _logger;

    public TicketClassificationController(IMediator mediator, ILogger<TicketClassificationController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    // ----------------------------------------------------------------------------------------------------
    // 1️⃣ CREATE - Add new Ticket Classification
    // ----------------------------------------------------------------------------------------------------
    /// <summary>
    /// Used-In-Angular: creates classification.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: creates classification.</para>
    /// <para>Handler flow: AddClassificationCommand is processed by AddClassificationCommandHandler; operation(s): AddAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetClassificationResponseDTO: Id (int), ClassificationName (string), Description (string?), IsActive (bool)</para>
    /// <para>Angular function(s): TicketApi.addClassification (app/features/tickets/ticket-api.ts:56).</para>
    /// <para>Angular purpose: creates classification.</para>
    /// <para>Integrated UI page(s): /app/tickets/classifications</para>
    /// <para>Angular UI component(s): TicketClassificationManageDialog (app/features/tickets/ticket-classification/ticket-classification-manage-dialog/ticket-classification-manage-dialog.ts); TicketClassificationComponent (app/features/tickets/ticket-classification/ticket-classification.ts)</para>
    /// </remarks>
    [HttpPost("create")]
    public async Task<IActionResult> CreateTicketClassification([FromBody] AddClassificationRequestDTO dto)
    {

            _logger.LogInformation("🎯 Received request to create TicketClassification: {Data}", JsonConvert.SerializeObject(dto));
            var command = new AddClassificationCommand(dto);
            var result = await _mediator.Send(command);
            return Ok(result);


    }

    // ----------------------------------------------------------------------------------------------------
    // 2️⃣ READ - Get all Ticket Classifications
    // ----------------------------------------------------------------------------------------------------
    /// <summary>
    /// Used-In-Angular: retrieves classifications.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves all classification.</para>
    /// <para>Handler flow: GetAllClassificationCommand is processed by GetAllClassificationCommandHandler; operation(s): GetAllAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetClassificationResponseDTO: Id (int), ClassificationName (string), Description (string?), IsActive (bool)</para>
    /// <para>Angular function(s): TicketApi.getClassifications (app/features/tickets/ticket-api.ts:48).</para>
    /// <para>Angular purpose: retrieves classifications.</para>
    /// <para>Integrated UI page(s): /app/tickets/support-teams; /app/tickets/classifications; /app/tickets/:id; /app/tickets/headers; /app/tickets; /app/tickets/types</para>
    /// <para>Angular UI component(s): TicketsStore (app/features/tickets/tickets.store.ts); TicketAgents (app/features/tickets/ticket-agents/ticket-agents.ts); TicketClassificationComponent (app/features/tickets/ticket-classification/ticket-classification.ts); TicketDetailSidebar (app/features/tickets/ticket-details/ticket-detail-sidebar/ticket-detail-sidebar.ts); TicketDetailsHeader (app/features/tickets/ticket-details/ticket-details-header/ticket-details-header.ts); TicketDetails (app/features/tickets/ticket-details/ticket-details.ts); TicketHeaderComponent (app/features/tickets/ticket-header/ticket-header.ts); TicketLists (app/features/tickets/ticket-lists/ticket-lists.ts)</para>
    /// </remarks>
    [HttpGet("all")]
    public async Task<IActionResult> GetAllTicketClassifications([FromQuery] GetAllClassificationRequestDTO dto)
    {

        _logger.LogInformation("📦 Fetching all Ticket Classifications...");
        var command = new GetAllClassificationCommand(dto);
        var result = await _mediator.Send(command);
        return Ok(result);


    }
    /// <summary>
    /// Used-In-Angular: retrieves classification ddl.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves classification.</para>
    /// <para>Handler flow: DDLClassificationCommand is processed by DDLClassificationCommandHandler; operation(s): GetDDLAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetClassificationResponseDTO: Id (int), ClassificationName (string), Description (string?), IsActive (bool)</para>
    /// <para>Angular function(s): TicketApi.getClassificationDdl (app/features/tickets/ticket-api.ts:36).</para>
    /// <para>Angular purpose: retrieves classification ddl.</para>
    /// <para>Integrated UI page(s): /app/tickets/support-teams; /app/tickets/classifications; /app/tickets/:id; /app/tickets/headers; /app/tickets; /app/tickets/types</para>
    /// <para>Angular UI component(s): TicketManageDialog (app/features/tickets/ticket-lists/ticket-manage-dialog/ticket-manage-dialog.ts); TicketsStore (app/features/tickets/tickets.store.ts); TicketAgents (app/features/tickets/ticket-agents/ticket-agents.ts); TicketClassificationComponent (app/features/tickets/ticket-classification/ticket-classification.ts); TicketDetailSidebar (app/features/tickets/ticket-details/ticket-detail-sidebar/ticket-detail-sidebar.ts); TicketDetailsHeader (app/features/tickets/ticket-details/ticket-details-header/ticket-details-header.ts); TicketDetails (app/features/tickets/ticket-details/ticket-details.ts); TicketHeaderComponent (app/features/tickets/ticket-header/ticket-header.ts)</para>
    /// </remarks>
    [HttpGet("ddl-list")]
    public async Task<IActionResult> GetAllTicketClassifications([FromQuery] DDLClassificationRequestDTO dto)
    {

        _logger.LogInformation("📦 Fetching all Ticket Classifications...");
        var command = new DDLClassificationCommand(dto);
        var result = await _mediator.Send(command);
        return Ok(result);


    }

    // ----------------------------------------------------------------------------------------------------
    // 3️⃣ READ (BY ID) - Get specific Ticket Classification
    // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Not-Used-In-Angular.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Not-Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves classification by id.</para>
        /// <para>Handler flow: GetClassificationByIdQuery is processed by GetClassificationByIdQueryHandler; operation(s): GetByIdAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetClassificationResponseDTO: Id (int), ClassificationName (string), Description (string?), IsActive (bool)</para>
        /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
        /// <para>Backend endpoint: GET /api/ticketclassification/get.</para>
        /// </remarks>
        [HttpGet("get")]
        public async Task<IActionResult> GetTicketClassificationById([FromQuery] GetClassificationRequestDTO dto)
        {

                _logger.LogInformation("🔍 Fetching TicketClassification details for Id = {Id}", dto.Id);
                var command = new GetClassificationByIdQuery(dto);
                var result = await _mediator.Send(command);
                return Ok(result);

        }

    // ----------------------------------------------------------------------------------------------------
    // 4️⃣ UPDATE - Modify existing Ticket Classification
    // ----------------------------------------------------------------------------------------------------
    /// <summary>
    /// Used-In-Angular: updates classification.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: updates classification.</para>
    /// <para>Handler flow: UpdateClassificationCommand is processed by UpdateClassificationCommandHandler; operation(s): GetByIdForInstitutionAsync, Map, UpdateAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetClassificationResponseDTO: Id (int), ClassificationName (string), Description (string?), IsActive (bool)</para>
    /// <para>Angular function(s): TicketApi.updateClassification (app/features/tickets/ticket-api.ts:64).</para>
    /// <para>Angular purpose: updates classification.</para>
    /// <para>Integrated UI page(s): /app/tickets/classifications</para>
    /// <para>Angular UI component(s): TicketClassificationManageDialog (app/features/tickets/ticket-classification/ticket-classification-manage-dialog/ticket-classification-manage-dialog.ts); TicketClassificationComponent (app/features/tickets/ticket-classification/ticket-classification.ts)</para>
    /// </remarks>
    [HttpPut("update")]
    public async Task<IActionResult> UpdateTicketClassification([FromBody] UpdateClassificationRequestDTO dto)
    {

            _logger.LogInformation("🛠️ Updating TicketClassification: {Data}", JsonConvert.SerializeObject(dto));
            var command = new UpdateClassificationCommand(dto);
            var result = await _mediator.Send(command);
            return Ok(result);

    }

    // ----------------------------------------------------------------------------------------------------
    // 5️⃣ DELETE - Soft delete Ticket Classification
    // ----------------------------------------------------------------------------------------------------
    /// <summary>
    /// Used-In-Angular: deletes classification.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: deletes classification.</para>
    /// <para>Handler flow: DeleteClassificationCommand is processed by DeleteClassificationCommandHandler; operation(s): DeleteAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
    /// <para>Angular function(s): TicketApi.deleteClassification (app/features/tickets/ticket-api.ts:72).</para>
    /// <para>Angular purpose: deletes classification.</para>
    /// <para>Integrated UI page(s): /app/tickets/support-teams; /app/tickets/classifications; /app/tickets/:id; /app/tickets/headers; /app/tickets; /app/tickets/types</para>
    /// <para>Angular UI component(s): TicketsStore (app/features/tickets/tickets.store.ts); TicketAgents (app/features/tickets/ticket-agents/ticket-agents.ts); TicketClassificationComponent (app/features/tickets/ticket-classification/ticket-classification.ts); TicketDetailSidebar (app/features/tickets/ticket-details/ticket-detail-sidebar/ticket-detail-sidebar.ts); TicketDetailsHeader (app/features/tickets/ticket-details/ticket-details-header/ticket-details-header.ts); TicketDetails (app/features/tickets/ticket-details/ticket-details.ts); TicketHeaderComponent (app/features/tickets/ticket-header/ticket-header.ts); TicketLists (app/features/tickets/ticket-lists/ticket-lists.ts)</para>
    /// </remarks>
    [HttpDelete("delete")]
    public async Task<IActionResult> DeleteTicketClassification([FromBody] DeleteClassificationRequestDTO dto)
    {

            _logger.LogInformation("🗑️ Request received to delete TicketClassification with Id = {Id}", dto.Id);
            var command = new DeleteClassificationCommand(dto);
            var result = await _mediator.Send(new DeleteClassificationCommand(dto));

            return Ok(result);


    }
}
