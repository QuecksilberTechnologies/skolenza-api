using skolenza.application.DTOS.Student;
using skolenza.application.Features.StudentCmd;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Student;

/// <summary>Manages institution-owned benefit schemes and their location/session-specific discount bounds.</summary>
[ApiController]
[Authorize]
[Route("api/Student/benefit-scheme-masters")]
public sealed class StudentBenefitSchemeController(IMediator mediator) : ControllerBase
{
    /// <summary>Returns a paged list of schemes applicable to the selected location and academic session.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPage(
        [FromQuery] StudentBenefitSchemeFilterDTO filter,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetStudentBenefitSchemesQuery(filter), cancellationToken));

    /// <summary>Creates a benefit scheme with validated discount bounds and optional evidence requirement.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] StudentBenefitSchemeRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateStudentBenefitSchemeCommand(request), cancellationToken));

    /// <summary>Updates an institution-owned benefit scheme.</summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] StudentBenefitSchemeRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new UpdateStudentBenefitSchemeCommand(id, request), cancellationToken));

    /// <summary>Soft-deletes an institution-owned benefit scheme.</summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(
        long id,
        [FromQuery] StudentFeeMasterDeleteDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new DeleteStudentBenefitSchemeCommand(id, request), cancellationToken));
}
