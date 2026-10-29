using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using skolenza.application.DTOS.Student;
using skolenza.application.Features.StudentCmd;

namespace skolenza.api.Controllers.Student;

/// <summary>Manages institution-configured country and authority education identifier types.</summary>
[ApiController]
[Authorize]
[Route("api/Student/education-identifier-masters")]
public sealed class StudentEducationIdentifierMasterController(IMediator mediator) : ControllerBase
{
    /// <summary>Returns the institution's paged education identifier types, optionally filtered by country.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPage(
        [FromQuery] StudentEducationIdentifierMasterFilterDTO filter,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetStudentEducationIdentifierMastersQuery(filter), cancellationToken));

    /// <summary>Creates an institution-owned country/authority identifier type.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] StudentEducationIdentifierMasterRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateStudentEducationIdentifierMasterCommand(request), cancellationToken));

    /// <summary>Updates an institution-owned country/authority identifier type.</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] StudentEducationIdentifierMasterRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new UpdateStudentEducationIdentifierMasterCommand(id, request), cancellationToken));

    /// <summary>Soft-deletes an institution-owned country/authority identifier type.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(
        long id,
        [FromQuery] StudentEducationIdentifierMasterDeleteDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new DeleteStudentEducationIdentifierMasterCommand(id, request), cancellationToken));
}
