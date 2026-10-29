using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using skolenza.application.DTOS.Student;
using skolenza.application.Features.StudentCmd;

namespace skolenza.api.Controllers.Student;

/// <summary>Configures an institution's verified portal hostname for future tenant-scoped portal login.</summary>
[ApiController]
[Authorize]
[Route("api/Student/portal-domains")]
public sealed class InstitutionPortalDomainMasterController(IMediator mediator) : ControllerBase
{
    /// <summary>Returns the institution-owned portal domain master entries.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPage(
        [FromQuery] InstitutionPortalDomainFilterDTO filter,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetInstitutionPortalDomainsQuery(filter), cancellationToken));

    /// <summary>Adds the institution hostname and returns a one-time TXT challenge.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] InstitutionPortalDomainCreateDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateInstitutionPortalDomainCommand(request), cancellationToken));

    /// <summary>Renews an expired or unpublished challenge and returns the new TXT value once.</summary>
    [HttpPost("{id:long}/challenge")]
    public async Task<IActionResult> RenewChallenge(
        long id,
        [FromBody] InstitutionPortalDomainVerifyDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new RenewInstitutionPortalDomainChallengeCommand(id, request), cancellationToken));

    /// <summary>Checks DNS TXT ownership and enables the hostname only when the current challenge matches.</summary>
    [HttpPost("{id:long}/verify")]
    public async Task<IActionResult> Verify(
        long id,
        [FromBody] InstitutionPortalDomainVerifyDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new VerifyInstitutionPortalDomainCommand(id, request), cancellationToken));

    /// <summary>Deactivates and soft-deletes the institution's portal hostname.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(
        long id,
        [FromQuery] InstitutionPortalDomainDeleteDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new DeleteInstitutionPortalDomainCommand(id, request), cancellationToken));
}
