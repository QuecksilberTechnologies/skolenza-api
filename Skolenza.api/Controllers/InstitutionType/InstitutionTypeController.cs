// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Coordinates HTTP requests for Institution type operations.
// ================================================================

using skolenza.application.DTOs.Institution;
using skolenza.application.DTOs.InstitutionType;
using skolenza.application.Features.InstitutionConfigurationCmd.Institution.Queries;
using skolenza.application.Features.InstitutionTypeCmd.Handlers;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;


namespace skolenza.api.Controllers.InstitutionType
{
    /// <summary>
    /// Handles Institution type lookups and Institution subscription plans.
    /// </summary>

    [Route("api/InstitutionType")]
    [ApiController]
    public class InstitutionTypeController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILoggerService _logger;  // Logger service ka declaration

        public InstitutionTypeController(IMediator mediator, ILoggerService logger)
        {
            _mediator = mediator;
            _logger = logger;  // Logger service ko inject karna
        }


        /// <summary>
        /// Used-In-Angular: retrieves Institution types.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves all active Institution types.</para>
        /// <para>Handler flow: GetAllInstitutionTypeQuery is processed by GetAllInstitutionTypeQueryHandler; operation(s): GetAllActiveInstitutionTypesAsync.</para>
        /// <para>Response DTO property analysis: InstitutionTypeResponseDTO: Id (int), InstitutionTypeName (string), Description (string?), Remark (string?), IsActive (bool).</para>
        /// <para>Angular function: InstitutionTypesApi.getInstitutionTypes.</para>
        /// <para>Angular purpose: retrieves Institution types.</para>
        /// <para>Integrated UI page(s): /auth/register-institution; /app/institutions/new; /app/institutions/:institutionId/edit</para>
        /// <para>Angular UI component(s): Registration (app/features/authentication/registration/registration.ts); InstitutionForm (app/features/host/institutions/institution-form/institution-form.ts)</para>
        /// </remarks>
        [HttpGet("get-institution-types")]
        public async Task<IActionResult> GetAllInstitutionBySubscriptionIdAsync([FromQuery] int planId)
        {
            _logger.LogInfo($"Retrieving Institution types for subscription plan {planId}.");
            var query = new GetAllInstitutionTypeQuery(planId);
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        /// <summary>Returns the supported Institution ownership and management categories.</summary>
        /// <remarks>
        /// Used by the Institution create/edit profile category dropdown. Values come from the
        /// backend InstitutionCategory contract; the UI must not maintain a separate list.
        /// </remarks>
        [HttpGet("category-options")]
        public async Task<IActionResult> GetInstitutionCategoryOptionsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInfo("Retrieving Institution category options.");
            var result = await _mediator.Send(new GetInstitutionCategoryOptionsQuery(), cancellationToken);
            return Ok(result);
        }

        /// <summary>Returns education-specific Campus types for Institution create/edit.</summary>
        /// <remarks>
        /// Existing CampusType numeric values remain stable for database and API compatibility.
        /// Industrial legacy location types are deliberately excluded from this Institution dropdown.
        /// </remarks>
        [HttpGet("campus-type-options")]
        public async Task<IActionResult> GetInstitutionCampusTypeOptionsAsync(CancellationToken cancellationToken)
        {
            _logger.LogInfo("Retrieving Institution campus type options.");
            var result = await _mediator.Send(new GetInstitutionCampusTypeOptionsQuery(), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: retrieves institution subscription plans.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves institution subscription.</para>
        /// <para>Handler flow: GetInstitutionSubscriptionQuery is processed by GetInstitutionSubscriptionQueryHandler; operation(s): GetInstitutionSubscriptionPlanInfoAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); InstitutionSubscriptionPlanResponseDTO: Id (long), InstitutionId (long?), SubscriptionPlanId (int), SubscriptionStartDate (DateTime), SubscriptionEndDate (DateTime?), IsActive (bool), PaymentTxnId (string?), PaymentMode (string?), IsTrial (bool)</para>
        /// <para>Angular function(s): IndustriesApi.getInstitutionSubscriptionPlans (app/core/services/industries-api.ts:59).</para>
        /// <para>Angular purpose: retrieves institution subscription plans.</para>
        /// <para>Integrated UI page(s): /app/subscriptions</para>
        /// <para>Angular UI component(s): SubscriptionPlanDetail (app/features/host/subscriptions/subscription-plan-detail/subscription-plan-detail.ts); SubscriptionsStore (app/features/host/subscriptions/subscriptions.store.ts); Subscriptions (app/features/host/subscriptions/subscriptions.ts)</para>
        /// </remarks>
        [HttpGet("get-institution-subscription-plan")]
        public async Task<IActionResult> GetInstitutionSubscriptionPlanInfoAsync([FromQuery] InstitutionSubscriptionPlanRequestDTO code)
        {
            _logger.LogInfo("Retrieving Institution subscriptions for the requested filter.");
            var query = new GetInstitutionSubscriptionQuery(code);
            var result = await _mediator.Send(query);
            return Ok(result);
        }

    }
}
