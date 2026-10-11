// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Entity operations.
// ================================================================

using skolenza.application.DTOs.Entity;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Entity
{

    [ApiController]
    [Route("api/[controller]")]
    public class EntityController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILoggerService _logger;  // Logger service ka declaration

        public EntityController(IMediator mediator, ILoggerService logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: retrieves static entity names.</para>
                /// <para>Handler flow: No application request/handler class was statically resolved from the controller action.</para>
                /// <para>Response DTO property analysis: No concrete response DTO properties were statically resolved from the request/handler declaration.</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: GET /api/entity/get.</para>
                /// </remarks>
                [HttpGet("get")]
                public IActionResult GetStaticEntityNames([FromQuery] GetEntityNameRequestDTO dTO)
                {
                    var entities = new List<GetEntityNameResponseDTO>
                   {
                new GetEntityNameResponseDTO { Id = 1, Name = "StaffMember" },
                new GetEntityNameResponseDTO { Id = 2, Name = "StaffMemberBankDetail" },
                new GetEntityNameResponseDTO { Id = 3, Name = "StaffMemberExperience" },
                new GetEntityNameResponseDTO { Id = 4, Name = "StaffMemberFamily" },
                new GetEntityNameResponseDTO { Id = 5, Name = "StaffMemberPersonalDetail" },
                new GetEntityNameResponseDTO { Id = 6, Name = "StaffMemberEducation" },
                new GetEntityNameResponseDTO { Id = 6, Name = "StaffMemberDependent" },
                new GetEntityNameResponseDTO { Id = 6, Name = "StaffMemberInsurance" },
                new GetEntityNameResponseDTO { Id = 6, Name = "StaffMemberContact" }
                   };

                    return Ok(ApiResponse<List<GetEntityNameResponseDTO>>.Success(entities));
                }

    }
}
