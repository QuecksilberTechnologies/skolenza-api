using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using skolenza.application.Features.StudentWebhookCmd;

namespace skolenza.api.Controllers.Student;

/// <summary>Receives Cashfree-signed student tuition payment notifications.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/student/payment-gateways/cashfree/webhook")]
public sealed class CashfreeStudentPaymentWebhookController(IMediator mediator) : ControllerBase
{
    /// <summary>Verifies the original Cashfree body signature, retrieves payment state server-side, and posts it idempotently.</summary>
    [HttpPost]
    [RequestSizeLimit(65536)]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["x-webhook-signature"].ToString();
        var timestamp = Request.Headers["x-webhook-timestamp"].ToString();
        var eventId = Request.Headers["x-idempotency-header"].ToString();
        await mediator.Send(new ProcessCashfreeStudentPaymentWebhookCommand(
            rawBody, timestamp, signature, eventId), cancellationToken);
        return Ok(new { received = true });
    }
}
