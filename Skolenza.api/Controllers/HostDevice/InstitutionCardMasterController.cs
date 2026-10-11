using skolenza.application.DTOS.Host;
using skolenza.application.Features.HostDeviceCmd.Handlers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.HostDevice;

/// <summary>Host-only physical card procurement inventory for a selected Institution.</summary>
[Authorize]
[ApiController]
[Route("api/InstitutionCardMaster")]
public sealed class InstitutionCardMasterController(IMediator mediator) : ControllerBase
{
    /// <summary>Creates a Host-owned physical card inventory item so a Institution can later assign an approved card to an staffMember device user.</summary>
    [HttpPost("create")] public async Task<IActionResult> Create(CreateInstitutionCardMasterRequestDTO dto, CancellationToken ct) => Ok(await mediator.Send(new CreateInstitutionCardMasterCommand(dto), ct));
    /// <summary>Reads one Host card inventory item with its number masked, so procurement data can be edited without exposing the credential.</summary>
    [HttpGet("get-by-id/{id}")] public async Task<IActionResult> GetById(string id, [FromQuery] InstitutionDeviceAccessRequestDTO access, CancellationToken ct) => Ok(await mediator.Send(new GetInstitutionCardMasterByIdQuery(id, access), ct));
    /// <summary>Lists Host card inventory for selected-Institution procurement and availability screens; it is the source for institution card binding selection.</summary>
    [HttpGet("get-all")] public async Task<IActionResult> GetAll([FromQuery] InstitutionCardMasterFilterRequestDTO filter, CancellationToken ct) => Ok(await mediator.Send(new GetInstitutionCardMastersQuery(filter), ct));
    /// <summary>Updates procurement and tax information for an unassigned card; assigned cards are deliberately protected from editing.</summary>
    [HttpPost("update")] public async Task<IActionResult> Update(UpdateInstitutionCardMasterRequestDTO dto, CancellationToken ct) => Ok(await mediator.Send(new UpdateInstitutionCardMasterCommand(dto), ct));
    /// <summary>Activates or deactivates an unassigned card so only eligible inventory can be bound to staffMembers.</summary>
    [HttpPost("update-status")] public async Task<IActionResult> UpdateStatus(UpdateInstitutionCardMasterStatusRequestDTO dto, CancellationToken ct) => Ok(await mediator.Send(new UpdateInstitutionCardMasterStatusCommand(dto), ct));
    /// <summary>Soft-deletes an unassigned card when it should no longer appear in Host inventory; active device assignments block deletion.</summary>
    [HttpDelete("delete/{id}")] public async Task<IActionResult> Delete(string id, [FromQuery] InstitutionDeviceAccessRequestDTO access, CancellationToken ct) => Ok(await mediator.Send(new DeleteInstitutionCardMasterCommand(id, access), ct));
}
