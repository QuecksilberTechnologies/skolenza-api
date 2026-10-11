// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Exposes Host-managed Subscription Plan to Module mapping endpoints.
// ================================================================

using skolenza.application.DTOs.PlanModule;
using skolenza.application.Features.PlanModuleMappingCmd.Handlers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Subscription;

/// <summary>
/// Exposes HTTP transport endpoints for Host-managed Subscription Plan Module mappings.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class PlanModuleMappingController : ControllerBase
{
    #region Fields

    private readonly IMediator _mediator;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="PlanModuleMappingController"/> class.
    /// </summary>
    /// <param name="mediator">Dispatches Subscription Plan Module mapping commands and queries.</param>
    public PlanModuleMappingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    #endregion

    #region Plan Module Mapping Endpoints
        /// <summary>
        /// Not-Used-In-Angular.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Not-Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves plan module mapping options.</para>
        /// <para>Handler flow: GetPlanModuleMappingOptionsQuery is processed by GetPlanModuleMappingOptionsQueryHandler; operation(s): GetNonDeletedSubscriptionPlanByIdAsync, GetEligibleModulesForPlanMappingAsync, GetActiveMappedModuleIdsAsync, Create.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); PlanModuleMappingOptionsResponseDTO: SubscriptionPlanId (int), Modules (IReadOnlyCollection&lt;PlanModuleOptionResponseDTO&gt;)</para>
        /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
        /// <para>Backend endpoint: GET /api/planmodulemapping/options/{}.</para>
        /// </remarks>

        [HttpGet("options/{subscriptionPlanId:int}")]
        public async Task<IActionResult> GetOptions(
            int subscriptionPlanId,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetPlanModuleMappingOptionsQuery(subscriptionPlanId),
                cancellationToken);

            return Ok(result);
        }

    /// <summary>
    /// Used-In-Angular: updates plan module mapping.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: updates plan module mapping.</para>
    /// <para>Handler flow: SavePlanModuleMappingCommand is processed by SavePlanModuleMappingCommandHandler; operation(s): GetNonDeletedSubscriptionPlanByIdAsync, GetEligibleModulesForPlanMappingAsync, Create, SaveChangesAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); SavePlanModuleMappingResponseDTO: SubscriptionPlanId (int), SelectedModuleCount (int), AddedCount (int), ReactivatedCount (int), DeactivatedCount (int)</para>
    /// <para>Angular function(s): PlanModuleMappingApi.savePlanModuleMapping (app/core/services/plan-module-mapping-api.ts:39).</para>
    /// <para>Angular purpose: updates plan module mapping.</para>
    /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
    /// <para>Angular UI component(s): SubscriptionPlanModuleMap (app/features/host/subscriptions/subscription-plan-module-map/subscription-plan-module-map.ts)</para>
    /// </remarks>
    [HttpPost("save")]
    public async Task<IActionResult> Save(
        [FromBody] SavePlanModuleMappingRequestDTO requestDTO,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SavePlanModuleMappingCommand(requestDTO),
            cancellationToken);

        return Ok(result);
    }

    #endregion
}
