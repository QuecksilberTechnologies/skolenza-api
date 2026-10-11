// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Client operations.
// ================================================================


using skolenza.application.DTOs.Client;
using skolenza.application.DTOS.Common;
using skolenza.application.Features.ClientCmd.Commands;
using skolenza.application.Features.ClientCmd.Handlers;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Client
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientController : ControllerBase
        {
            private readonly IMediator _mediator;
            private readonly ILoggerService _logger;  // Logger service ka declaration

            public ClientController(IMediator mediator, ILoggerService logger)
            {
                _mediator = mediator;
                _logger = logger;
            }
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: retrieves all client type.</para>
                /// <para>Handler flow: No application request/handler class was statically resolved from the controller action.</para>
                /// <para>Response DTO property analysis: No concrete response DTO properties were statically resolved from the request/handler declaration.</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: GET /api/client/get.</para>
                /// </remarks>

                [HttpGet("get")]
                public async Task<IActionResult> GetAllClientType([FromQuery] GetOptionRequestDTO clientRequestType)
                {
                    _logger.LogInfo($"📩 Received request to get client list for userId: {clientRequestType.UserStaffMemberId}");

                    // 🧩 Dummy Data (Temporary Static List)
                    var clientList = new List<GetClientOptionResponseDTO>
            {
                new GetClientOptionResponseDTO { Id = "1", ClientName = "TechNova Solutions Pvt. Ltd." },
                new GetClientOptionResponseDTO { Id = "2", ClientName = "InfyCore Technologies LLP" },
                new GetClientOptionResponseDTO { Id = "3", ClientName = "BluePeak Consulting Services" },
                new GetClientOptionResponseDTO { Id = "4", ClientName = "SkyBridge Digital Systems" },
                new GetClientOptionResponseDTO { Id = "5", ClientName = "NextEra IT Innovations" },
                new GetClientOptionResponseDTO { Id = "6", ClientName = "VirtuWorks Global Pvt. Ltd." },
                new GetClientOptionResponseDTO { Id = "7", ClientName = "DataMinds Analytics" },
                new GetClientOptionResponseDTO { Id = "8", ClientName = "ProEdge Business Solutions" },
                new GetClientOptionResponseDTO { Id = "9", ClientName = "CloudNest Technologies" },
                new GetClientOptionResponseDTO { Id = "10", ClientName = "Skolenza Workforce Systems" }
            };

                    // 🧾 Wrap Response (Optional Standard Format)
                    var response = new
                    {
                        IsSucceeded = true,
                        Message = "Client list fetched successfully.",
                        Data = clientList
                    };

                    _logger.LogInfo($"✅ Returning {clientList.Count} clients successfully.");
                    return Ok(response);
                }

        //[HttpGet("get")]
        //public async Task<IActionResult> GetAllClientType([FromQuery] ClientRequestTypeDTO clientRequestType)
        //{
        //    _logger.LogInfo($"Received request to get clientRequestType from userId: {clientRequestType.Id}");

        //    var command = new GetClientTypeQuery(clientRequestType);
        //    var result = await _mediator.Send(command);

        //    if (!result.IsSucceeded)
        //    {
        //        return Unauthorized(result);
        //    }
        //    return Ok(result);
        //}
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: creates client type.</para>
                /// <para>Handler flow: CreateClientTypeCommand is processed by CreateClientTypeCommandHandler; operation(s): CreateClientTypeAsync.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetClientTypeDTO: Id (string), TypeName (string), IsActive (bool), Remark (string?), Description (string?)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: POST /api/client/add.</para>
                /// </remarks>
                [HttpPost("add")]
                public async Task<IActionResult> CreateClientType([FromBody] CreateClientTypeDTO createClientTypeDTO)
                {
                    if (createClientTypeDTO == null)
                    {
                        _logger.LogInfo("Received null request for creating leave type.");  // ✅ अब सही है
                        return BadRequest(new { success = false, message = "Invalid request" });
                    }

                    _logger.LogInfo($"Received request to create a new leave type: {createClientTypeDTO.TypeName}");

                    var command = new  CreateClientTypeCommand(createClientTypeDTO);
                    var result = await _mediator.Send(command);


                    return Ok(result);
                }
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: updates client type.</para>
                /// <para>Handler flow: UpdateClientTypeCommand is processed by UpdateClientTypeCommandHandler; operation(s): UpdateClientTypeAsync.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetClientTypeDTO: Id (string), TypeName (string), IsActive (bool), Remark (string?), Description (string?)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: POST /api/client/update.</para>
                /// </remarks>

                [HttpPost("update")]
                public async Task<IActionResult> UpdateClientType([FromBody] UpdateClientTypeDTO updateClientTypeDTO)
                {
                    _logger.LogInfo("Received request for update a leave" + updateClientTypeDTO.ToString());
                    var command = new UpdateClientTypeCommand(updateClientTypeDTO);
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
