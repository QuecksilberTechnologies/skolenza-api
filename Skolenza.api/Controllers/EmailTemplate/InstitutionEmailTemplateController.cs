using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOs.EmailTemplate;
using skolenza.application.Features.InstitutionEmailTemplateCmd;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.EmailTemplate;

[Authorize]
[ApiController]
[Route("api/InstitutionEmailTemplate")]
public sealed class InstitutionEmailTemplateController(IMediator mediator) : ControllerBase
{
    /// <summary>Creates a template owned by the authenticated Institution.</summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create(
        [FromBody] CreateInstitutionEmailTemplateRequestDTO dto,
        CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(
            new CreateInstitutionEmailTemplateCommand(dto),
            cancellationToken));
    }

    /// <summary>Returns the authenticated Institution's paged template list.</summary>
    [HttpGet("get-all")]
    public async Task<IActionResult> GetAll(
        [FromQuery] InstitutionEmailTemplateListRequestDTO? filter,
        CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(
            new GetAllInstitutionEmailTemplatesQuery(filter ?? new InstitutionEmailTemplateListRequestDTO()),
            cancellationToken));
    }

    /// <summary>Returns one template from the authenticated Institution.</summary>
    [HttpGet("get-by-id/{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        [FromQuery] PermissionRequestDTO permissionRequest,
        CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(
            new GetInstitutionEmailTemplateByIdQuery(id, permissionRequest),
            cancellationToken));
    }

    /// <summary>Updates template content inside the authenticated Institution.</summary>
    [HttpPost("update")]
    public async Task<IActionResult> Update(
        [FromBody] UpdateInstitutionEmailTemplateRequestDTO dto,
        CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(
            new UpdateInstitutionEmailTemplateCommand(dto),
            cancellationToken));
    }

    /// <summary>Activates or deactivates a Institution template.</summary>
    [HttpPost("update-status")]
    public async Task<IActionResult> UpdateStatus(
        [FromBody] UpdateInstitutionEmailTemplateStatusRequestDTO dto,
        CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(
            new UpdateInstitutionEmailTemplateStatusCommand(dto),
            cancellationToken));
    }

    /// <summary>
    /// Copies active default templates that do not already exist for the authenticated Institution.
    /// Existing active or inactive Institution templates are left unchanged. Requires Add permission.
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> Sync(
        [FromBody] SyncInstitutionEmailTemplatesRequestDTO dto,
        CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(
            new SyncInstitutionEmailTemplatesCommand(dto),
            cancellationToken));
    }

    /// <summary>Deletes an inactive template from the authenticated Institution.</summary>
    [HttpDelete("delete/{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        [FromQuery] PermissionRequestDTO permissionRequest,
        CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(
            new DeleteInstitutionEmailTemplateCommand(id, permissionRequest),
            cancellationToken));
    }
}
