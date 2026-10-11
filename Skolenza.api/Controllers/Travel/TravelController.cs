// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Travel operations.
// ================================================================

using skolenza.application.DTOs.Transport;
using skolenza.application.Features.ClientCmd.Commands;
using skolenza.application.Features.ClientCmd.Queries;
using skolenza.application.Features.TransportCmd.Commands;
using skolenza.application.Features.TransportCmd.Queries;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Travel
{
    [ApiController]
    [Route("api/[controller]")]
    public class TravelController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILoggerService _logger;  // Logger service ka declaration

        public TravelController(IMediator mediator, ILoggerService logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: retrieves all travel mode type.</para>
                /// <para>Handler flow: GetAllTravelModeTypeQuery is processed by GetAllTravelModeTypeQueryHandler; operation(s): GetAllTravelModeTypeAsync.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetAllTravelModeDTO: Id (int?), TravelModeName (string), Description (string?), IsActive (bool), AddedById (long?), AddedDateTime (DateTime), UpdatedById (long?), UpdatedDateTime (DateTime)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: GET /api/travel/getalltravelmodetype.</para>
                /// </remarks>


                [HttpGet("getalltravelmodetype")]
                public async Task<IActionResult> GetAllTravelModeType([FromQuery] TravelModeRequestDTO travelModeRequestDTO)
                {
                    _logger.LogInfo($"Received request to get clientRequestType from userId: {travelModeRequestDTO.Id}");

                    var command = new GetAllTravelModeTypeQuery(travelModeRequestDTO);
                    var result = await _mediator.Send(command);
                    return Ok(result);
                }
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: creates travel mode type.</para>
                /// <para>Handler flow: CreateTravelModeTypeCommand is processed by CreateTravelModeTypeCommandHandler; operation(s): CreateTravelTypeAsync.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetAllTravelModeDTO: Id (int?), TravelModeName (string), Description (string?), IsActive (bool), AddedById (long?), AddedDateTime (DateTime), UpdatedById (long?), UpdatedDateTime (DateTime)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: POST /api/travel/addtravelmode.</para>
                /// </remarks>
                [HttpPost("addtravelmode")]
                public async Task<IActionResult> CreateTravelModeType([FromBody] CreateTravelModeDTO createTravelModeDTO)
                {

                    _logger.LogInfo($"Received request to create a new leave type: {createTravelModeDTO.TravelModeName}");
                    var command = new CreateTravelModeTypeCommand(createTravelModeDTO);
                    var result = await _mediator.Send(command);
                    return Ok(result);        }
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: updates travel mode type.</para>
                /// <para>Handler flow: UpdateTravelModeTypeCommand is processed by UpdateTravelModeTypeCommandHandler; operation(s): UpdateClientTypeAsync.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetAllTravelModeDTO: Id (int?), TravelModeName (string), Description (string?), IsActive (bool), AddedById (long?), AddedDateTime (DateTime), UpdatedById (long?), UpdatedDateTime (DateTime)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: POST /api/travel/updatetravelmodetype.</para>
                /// </remarks>

                [HttpPost("updatetravelmodetype")]
                public async Task<IActionResult> UpdateTravelModeType([FromBody] UpdateTravelModeDTO updateTravelModeDTO)
                {
                    _logger.LogInfo("Received request for update a leave" + updateTravelModeDTO.ToString());
                    var command = new UpdateTravelModeTypeCommand(updateTravelModeDTO);
                    var result = await _mediator.Send(command);
                    return Ok(result);
                }
        //  [HttpPost("getalltendermaincategory")]
        //public async Task<IActionResult> GetAllTenderMainCategories([FromBody] TenderCategoryRequestDTO? tenderCategoryRequestDTO)
        //{
        //    _logger.LogInfo("Received  request to get categories from userId: {LoginId}" + tenderCategoryRequestDTO.Id.ToString());
        //    var command = new GetTenderMainCategoryRequestCommand(tenderCategoryRequestDTO);
        //    var result = await _mediator.Send(command);
        //    if (!result.IsSuccecced)
        //    {
        //        return Unauthorized(result);
        //    }
        //    return Ok(result);
        //}


        //[HttpPost("getallmainchildcategory")]
        //public async Task<IActionResult> GetAllMainChildCategories([FromBody] CategoryRequestDTO? categoryRequestDTO)
        //{
        //    _logger.LogInfo("Received  request to get sub-categories from userId: {LoginId}" + categoryRequestDTO.Id.ToString());
        //    var command = new GetMainChildCategoryCommand(categoryRequestDTO);
        //    var result = await _mediator.Send(command);
        //    if (!result.IsSuccecced)
        //    {
        //        return Unauthorized(result);
        //    }
        //    return Ok(result);
        //}


    }

}
