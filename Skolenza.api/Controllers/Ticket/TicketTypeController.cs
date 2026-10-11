// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Ticket Type operations.
// ================================================================

using skolenza.application.DTOS.TicketDTO.TicketType;

using skolenza.application.Features.TickeAllCmd.TicketType.Handlers;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace skolenza.api.Controllers.Ticket
{
    /// <summary>
    /// Controller responsible for managing Ticket Type operations.
    /// Handles all Create, Read, Update, and Delete (CRUD) APIs for Ticket Types.
    /// </summary>
    [ApiController]
    [Route("api/Ticket/[controller]")]
    public class TicketTypeController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<TicketTypeController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TicketTypeController"/> class.
        /// </summary>
        /// <param name="mediator">Mediator instance for handling CQRS commands/queries.</param>
        /// <param name="logger">Logger instance for logging controller actions.</param>
        public TicketTypeController(IMediator mediator, ILogger<TicketTypeController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // ----------------------------------------------------------------------------------------------------
        // 1️⃣ CREATE - Add new Ticket Type
        // ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Used-In-Angular: creates type.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: creates ticket type.</para>
        /// <para>Handler flow: CreateTicketTypeCommand is processed by CreateTicketTypeCommandHandler; operation(s): GetByIdAsync, GetByIdAsync1, AddAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetTicketTypeResponseDTO: Id (long), ClassificationId (int), TicketTypeName (string), TicketHeaderId (long), TicketHeaderName (string?), InstitutionId (long), ResponsibleRoleId (int?), ResponsibleRoleName (string?), IsApprovalRequired (bool), ApprovalId (int?), ApprovalRoleName (string?), AutoApproveIfSameRole (bool), SLAHours (int?), IsAttachmentRequired (bool), IsActiveForAllUsers (bool), Description (string?), IsActive (bool)</para>
        /// <para>Angular function(s): TicketApi.addType (app/features/tickets/ticket-api.ts:118).</para>
        /// <para>Angular purpose: creates type.</para>
        /// <para>Integrated UI page(s): /app/tickets/types</para>
        /// <para>Angular UI component(s): TicketTypeManageDialog (app/features/tickets/ticket-type/ticket-type-manage-dialog/ticket-type-manage-dialog.ts); TicketTypeComponent (app/features/tickets/ticket-type/ticket-type.ts)</para>
        /// </remarks>
        [HttpPost("create")]
        public async Task<IActionResult> CreateTicketType([FromBody] AddTicketTypeRequestDTO dto)
        {

            _logger.LogInformation("🎯 Received request to create TicketType: {Data}", JsonConvert.SerializeObject(dto));
            var result = await _mediator.Send(new CreateTicketTypeCommand(dto));
            return Ok(result);
        }



        // ----------------------------------------------------------------------------------------------------
        // 2️⃣ READ - Get all Ticket Types
        // ----------------------------------------------------------------------------------------------------

        // <summary>
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: retrieves all ticket type.</para>
                /// <para>Handler flow: GetAllTicketTypeQuery is processed by GetAllTicketTypeQueryHandler.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetTicketTypeResponseDTO: Id (long), ClassificationId (int), TicketTypeName (string), TicketHeaderId (long), TicketHeaderName (string?), InstitutionId (long), ResponsibleRoleId (int?), ResponsibleRoleName (string?), IsApprovalRequired (bool), ApprovalId (int?), ApprovalRoleName (string?), AutoApproveIfSameRole (bool), SLAHours (int?), IsAttachmentRequired (bool), IsActiveForAllUsers (bool), Description (string?), IsActive (bool)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: GET /api/ticket/tickettype/get-all.</para>
                /// </remarks>


                [HttpGet("get-all")]
                public async Task<IActionResult> GetAllTicketTypes([FromQuery] GetTicketTypeRequestDTO dto)
                {

                    _logger.LogInformation("📦 Fetching all Ticket Types...");
                    var result = await _mediator.Send(new GetAllTicketTypeQuery(dto));
                    return Ok(result);
                }
        /// <summary>
        /// Used-In-Angular: retrieves ticket type ddl.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves ticket type.</para>
        /// <para>Handler flow: DDLTicketTypeQuery is processed by DDLTicketTypeQueryHandler; operation(s): GetDDLAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetDDLTicketTypeResponseDTO: Id (long), TicketTypeName (string), Description (string?), IsActive (bool)</para>
        /// <para>Angular function(s): TicketApi.getTicketTypeDdl (app/features/tickets/ticket-api.ts:137).</para>
        /// <para>Angular purpose: retrieves ticket type ddl.</para>
        /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
        /// <para>Angular UI component(s): TicketManageDialog (app/features/tickets/ticket-lists/ticket-manage-dialog/ticket-manage-dialog.ts)</para>
        /// </remarks>

        [HttpGet("ddl-list")]
        public async Task<IActionResult> GetDDLTicketTypes([FromQuery] GetDDLTicketTypeRequestDTO dto)
        {

            _logger.LogInformation("📦 Fetching all Ticket Types for DDL...");
            var result = await _mediator.Send(new DDLTicketTypeQuery(dto));
            return Ok(result);
        }

        // ----------------------------------------------------------------------------------------------------
        // 3️⃣ READ (BY ID) - Get specific Ticket Type
        // ----------------------------------------------------------------------------------------------------
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: retrieves ticket type by id.</para>
                /// <para>Handler flow: GetTicketTypeByIdQuery is processed by GetTicketTypeByIdQueryHandler; operation(s): GetByIdAsync.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetTicketTypeResponseDTO: Id (long), ClassificationId (int), TicketTypeName (string), TicketHeaderId (long), TicketHeaderName (string?), InstitutionId (long), ResponsibleRoleId (int?), ResponsibleRoleName (string?), IsApprovalRequired (bool), ApprovalId (int?), ApprovalRoleName (string?), AutoApproveIfSameRole (bool), SLAHours (int?), IsAttachmentRequired (bool), IsActiveForAllUsers (bool), Description (string?), IsActive (bool)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: GET /api/ticket/tickettype/get-by-id.</para>
                /// </remarks>

                [HttpGet("get-by-id")]
                public async Task<IActionResult> GetTicketTypeById([FromQuery] GetTicketTypeByIdRequestDTO dto)
                {

                    _logger.LogInformation("🔍 Fetching TicketType details for Id = {Id}", dto);
                    var result = await _mediator.Send(new GetTicketTypeByIdQuery(dto));
                    return Ok(result);

                }

        // ----------------------------------------------------------------------------------------------------
        // 4️⃣ UPDATE - Modify existing Ticket Type
        // ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Used-In-Angular: updates type.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates ticket type.</para>
        /// <para>Handler flow: UpdateTicketTypeCommand is processed by UpdateTicketTypeCommandHandler; operation(s): GetByIdAsync, GetByIdAsync1, GetByIdForInstitutionAsync, Map, UpdateAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): TicketApi.updateType (app/features/tickets/ticket-api.ts:124).</para>
        /// <para>Angular purpose: updates type.</para>
        /// <para>Integrated UI page(s): /app/tickets/types</para>
        /// <para>Angular UI component(s): TicketTypeManageDialog (app/features/tickets/ticket-type/ticket-type-manage-dialog/ticket-type-manage-dialog.ts); TicketTypeComponent (app/features/tickets/ticket-type/ticket-type.ts)</para>
        /// </remarks>
        [HttpPut("update")]
        public async Task<IActionResult> UpdateTicketType([FromBody] UpdateTicketTypeRequestDTO dto)
        {

                var result = await _mediator.Send(new UpdateTicketTypeCommand(dto));

                return Ok(result);

        }

        // ----------------------------------------------------------------------------------------------------
        // 5️⃣ DELETE - Soft delete Ticket Type
        // ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Used-In-Angular: deletes type.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: deletes ticket type.</para>
        /// <para>Handler flow: DeleteTicketTypeCommand is processed by DeleteTicketTypeCommandHandler; operation(s): DeleteAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): TicketApi.deleteType (app/features/tickets/ticket-api.ts:130).</para>
        /// <para>Angular purpose: deletes type.</para>
        /// <para>Integrated UI page(s): /app/tickets/support-teams; /app/tickets/classifications; /app/tickets/:id; /app/tickets/headers; /app/tickets; /app/tickets/types</para>
        /// <para>Angular UI component(s): TicketsStore (app/features/tickets/tickets.store.ts); TicketAgents (app/features/tickets/ticket-agents/ticket-agents.ts); TicketClassificationComponent (app/features/tickets/ticket-classification/ticket-classification.ts); TicketDetailSidebar (app/features/tickets/ticket-details/ticket-detail-sidebar/ticket-detail-sidebar.ts); TicketDetailsHeader (app/features/tickets/ticket-details/ticket-details-header/ticket-details-header.ts); TicketDetails (app/features/tickets/ticket-details/ticket-details.ts); TicketHeaderComponent (app/features/tickets/ticket-header/ticket-header.ts); TicketLists (app/features/tickets/ticket-lists/ticket-lists.ts)</para>
        /// </remarks>
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteTicketType([FromBody]DeleteTicketTypeRequestDTO dto)
        {

                _logger.LogInformation("🗑️ Request received to delete TicketType with Id = {Id}", dto);
                var result = await _mediator.Send(new DeleteTicketTypeCommand(dto));
                return Ok(result);

        }

        // ----------------------------------------------------------------------------------------------------
        // 6️⃣ GET BY MODULE ID - Filter Ticket Types by Module
        // ----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Retrieves Ticket Types associated with a specific Module ID.
        /// </summary>

                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: retrieves all ticket type by header id.</para>
                /// <para>Handler flow: GetAllTicketTypeByHeaderIdQuery is processed by GetAllTicketTypeByHeaderIdQueryHandler.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetTicketTypeResponseDTO: Id (long), ClassificationId (int), TicketTypeName (string), TicketHeaderId (long), TicketHeaderName (string?), InstitutionId (long), ResponsibleRoleId (int?), ResponsibleRoleName (string?), IsApprovalRequired (bool), ApprovalId (int?), ApprovalRoleName (string?), AutoApproveIfSameRole (bool), SLAHours (int?), IsAttachmentRequired (bool), IsActiveForAllUsers (bool), Description (string?), IsActive (bool)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: GET /api/ticket/tickettype/get-by-header-id.</para>
                /// </remarks>
                [HttpGet("get-by-header-id")]
                public async Task<IActionResult> GetTicketTypesByHeaderId( [FromQuery] GetTicketTypeByHeaderIdRequestDTO dto)
                {

                        _logger.LogInformation("📂 Fetching Ticket Types for ModuleId = {ModuleId}", dto.TicketHeaderId);
                        var result = await _mediator.Send(new GetAllTicketTypeByHeaderIdQuery(dto));
                    return Ok(result);

                }
        /// <summary>
        /// Retrieves Ticket Types associated with a specific Module ID.
        /// </summary>

        /// <param name="dto">Role ID to filter Ticket Types.</param>
        /// <returns>Returns a list of Ticket Types linked to the provided Role ID.</returns>
        //[HttpGet("get-by-role-id")]
        //public async Task<IActionResult> GetTicketTypesByRoleId([FromQuery] GetTicketTypeByRoleIdRequestDTO dto)
        //{
        //    var result = await _mediator.Send(new GetAllTicketTypeByRoleIdQuery(dto));
        //        return Ok(result);

        //}

    }
}
