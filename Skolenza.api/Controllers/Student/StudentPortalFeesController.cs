using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using skolenza.application.Constants;
using skolenza.application.DTOS.Student;
using skolenza.application.Features.StudentPortalAuthCmd;

namespace skolenza.api.Controllers.Student;

/// <summary>Provides a Guardian access to fee information for their linked student.</summary>
[ApiController]
[Authorize(Policy = StudentConstants.PortalAuthenticationPolicy)]
[Route("api/Student/portal/students/{studentId:long}/fees")]
public sealed class StudentPortalFeesController(IMediator mediator) : ControllerBase
{
    /// <summary>Lists approved admissions and their fee quotes for the linked student.</summary>
    [HttpGet("admissions")]
    public async Task<IActionResult> GetAdmissions(long studentId, [FromQuery] StudentPortalFeeFilterDTO filter, CancellationToken cancellationToken)
    {
        if (!TryGetPortalIdentity(out var institutionId, out var accountId))
        {
            return Unauthorized();
        }
        return Ok(await mediator.Send(new GetStudentPortalFeeAdmissionsQuery(institutionId, accountId, studentId, filter), cancellationToken));
    }

    /// <summary>Returns an admission's fee breakdown for its linked Guardian.</summary>
    [HttpGet("admissions/{admissionId:long}/quote")]
    public async Task<IActionResult> GetQuote(long studentId, long admissionId, CancellationToken cancellationToken)
    {
        if (!TryGetPortalIdentity(out var institutionId, out var accountId))
        {
            return Unauthorized();
        }
        return Ok(await mediator.Send(new GetStudentPortalFeeQuoteQuery(institutionId, accountId, studentId, admissionId), cancellationToken));
    }

    /// <summary>Returns current installments, amounts paid and outstanding balance.</summary>
    [HttpGet("admissions/{admissionId:long}/balance")]
    public async Task<IActionResult> GetBalance(long studentId, long admissionId, CancellationToken cancellationToken)
    {
        if (!TryGetPortalIdentity(out var institutionId, out var accountId))
        {
            return Unauthorized();
        }
        return Ok(await mediator.Send(new GetStudentPortalFeeBalanceQuery(institutionId, accountId, studentId, admissionId), cancellationToken));
    }

    /// <summary>Returns paged fee receipts for the specified admission.</summary>
    [HttpGet("admissions/{admissionId:long}/receipts")]
    public async Task<IActionResult> GetReceipts(long studentId, long admissionId, [FromQuery] StudentPortalFeeFilterDTO filter, CancellationToken cancellationToken)
    {
        if (!TryGetPortalIdentity(out var institutionId, out var accountId))
        {
            return Unauthorized();
        }
        return Ok(await mediator.Send(new GetStudentPortalFeeReceiptsQuery(institutionId, accountId, studentId, admissionId, filter), cancellationToken));
    }

    /// <summary>Creates or resumes a Cashfree checkout for the linked student's installment.</summary>
    [HttpPost("admissions/{admissionId:long}/installments/{installmentId:long}/cashfree-checkout")]
    public async Task<IActionResult> CreateCheckout(
        long studentId,
        long admissionId,
        long installmentId,
        [FromBody] StudentPortalCashfreeCheckoutRequestDTO request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPortalIdentity(out var institutionId, out var accountId))
        {
            return Unauthorized();
        }

        return Ok(await mediator.Send(new CreateStudentPortalCashfreeCheckoutCommand(
            institutionId, accountId, studentId, admissionId, installmentId, request), cancellationToken));
    }

    private bool TryGetPortalIdentity(out long institutionId, out long accountId)
    {
        institutionId = 0;
        accountId = 0;
        return long.TryParse(User.FindFirstValue("InstitutionId"), out institutionId) &&
            long.TryParse(User.FindFirstValue(StudentConstants.PortalAccountIdClaim), out accountId);
    }
}
