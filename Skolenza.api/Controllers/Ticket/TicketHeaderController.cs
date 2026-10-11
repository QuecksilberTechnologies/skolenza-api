// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Ticket Header operations.
// ================================================================

using skolenza.application.DTOS.TicketDTO.Header;
using skolenza.application.DTOS.TicketDTO.TicketType;
using skolenza.application.Features.TickeAllCmd.TicketHeader.Handlers;
using skolenza.application.Features.TickeAllCmd.TicketType.Handlers;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace skolenza.api.Controllers.Ticket
{
    /// <summary>
    /// Controller responsible for managing Ticket Header operations.
    /// Handles all Create, Read, Update, and Delete (CRUD) APIs for Ticket Headers.
    /// </summary>
    [ApiController]
    [Route("api/Ticket/[controller]")]
    public class TicketHeaderController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<TicketHeaderController> _logger;

        public TicketHeaderController(IMediator mediator, ILogger<TicketHeaderController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // ----------------------------------------------------------------------------------------------------
        // 1️⃣ CREATE - Add new Ticket Header
        // ----------------------------------------------------------------------------------------------------
           /// <summary>
           /// Used-In-Angular: creates header.
           /// </summary>
           /// <remarks>
           /// <para>Angular usage status: Used-In-Angular.</para>
           /// <para>API endpoint purpose: creates header.</para>
           /// <para>Handler flow: AddHeaderCommand is processed by AddHeaderCommandHandler; operation(s): AddAsync.</para>
           /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetHeaderResponseDTO: Id (long), HeaderName (string), TicketClassificationId (int), TicketClassificationName (string?), Description (string?), IsActive (bool)</para>
           /// <para>Angular function(s): TicketApi.addHeader (app/features/tickets/ticket-api.ts:89).</para>
           /// <para>Angular purpose: creates header.</para>
           /// <para>Integrated UI page(s): /app/tickets/headers</para>
           /// <para>Angular UI component(s): TicketHeaderManageDialog (app/features/tickets/ticket-header/ticket-header-manage-dialog/ticket-header-manage-dialog.ts); TicketHeaderComponent (app/features/tickets/ticket-header/ticket-header.ts)</para>
           /// </remarks>
           [HttpPost("create")]
         public async Task<IActionResult> CreateHeader([FromBody] AddHeaderRequestDTO dto)
         {

               _logger.LogInformation("🎯 Received request to create Ticket Header: {Data}", JsonConvert.SerializeObject(dto));
                var result = await _mediator.Send(new AddHeaderCommand(dto));
                  return Ok(result);

         }

        // ----------------------------------------------------------------------------------------------------
        // 2️⃣ READ - Get all Ticket Headers with filters
        // ----------------------------------------------------------------------------------------------------
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: retrieves header by classify id.</para>
                /// <para>Handler flow: GetHeaderByClassifyIdQuery is processed by GetHeaderByClassifyIdQueryHandler; operation(s): GetByClassificationIdAsync.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetHeaderResponseDTO: Id (long), HeaderName (string), TicketClassificationId (int), TicketClassificationName (string?), Description (string?), IsActive (bool)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: GET /api/ticket/ticketheader/get-by-classification-id.</para>
                /// </remarks>
                [HttpGet("get-by-classification-id")]
                public async Task<IActionResult> GetAllHeaderFilterAsync([FromQuery] GetTicketHeaderByClassifyIdRequestDTO dto)
                {

                        _logger.LogInformation("📦 Fetching Ticket Headers with applied filters: {Filters}",
                            JsonConvert.SerializeObject(dto));

                        var result = await _mediator.Send(new GetHeaderByClassifyIdQuery(dto));
                        _logger.LogInformation("✅ {Count} Ticket Headers fetched successfully.", result.Data?.Count ?? 0);
                        return Ok(result);


                }
        //[HttpGet("get-filter")]
        //public async Task<IActionResult> GetAllHeaderFilterAsync([FromQuery] GetHeaderRequestDTO dto)
        //{

        //        _logger.LogInformation("📦 Fetching Ticket Headers with applied filters: {Filters}",
        //            JsonConvert.SerializeObject(dto));

        //        var result = await _mediator.Send(new GetHeaderFilterCommand(dto));
        //        _logger.LogInformation("✅ {Count} Ticket Headers fetched successfully.", result.Data?.Count ?? 0);
        //        return Ok(result);


        //}

        // ----------------------------------------------------------------------------------------------------
        // 3️⃣ UPDATE - Modify existing Ticket Header
        // ----------------------------------------------------------------------------------------------------
          /// <summary>
          /// Used-In-Angular: updates header.
          /// </summary>
          /// <remarks>
          /// <para>Angular usage status: Used-In-Angular.</para>
          /// <para>API endpoint purpose: updates header.</para>
          /// <para>Handler flow: UpdateHeaderCommand is processed by UpdateHeaderCommandHandler; operation(s): GetByIdForInstitutionAsync, Map, UpdateAsync.</para>
          /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetHeaderResponseDTO: Id (long), HeaderName (string), TicketClassificationId (int), TicketClassificationName (string?), Description (string?), IsActive (bool)</para>
          /// <para>Angular function(s): TicketApi.updateHeader (app/features/tickets/ticket-api.ts:95).</para>
          /// <para>Angular purpose: updates header.</para>
          /// <para>Integrated UI page(s): /app/tickets/headers</para>
          /// <para>Angular UI component(s): TicketHeaderManageDialog (app/features/tickets/ticket-header/ticket-header-manage-dialog/ticket-header-manage-dialog.ts); TicketHeaderComponent (app/features/tickets/ticket-header/ticket-header.ts)</para>
          /// </remarks>
          [HttpPut("update")]



        public async Task<IActionResult> UpdateHeader([FromBody] UpdateHeaderRequestDTO dto)
        {
            try
            {
                _logger.LogInformation("🛠️ Updating Ticket Header: {Data}", JsonConvert.SerializeObject(dto));

                var result = await _mediator.Send(new UpdateHeaderCommand(dto));

                if (!result.IsSucceeded)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error occurred while updating Ticket Header.");
                return StatusCode(500, new ApiResponse<string>
                {
                    IsSucceeded = false,
                    Message = $"Internal Server Error: {ex.Message}"
                });
            }
        }

        // ----------------------------------------------------------------------------------------------------
        // 4️⃣ DELETE - Soft delete Ticket Header
        // ----------------------------------------------------------------------------------------------------
        /// <summary>
        /// Used-In-Angular: deletes header.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: deletes header.</para>
        /// <para>Handler flow: DeleteHeaderCommand is processed by DeleteHeaderCommandHandler; operation(s): DeleteAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): TicketApi.deleteHeader (app/features/tickets/ticket-api.ts:101).</para>
        /// <para>Angular purpose: deletes header.</para>
        /// <para>Integrated UI page(s): /app/tickets/support-teams; /app/tickets/classifications; /app/tickets/:id; /app/tickets/headers; /app/tickets; /app/tickets/types</para>
        /// <para>Angular UI component(s): TicketsStore (app/features/tickets/tickets.store.ts); TicketAgents (app/features/tickets/ticket-agents/ticket-agents.ts); TicketClassificationComponent (app/features/tickets/ticket-classification/ticket-classification.ts); TicketDetailSidebar (app/features/tickets/ticket-details/ticket-detail-sidebar/ticket-detail-sidebar.ts); TicketDetailsHeader (app/features/tickets/ticket-details/ticket-details-header/ticket-details-header.ts); TicketDetails (app/features/tickets/ticket-details/ticket-details.ts); TicketHeaderComponent (app/features/tickets/ticket-header/ticket-header.ts); TicketLists (app/features/tickets/ticket-lists/ticket-lists.ts)</para>
        /// </remarks>
        [HttpDelete("delete")]

        public async Task<IActionResult> DeleteTicketHeader([FromBody]  DeleteHeaderRequestDTO dto)
        {

            _logger.LogInformation("🗑️ Request received to delete TicketHeader with Id = {Id}", dto.Id);
            var result = await _mediator.Send(new DeleteHeaderCommand(dto));
            return Ok(result);

        }


    }
}
