// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Category operations.
// ================================================================


using skolenza.application.DTOs.Category;


//using skolenza.application.Features.AttendanceCmd.Command;
using skolenza.application.Features.CategoryCmd.Command;
using skolenza.application.Features.CategoryCmd.Handlers;
using skolenza.application.Interfaces.ILogger;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Category
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILoggerService _logger;  // Logger service ka declaration

        public CategoryController(IMediator mediator, ILoggerService logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: retrieves main category.</para>
                /// <para>Handler flow: GetMainCategoryCommand is processed by GetMainCategoryCommandHandler; operation(s): GetAllMainCategoriesAsync.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); CategoryResponseDTO: Name (string), Depth (int), Tags (string), IsActive (bool)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: POST /api/category/get.</para>
                /// </remarks>
                [HttpPost("get")]

                public async Task<IActionResult> GetAllMainCategories([FromBody] CategoryRequestDTO? categoryRequestDTO)
                {
                    _logger.LogInfo("Received  request to get categories from userId: {LoginId}" );
                    var command = new GetMainCategoryCommand(categoryRequestDTO);
                    var result = await _mediator.Send(command);
                    if (!result.IsSucceeded)
                    {
                        return Unauthorized(result);
                    }
                    return Ok(result);
                }



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
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: retrieves main child category.</para>
                /// <para>Handler flow: GetMainChildCategoryCommand is processed by GetMainCategoryChildRequestCommandHandler; operation(s): GetAllChildCategoryByIdAsync.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); CategoryResponseDTO: Name (string), Depth (int), Tags (string), IsActive (bool)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: POST /api/category/getallmainchildcategory.</para>
                /// </remarks>


                [HttpPost("getallmainchildcategory")]
                public async Task<IActionResult> GetAllMainChildCategories([FromBody] CategoryRequestDTO? categoryRequestDTO)
                {
                    _logger.LogInfo("Received  request to get sub-categories from userId: {LoginId}" );
                    var command = new GetMainChildCategoryCommand(categoryRequestDTO);
                    var result = await _mediator.Send(command);
                    if (!result.IsSucceeded)
                    {
                        return Unauthorized(result);
                    }
                    return Ok(result);
                }


    }
}
