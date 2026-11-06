using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using skolenza.application.Constants;
using skolenza.application.DTOS.Student;
using skolenza.application.Features.StudentPortalAuthCmd;

namespace skolenza.api.Controllers.Student;

/// <summary>Provides Student/Guardian portal authentication isolated from Staff and Host sessions.</summary>
[ApiController]
[Route("api/Student/portal-auth")]
public sealed class StudentPortalAuthController(IMediator mediator) : ControllerBase
{
    /// <summary>Authenticates a verified portal account using the optional portal hostname or the request Host fallback.</summary>
    /// <remarks>The hostname selects an active verified institution; it does not authenticate the account.</remarks>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] StudentPortalLoginRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new StudentPortalLoginCommand(ResolvePortalHostname(request), request), cancellationToken));

    /// <summary>Rotates a portal refresh token; the hostname must resolve to the token's institution.</summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] StudentPortalRefreshRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new StudentPortalRefreshCommand(ResolvePortalHostname(request), request), cancellationToken));

    /// <summary>Revokes this portal account's supplied refresh token.</summary>
    [Authorize(Policy = StudentConstants.PortalAuthenticationPolicy)]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] StudentPortalRefreshRequestDTO request,
        CancellationToken cancellationToken)
    {
        if (!long.TryParse(User.FindFirstValue("InstitutionId"), out var institutionId) ||
            !long.TryParse(User.FindFirstValue(StudentConstants.PortalAccountIdClaim), out var accountId))
        {
            return Unauthorized();
        }

        return Ok(await mediator.Send(
            new StudentPortalRevokeCommand(institutionId, accountId, request),
            cancellationToken));
    }

    /// <summary>Returns only the active Guardian and Student personas scoped to this portal account and institution.</summary>
    [Authorize(Policy = StudentConstants.PortalAuthenticationPolicy)]
    [HttpGet("me")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        if (!long.TryParse(User.FindFirstValue("InstitutionId"), out var institutionId) ||
            !long.TryParse(User.FindFirstValue(StudentConstants.PortalAccountIdClaim), out var accountId))
        {
            return Unauthorized();
        }

        return Ok(await mediator.Send(
            new GetStudentPortalProfileQuery(institutionId, accountId), cancellationToken));
    }

    /// <summary>Accepts an invitation for a new portal account and establishes its password.</summary>
    [AllowAnonymous]
    [HttpPost("invitations/accept")]
    public async Task<IActionResult> AcceptInvitation(
        [FromBody] StudentPortalInvitationAcceptanceRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new AcceptStudentPortalInvitationCommand(
            ResolvePortalHostname(request), request, null), cancellationToken));

    /// <summary>Links an invited Guardian to the signed-in portal account that matches the Guardian email.</summary>
    [Authorize(Policy = StudentConstants.PortalAuthenticationPolicy)]
    [HttpPost("invitations/claim")]
    public async Task<IActionResult> ClaimInvitation(
        [FromBody] StudentPortalInvitationAcceptanceRequestDTO request,
        CancellationToken cancellationToken)
    {
        if (!long.TryParse(User.FindFirstValue("InstitutionId"), out var institutionId) ||
            !long.TryParse(User.FindFirstValue(StudentConstants.PortalAccountIdClaim), out var accountId))
        {
            return Unauthorized();
        }

        return Ok(await mediator.Send(new AcceptStudentPortalInvitationCommand(
            ResolvePortalHostname(request), request, accountId), cancellationToken));
    }

    /// <summary>Requests a generic-response password-reset email for an active portal account on this verified domain.</summary>
    [AllowAnonymous]
    [HttpPost("password-reset/request")]
    public async Task<IActionResult> RequestPasswordReset(
        [FromBody] StudentPortalPasswordResetRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new RequestStudentPortalPasswordResetCommand(
            ResolvePortalHostname(request), request), cancellationToken));

    /// <summary>Completes a single-use, institution-scoped portal password reset and revokes existing refresh sessions.</summary>
    [AllowAnonymous]
    [HttpPost("password-reset/complete")]
    public async Task<IActionResult> CompletePasswordReset(
        [FromBody] StudentPortalPasswordResetCompletionRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CompleteStudentPortalPasswordResetCommand(
            ResolvePortalHostname(request), request), cancellationToken));

    private string ResolvePortalHostname(StudentPortalTenantRequestDTO request) =>
        string.IsNullOrWhiteSpace(request.PortalHostname) ? Request.Host.Host : request.PortalHostname;
}
