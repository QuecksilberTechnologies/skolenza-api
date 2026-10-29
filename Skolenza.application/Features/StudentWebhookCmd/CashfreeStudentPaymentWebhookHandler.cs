using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using skolenza.application.Common.Enums;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IRepositories;

namespace skolenza.application.Features.StudentWebhookCmd;

public sealed record ProcessCashfreeStudentPaymentWebhookCommand(
    string RawBody,
    string Timestamp,
    string Signature,
    string? EventId) : IRequest<bool>;

public sealed class ProcessCashfreeStudentPaymentWebhookCommandHandler(
    IStudentFeeLedgerRepository feeRepository,
    IStudentPaymentGatewayConfigurationRepository configurationRepository,
    IStudentGatewaySecretProtector secretProtector,
    IStudentCashfreeClient cashfreeClient)
    : IRequestHandler<ProcessCashfreeStudentPaymentWebhookCommand, bool>
{
    public async Task<bool> Handle(
        ProcessCashfreeStudentPaymentWebhookCommand request,
        CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(request.RawBody);
        var root = document.RootElement;
        var data = GetProperty(root, "data");
        var order = GetProperty(data, "order");
        var payment = GetProperty(data, "payment");
        var orderId = ReadString(order, "order_id");
        var paymentId = ReadString(payment, "cf_payment_id");
        var eventType = ReadString(root, "type");
        if (string.IsNullOrWhiteSpace(orderId) || string.IsNullOrWhiteSpace(paymentId))
        {
            throw new ValidationErrorException("Cashfree webhook does not contain an order ID and payment ID.");
        }

        var attempt = await feeRepository.GetCashfreeAttemptByOrderAsync(orderId, cancellationToken);
        if (attempt.CurrencyDecimalPlaces == 3)
        {
            throw new ConflictException("Cashfree webhook cannot settle an attempt denominated in a 3-decimal currency.");
        }

        var configuration = await configurationRepository.GetCredentialsAsync(attempt.InstitutionId, cancellationToken)
            ?? throw new NotFoundException("Active Cashfree tuition gateway configuration was not found.");
        var webhookSecret = await secretProtector.UnprotectAsync(
            attempt.InstitutionId, configuration.ProtectedWebhookSecret, cancellationToken);
        if (!cashfreeClient.VerifyWebhookSignature(request.RawBody, request.Timestamp, request.Signature, webhookSecret))
        {
            throw new UnauthorizedAccessException("Cashfree webhook signature verification failed.");
        }

        var clientSecret = await secretProtector.UnprotectAsync(
            attempt.InstitutionId, configuration.ProtectedClientSecret, cancellationToken);
        var verifiedPayment = await cashfreeClient.GetPaymentAsync(
            (StudentPaymentGatewayEnvironment)configuration.Environment,
            configuration.ClientId,
            clientSecret,
            orderId,
            paymentId,
            cancellationToken);
        var suppliedEventId = request.EventId?.Trim();
        var eventId = string.IsNullOrWhiteSpace(suppliedEventId)
            ? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Timestamp + request.RawBody)))
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(suppliedEventId)));
        var payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.RawBody)));
        await feeRepository.RecordCashfreePaymentAsync(
            attempt.PaymentAttemptId,
            verifiedPayment,
            eventId,
            string.IsNullOrWhiteSpace(eventType) ? "UNKNOWN" : eventType,
            payloadHash,
            cancellationToken);
        return true;
    }

    private static JsonElement GetProperty(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value)
            ? value
            : default;

    private static string ReadString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
}
