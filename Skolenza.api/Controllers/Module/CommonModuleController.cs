// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Common Module operations.
// ================================================================

using skolenza.application.DTOs.Module;

using skolenza.application.DTOS.Module.CommonModule;
using skolenza.application.DTOS.Module.ParentModule;
using skolenza.application.Features.ModuleCmd.Common.Commands;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Module
{
    /// <summary>
    /// Handles all module-related operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CommonModuleController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILoggerService _logger;

        public CommonModuleController(IMediator mediator, ILoggerService logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        #region Create Module
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: creates common module.</para>
                /// <para>Handler flow: CreateCommonModuleCommand is processed by CreateCommonModuleCommandHandler; operation(s): AddCommonModuleAsync.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetCommonModuleResponseDTO: Id (int), ModuleName (string?), DisplayName (string?), IsModuleDisplayInUI (bool), IsActive (bool), ImageIconWeb (string?), ImageIconMobile (string?), ItemPriority (int?), Remark (string?), AddedById (long?), AddedDateTime (DateTime?), UpdatedById (long?), UpdatedDateTime (DateTime?)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: POST /api/commonmodule/add.</para>
                /// </remarks>

                [HttpPost("add")]
                public async Task<IActionResult> AddModule([FromBody] CreateCommonModuleRequestDTO? requestDto)
                {


                    var command = new CreateCommonModuleCommand(requestDto);
                    var result = await _mediator.Send(command);


                    return Ok(result);
                }


        #endregion

    }
}
