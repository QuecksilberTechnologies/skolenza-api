// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Exposes streamlined Host and Institution StaffMember authentication through MediatR.
// ================================================================

using skolenza.application.DTOs.UserLogin;
using skolenza.application.Features.UserLoginAndDashboardCmd.Handlers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Login;

/// <summary>
/// Exposes the compact Host and Institution StaffMember login endpoint while preserving the existing Auth login endpoint.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class NewLoginController : ControllerBase
{
    #region Fields

    private readonly IMediator _mediator;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="NewLoginController"/> class.
    /// </summary>
    /// <param name="mediator">Dispatches the streamlined login command.</param>
    public NewLoginController(IMediator mediator)
    {
        _mediator = mediator;
    }

    #endregion

    #region Authentication Endpoints

    /// <summary>
    /// Used-In-Angular: authenticates the user.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: performs the Angular function new login.</para>
    /// <para>Handler flow: NewLoginCommand is processed by NewLoginCommandHandler; operation(s): GetByLoginIdAsync, GetNewLoginBootstrapAsync, GetValidateInstitutionPlan, GetStaffMemberRolesWithDetailsByIdAsync, GetActiveKeyByInstitutionIdAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); NewLoginResponseDTO: UserType (string), AccessToken (string), RefreshToken (string), AccessTokenExpiresAtUtc (DateTime), RefreshTokenExpiresAtUtc (DateTime), User (NewLoginUserContextDTO?), HostUser (NewLoginHostUserContextDTO?), HostRole (NewLoginHostRoleDTO?)</para>
    /// <para>Angular function(s): AuthApi.login (app/core/services/auth-api.ts:162).</para>
    /// <para>Angular purpose: authenticates the user.</para>
    /// <para>Integrated UI page(s): /auth/login</para>
    /// <para>Angular UI component(s): Login (app/features/authentication/login/login.ts)</para>
    /// </remarks>
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDTO request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new NewLoginCommand(request),
            cancellationToken);

        return Ok(result);
    }

    #endregion
}
