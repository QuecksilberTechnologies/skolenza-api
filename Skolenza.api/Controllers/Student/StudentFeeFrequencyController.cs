using skolenza.application.DTOS.Student;
using skolenza.application.Features.StudentCmd;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Student;

/// <summary>Manages institution-configured payment frequencies available to fee structures.</summary>
[ApiController]
[Authorize]
[Route("api/Student/fee-frequency-masters")]
public sealed class StudentFeeFrequencyController(IMediator mediator) : ControllerBase
{
    /// <summary>Returns a paged list of fee frequencies.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPage(
        [FromQuery] StudentFeeFrequencyFilterDTO filter,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetStudentFeeFrequenciesQuery(filter), cancellationToken));

    /// <summary>Creates a fee frequency for the current institution.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] StudentFeeFrequencyRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateStudentFeeFrequencyCommand(request), cancellationToken));

    /// <summary>Updates a fee frequency for the current institution.</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] StudentFeeFrequencyRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new UpdateStudentFeeFrequencyCommand(id, request), cancellationToken));

    /// <summary>Soft-deletes a fee frequency.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(
        long id,
        [FromQuery] StudentFeeMasterDeleteDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new DeleteStudentFeeFrequencyCommand(id, request), cancellationToken));
}
