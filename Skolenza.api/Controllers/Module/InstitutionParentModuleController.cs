// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates Host Super Admin HTTP requests for Institution-enabled Parent/Header Modules.
// ================================================================

using skolenza.application.DTOS.Module.InstitutionParentModule;
using skolenza.application.Features.ModuleCmd.InstitutionParent.Handlers;
using skolenza.application.Features.ModuleCmd.InstitutionParent.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Module;

/// <summary>
/// Provides Host Super Admin read endpoints for a Institution's explicitly provisioned Parent and Sub-Parent Header Modules.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class InstitutionParentModuleController : ControllerBase
{
    #region Fields

    private readonly IMediator _mediator;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="InstitutionParentModuleController"/> class.
    /// </summary>
    /// <param name="mediator">Dispatches Institution Parent Module queries.</param>
    public InstitutionParentModuleController(IMediator mediator)
    {
        _mediator = mediator;
    }

    #endregion

    #region Institution Parent Module Queries
        /// <summary>
        /// Not-Used-In-Angular.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Not-Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves institution parent module headers.</para>
        /// <para>Handler flow: GetInstitutionParentModuleHeadersQuery is processed by GetInstitutionParentModuleHeadersQueryHandler; operation(s): GetHeaderTreeAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); InstitutionParentModuleResponseDTO: InstitutionId (string), Id (int), ModuleCode (string?), ModuleName (string?), DisplayName (string?), UrlPath (string?), ImageIconWeb (string?), ImageIconMobile (string?), ItemPriority (int?), ParentModuleId (int?), IsLeafNode (bool?), IsEnabled (bool), ModuleScope (short), Children (List&lt;InstitutionParentModuleResponseDTO&gt;)</para>
        /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
        /// <para>Backend endpoint: GET /api/institutionparentmodule/get-module-headers.</para>
        /// </remarks>

        [HttpGet("get-module-headers")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetModuleHeaders(
            [FromQuery] InstitutionParentModuleHeaderRequestDTO requestDTO,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetInstitutionParentModuleHeadersQuery(requestDTO),
                cancellationToken);

            return Ok(result);
        }
        /// <summary>
        /// Not-Used-In-Angular.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Not-Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves institution parent modules.</para>
        /// <para>Handler flow: GetInstitutionParentModulesQuery is processed by GetInstitutionParentModulesQueryHandler; operation(s): GetPagedMainParentHeadersAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); InstitutionParentModuleResponseDTO: InstitutionId (string), Id (int), ModuleCode (string?), ModuleName (string?), DisplayName (string?), UrlPath (string?), ImageIconWeb (string?), ImageIconMobile (string?), ItemPriority (int?), ParentModuleId (int?), IsLeafNode (bool?), IsEnabled (bool), ModuleScope (short), Children (List&lt;InstitutionParentModuleResponseDTO&gt;)</para>
        /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
        /// <para>Backend endpoint: GET /api/institutionparentmodule/list.</para>
        /// </remarks>

        [HttpGet("list")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetModules(
            [FromQuery] InstitutionParentModuleListRequestDTO requestDTO,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetInstitutionParentModulesQuery(requestDTO),
                cancellationToken);

            return Ok(result);
        }
        /// <summary>
        /// Not-Used-In-Angular.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Not-Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves institution parent module by id.</para>
        /// <para>Handler flow: GetInstitutionParentModuleByIdQuery is processed by GetInstitutionParentModuleByIdQueryHandler; operation(s): GetHeaderByModuleIdAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); InstitutionParentModuleResponseDTO: InstitutionId (string), Id (int), ModuleCode (string?), ModuleName (string?), DisplayName (string?), UrlPath (string?), ImageIconWeb (string?), ImageIconMobile (string?), ItemPriority (int?), ParentModuleId (int?), IsLeafNode (bool?), IsEnabled (bool), ModuleScope (short), Children (List&lt;InstitutionParentModuleResponseDTO&gt;)</para>
        /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
        /// <para>Backend endpoint: GET /api/institutionparentmodule/{}.</para>
        /// </remarks>

        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetModuleById(
            int id,
            [FromQuery] InstitutionParentModuleByIdRequestDTO requestDTO,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetInstitutionParentModuleByIdQuery(id, requestDTO),
                cancellationToken);

            return Ok(result);
        }

    #endregion

    #region Institution Parent Module Status
        /// <summary>
        /// Not-Used-In-Angular.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Not-Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates institution parent module status.</para>
        /// <para>Handler flow: UpdateInstitutionParentModuleStatusCommand is processed by UpdateInstitutionParentModuleStatusCommandHandler; operation(s): SaveChangesAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); InstitutionParentModuleResponseDTO: InstitutionId (string), Id (int), ModuleCode (string?), ModuleName (string?), DisplayName (string?), UrlPath (string?), ImageIconWeb (string?), ImageIconMobile (string?), ItemPriority (int?), ParentModuleId (int?), IsLeafNode (bool?), IsEnabled (bool), ModuleScope (short), Children (List&lt;InstitutionParentModuleResponseDTO&gt;)</para>
        /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
        /// <para>Backend endpoint: PATCH /api/institutionparentmodule/{}/status.</para>
        /// </remarks>

        [HttpPatch("{id:int}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateModuleStatus(
            int id,
            [FromBody] UpdateInstitutionParentModuleStatusRequestDTO requestDTO,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new UpdateInstitutionParentModuleStatusCommand(id, requestDTO),
                cancellationToken);

            return Ok(result);
        }

    #endregion
}
