using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Student;
using skolenza.application.Features.StudentCmd;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Student;

/// <summary>Provides institution-scoped fee structures, admission quotes and recorded student payments.</summary>
[ApiController]
[Authorize]
[Route("api/student/fees")]
public sealed class StudentFeeLedgerController(IMediator mediator) : ControllerBase
{
    /// <summary>Lists fee structures by campus, academic session and required academic group.</summary>
    [HttpGet("structures")]
    public async Task<IActionResult> GetStructures([FromQuery] StudentFeeStructureFilterDTO request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetStudentFeeStructuresQuery(request), cancellationToken));

    /// <summary>Creates a fee structure and explicit due dates for every allowed installment frequency.</summary>
    [HttpPost("structures")]
    public async Task<IActionResult> CreateStructure([FromBody] StudentFeeStructureRequestDTO request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateStudentFeeStructureCommand(request), cancellationToken));

    /// <summary>Replaces an unquoted structure configuration; quoted structures are immutable.</summary>
    [HttpPut("structures/{id:long}")]
    public async Task<IActionResult> UpdateStructure(long id, [FromBody] StudentFeeStructureRequestDTO request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new UpdateStudentFeeStructureCommand(id, request), cancellationToken));

    /// <summary>Deactivates an unquoted fee structure.</summary>
    [HttpDelete("structures/{id:long}")]
    public async Task<IActionResult> DeactivateStructure(long id, [FromQuery] PermissionRequestDTO permission, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new DeactivateStudentFeeStructureCommand(id, new StudentFeeMasterDeleteDTO
        {
            ModuleId = permission.ModuleId,
            OperationId = permission.OperationId
        }), cancellationToken));

    /// <summary>Lists immutable admission fee quotes with their installment schedules.</summary>
    [HttpGet("quotes")]
    public async Task<IActionResult> GetQuotes([FromQuery] StudentFeeQuoteListRequestDTO request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetStudentFeeQuotesQuery(request), cancellationToken));

    /// <summary>Returns the immutable quote, paid totals, overdue amounts and current installment balances for an admission.</summary>
    [HttpGet("admissions/{admissionId:long}/balance")]
    public async Task<IActionResult> GetAdmissionBalance(
        long admissionId,
        [FromQuery] StudentAdmissionFeeBalanceRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetStudentAdmissionFeeBalanceQuery(admissionId, request), cancellationToken));

    /// <summary>Lists issued fee receipts, optionally filtered by admission or installment.</summary>
    [HttpGet("receipts")]
    public async Task<IActionResult> GetReceipts([FromQuery] StudentFeeReceiptListRequestDTO request, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetStudentFeeReceiptsQuery(request), cancellationToken));

    /// <summary>Creates one immutable quote for the admission, applying a single eligible scheme if selected.</summary>
    [HttpPost("admissions/{admissionId:long}/quotes")]
    public async Task<IActionResult> CreateQuote(
        long admissionId,
        [FromBody] StudentAdmissionFeeQuoteRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateStudentAdmissionFeeQuoteCommand(admissionId, request), cancellationToken));

    /// <summary>Records an institution-staff offline fee payment and issues an immutable receipt.</summary>
    [HttpPost("installments/{installmentId:long}/offline-payments")]
    public async Task<IActionResult> RecordOfflinePayment(
        long installmentId,
        [FromBody] StudentOfflinePaymentRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new RecordStudentOfflineFeePaymentCommand(installmentId, request), cancellationToken));

    /// <summary>Creates or resumes an idempotent Cashfree order for the server-calculated installment balance.</summary>
    [HttpPost("installments/{installmentId:long}/cashfree-checkout")]
    public async Task<IActionResult> CreateCashfreeCheckout(
        long installmentId,
        [FromBody] StudentCashfreeCheckoutRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateStudentCashfreeCheckoutCommand(installmentId, request), cancellationToken));

    /// <summary>Reconciles an attempt against Cashfree's server-side payment API and atomically posts success.</summary>
    /// <remarks>Requires the Student Fees Update permission because reconciliation can update payment, installment and receipt records.</remarks>
    [HttpPost("cashfree-attempts/{attemptId:long}/reconcile")]
    public async Task<IActionResult> ReconcileCashfreePayment(
        long attemptId,
        [FromBody] StudentFeePaymentStatusRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new ReconcileStudentCashfreePaymentCommand(attemptId, request), cancellationToken));

    /// <summary>Returns school-owned Cashfree tuition gateway configuration without exposing credential values.</summary>
    [HttpGet("gateway-configuration")]
    public async Task<IActionResult> GetGatewayConfiguration(
        [FromQuery] StudentPaymentGatewayConfigurationFilterDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetStudentPaymentGatewayConfigurationQuery(request), cancellationToken));

    /// <summary>Creates school-owned Cashfree tuition credentials and stores API secrets using AES-GCM.</summary>
    [HttpPost("gateway-configuration")]
    public async Task<IActionResult> CreateGatewayConfiguration(
        [FromBody] StudentPaymentGatewayConfigurationRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new CreateStudentPaymentGatewayConfigurationCommand(request), cancellationToken));

    /// <summary>Replaces school-owned Cashfree tuition credentials using the configuration version for concurrency control.</summary>
    [HttpPut("gateway-configuration")]
    public async Task<IActionResult> UpdateGatewayConfiguration(
        [FromBody] StudentPaymentGatewayConfigurationRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new UpdateStudentPaymentGatewayConfigurationCommand(request), cancellationToken));
}
