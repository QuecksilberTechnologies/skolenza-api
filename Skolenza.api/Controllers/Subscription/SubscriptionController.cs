// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Exposes HTTP endpoints for subscription plan management and delegates application logic through MediatR.
// ================================================================

using skolenza.application.DTOs.SubscriptionModule;
using skolenza.application.DTOs.Institution;
using skolenza.application.DTOS.SubscriptionModule;
using skolenza.application.Features.SubscriptionCmd.Handlers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Subscription;

/// <summary>
/// Exposes HTTP transport endpoints for subscription plan queries and commands.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SubscriptionController : ControllerBase
{
    #region Fields

    private readonly IMediator _mediator;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SubscriptionController"/> class.
    /// </summary>
    /// <param name="mediator">Dispatches subscription commands and queries.</param>
    public SubscriptionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    #endregion

    #region Subscription Plan Queries
    /// <summary>
    /// Not-Used-In-Angular.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Not-Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves subscription plan.</para>
    /// <para>Handler flow: GetSubscriptionPlanQuery is processed by GetSubscriptionPlanQueryHandler; operation(s): GetAllPlansAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); SubscriptionActivePlanDTO: Id (long), PlanName (string?), IsActive (bool), MaxUsers (int?), IsMostPopular (bool?), IsCustom (bool?), IsFree (bool?), CurrencyKey (string?), PerDayPrice (decimal?), MonthlyPrice (decimal?), YearlyPrice (decimal?), Modules (List&lt;ModuleActiveDTO&gt;)</para>
    /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
    /// <para>Backend endpoint: GET /api/subscription/get-all-subscription-plan.</para>
    /// </remarks>
    [AllowAnonymous]
    [HttpGet("get-all-subscription-plan")]
    public async Task<IActionResult> GetAllSubscriptionPlan(
        [FromQuery] SubscriptionPlanRequestDTO requestDTO,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetSubscriptionPlanQuery(requestDTO),
            cancellationToken);

        return Ok(result);
    }
    /// <summary>
    /// Not-Used-In-Angular.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Not-Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves host subscription plans.</para>
    /// <para>Handler flow: GetHostSubscriptionPlansQuery is processed by GetHostSubscriptionPlansQueryHandler; operation(s): GetHostPlansAsync.</para>
    /// <para>Response DTO property analysis: PagedApiResponse: IsSucceeded (bool), Message (string), Data (List&lt;T&gt;), TotalCount (int), PageNumber (int), PageSize (int), TotalPages (int), HasPrevious (bool), HasNext (bool), HasUploadedAll (bool?), IsPrimaryMarked (bool?), CompletionPercentage (double?); SubscriptionActivePlanDTO: Id (long), PlanName (string?), IsActive (bool), MaxUsers (int?), IsMostPopular (bool?), IsCustom (bool?), IsFree (bool?), CurrencyKey (string?), PerDayPrice (decimal?), MonthlyPrice (decimal?), YearlyPrice (decimal?), Modules (List&lt;ModuleActiveDTO&gt;)</para>
    /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
    /// <para>Backend endpoint: POST /api/subscription/get-all-host-subscription-plans.</para>
    /// </remarks>
    [Authorize]
    [HttpPost("get-all-host-subscription-plans")]
    public async Task<IActionResult> GetAllHostSubscriptionPlans(
        [FromBody] HostSubscriptionPlanListRequestDTO? requestDTO,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetHostSubscriptionPlansQuery(requestDTO),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Not-Used-In-Angular.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Not-Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves institution subscription plan.</para>
    /// <para>Handler flow: GetInstitutionSubscriptionPlanQuery is processed by GetInstitutionSubscriptionPlanQueryHandler; operation(s): GetValidateInstitutionPlan.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); InstitutionSubscriptionPlanResponseDTO: Id (long), InstitutionId (long?), SubscriptionPlanId (int), SubscriptionStartDate (DateTime), SubscriptionEndDate (DateTime?), IsActive (bool), PaymentTxnId (string?), PaymentMode (string?), IsTrial (bool)</para>
    /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
    /// <para>Backend endpoint: GET /api/subscription/get-institution-subscription-plan-info.</para>
    /// </remarks>
    [HttpGet("get-institution-subscription-plan-info")]
    public async Task<IActionResult> GetInstitutionSubscriptionPlanInfo(
        [FromQuery] InstitutionSubscriptionPlanRequestDTO requestDTO,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetInstitutionSubscriptionPlanQuery(requestDTO),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Not-Used-In-Angular.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Not-Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves subscription plan modules.</para>
    /// <para>Handler flow: GetSubscriptionPlanModulesQuery is processed by GetSubscriptionPlanModulesQueryHandler; operation(s): GetNonDeletedSubscriptionPlanByIdAsync, GetModulesBySubscriptionPlanIdAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); PlanModuleMappingResponseDTO: Id (int), SubscriptionPlanId (int), SubscriptionPlanName (string?), ModuleId (int), ModuleName (string?), IsActive (bool?), Remark (string?), AddedDateTime (DateTime?), UpdatedDateTime (DateTime?)</para>
    /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
    /// <para>Backend endpoint: GET /api/subscription/get-all-institution-accessible-modules.</para>
    /// </remarks>
    [HttpGet("get-all-institution-accessible-modules")]
    public async Task<IActionResult> GetAllInstitutionAccessibleModules(
        [FromQuery] PlanModuleMappingRequestDTO requestDTO,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetSubscriptionPlanModulesQuery(requestDTO),
            cancellationToken);

        return Ok(result);
    }

    #endregion

    #region Subscription Plan Commands
    /// <summary>
    /// Not-Used-In-Angular.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Not-Used-In-Angular.</para>
    /// <para>API endpoint purpose: creates subscription plan.</para>
    /// <para>Handler flow: CreateSubscriptionPlanCommand is processed by CreateSubscriptionPlanCommandHandler; operation(s): AddSubscriptionPlanAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); SubscriptionActivePlanDTO: Id (long), PlanName (string?), IsActive (bool), MaxUsers (int?), IsMostPopular (bool?), IsCustom (bool?), IsFree (bool?), CurrencyKey (string?), PerDayPrice (decimal?), MonthlyPrice (decimal?), YearlyPrice (decimal?), Modules (List&lt;ModuleActiveDTO&gt;)</para>
    /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
    /// <para>Backend endpoint: POST /api/subscription/add.</para>
    /// </remarks>
    [Authorize]
    [HttpPost("add")]
    public async Task<IActionResult> CreateSubscription(
        [FromBody] CreateSubscriptionRequestDTO requestDTO,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new CreateSubscriptionPlanCommand(requestDTO),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Used-In-Angular: updates subscription plan.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: updates subscription plan.</para>
    /// <para>Handler flow: UpdateSubscriptionPlanCommand is processed by UpdateSubscriptionPlanCommandHandler; operation(s): GetNonDeletedSubscriptionPlanByIdAsync, Map, GetEligibleModulesForPlanMappingAsync, Create, UpdateSubscriptionPlanAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); SubscriptionActivePlanDTO: Id (long), PlanName (string?), IsActive (bool), MaxUsers (int?), IsMostPopular (bool?), IsCustom (bool?), IsFree (bool?), CurrencyKey (string?), PerDayPrice (decimal?), MonthlyPrice (decimal?), YearlyPrice (decimal?), Modules (List&lt;ModuleActiveDTO&gt;)</para>
    /// <para>Angular function(s): SubscriptionsApi.updateSubscriptionPlan (app/core/services/subscriptions-api.ts:95).</para>
    /// <para>Angular purpose: updates subscription plan.</para>
    /// <para>Integrated UI page(s): /app/subscriptions</para>
    /// <para>Angular UI component(s): SubscriptionPlanForm (app/features/host/subscriptions/subscription-plan-form/subscription-plan-form.ts); SubscriptionsStore (app/features/host/subscriptions/subscriptions.store.ts); Subscriptions (app/features/host/subscriptions/subscriptions.ts)</para>
    /// </remarks>
    [Authorize]
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateSubscription(
        long id,
        [FromBody] UpdateSubscriptionRequestDTO requestDTO,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateSubscriptionPlanCommand(id, requestDTO),
            cancellationToken);

        return Ok(result);
    }
    /// <summary>
    /// Not-Used-In-Angular.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Not-Used-In-Angular.</para>
    /// <para>API endpoint purpose: deletes subscription plan.</para>
    /// <para>Handler flow: DeleteSubscriptionPlanCommand is processed by DeleteSubscriptionPlanCommandHandler; operation(s): GetNonDeletedSubscriptionPlanByIdAsync, DeleteAllBySubscriptionPlanIdAsync, SaveChangesAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
    /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
    /// <para>Backend endpoint: POST /api/subscription/delete-subscription-plan.</para>
    /// </remarks>
    [Authorize]
    [HttpPost("delete-subscription-plan")]
    public async Task<IActionResult> DeleteSubscriptionPlan(
        [FromBody] DeleteSubscriptionPlanRequestDTO requestDTO,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new DeleteSubscriptionPlanCommand(requestDTO),
            cancellationToken);

        return Ok(result);
    }

    #endregion
}
