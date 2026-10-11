// ================================================================
// Purpose : Exposes authenticated submission to the transport-neutral device command queue.
// ================================================================

using skolenza.application.DTOS.Host;
using skolenza.application.Features.DeviceCommandCmd;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.HostDevice;

/// <summary>Queues supported commands for Institution devices through their configured transport adapter.</summary>
[Authorize]
[ApiController]
[Route("api/device-commands")]
public sealed class DeviceCommandController(IMediator mediator) : ControllerBase
{
    /// <summary>Queues a protocol-confirmed diagnostic command when no typed UI endpoint applies; normal Angular screens should use typed device, credential, and reboot APIs.</summary>
    [HttpPost("submit")]
    public async Task<IActionResult> Submit(
        [FromBody] SubmitDeviceCommandRequestDTO dto,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new SubmitDeviceCommand(dto), cancellationToken));
}
