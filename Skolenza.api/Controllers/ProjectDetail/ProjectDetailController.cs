// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Exposes public read-only project feature-page metadata.
// ================================================================

using skolenza.application.Features.FeaturePages.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.ProjectDetail;

/// <summary>
/// Exposes active master feature headers, operational pages, and their operation metadata.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ProjectDetailController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectDetailController"/> class.
    /// </summary>
    /// <param name="mediator">Dispatches feature-page queries.</param>
    public ProjectDetailController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Used-In-Angular: retrieves feature pages.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves feature pages.</para>
    /// <para>Handler flow: GetFeaturePagesQuery is processed by GetFeaturePagesQueryHandler; operation(s): GetActiveFeaturePagesAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); FeaturePageResponseDTO: Id (int), ModuleCode (string?), ModuleName (string), DisplayName (string?), UrlPath (string?), IconKey (string?), ParentModuleId (int?), IsLeafNode (bool), IsModuleDisplayInUI (bool), ModuleScope (short), ModuleScopeName (string), IsCommonMenu (bool), ChildHeaders (List&lt;FeaturePageResponseDTO&gt;?), OperationalPages (List&lt;FeaturePageResponseDTO&gt;?), Operations (List&lt;FeaturePageOperationResponseDTO&gt;?)</para>
    /// <para>Angular function(s): FeaturePageApi.getFeaturePages (app/core/services/feature-page-api.ts:25).</para>
    /// <para>Angular purpose: retrieves feature pages.</para>
    /// <para>Integrated UI page(s): /auth/login</para>
    /// <para>Angular UI component(s): FeaturePageStore (app/core/stores/feature-page.store.ts); moduleOperationInterceptor (app/core/interceptors/module-operation-interceptor.ts); Login (app/features/authentication/login/login.ts); AppAsideMenu (app/layout/app-aside-menu/app-aside-menu.ts); AppHeader (app/layout/app-header/app-header.ts); appConfig (app/app.config.ts)</para>
    /// </remarks>
    [HttpGet("get-all")]
    public async Task<IActionResult> GetAll(
        [FromQuery(Name = "Scope")] short? scope,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetFeaturePagesQuery(scope), cancellationToken);
        return Ok(result);
    }
}
