// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Workflow Stage operations.
// ================================================================


using skolenza.application.DTOs.WorkflowStage;
using skolenza.application.Features.WorkflowStage.Commands;
using skolenza.application.Features.WorkflowStage.Queries;

using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace skolenza.api.Controllers.WorkflowStage
{
    /// <summary>
    /// Controller responsible for managing Workflow Stage operations.
    /// Handles all Create, Read, Update, and Delete (CRUD) APIs for Workflow Stages.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class WorkflowStageController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<WorkflowStageController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowStageController"/> class.
        /// </summary>
        /// <param name="mediator">Mediator instance for sending commands/queries.</param>
        /// <param name="logger">Logger instance for tracking actions.</param>
        public WorkflowStageController(IMediator mediator, ILogger<WorkflowStageController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // =============================================================================================
        #region 🔹 CREATE WORKFLOW STAGE
        // =============================================================================================
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: creates workflow stage.</para>
                /// <para>Handler flow: CreateWorkflowStageCommand is processed by CreateWorkflowStageCommandHandler; operation(s): AddAsync.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetWorkflowStageResponseDTO: Id (int), StageName (string?), ColorKey (string?), StageOrder (int?), Description (string?), IsActive (bool?), IsSoftDeleted (bool?), AddedById (long?), AddedDateTime (DateTime?), UpdatedById (long?), UpdatedDateTime (DateTime?), SoftDeletedById (long?), DeletedDateTime (DateTime?)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: POST /api/workflowstage/create.</para>
                /// </remarks>

                [HttpPost("create")]
                public async Task<IActionResult> CreateWorkflowStage([FromBody] CreateWorkflowStageRequestDTO dto)
                {

                        _logger.LogInformation("🎯 Received request to create WorkflowStage: {Data}", JsonConvert.SerializeObject(dto));
                        var result = await _mediator.Send(new CreateWorkflowStageCommand(dto));
                        return Ok(result);

                }

        #endregion

        // =============================================================================================
        #region 🔹 GET ALL WORKFLOW STAGES
        // =============================================================================================
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: retrieves all workflow stage.</para>
                /// <para>Handler flow: GetAllWorkflowStageQuery is processed by GetAllWorkflowStageQueryHandler.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetWorkflowStageResponseDTO: Id (int), StageName (string?), ColorKey (string?), StageOrder (int?), Description (string?), IsActive (bool?), IsSoftDeleted (bool?), AddedById (long?), AddedDateTime (DateTime?), UpdatedById (long?), UpdatedDateTime (DateTime?), SoftDeletedById (long?), DeletedDateTime (DateTime?)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: GET /api/workflowstage/get-all.</para>
                /// </remarks>

                [HttpGet("get-all")]
                public async Task<IActionResult> GetAllWorkflowStages([FromQuery] GetWorkflowStageRequestDTO dto)
                {

                        _logger.LogInformation("📦 Fetching all Workflow Stages...");

                        var result = await _mediator.Send(new GetAllWorkflowStageQuery(dto));

                        return Ok(result);

                }

        #endregion

        // =============================================================================================
        #region 🔹 GET WORKFLOW STAGE BY ID
        // =============================================================================================
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: retrieves workflow stage by id.</para>
                /// <para>Handler flow: GetWorkflowStageByIdQuery is processed by GetWorkflowStageByIdQueryHandler; operation(s): GetByIdAsync.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetWorkflowStageResponseDTO: Id (int), StageName (string?), ColorKey (string?), StageOrder (int?), Description (string?), IsActive (bool?), IsSoftDeleted (bool?), AddedById (long?), AddedDateTime (DateTime?), UpdatedById (long?), UpdatedDateTime (DateTime?), SoftDeletedById (long?), DeletedDateTime (DateTime?)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: GET /api/workflowstage/get.</para>
                /// </remarks>

                [HttpGet("get")]
                public async Task<IActionResult> GetWorkflowStageById([FromQuery] GetWorkflowStageByIdRequestDTO dto)
                {

                        _logger.LogInformation("🔍 Fetching WorkflowStage details for Id = {Id}", dto.Id);
                        var result = await _mediator.Send(new GetWorkflowStageByIdQuery(dto));

                        return Ok(result);

                }

        #endregion

        // =============================================================================================
        #region 🔹 UPDATE WORKFLOW STAGE
        // =============================================================================================
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: updates workflow stage.</para>
                /// <para>Handler flow: UpdateWorkflowStageCommand is processed by UpdateWorkflowStageCommandHandler; operation(s): UpdateAsync.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: PUT /api/workflowstage/update.</para>
                /// </remarks>

                [HttpPut("update")]
                public async Task<IActionResult> UpdateWorkflowStage([FromBody] UpdateWorkflowStageRequestDTO dto)        {

                        _logger.LogInformation("🛠️ Updating WorkflowStage: {Data}", JsonConvert.SerializeObject(dto));
                        var result = await _mediator.Send(new UpdateWorkflowStageCommand(dto));
                        return Ok(result);

                }

        #endregion

        // =============================================================================================
        #region 🔹 DELETE WORKFLOW STAGE
        // =============================================================================================
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: deletes workflow stage.</para>
                /// <para>Handler flow: DeleteWorkflowStageCommand is processed by DeleteWorkflowStageCommandHandler; operation(s): GetByIdAsync, DeleteAsync.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: DELETE /api/workflowstage/delete.</para>
                /// </remarks>

                [HttpDelete("delete")]
                public async Task<IActionResult> DeleteWorkflowStage([FromBody] DeleteWorkflowStageRequestDTO dto)
                {

                        _logger.LogInformation("🗑️ Request received to delete WorkflowStage with Id = {Id}", dto.Id);
                        var result = await _mediator.Send(new DeleteWorkflowStageCommand(dto));
                        return Ok(result);


                }

        #endregion
    }
}
