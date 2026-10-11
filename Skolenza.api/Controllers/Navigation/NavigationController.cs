// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Exposes the authenticated user's lightweight, permission-filtered application navigation.
// ================================================================

using skolenza.application.Features.NavigationCmd.Handlers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Navigation;

/// <summary>
/// Provides navigation data for the authenticated application's hamburger menu.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class NavigationController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationController"/> class.
    /// </summary>
    public NavigationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Used by the Angular application shell to build the authenticated user's menu.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves my navigation menu.</para>
    /// <para>Handler flow: GetMyNavigationMenuQuery is processed by GetMyNavigationMenuQueryHandler; operation(s): GetInstitutionNavigationMenuAsync, GetHostNavigationMenuAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); NavigationMenuResponseDTO: UserType (string), Items (IReadOnlyCollection&lt;NavigationMenuItemResponseDTO&gt;)</para>
    /// <para>The Angular shell calls this endpoint after authentication and uses Data.Items as the menu tree.</para>
    /// <para>Backend endpoint: GET /api/navigation/my-menu.</para>
    /// </remarks>
    [HttpGet("my-menu")]
    public async Task<IActionResult> GetMyMenu(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyNavigationMenuQuery(), cancellationToken);
        return Ok(result);
    }
}
