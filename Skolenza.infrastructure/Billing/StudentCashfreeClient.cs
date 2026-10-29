using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using skolenza.application.Common.Enums;
using skolenza.application.Constants;
using skolenza.application.DTOS.Student;
using skolenza.application.Interfaces;

namespace skolenza.infrastructure.Billing;

public sealed class StudentCashfreeClient(HttpClient httpClient) : IStudentCashfreeClient
{
    public async Task<StudentCashfreeProviderOrderDTO> CreateOrderAsync(
        StudentPaymentGatewayEnvironment environment,
        string clientId,
        string clientSecret,
        StudentCashfreeCheckoutDataDTO checkout,
        Guid idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (checkout.CurrencyDecimalPlaces == 3)
        {
            throw new InvalidOperationException("Cashfree checkout does not support 3-decimal currencies.");
        }

        using var request = CreateRequest(
            HttpMethod.Post,
            environment,
            "pg/orders",
            clientId,
            clientSecret);
        request.Headers.Add("x-idempotency-key", idempotencyKey.ToString("D"));
        request.Content = JsonContent.Create(new
        {
            order_id = checkout.GatewayOrderId,
            order_amount = checkout.Amount,
            order_currency = checkout.CurrencyCode,
            customer_details = new
            {
                customer_id = checkout.CustomerId,
                customer_name = checkout.CustomerName,
                customer_email = checkout.CustomerEmail,
                customer_phone = checkout.CustomerPhone
            }
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = json.RootElement;
        var orderId = ReadString(root, "order_id");
        var sessionId = ReadString(root, "payment_session_id");
        if (!string.Equals(orderId, checkout.GatewayOrderId, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(sessionId))
        {
            throw new HttpRequestException("Cashfree returned an invalid order response.");
        }

        return new StudentCashfreeProviderOrderDTO
        {
            OrderId = orderId,
            PaymentSessionId = sessionId,
            ExpiresAt = ReadDateTimeOffset(root, "order_expiry_time")
        };
    }

    public async Task<IReadOnlyList<StudentCashfreePaymentDetailsDTO>> GetPaymentsAsync(
        StudentPaymentGatewayEnvironment environment,
        string clientId,
        string clientSecret,
        string orderId,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(
            HttpMethod.Get,
            environment,
            $"pg/orders/{Uri.EscapeDataString(orderId)}/payments",
            clientId,
            clientSecret);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (json.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new HttpRequestException("Cashfree returned an invalid payment list response.");
        }

        return json.RootElement.EnumerateArray().Select(ParsePayment).ToArray();
    }

    public async Task<StudentCashfreePaymentDetailsDTO> GetPaymentAsync(
        StudentPaymentGatewayEnvironment environment,
        string clientId,
        string clientSecret,
        string orderId,
        string paymentId,
        CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get,
            environment,
            $"pg/orders/{Uri.EscapeDataString(orderId)}/payments/{Uri.EscapeDataString(paymentId)}",
            clientId,
            clientSecret);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return ParsePayment(json.RootElement);
    }

    public bool VerifyWebhookSignature(string rawBody, string timestamp, string signature, string webhookSecret)
    {
        if (string.IsNullOrWhiteSpace(rawBody) || string.IsNullOrWhiteSpace(timestamp) ||
            string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(webhookSecret))
        {
            return false;
        }

        var payload = Encoding.UTF8.GetBytes(timestamp + rawBody);
        var secret = Encoding.UTF8.GetBytes(webhookSecret);
        var signatureBytes = HMACSHA256.HashData(secret, payload);
        var expected = Convert.ToBase64String(signatureBytes);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(signature));
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        StudentPaymentGatewayEnvironment environment,
        string path,
        string clientId,
        string clientSecret)
    {
        var request = new HttpRequestMessage(method, new Uri(ResolveBaseAddress(environment), path));
        request.Headers.Add("x-api-version", StudentConstants.CashfreeApiVersion);
        request.Headers.Add("x-client-id", clientId);
        request.Headers.Add("x-client-secret", clientSecret);
        return request;
    }

    private static Uri ResolveBaseAddress(StudentPaymentGatewayEnvironment environment) => environment switch
    {
        StudentPaymentGatewayEnvironment.Sandbox => new Uri("https://sandbox.cashfree.com/"),
        StudentPaymentGatewayEnvironment.Production => new Uri("https://api.cashfree.com/"),
        _ => throw new InvalidOperationException("The configured Cashfree environment is invalid.")
    };

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Cashfree request failed with HTTP {(int)response.StatusCode}.",
                null,
                response.StatusCode);
        }

        await Task.CompletedTask;
    }

    private static StudentCashfreePaymentDetailsDTO ParsePayment(JsonElement json)
    {
        return new StudentCashfreePaymentDetailsDTO
        {
            OrderId = ReadString(json, "order_id"),
            PaymentId = ReadString(json, "cf_payment_id"),
            Status = ReadString(json, "payment_status"),
            Amount = ReadDecimal(json, "payment_amount"),
            CurrencyCode = ReadString(json, "payment_currency", "order_currency"),
            BankReference = ReadOptionalString(json, "bank_reference"),
            PaidAt = ReadDateTimeOffset(json, "payment_completion_time") ?? ReadDateTimeOffset(json, "payment_time")
        };
    }

    private static string ReadString(JsonElement json, params string[] names)
    {
        foreach (var name in names)
        {
            if (json.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static string? ReadOptionalString(JsonElement json, string name) =>
        json.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static decimal ReadDecimal(JsonElement json, string name) =>
        json.TryGetProperty(name, out var value) && value.TryGetDecimal(out var amount)
            ? amount
            : 0m;

    private static DateTimeOffset? ReadDateTimeOffset(JsonElement json, string name) =>
        json.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String &&
        DateTimeOffset.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result)
            ? result
            : null;
}
