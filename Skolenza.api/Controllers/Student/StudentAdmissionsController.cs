using skolenza.application.DTOS.Student;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.Features.StudentCmd;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Student;

/// <summary>Manages institution-scoped student admission drafts, review and student exit history.</summary>
[ApiController]
[Authorize]
[Route("api/Student")]
public sealed class StudentAdmissionsController(IMediator mediator) : ControllerBase
{
    /// <summary>Returns centrally defined Student workflow enum values for an authorized institution user.</summary>
    [HttpGet("options")]
    public async Task<IActionResult> GetOptions(
        [FromQuery] StudentOptionsRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetStudentOptionsQuery(request), cancellationToken));

    /// <summary>Returns a paged institution-scoped student record list.</summary>
    [HttpGet("records")]
    public async Task<IActionResult> GetStudents(
        [FromQuery] StudentRecordFilterDTO filter,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetStudentsQuery(filter), cancellationToken));

    /// <summary>Returns a paged institution-scoped admission list.</summary>
    [HttpGet("admissions")]
    public async Task<IActionResult> GetAdmissions(
        [FromQuery] StudentAdmissionFilterDTO filter,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetStudentAdmissionsQuery(filter), cancellationToken));

    /// <summary>Returns one institution-scoped admission with its current status and linked Student summary.</summary>
    /// <remarks>Requires Student Admissions View permission; pass the dynamically resolved moduleId and operationId in the query.</remarks>
    [HttpGet("admissions/{id:long}")]
    public async Task<IActionResult> GetAdmission(
        long id,
        [FromQuery] PermissionRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetStudentAdmissionQuery(id, request), cancellationToken));

    /// <summary>Returns paged, append-only status transition history for one institution-scoped admission.</summary>
    [HttpGet("admissions/{id:long}/status-history")]
    public async Task<IActionResult> GetAdmissionStatusHistory(
        long id,
        [FromQuery] StudentAdmissionStatusHistoryFilterDTO filter,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetStudentAdmissionStatusHistoryQuery(id, filter), cancellationToken));

    /// <summary>Creates an admission draft for the selected institution session, campus and academic group.</summary>
    [HttpPost("admissions")]
    public async Task<IActionResult> CreateAdmission(
        [FromBody] StudentAdmissionRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateStudentAdmissionCommand(request), cancellationToken));

    /// <summary>Updates a Draft or Returned for Correction admission.</summary>
    [HttpPut("admissions/{id:long}")]
    public async Task<IActionResult> UpdateAdmission(
        long id,
        [FromBody] StudentAdmissionRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new UpdateStudentAdmissionCommand(id, request), cancellationToken));

    /// <summary>Lists captured Guardian relationships for an admission; closed relationships are optionally included.</summary>
    [HttpGet("admissions/{id:long}/guardians")]
    public async Task<IActionResult> GetAdmissionGuardians(
        long id,
        [FromQuery] StudentGuardianLinksFilterDTO filter,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetAdmissionGuardiansQuery(id, filter), cancellationToken));

    /// <summary>Adds an existing institution Guardian profile to a Draft or Returned for Correction admission.</summary>
    [HttpPost("admissions/{id:long}/guardians")]
    public async Task<IActionResult> AddAdmissionGuardian(
        long id,
        [FromBody] StudentAdmissionGuardianRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new AddAdmissionGuardianCommand(id, request), cancellationToken));

    /// <summary>Updates relationship and access flags for an admission Guardian.</summary>
    [HttpPut("admissions/{id:long}/guardians/{linkId:long}")]
    public async Task<IActionResult> UpdateAdmissionGuardian(
        long id,
        long linkId,
        [FromBody] StudentAdmissionGuardianRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new UpdateAdmissionGuardianCommand(id, linkId, request), cancellationToken));

    /// <summary>Closes an admission Guardian relationship without deleting the Guardian profile.</summary>
    [HttpDelete("admissions/{id:long}/guardians/{linkId:long}")]
    public async Task<IActionResult> RemoveAdmissionGuardian(
        long id,
        long linkId,
        [FromBody] StudentGuardianUnlinkRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new RemoveAdmissionGuardianCommand(id, linkId, request), cancellationToken));

    /// <summary>Uploads a versioned Student or captured Guardian admission document for staff-assisted review.</summary>
    /// <remarks>
    /// Send multipart/form-data with requirementId, personScope, guardianId (Guardian scope only),
    /// expiryDate when required by the master, file, moduleId and operationId. Requires the existing
    /// Student Admissions Add permission. Guardian self-upload is added with the separate portal-auth API.
    /// </remarks>
    [HttpPost("admissions/{id:long}/documents")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(105_906_176)]
    [RequestFormLimits(MultipartBodyLengthLimit = 105_906_176)]
    public async Task<IActionResult> UploadAdmissionDocument(
        long id,
        [FromForm] StudentAdmissionDocumentUploadDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new UploadStudentAdmissionDocumentCommand(id, request), cancellationToken));

    /// <summary>Returns a paged list of retained document versions for an admission.</summary>
    [HttpGet("admissions/{id:long}/documents")]
    public async Task<IActionResult> GetAdmissionDocuments(
        long id,
        [FromQuery] StudentAdmissionDocumentFilterDTO filter,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetStudentAdmissionDocumentsQuery(id, filter), cancellationToken));

    /// <summary>Creates a short-lived signed download URL after institution and admission scoping.</summary>
    [HttpGet("admissions/{id:long}/documents/{documentId:long}/download")]
    public async Task<IActionResult> GetAdmissionDocumentDownload(
        long id,
        long documentId,
        [FromQuery] StudentDocumentAccessRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetStudentAdmissionDocumentDownloadQuery(id, documentId, request), cancellationToken));

    /// <summary>Approves, rejects or returns an uploaded admission document with an auditable decision.</summary>
    [HttpPost("admissions/{id:long}/documents/{documentId:long}/review")]
    public async Task<IActionResult> ReviewAdmissionDocument(
        long id,
        long documentId,
        [FromBody] StudentAdmissionDocumentReviewDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new ReviewStudentAdmissionDocumentCommand(id, documentId, request), cancellationToken));

    /// <summary>Approves, rejects or returns a post-enrollment Student/Guardian portal document.</summary>
    /// <remarks>Requires the existing Student Admissions Update permission, discovered through the authenticated menu.</remarks>
    [HttpPost("records/{studentId:long}/documents/{documentId:long}/review")]
    public async Task<IActionResult> ReviewStudentRecordDocument(
        long studentId,
        long documentId,
        [FromBody] StudentAdmissionDocumentReviewDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new ReviewStudentRecordDocumentCommand(studentId, documentId, request), cancellationToken));

    /// <summary>Submits, reviews, approves, rejects, returns or cancels an admission.</summary>
    [HttpPost("admissions/{id:long}/review")]
    public async Task<IActionResult> ReviewAdmission(
        long id,
        [FromBody] StudentAdmissionReviewDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new ReviewStudentAdmissionCommand(id, request), cancellationToken));

    /// <summary>Records a Student exit and closes the active enrollment; this is not a physical delete.</summary>
    [HttpDelete("records/{id:long}")]
    public async Task<IActionResult> ExitStudent(
        long id,
        [FromBody] StudentExitRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new ExitStudentCommand(id, request), cancellationToken));

    /// <summary>Returns paged, institution-scoped exit history for one student.</summary>
    [HttpGet("records/{id:long}/exit-history")]
    public async Task<IActionResult> GetExitHistory(
        long id,
        [FromQuery] StudentExitHistoryFilterDTO filter,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetStudentExitHistoryQuery(id, filter), cancellationToken));

    /// <summary>Returns a paged institution-scoped Guardian directory.</summary>
    [HttpGet("guardians")]
    public async Task<IActionResult> GetGuardians(
        [FromQuery] StudentGuardianFilterDTO filter,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetGuardiansQuery(filter), cancellationToken));

    /// <summary>Creates an institution-scoped Guardian profile without granting a portal credential.</summary>
    [HttpPost("guardians")]
    public async Task<IActionResult> CreateGuardian(
        [FromBody] StudentGuardianProfileRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateGuardianCommand(request), cancellationToken));

    /// <summary>Updates an institution-scoped Guardian profile shared by its Student links.</summary>
    [HttpPut("guardians/{id:long}")]
    public async Task<IActionResult> UpdateGuardian(
        long id,
        [FromBody] StudentGuardianProfileRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new UpdateGuardianCommand(id, request), cancellationToken));

    /// <summary>Lists active Guardian links for a Student; includeInactive adds retained closed links.</summary>
    [HttpGet("records/{id:long}/guardians")]
    public async Task<IActionResult> GetStudentGuardians(
        long id,
        [FromQuery] StudentGuardianLinksFilterDTO filter,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetStudentGuardiansQuery(id, filter), cancellationToken));

    /// <summary>Links an existing Guardian profile to a Student.</summary>
    [HttpPost("records/{id:long}/guardians")]
    public async Task<IActionResult> LinkGuardian(
        long id,
        [FromBody] StudentGuardianLinkRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new LinkGuardianCommand(id, request), cancellationToken));

    /// <summary>Updates relationship, access and contact permissions for an active Student-Guardian link.</summary>
    [HttpPut("records/{id:long}/guardians/{linkId:long}")]
    public async Task<IActionResult> UpdateGuardianLink(
        long id,
        long linkId,
        [FromBody] StudentGuardianLinkRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new UpdateGuardianLinkCommand(id, linkId, request), cancellationToken));

    /// <summary>Ends Student-Guardian access while retaining the Guardian and link history.</summary>
    [HttpDelete("records/{id:long}/guardians/{linkId:long}")]
    public async Task<IActionResult> RemoveGuardianLink(
        long id,
        long linkId,
        [FromBody] StudentGuardianUnlinkRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new RemoveGuardianLinkCommand(id, linkId, request), cancellationToken));
}
