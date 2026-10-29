using skolenza.application.Common.Enums;
using skolenza.application.DTOS.Student;

namespace skolenza.application.Interfaces;

public interface IStudentCashfreeClient
{
    Task<StudentCashfreeProviderOrderDTO> CreateOrderAsync(
        StudentPaymentGatewayEnvironment environment,
        string clientId,
        string clientSecret,
        StudentCashfreeCheckoutDataDTO checkout,
        Guid idempotencyKey,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<StudentCashfreePaymentDetailsDTO>> GetPaymentsAsync(
        StudentPaymentGatewayEnvironment environment,
        string clientId,
        string clientSecret,
        string orderId,
        CancellationToken cancellationToken);

    Task<StudentCashfreePaymentDetailsDTO> GetPaymentAsync(
        StudentPaymentGatewayEnvironment environment,
        string clientId,
        string clientSecret,
        string orderId,
        string paymentId,
        CancellationToken cancellationToken);

    bool VerifyWebhookSignature(string rawBody, string timestamp, string signature, string webhookSecret);
}

public sealed class StudentCashfreeProviderOrderDTO
{
    public string OrderId { get; set; } = string.Empty;
    public string PaymentSessionId { get; set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; set; }
}
