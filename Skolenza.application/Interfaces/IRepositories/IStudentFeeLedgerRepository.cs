using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Student;

namespace skolenza.application.Interfaces.IRepositories;

public interface IStudentFeeLedgerRepository
{
    Task<PagedResponseDTO<StudentFeeStructureResponseDTO>> GetStructuresAsync(
        long institutionId,
        StudentFeeStructureFilterDTO filter,
        CancellationToken cancellationToken);

    Task<StudentFeeStructureResponseDTO> SaveStructureAsync(
        long? id,
        long institutionId,
        long actorId,
        StudentFeeStructureRequestDTO request,
        CancellationToken cancellationToken);

    Task DeactivateStructureAsync(long id, long institutionId, long actorId, CancellationToken cancellationToken);

    Task<StudentAdmissionFeeQuoteResponseDTO> CreateQuoteAsync(
        long admissionId,
        long institutionId,
        long actorId,
        StudentAdmissionFeeQuoteRequestDTO request,
        CancellationToken cancellationToken);

    Task<PagedResponseDTO<StudentAdmissionFeeQuoteResponseDTO>> GetQuotesAsync(
        long institutionId,
        StudentFeeQuoteListRequestDTO filter,
        CancellationToken cancellationToken);

    Task<StudentAdmissionFeeBalanceResponseDTO> GetAdmissionBalanceAsync(
        long admissionId,
        long institutionId,
        CancellationToken cancellationToken);

    Task<PagedResponseDTO<StudentFeeReceiptResponseDTO>> GetReceiptsAsync(
        long institutionId,
        StudentFeeReceiptListRequestDTO filter,
        CancellationToken cancellationToken);

    Task<StudentFeePaymentResponseDTO> RecordOfflinePaymentAsync(
        long installmentId,
        long institutionId,
        long actorId,
        StudentOfflinePaymentRequestDTO request,
        CancellationToken cancellationToken);

    Task<StudentCashfreeCheckoutDataDTO> PrepareCashfreeCheckoutAsync(
        long installmentId,
        long institutionId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<StudentCashfreeCheckoutResponseDTO> SaveCashfreeOrderAsync(
        long attemptId,
        long institutionId,
        string paymentSessionId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);

    Task<StudentCashfreeCheckoutDataDTO> GetCashfreeAttemptAsync(
        long attemptId,
        long institutionId,
        CancellationToken cancellationToken);

    Task<StudentCashfreeCheckoutDataDTO> GetCashfreeAttemptByOrderAsync(
        string gatewayOrderId,
        CancellationToken cancellationToken);

    Task<StudentCashfreePaymentStatusResponseDTO> RecordCashfreePaymentAsync(
        long attemptId,
        StudentCashfreePaymentDetailsDTO payment,
        string? webhookEventId,
        string? webhookEventType,
        string? webhookPayloadSha256,
        CancellationToken cancellationToken);
}
