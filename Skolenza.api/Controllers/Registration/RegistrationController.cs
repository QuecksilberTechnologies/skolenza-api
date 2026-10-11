// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Registration operations.
// ================================================================

using skolenza.application.DTOs.Registration;
using skolenza.application.DTOs.UserLogin;
using skolenza.application.Features.UserLoginAndDashboardCmd.Commands;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Registration
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegistrationController :  ControllerBase
    {
        private readonly IMediator _mediator;
    private readonly ILoggerService _logger;  // Logger service ka declaration

    public RegistrationController(IMediator mediator, ILoggerService logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: performs the Angular function candidate registration.</para>
                /// <para>Handler flow: CandidateRegistrationCommand is processed by CandidateRegistrationCommandHandler; operation(s): AddCandidateAsync, AddSkillsAsync.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); CandidateResponseDTO: Success (bool), CandidateId (long?), InstitutionId (long?), Message (string)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: POST /api/registration/candidate.</para>
                /// </remarks>

                [HttpPost("candidate")]

                // [Authorize]
                public async Task<IActionResult> Login([FromBody] CandidateRequestDTO candidateRegistrationDTO)
                {
                    _logger.LogInfo("Received request for register a new candidate" + candidateRegistrationDTO.ToString());
                    var command = new CandidateRegistrationCommand(candidateRegistrationDTO);
                    var result = await _mediator.Send(command);
                    return Ok(result);
                }
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: performs the Angular function staffMember type basic menu.</para>
                /// <para>Handler flow: StaffEmploymentTypeBasicMenuCommand is processed by AttendanceRequestHandler; operation(s): GetBasicMenuDTO.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); AccessDetailResponseDTO: StaffMemberId (long), ForPlatform (int?), BasicMenus (IEnumerable&lt;BasicMenuDTO&gt;?), UserRolesPermissionOnModule (IEnumerable&lt;UserRolesPermissionOnModuleDTO&gt;?)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: POST /api/registration/accessdetails.</para>
                /// </remarks>


                [HttpPost("AccessDetails")]
                // [Authorize] // Ensures the user is authenticated via token
                public async Task<IActionResult> UserAccessDetailsAsync([FromBody] AccessDetailRequestDTO accessDetailsDTO)
                {

                    // Create and send the command
                    var command = new StaffEmploymentTypeBasicMenuCommand(accessDetailsDTO);
                    var result = await _mediator.Send(command);

                    // Success response
                    //  _logger.LogInformation("AccessDetail successfully retrieved for StaffMemberId: {StaffMemberId}", accessDetailsDTO.StaffMemberId);
                    return Ok(result);
                }


}
}
