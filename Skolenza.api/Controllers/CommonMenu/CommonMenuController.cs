// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Exposes authenticated application-wide Common menu navigation for Host and Institution users.
// ================================================================

using skolenza.application.Features.ModuleCmd.CommonMenu.Handlers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.CommonMenu;

/// <summary>
/// Retrieves the shared Common navigation hierarchy for an authenticated Host or Institution user.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class CommonMenuController : ControllerBase
{
    #region Fields

    private readonly IMediator _mediator;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="CommonMenuController"/> class.
    /// </summary>
    /// <param name="mediator">Dispatches authenticated Common-menu queries.</param>
    public CommonMenuController(IMediator mediator)
    {
        _mediator = mediator;
    }

    #endregion

    #region Common Menu Endpoints

    /// <summary>
    /// Used-In-Angular: retrieves common menu.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves common menu.</para>
    /// <para>Handler flow: GetCommonMenuQuery is processed by GetCommonMenuQueryHandler; operation(s): GetCommonMenuHierarchyAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); CommonMenuItemResponseDTO: ModuleId (int), ModuleName (string), DisplayName (string?), UrlPath (string?), ImageIconWeb (string?), ImageIconMobile (string?), IsLeafNode (bool), ItemPriority (int?), Children (IReadOnlyCollection&lt;CommonMenuItemResponseDTO&gt;)</para>
    /// <para>Angular function(s): CommonMenuApi.getCommonMenu (app/core/services/common-menu-api.ts:20).</para>
    /// <para>Angular purpose: retrieves common menu.</para>
    /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
    /// <para>Angular UI component(s): isLogoutMenuItem (app/core/stores/auth.store.ts); UserMenu (app/layout/user-menu/user-menu.ts); AppHeader (app/layout/app-header/app-header.ts)</para>
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCommonMenuQuery(), cancellationToken);
        return Ok(result);
    }

    #endregion
}
