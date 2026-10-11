// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates staffMember-manager mapping HTTP requests.
// ================================================================

using skolenza.application.DTOS.StaffManagerMappings;
//using skolenza.application.Features.StaffMemberManagerMapping.Command;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.ManagerMapping
{
    [Route("api/StaffReportingMapping")]
    [ApiController]
    public class StaffReportingMappingController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<StaffReportingMappingController> _logger;

        public StaffReportingMappingController(IMediator mediator, ILogger<StaffReportingMappingController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        //[HttpPost("get")]
        //public async Task<IActionResult> CreateLeaveType([FromBody] CreateLeaveTypeDTO createLeaveTypeDTO)
        //{
        //    if (createLeaveTypeDTO == null)
        //    {
        //        _logger.LogWarning("Received null request for creating leave type.");  // ✅ अब सही है
        //        return BadRequest(new { success = false, message = "Invalid request" });
        //    }

        //    _logger.LogInformation($"Received request to create a new leave type: {createLeaveTypeDTO.LeaveName}");

        //    var command = new CreateLeaveTypeCommand(createLeaveTypeDTO);
        //    var result = await _mediator.Send(command);

        //    if (!result.IsSucceeded)
        //    {
        //        return BadRequest(result);
        //    }

        //    return Ok(result);
        //}
        /// <summary>
        /// Returns list of all StaffMember Types.
        /// </summary>


        //[HttpPost("add")]
        //// [Authorize]
        //public async Task<IActionResult> CreateRole([FromBody] AddStaffMemberManagerMappingDTO DTO)
        //{
        //    _logger.LogInformation("Received request to mapping manager: " + DTO.ToString());
        //    var command = new AddStaffMemberManagerMappingCommand(DTO);
        //    var result = await _mediator.Send(command);
        //    return Ok(result);
        //}



    }

}
