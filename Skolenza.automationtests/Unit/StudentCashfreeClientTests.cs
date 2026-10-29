using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using skolenza.application.Common.Enums;
using skolenza.application.DTOS.Student;
using skolenza.infrastructure.Billing;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StudentCashfreeProvider")]
public sealed class StudentCashfreeClientTests
{
    [Test]
    public async Task Create_order_uses_sandbox_endpoint_and_returns_provider_session()
    {
        string? capturedUri = null;
        string? capturedClientId = null;
        string? capturedBody = null;
        var handler = new RecordingHttpMessageHandler(request =>
        {
            capturedUri = request.RequestUri?.AbsoluteUri;
            capturedClientId = request.Headers.GetValues("x-client-id").Single();
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {"order_id":"S123","payment_session_id":"session-123","order_expiry_time":"2026-10-29T12:00:00Z"}
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        });
        var client = new StudentCashfreeClient(new HttpClient(handler));
        var checkout = new StudentCashfreeCheckoutDataDTO
        {
            GatewayOrderId = "S123",
            Amount = 125.50m,
            CurrencyCode = "INR",
            CurrencyDecimalPlaces = 2,
            CustomerId = "student-123",
            CustomerPhone = "9000000000"
        };

        var result = await client.CreateOrderAsync(
            StudentPaymentGatewayEnvironment.Sandbox,
            "client-id",
            "client-secret",
            checkout,
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(capturedUri, Is.EqualTo("https://sandbox.cashfree.com/pg/orders"));
            Assert.That(capturedClientId, Is.EqualTo("client-id"));
            Assert.That(result.OrderId, Is.EqualTo("S123"));
            Assert.That(result.PaymentSessionId, Is.EqualTo("session-123"));
        });
        using var json = JsonDocument.Parse(capturedBody!);
        Assert.That(json.RootElement.GetProperty("order_amount").GetDecimal(), Is.EqualTo(125.50m));
    }

    [Test]
    public void Create_order_rejects_three_decimal_currency_without_calling_provider()
    {
        var handler = new RecordingHttpMessageHandler(_ => throw new AssertionException("Provider must not be called."));
        var client = new StudentCashfreeClient(new HttpClient(handler));

        Assert.ThrowsAsync<InvalidOperationException>(() => client.CreateOrderAsync(
            StudentPaymentGatewayEnvironment.Sandbox,
            "client-id",
            "client-secret",
            new StudentCashfreeCheckoutDataDTO { CurrencyDecimalPlaces = 3 },
            Guid.NewGuid(),
            CancellationToken.None));
        Assert.That(handler.RequestCount, Is.Zero);
    }

    [Test]
    public void Webhook_signature_uses_timestamp_plus_raw_body_hmac()
    {
        const string body = "{\"data\":{}}";
        const string timestamp = "1729000000";
        const string secret = "sandbox-webhook-secret";
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(timestamp + body));
        var signature = Convert.ToBase64String(hash);
        var client = new StudentCashfreeClient(new HttpClient());

        Assert.Multiple(() =>
        {
            Assert.That(client.VerifyWebhookSignature(body, timestamp, signature, secret), Is.True);
            Assert.That(client.VerifyWebhookSignature(body + " ", timestamp, signature, secret), Is.False);
        });
    }

    private sealed class RecordingHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(responseFactory(request));
        }
    }
}
