using skolenza.application.DTOS.Student;
using skolenza.application.Features.StudentCmd;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Student;

/// <summary>Manages fee components and whether each charge recurs annually or is one-time.</summary>
[ApiController]
[Authorize]
[Route("api/Student/fee-component-masters")]
public sealed class StudentFeeComponentController(IMediator mediator) : ControllerBase
{
    /// <summary>Returns a paged list of fee components.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPage(
        [FromQuery] StudentFeeComponentFilterDTO filter,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetStudentFeeComponentsQuery(filter), cancellationToken));

    /// <summary>Creates a fee component for the current institution.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] StudentFeeComponentRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateStudentFeeComponentCommand(request), cancellationToken));

    /// <summary>Updates a fee component for the current institution.</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] StudentFeeComponentRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new UpdateStudentFeeComponentCommand(id, request), cancellationToken));

    /// <summary>Soft-deletes a fee component.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(
        long id,
        [FromQuery] StudentFeeMasterDeleteDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new DeleteStudentFeeComponentCommand(id, request), cancellationToken));
}
