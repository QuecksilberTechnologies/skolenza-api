// ================================================================
// Purpose : Exposes Host-only CRUD endpoints for the default SMTP configuration used by first-time Institution registration.
// ================================================================

using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOs.DefaultEmailConfig;
using skolenza.application.Features.DefaultEmailConfigCmd.Handlers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.DefaultEmailConfig;

/// <summary>
/// Manages the central SMTP configuration copied into a Institution's email configuration when the Institution first self-registers.
/// Every endpoint requires a Host session and the current Host module-operation permission.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class DefaultEmailConfigController(
    IMediator mediator,
    ILogger<DefaultEmailConfigController> logger) : ControllerBase
{
    /// <summary>Creates a default SMTP configuration. The SMTP secret is write-only.</summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create(
        [FromBody] CreateDefaultEmailConfigRequestDTO dto,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received default email configuration create request.");
        return Ok(await mediator.Send(new CreateDefaultEmailConfigCommand(dto), cancellationToken));
    }

    /// <summary>Returns all central SMTP configuration records without exposing SMTP secrets.</summary>
    [HttpGet("get-all")]
    public async Task<IActionResult> GetAll(
        [FromQuery] PermissionRequestDTO? permissionRequest,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received default email configuration list request.");
        return Ok(await mediator.Send(new GetAllDefaultEmailConfigsQuery(permissionRequest), cancellationToken));
    }

    /// <summary>Returns one central SMTP configuration without exposing SMTP secrets.</summary>
    [HttpGet("get-by-id/{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        [FromQuery] PermissionRequestDTO? permissionRequest,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received default email configuration read request for {DefaultEmailConfigId}.", id);
        return Ok(await mediator.Send(
            new GetDefaultEmailConfigByIdQuery(id, permissionRequest),
            cancellationToken));
    }

    /// <summary>Updates a central SMTP configuration. Omit <c>SmtpPassword</c> to preserve its existing value.</summary>
    [HttpPost("update")]
    public async Task<IActionResult> Update(
        [FromBody] UpdateDefaultEmailConfigRequestDTO dto,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received default email configuration update request for {DefaultEmailConfigId}.", dto.Id);
        return Ok(await mediator.Send(new UpdateDefaultEmailConfigCommand(dto), cancellationToken));
    }

    /// <summary>Deletes an inactive central SMTP configuration. The active configuration must be replaced first.</summary>
    [HttpDelete("delete/{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        [FromQuery] PermissionRequestDTO? permissionRequest,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received default email configuration delete request for {DefaultEmailConfigId}.", id);
        return Ok(await mediator.Send(
            new DeleteDefaultEmailConfigCommand(id, permissionRequest),
            cancellationToken));
    }
}
