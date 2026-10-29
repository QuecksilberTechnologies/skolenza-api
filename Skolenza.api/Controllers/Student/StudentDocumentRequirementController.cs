using skolenza.application.DTOS.Student;
using skolenza.application.Features.StudentCmd;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Student;

/// <summary>Manages institution-configured, location/session-specific student admission document requirements.</summary>
[ApiController]
[Authorize]
[Route("api/Student/admission-document-requirements")]
public sealed class StudentDocumentRequirementController(IMediator mediator) : ControllerBase
{
    /// <summary>Returns a page of document requirements for the selected campus, session and person scope.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPage(
        [FromQuery] StudentDocumentRequirementFilterDTO filter,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetStudentDocumentRequirementsQuery(filter), cancellationToken));

    /// <summary>Creates an institution-owned document requirement.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] StudentDocumentRequirementRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateStudentDocumentRequirementCommand(request), cancellationToken));

    /// <summary>Updates an institution-owned document requirement.</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] StudentDocumentRequirementRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new UpdateStudentDocumentRequirementCommand(id, request), cancellationToken));

    /// <summary>Soft-deletes an institution-owned document requirement.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(
        long id,
        [FromQuery] StudentDocumentRequirementDeleteDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new DeleteStudentDocumentRequirementCommand(id, request), cancellationToken));
}
