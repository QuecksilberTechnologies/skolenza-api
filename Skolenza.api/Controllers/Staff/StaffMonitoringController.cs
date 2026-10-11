using skolenza.application.DTOS.StaffMonitoring;
using skolenza.application.Features.StaffMemberCmd.StaffMemberMonitoring;
using skolenza.application.Features.StaffMonitoringAgent;
using skolenza.application.Interfaces.ILogger;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace skolenza.api.Controllers.Staff;

/// <summary>Configures staffMember monitoring and exposes authenticated agent synchronization endpoints.</summary>
[ApiController]
[Route("api/Staff/Monitoring")]
public sealed class StaffMonitoringController(IMediator mediator, ILoggerService logger) : ControllerBase
{
    #region Institution administration

    /// <summary>Creates or updates the current Institution's staffMember-monitoring policy.</summary>
    [Authorize]
    [HttpPost("policy")]
    public async Task<IActionResult> UpsertPolicy([FromBody] UpsertStaffMemberMonitoringPolicyRequestDTO dto, CancellationToken cancellationToken)
    {
        logger.LogInfo("Received staffMember monitoring policy request.");
        return Ok(await mediator.Send(new UpsertStaffMemberMonitoringPolicyCommand(dto), cancellationToken));
    }

    /// <summary>Registers an staffMember PC agent and returns its credential once.</summary>
    [Authorize]
    [HttpPost("agent/register")]
    public async Task<IActionResult> RegisterAgent([FromBody] RegisterStaffMemberMonitoringAgentRequestDTO dto, CancellationToken cancellationToken)
    {
        logger.LogInfo("Received staffMember monitoring agent registration request.");
        return Ok(await mediator.Send(new RegisterStaffMemberMonitoringAgentCommand(dto), cancellationToken));
    }

    /// <summary>Returns current agent connectivity and screenshot-upload status.</summary>
    [Authorize]
    [HttpGet("agent/status")]
    public async Task<IActionResult> GetAgentStatus([FromQuery] StaffMemberMonitoringPermissionRequestDTO permissionRequest, CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new GetStaffMemberMonitoringAgentsQuery(permissionRequest), cancellationToken));
    }

    #endregion

    #region Installed agent runtime

    /// <summary>Returns the server-owned policy for an enrolled agent.</summary>
    [AllowAnonymous]
    [EnableRateLimiting("staffMember-monitoring-agent")]
    [HttpGet("runtime/configuration")]
    public async Task<IActionResult> GetRuntimeConfiguration([FromHeader(Name = "X-Agent-Credential")] string credential, CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new GetAgentConfigurationQuery(credential), cancellationToken));
    }

    /// <summary>Records the agent's latest connectivity and local-queue state.</summary>
    [AllowAnonymous]
    [EnableRateLimiting("staffMember-monitoring-agent")]
    [HttpPost("runtime/heartbeat")]
    public async Task<IActionResult> Heartbeat(
        [FromHeader(Name = "X-Agent-Credential")] string credential,
        [FromBody] AgentHeartbeatRequestDTO dto,
        CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new RecordAgentHeartbeatCommand(credential, dto), cancellationToken));
    }

    /// <summary>Idempotently uploads one offline or online staffMember screenshot.</summary>
    [AllowAnonymous]
    [EnableRateLimiting("staffMember-monitoring-agent")]
    [HttpPost("runtime/captures")]
    [RequestSizeLimit(5_242_880)]
    public async Task<IActionResult> UploadCapture(
        [FromHeader(Name = "X-Agent-Credential")] string credential,
        [FromForm] Guid captureId,
        [FromForm] DateTime capturedAtUtc,
        [FromForm] int monitorNumber,
        [FromForm] string? checksumSha256,
        IFormFile screenshot,
        CancellationToken cancellationToken)
    {
        await using var content = screenshot.OpenReadStream();
        var command = new UploadStaffMemberScreenCaptureCommand(
            credential,
            captureId,
            capturedAtUtc,
            monitorNumber,
            screenshot.ContentType,
            screenshot.Length,
            checksumSha256,
            content);
        return Ok(await mediator.Send(command, cancellationToken));
    }

    #endregion
}
