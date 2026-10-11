// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Option operations.
// ================================================================

using skolenza.application.DTOs.Operation;

using skolenza.application.Features.OperationCmd.Commands;
using skolenza.application.Features.OperationCmd.Queries;

using skolenza.application.Features.TransportCmd.Commands;
using skolenza.application.Features.TransportCmd.Queries;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Operation
{
    /// <summary>
    /// handled-operation-related-actions.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OptionController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILoggerService _logger;  // Logger service ka declaration

        public OptionController(IMediator mediator, ILoggerService logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Used-In-Angular: retrieves options.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves all operation.</para>
        /// <para>Handler flow: GetAllOperationCommand is processed by GetAllOperationQueryHandler; operation(s): GetAllOperationAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetOperationResponseDTO: Id (int?), OperationName (string?), Remark (string?), IsActive (bool), AddedById (long?), AddedDateTime (DateTime), UpdatedById (long?), UpdatedDateTime (DateTime?)</para>
        /// <para>Angular function(s): OptionApi.getOptions (app/core/services/option-api.ts:31).</para>
        /// <para>Angular purpose: retrieves options.</para>
        /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
        /// <para>Angular UI component(s): No consuming Angular component was statically resolved.</para>
        /// </remarks>
        [HttpGet("get")]
        public async Task<IActionResult> GetAllOperationAsyc([FromQuery] GetOperationRequestDTO operationRequestDTO)
        {
            _logger.LogInfo($"Received request to get operationRequestDTO from userId: {operationRequestDTO.StaffMemberId}");

            var command = new GetAllOperationCommand(operationRequestDTO);
            var result = await _mediator.Send(command);

            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: creates option.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: creates operation.</para>
        /// <para>Handler flow: CreateOperationCommand is processed by CreateOperationCommandHandler; operation(s): CreateOperationAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetOperationResponseDTO: Id (int?), OperationName (string?), Remark (string?), IsActive (bool), AddedById (long?), AddedDateTime (DateTime), UpdatedById (long?), UpdatedDateTime (DateTime?)</para>
        /// <para>Angular function(s): OptionApi.addOption (app/core/services/option-api.ts:37).</para>
        /// <para>Angular purpose: creates option.</para>
        /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
        /// <para>Angular UI component(s): No consuming Angular component was statically resolved.</para>
        /// </remarks>
        [HttpPost("create")]

        public async Task<IActionResult> CreateOperation([FromBody] CreateOperationRequestDTO createOperationDTO)
        {

            _logger.LogInfo($"Received request to create a new operationRequestDTO: {createOperationDTO.OperationName}");

            var command = new CreateOperationCommand(createOperationDTO);
            var result = await _mediator.Send(command);


            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: updates option.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates operation.</para>
        /// <para>Handler flow: UpdateOperationCommand is processed by UpdateOperationCommandHandler; operation(s): GetOperationByIdAsync, UpdateOperationAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetOperationResponseDTO: Id (int?), OperationName (string?), Remark (string?), IsActive (bool), AddedById (long?), AddedDateTime (DateTime), UpdatedById (long?), UpdatedDateTime (DateTime?)</para>
        /// <para>Angular function(s): OptionApi.updateOption (app/core/services/option-api.ts:43).</para>
        /// <para>Angular purpose: updates option.</para>
        /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
        /// <para>Angular UI component(s): No consuming Angular component was statically resolved.</para>
        /// </remarks>
        [HttpPost("update")]

        public async Task<IActionResult> UpdateOperation([FromBody] UpdateOperationRequestDTO updateOperationDTO)
        {
            _logger.LogInfo("Received request for update a leave" + updateOperationDTO.ToString());
            var command = new UpdateOperationCommand(updateOperationDTO);
            var result = await _mediator.Send(command);
            return Ok(result);
        }
        /// <summary>
        /// Used-In-Angular: updates option.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves page operation permission.</para>
        /// <para>Handler flow: GetPageOperationPermissionQuery is processed by GetPageOperationPermissionQueryHandler; operation(s): GetHasAccessOperation.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetHasAccessOperationDTO: Success (bool), Message (string?), Status (bool?)</para>
        /// <para>Angular function(s): OptionApi.updateOption (app/core/services/option-api.ts:50).</para>
        /// <para>Angular purpose: updates option.</para>
        /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
        /// <para>Angular UI component(s): No consuming Angular component was statically resolved.</para>
        /// </remarks>
         [Authorize]
        [HttpGet("has-access")]
        public async Task<IActionResult> HasPageOperationAccess([FromQuery] GetCheckOperationPermissionRequestDTO? checkOperationPermissionRequest)
        {


            var query = new GetPageOperationPermissionQuery(checkOperationPermissionRequest);
            var result = await _mediator.Send(query);
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
