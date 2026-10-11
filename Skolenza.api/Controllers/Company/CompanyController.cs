// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates the Company controller's current diagnostic route.
// ================================================================

using skolenza.application.DTOs.Registration;
using skolenza.application.DTOs.Institution;
using skolenza.application.DTOs.Verify;

using skolenza.application.Features.UserLoginAndDashboardCmd.Commands;

using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;

namespace skolenza.api.Controllers.Company;

[Route("api/[controller]")]
[ApiController]
public class CompanyController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILoggerService _logger;  // Logger service ka declaration

    public CompanyController(IMediator mediator, ILoggerService logger)
    {
        _mediator = mediator;
        _logger = logger;  // Logger service ko inject karna
    }
        /// <summary>
        /// Not-Used-In-Angular.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Not-Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves data.</para>
        /// <para>Handler flow: No application request/handler class was statically resolved from the controller action.</para>
        /// <para>Response DTO property analysis: No concrete response DTO properties were statically resolved from the request/handler declaration.</para>
        /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
        /// <para>Backend endpoint: GET /api/company/{}/{}.</para>
        /// </remarks>


        [HttpGet("{firstname}/{lastname}")]
       // [Authorize]
        public async Task<IActionResult> Get(string firstname, string lastname)
        {
            _logger.LogInfo("Company is created");
            return Ok();
        }

}
