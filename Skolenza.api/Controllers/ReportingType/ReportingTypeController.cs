// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Reporting Type operations.
// ================================================================

using skolenza.application.DTOs.Manager.ReportingType;
using skolenza.application.Features.ReportTypeCmd.Handlers;

using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace skolenza.api.Controllers.ReportingType
{
    /// <summary>
    /// Controller responsible for managing reporting type operations.
    /// Handles all Create, Read, Update, and Delete (CRUD) APIs for reporting types.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]

    public class ReportingTypeController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<ReportingTypeController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReportingTypeController"/> class.
        /// </summary>
        /// <param name="mediator">Mediator instance for sending commands/queries.</param>
        /// <param name="logger">Logger instance for tracking actions.</param>

        public ReportingTypeController(
            IMediator mediator,
            ILogger<ReportingTypeController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
        // =============================================================================================
        #region 🔹 CREATE reporting type
        // =============================================================================================

        /// <summary>
        /// Used-In-Angular: creates report type.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: creates reporting type.</para>
        /// <para>Handler flow: CreateReportingTypeCommand is processed by CreateReportingTypeCommandHandler; operation(s): AddAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetReportingTypeResponseDTO: Id (int), TypeName (string?), Description (string?), IsActive (bool)</para>
        /// <para>Angular function(s): ReportTypeApi.addReportType (app/core/services/report-type-api.ts:32).</para>
        /// <para>Angular purpose: creates report type.</para>
        /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
        /// <para>Angular UI component(s): ReportTypeManageDialog (app/features/report-type/report-type-manage-dialog/report-type-manage-dialog.ts)</para>
        /// </remarks>

        [HttpPost("create")]

        public async Task<IActionResult> CreateReportingType([FromBody] CreateReportingTypeRequestDTO dto)
         {

                _logger.LogInformation("🎯 Received request to create ReportingType: {Data}", JsonConvert.SerializeObject(dto));

                var result = await _mediator.Send(new CreateReportingTypeCommand(dto));

                return Ok(result);

        }

        #endregion

        // =============================================================================================
        #region 🔹 GET ALL reporting typeS
        // =============================================================================================

        /// <summary>
        /// Used-In-Angular: retrieves report types.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves all reporting type.</para>
        /// <para>Handler flow: GetAllReportingTypeQuery is processed by GetAllReportingTypeQueryHandler.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetReportingTypeResponseDTO: Id (int), TypeName (string?), Description (string?), IsActive (bool)</para>
        /// <para>Angular function(s): ReportTypeApi.getReportTypes (app/core/services/report-type-api.ts:26).</para>
        /// <para>Angular purpose: retrieves report types.</para>
        /// <para>Integrated UI page(s): /app/report-types</para>
        /// <para>Angular UI component(s): ReportTypeStore (app/features/report-type/report-type.store.ts); ReportTypeComponent (app/features/report-type/report-type.ts)</para>
        /// </remarks>
        [HttpGet("get-all")]
        public async Task<IActionResult> GetAllReportingTypes([FromQuery] GetReportingTypeRequestDTO dto)
        {

                _logger.LogInformation("📦 Fetching all reporting types...");
                var result = await _mediator.Send(new GetAllReportingTypeQuery(dto));
                return Ok(result);

        }

        #endregion

        // =============================================================================================
        #region 🔹 GET reporting type BY ID
        // =============================================================================================
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: retrieves reporting type by id.</para>
                /// <para>Handler flow: GetReportingTypeByIdQuery is processed by GetReportingTypeByIdQueryHandler; operation(s): GetByIdAsync.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetReportingTypeResponseDTO: Id (int), TypeName (string?), Description (string?), IsActive (bool)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: GET /api/reportingtype/get-by-id.</para>
                /// </remarks>

                [HttpGet("get-by-id")]
                public async Task<IActionResult> GetReportingTypeById([FromQuery] GetReportingTypeByIdRequestDTO dto)
                {

                        var result = await _mediator.Send(new GetReportingTypeByIdQuery(dto));

                        return Ok(result);

                }

        #endregion

        // =============================================================================================
        #region 🔹 UPDATE reporting type
        // =============================================================================================

        /// <summary>
        /// Used-In-Angular: updates report type.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates reporting type.</para>
        /// <para>Handler flow: UpdateReportingTypeCommand is processed by UpdateReportingTypeCommandHandler; operation(s): UpdateAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): ReportTypeApi.updateReportType (app/core/services/report-type-api.ts:38).</para>
        /// <para>Angular purpose: updates report type.</para>
        /// <para>Integrated UI page(s): /app/report-types</para>
        /// <para>Angular UI component(s): ReportTypeManageDialog (app/features/report-type/report-type-manage-dialog/report-type-manage-dialog.ts); ReportTypeStore (app/features/report-type/report-type.store.ts); ReportTypeComponent (app/features/report-type/report-type.ts)</para>
        /// </remarks>
        [HttpPut("update")]
        public async Task<IActionResult> UpdateReportingType([FromBody] UpdateReportingTypeRequestDTO dto)
        {

                _logger.LogInformation("🛠️ Updating ReportingType: {Data}", JsonConvert.SerializeObject(dto));

                var result = await _mediator.Send(new UpdateReportingTypeCommand(dto));
              return Ok(result);

        }

        #endregion

        // =============================================================================================
        #region 🔹 DELETE reporting type
        // =============================================================================================

        /// <summary>
        /// Used-In-Angular: deletes report type.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: deletes reporting type.</para>
        /// <para>Handler flow: DeleteReportingTypeCommand is processed by DeleteReportingTypeCommandHandler; operation(s): DeleteAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): ReportTypeApi.deleteReportType (app/core/services/report-type-api.ts:45).</para>
        /// <para>Angular purpose: deletes report type.</para>
        /// <para>Integrated UI page(s): /app/report-types</para>
        /// <para>Angular UI component(s): ReportTypeStore (app/features/report-type/report-type.store.ts); ReportTypeComponent (app/features/report-type/report-type.ts)</para>
        /// </remarks>
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteReportingType([FromQuery] DeleteReportingTypeRequestDTO dto)
        {

                var result = await _mediator.Send(new DeleteReportingTypeCommand(dto));
                return Ok(result);


        }

        #endregion
    }


}

