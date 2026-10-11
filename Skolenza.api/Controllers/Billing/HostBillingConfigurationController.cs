using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Billing;
using skolenza.application.Features.BillingCmd;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Billing;

/// <summary>Exposes HostAdmin-only payment and invoice configuration endpoints.</summary>
[ApiController]
[Authorize]
[Route("api/host/billing/configuration")]
public sealed class HostBillingConfigurationController(IMediator mediator) : ControllerBase
{
    /// <summary>Gets the Cashfree and seller configuration without returning any credential value.</summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] PermissionRequestDTO permissionRequest, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetHostBillingConfigurationQuery(permissionRequest), cancellationToken));

    /// <summary>Updates Host-owned seller, GST, invoice and bounded retry/grace configuration.</summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] HostBillingConfigurationRequestDTO request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new UpdateHostBillingConfigurationCommand(request), cancellationToken));
}
