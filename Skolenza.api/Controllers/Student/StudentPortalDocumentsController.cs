using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using skolenza.application.Common.Enums;
using skolenza.application.Constants;
using skolenza.application.DTOS.Student;
using skolenza.application.Features.StudentPortalAuthCmd;

namespace skolenza.api.Controllers.Student;

/// <summary>Provides Student and linked Guardian document access under the separate portal credential.</summary>
[ApiController]
[Authorize(Policy = StudentConstants.PortalAuthenticationPolicy)]
[Route("api/Student/portal/students/{studentId:long}/documents")]
public sealed class StudentPortalDocumentsController(IMediator mediator) : ControllerBase
{
    /// <summary>Lists active location/session-specific requirements available to this portal account.</summary>
    [HttpGet("requirements")]
    public async Task<IActionResult> GetRequirements(
        long studentId,
        [FromQuery] StudentDocumentPersonScope personScope,
        CancellationToken cancellationToken)
    {
        if (!TryGetPortalIdentity(out var institutionId, out var accountId))
        {
            return Unauthorized();
        }

        return Ok(await mediator.Send(new GetStudentPortalDocumentRequirementsQuery(
            institutionId, accountId, studentId, personScope), cancellationToken));
    }

    /// <summary>Uploads a versioned Guardian or Student document, scoped to the active portal account's linked student.</summary>
    /// <remarks>
    /// Send multipart/form-data with requirementId, personScope, guardianId (required for Guardian scope),
    /// expiryDate when required by the requirement, and file. Requires a StudentPortal bearer token.
    /// The API derives InstitutionId and PortalAccountId from signed claims and checks active enrollment,
    /// Guardian link access, requirement scope, MIME type and size before storing the file privately.
    /// </remarks>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(105_906_176)]
    [RequestFormLimits(MultipartBodyLengthLimit = 105_906_176)]
    public async Task<IActionResult> Upload(
        long studentId,
        [FromForm] StudentPortalDocumentUploadDTO request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPortalIdentity(out var institutionId, out var accountId))
        {
            return Unauthorized();
        }

        return Ok(await mediator.Send(new UploadStudentPortalDocumentCommand(
            institutionId, accountId, studentId, request), cancellationToken));
    }

    /// <summary>Returns a paged list of document versions visible to this Guardian/Student account.</summary>
    [HttpGet]
    public async Task<IActionResult> GetPage(
        long studentId,
        [FromQuery] StudentPortalDocumentFilterDTO filter,
        CancellationToken cancellationToken)
    {
        if (!TryGetPortalIdentity(out var institutionId, out var accountId))
        {
            return Unauthorized();
        }

        return Ok(await mediator.Send(new GetStudentPortalDocumentsQuery(
            institutionId, accountId, studentId, filter), cancellationToken));
    }

    /// <summary>Creates a five-minute signed URL only after verifying this account can see the requested document.</summary>
    [HttpGet("{documentId:long}/download")]
    public async Task<IActionResult> GetDownload(
        long studentId,
        long documentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetPortalIdentity(out var institutionId, out var accountId))
        {
            return Unauthorized();
        }

        return Ok(await mediator.Send(new GetStudentPortalDocumentDownloadQuery(
            institutionId, accountId, studentId, documentId), cancellationToken));
    }

    private bool TryGetPortalIdentity(out long institutionId, out long accountId)
    {
        institutionId = 0;
        accountId = 0;
        return long.TryParse(User.FindFirstValue("InstitutionId"), out institutionId) &&
            long.TryParse(User.FindFirstValue(StudentConstants.PortalAccountIdClaim), out accountId);
    }
}
