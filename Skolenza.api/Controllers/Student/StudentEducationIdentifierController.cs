using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using skolenza.application.DTOS.Student;
using skolenza.application.Features.StudentCmd;

namespace skolenza.api.Controllers.Student;

/// <summary>Stores and verifies externally issued Student education identifiers.</summary>
[ApiController]
[Authorize]
[Route("api/Student")]
public sealed class StudentEducationIdentifierController(IMediator mediator) : ControllerBase
{
    /// <summary>Returns a paged list of masked identifiers for a Student.</summary>
    [HttpGet("{studentId:long}/education-identifiers")]
    public async Task<IActionResult> GetPage(
        long studentId,
        [FromQuery] StudentEducationIdentifierFilterDTO filter,
        CancellationToken cancellationToken)
    {
        filter.StudentId = studentId;
        return Ok(await mediator.Send(
            new GetStudentEducationIdentifiersQuery(filter), cancellationToken));
    }

    /// <summary>Stores an externally issued identifier; the raw value is never returned.</summary>
    [HttpPost("{studentId:long}/education-identifiers")]
    public async Task<IActionResult> Create(
        long studentId,
        [FromBody] StudentEducationIdentifierRequestDTO request,
        CancellationToken cancellationToken)
    {
        request.StudentId = studentId;
        return Ok(await mediator.Send(
            new CreateStudentEducationIdentifierCommand(request), cancellationToken));
    }

    /// <summary>Replaces an identifier value and resets its verification state.</summary>
    [HttpPut("education-identifiers/{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] StudentEducationIdentifierUpdateDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new UpdateStudentEducationIdentifierCommand(id, request), cancellationToken));

    /// <summary>Records a staff verification or rejection decision and its actor/time.</summary>
    [HttpPost("education-identifiers/{id:long}/verification")]
    public async Task<IActionResult> SetVerificationStatus(
        long id,
        [FromBody] StudentEducationIdentifierVerificationDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(
            new SetStudentEducationIdentifierVerificationCommand(id, request), cancellationToken));
}
