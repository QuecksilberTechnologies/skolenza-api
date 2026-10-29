using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Student;
using skolenza.application.Features.BillingCmd;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Billing;

/// <summary>Maintains the host-owned currency catalogue used by campus-country fee calculations.</summary>
[ApiController]
[Authorize]
[Route("api/host/country-currency-masters")]
public sealed class HostCountryCurrencyMasterController(IMediator mediator) : ControllerBase
{
    /// <summary>Returns a paged country-currency catalogue; active rows are selected by default.</summary>
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] CountryCurrencyMasterFilterDTO request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetHostCountryCurrencyMastersQuery(request), cancellationToken));

    /// <summary>Adds a country currency mapping using the Host Add permission.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CountryCurrencyMasterRequestDTO request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new SaveHostCountryCurrencyMasterCommand(null, request), cancellationToken));

    /// <summary>Updates a country currency mapping using the Host Update permission.</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] CountryCurrencyMasterRequestDTO request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new SaveHostCountryCurrencyMasterCommand(id, request), cancellationToken));

    /// <summary>Deactivates a country currency mapping without deleting historical references.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Deactivate(long id, [FromQuery] PermissionRequestDTO permission, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new DeactivateHostCountryCurrencyMasterCommand(id, permission), cancellationToken));
}
