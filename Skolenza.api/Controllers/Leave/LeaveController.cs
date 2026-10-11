// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Leave operations.
// ================================================================



using skolenza.application.DTOs.Leave;
using skolenza.application.Features.LeaveCmd.Commands;
using skolenza.application.Features.LeaveCmd.Queries;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

#if false // LEGACY POLICY API: disabled in favor of InstitutionPolicyController.
namespace skolenza.api.Controllers.Leave
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaveController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<LeaveController> _logger;  // 🔹 Microsoft ILogger उपयोग करें

        public LeaveController(IMediator mediator, ILogger<LeaveController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: creates leave type.</para>
                /// <para>Handler flow: CreateLeaveTypeCommand is processed by CreateLeaveTypeCommandHandler; operation(s): CreateLeaveTypeAsync.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetLeaveTypResponseDTO: Id (int), LeaveName (string), Description (string?), IsActive (bool), AddedById (long?), UpdatedById (long?)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: POST /api/leave/add.</para>
                /// </remarks>

                [HttpPost("add")]
                public async Task<IActionResult> CreateLeaveType([FromBody] CreateLeaveTypeRequestDTO createLeaveTypeDTO)
                {
                    if (createLeaveTypeDTO == null)
                    {
                        _logger.LogWarning("Received null request for creating leave type.");  // ✅ अब सही है
                        return BadRequest(new { success = false, message = "Invalid request" });
                    }

                    _logger.LogInformation($"Received request to create a new leave type: {createLeaveTypeDTO.LeaveName}");

                    var command = new CreateLeaveTypeCommand(createLeaveTypeDTO);
                    var result = await _mediator.Send(command);

                    return Ok(result);
                }
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: retrieves all leave type.</para>
                /// <para>Handler flow: GetAllLeaveTypeQuery is processed by GetAllLeaveTypeQueryHandler; operation(s): GetAllLeaveAsync.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetLeaveTypResponseDTO: Id (int), LeaveName (string), Description (string?), IsActive (bool), AddedById (long?), UpdatedById (long?)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: GET /api/leave/get.</para>
                /// </remarks>
                [HttpGet("get")]
                public async Task<IActionResult> GetAllLeaves([FromQuery] GetLeaveTypeRequestDTO leaveRequestDTO)
                {
                    var query = new GetAllLeaveTypeQuery(leaveRequestDTO);
                    var result = await _mediator.Send(query);
                    return Ok(result);
                }
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: updates leave type.</para>
                /// <para>Handler flow: UpdateLeaveTypeCommand is processed by UpdateTicketTypeCommandHandler; operation(s): UpdateLeavTypeAsync.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: POST /api/leave/update.</para>
                /// </remarks>
                [HttpPost("update")]



                // [Authorize]
                public async Task<IActionResult> UpdateLeave([FromBody] UpdateLeaveTypeRequestDTO updateLeaveTypeDTO)
                {
                    _logger.LogInformation("Received request for update a leave" + updateLeaveTypeDTO.ToString());
                    var command = new UpdateLeaveTypeCommand(updateLeaveTypeDTO);
                    var result = await _mediator.Send(command);
                    return Ok(result);
                }
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: deletes leave type.</para>
                /// <para>Handler flow: DeleteLeaveTypeCommand is processed by DeleteLeaveTypeCommandHandler; operation(s): GetLeaveByIdAsync, DeleteLeaveAsync.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: GET /api/leave/delete.</para>
                /// </remarks>
                [HttpGet("delete")]
                // [Authorize]
                public async Task<IActionResult> DeleteLeave([FromQuery] DeleteLeaveRequestDTO request)
                {

                    _logger.LogInformation("Received request to delete LeaveType Id: {Id} by UserId: {UserId}", request.Id, request.StaffMemberId);
                    var command = new DeleteLeaveTypeCommand(request);
                    var result = await _mediator.Send(command);
                    _logger.LogInformation("Successfully deleted LeaveType Id: {Id}", request.Id);
                    return Ok(result);
                }



    }

}
#endif

// Compatibility namespace for existing using directives; no legacy routes are exposed.
namespace skolenza.api.Controllers.Leave { }


