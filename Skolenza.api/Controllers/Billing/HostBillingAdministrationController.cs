using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Billing;
using skolenza.application.Features.BillingCmd;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Billing;

/// <summary>Exposes HostAdmin pricing, tax, payment, refund, reconciliation and audit endpoints.</summary>
[ApiController]
[Authorize]
[Route("api/host/billing")]
public sealed class HostBillingAdministrationController(IMediator mediator) : ControllerBase
{
    [HttpGet("plan-prices")]
    public async Task<IActionResult> PlanPrices([FromQuery] PermissionRequestDTO permission, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetHostPlanPricesQuery(permission), ct));
    }

    [HttpPost("plan-prices")]
    public async Task<IActionResult> CreatePlanPrice([FromBody] HostPlanPriceRequestDTO dto, CancellationToken ct)
    {
        return Ok(await mediator.Send(new SaveHostPlanPriceCommand(null, dto), ct));
    }

    [HttpPut("plan-prices/{id:long}")]
    public async Task<IActionResult> UpdatePlanPrice(long id, [FromBody] HostPlanPriceRequestDTO dto, CancellationToken ct)
    {
        return Ok(await mediator.Send(new SaveHostPlanPriceCommand(id, dto), ct));
    }

    [HttpDelete("plan-prices/{id:long}")]
    public async Task<IActionResult> DeletePlanPrice(long id, [FromQuery] PermissionRequestDTO permission, CancellationToken ct)
    {
        return Ok(await mediator.Send(new DeleteHostPlanPriceCommand(id, permission), ct));
    }

    [HttpGet("tax-rules")]
    public async Task<IActionResult> TaxRules([FromQuery] PermissionRequestDTO permission, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetHostTaxRulesQuery(permission), ct));
    }

    [HttpPost("tax-rules")]
    public async Task<IActionResult> CreateTaxRule([FromBody] HostTaxRuleRequestDTO dto, CancellationToken ct)
    {
        return Ok(await mediator.Send(new SaveHostTaxRuleCommand(null, dto), ct));
    }

    [HttpPut("tax-rules/{id:long}")]
    public async Task<IActionResult> UpdateTaxRule(long id, [FromBody] HostTaxRuleRequestDTO dto, CancellationToken ct)
    {
        return Ok(await mediator.Send(new SaveHostTaxRuleCommand(id, dto), ct));
    }

    [HttpDelete("tax-rules/{id:long}")]
    public async Task<IActionResult> DeleteTaxRule(long id, [FromQuery] PermissionRequestDTO permission, CancellationToken ct)
    {
        return Ok(await mediator.Send(new DeleteHostTaxRuleCommand(id, permission), ct));
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> Transactions([FromQuery] HostBillingListRequestDTO dto, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetHostTransactionsQuery(dto), ct));
    }

    [HttpGet("refunds")]
    public async Task<IActionResult> Refunds([FromQuery] HostBillingListRequestDTO dto, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetHostRefundsQuery(dto), ct));
    }

    [HttpPost("refunds")]
    public async Task<IActionResult> Refund([FromBody] HostRefundRequestDTO dto, CancellationToken ct)
    {
        return Ok(await mediator.Send(new RequestHostRefundCommand(dto), ct));
    }

    [HttpGet("reconciliation")]
    public async Task<IActionResult> Reconciliation([FromQuery] PermissionRequestDTO permission, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetHostReconciliationQuery(permission), ct));
    }

    [HttpPost("webhooks/{id:long}/retry")]
    public async Task<IActionResult> RetryWebhook(long id, [FromBody] PermissionRequestDTO permission, CancellationToken ct)
    {
        return Ok(await mediator.Send(new RetryHostWebhookCommand(id, permission), ct));
    }

    [HttpGet("audit")]
    public async Task<IActionResult> Audit([FromQuery] HostBillingListRequestDTO dto, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetHostBillingAuditQuery(dto), ct));
    }
}
