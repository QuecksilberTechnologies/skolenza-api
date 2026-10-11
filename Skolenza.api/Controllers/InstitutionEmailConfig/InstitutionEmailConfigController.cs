// ================================================================
// Purpose : Exposes Host- and Institution-authorized CRUD endpoints for Institution SMTP configuration.
// ================================================================

using skolenza.application.DTOS.Configruations;
using skolenza.application.Features.InstitutionEmailConfigCmd.Handlers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.InstitutionEmailConfig;

/// <summary>
/// Manages Institution-specific SMTP settings. Host users supply the encrypted
/// Institution identifier; Institution users are automatically scoped to their own
/// Institution. SMTP secrets are write-only and never included in API responses.
/// </summary>
[Authorize]
[ApiController]
[Route("api/InstitutionEmailConfig")]
public sealed class InstitutionEmailConfigController(
    IMediator mediator,
    ILogger<InstitutionEmailConfigController> logger) : ControllerBase
{
    [HttpPost("create")]
    public async Task<IActionResult> Create(
        [FromBody] CreateInstitutionEmailConfigRequestDTO dto,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received Institution email configuration create request.");
        return Ok(await mediator.Send(new CreateInstitutionEmailConfigCommand(dto), cancellationToken));
    }

    [HttpGet("get-all")]
    public async Task<IActionResult> GetAll(
        [FromQuery] InstitutionEmailConfigAccessRequestDTO filter,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received Institution email configuration list request.");
        return Ok(await mediator.Send(new GetAllInstitutionEmailConfigsQuery(filter), cancellationToken));
    }

    [HttpGet("get-by-id/{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        [FromQuery] InstitutionEmailConfigAccessRequestDTO accessRequest,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received Institution email configuration read request for {InstitutionEmailConfigId}.", id);
        return Ok(await mediator.Send(new GetInstitutionEmailConfigByIdQuery(id, accessRequest), cancellationToken));
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update(
        [FromBody] UpdateInstitutionEmailConfigRequestDTO dto,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received Institution email configuration update request for {InstitutionEmailConfigId}.", dto.Id);
        return Ok(await mediator.Send(new UpdateInstitutionEmailConfigCommand(dto), cancellationToken));
    }

    [HttpDelete("delete/{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        [FromQuery] InstitutionEmailConfigAccessRequestDTO accessRequest,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received Institution email configuration delete request for {InstitutionEmailConfigId}.", id);
        return Ok(await mediator.Send(new DeleteInstitutionEmailConfigCommand(id, accessRequest), cancellationToken));
    }
}
