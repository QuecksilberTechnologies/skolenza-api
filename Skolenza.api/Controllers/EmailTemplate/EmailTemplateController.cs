// ================================================================
// Purpose : Exposes legacy email-template delivery routes and secured Host CRUD management routes.
// ================================================================

using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOs.EmailTemplate;
using skolenza.application.Features.EmailTemplateCmd.Handlers;
using skolenza.application.Features.EmailTemplateCmd.Queries;
using skolenza.application.Interfaces.IEmail;
using skolenza.application.Interfaces.ILogger;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.EmailTemplate;

[ApiController]
[Route("api/[controller]")]
public sealed class EmailTemplateController(
    IMediator mediator,
    IEmailService emailService,
    ILoggerService logger) : ControllerBase
{
    /// <summary>
    /// Legacy lookup route retained for existing integrations. New Host administration should use <c>get-by-id</c> and <c>get-all</c>.
    /// </summary>
    [Authorize]
    [HttpGet("get-template-by-code")]
    public async Task<IActionResult> GetTemplateByCodeAsync(
        [FromQuery] string code,
        CancellationToken cancellationToken)
    {
        logger.LogInfo($"Getting email template for code: {code}");
        return Ok(await mediator.Send(new GetEmailTemplateByCodeQuery(code), cancellationToken));
    }

    /// <summary>
    /// Legacy send route retained for existing integrations. It resolves only an active template and uses the configured SMTP fallback flow.
    /// </summary>
    [Authorize]
    [HttpPost("send-template")]
    public async Task<IActionResult> SendTemplatedEmail(
        [FromBody] SendEmailTemplatRequestDTO request,
        CancellationToken cancellationToken)
    {
        logger.LogInfo($"Sending email to {request.ToEmail} using template {request.TemplateCode}");
        await emailService.SendTemplatedEmailAsync(
            request.TemplateCode,
            request.ToEmail,
            request.InstitutionId,
            request.Placeholders);

        return Ok("Email sent successfully.");
    }

    /// <summary>Creates a centrally managed template. Requires the Host create/add permission.</summary>
    [Authorize]
    [HttpPost("create")]
    public async Task<IActionResult> Create(
        [FromBody] CreateEmailTemplateRequestDTO dto,
        CancellationToken cancellationToken)
    {
        logger.LogInfo("Received email-template create request.");
        return Ok(await mediator.Send(new CreateEmailTemplateCommand(dto), cancellationToken));
    }

    /// <summary>Returns a paged, searchable template list. Requires the Host view/read permission.</summary>
    [Authorize]
    [HttpGet("get-all")]
    public async Task<IActionResult> GetAll(
        [FromQuery] EmailTemplateListRequestDTO? filter,
        CancellationToken cancellationToken)
    {
        logger.LogInfo("Received email-template list request.");
        return Ok(await mediator.Send(new GetAllEmailTemplatesQuery(filter), cancellationToken));
    }

    /// <summary>Returns one template by identifier. Requires the Host view/read permission.</summary>
    [Authorize]
    [HttpGet("get-by-id/{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        [FromQuery] PermissionRequestDTO? permissionRequest,
        CancellationToken cancellationToken)
    {
        logger.LogInfo($"Received email-template read request for {id}.");
        return Ok(await mediator.Send(
            new GetEmailTemplateByIdQuery(id, permissionRequest),
            cancellationToken));
    }

    /// <summary>Updates template content and metadata. Requires the Host update/edit permission.</summary>
    [Authorize]
    [HttpPost("update")]
    public async Task<IActionResult> Update(
        [FromBody] UpdateEmailTemplateRequestDTO dto,
        CancellationToken cancellationToken)
    {
        logger.LogInfo($"Received email-template update request for {dto.Id}.");
        return Ok(await mediator.Send(new UpdateEmailTemplateCommand(dto), cancellationToken));
    }

    /// <summary>Changes only the active state. Inactive templates cannot be used by the mail-delivery flow.</summary>
    [Authorize]
    [HttpPost("update-status")]
    public async Task<IActionResult> UpdateStatus(
        [FromBody] UpdateEmailTemplateStatusRequestDTO dto,
        CancellationToken cancellationToken)
    {
        logger.LogInfo($"Received email-template status update request for {dto.Id}.");
        return Ok(await mediator.Send(new UpdateEmailTemplateStatusCommand(dto), cancellationToken));
    }

    /// <summary>
    /// Deletes an inactive template that has no queued or delivered-email history. Requires the Host delete permission.
    /// </summary>
    [Authorize]
    [HttpDelete("delete/{id:int}")]
    public async Task<IActionResult> Delete(
        int id,
        [FromQuery] PermissionRequestDTO? permissionRequest,
        CancellationToken cancellationToken)
    {
        logger.LogInfo($"Received email-template delete request for {id}.");
        return Ok(await mediator.Send(
            new DeleteEmailTemplateCommand(id, permissionRequest),
            cancellationToken));
    }
}
