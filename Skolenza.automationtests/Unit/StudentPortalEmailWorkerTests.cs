using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using skolenza.application.Constants;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IEmail;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.infrastructure.BackgroundJob;
using skolenza.infrastructure.EncryptionService;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StudentPortalEmailWorker")]
public sealed class StudentPortalEmailWorkerTests
{
    [TestCase(true, TestName = "Worker decrypts a protected Student portal email")]
    [TestCase(false, TestName = "Worker preserves existing plaintext email queue compatibility")]
    public async Task Worker_sends_decrypted_or_legacy_placeholders(bool encrypted)
    {
        var protector = (IStudentPortalEmailProtector)new StudentGatewaySecretProtector(new TestKeyResolver());
        var values = new Dictionary<string, string> { ["InvitationUrl"] = "https://example.test/invitation?token=synthetic" };
        var payload = JsonSerializer.Serialize(values);
        if (encrypted)
        {
            var protectedValue = await protector.ProtectAsync(7, payload, CancellationToken.None);
            payload = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                [StudentConstants.PortalEmailProtectedPayloadKey] = protectedValue
            });
        }

        var queue = new TestQueue(new EmailQueue
        {
            Id = 1,
            InstitutionId = 7,
            TemplateCode = StudentConstants.PortalInvitationTemplateCode,
            ToEmail = "guardian@example.test",
            PlaceholdersJson = payload
        });
        var mail = new TestEmailService();
        using var provider = new ServiceCollection()
            .AddSingleton<IEmailQueueRepository>(queue)
            .AddSingleton<IEmailService>(mail)
            .AddSingleton(protector)
            .BuildServiceProvider();
        var worker = new EmailQueueWorker(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<EmailQueueWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        try
        {
            await queue.Completed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }

        Assert.That(queue.Sent, Is.True);
        Assert.That(mail.Received, Is.Not.Null);
        Assert.That(mail.Received!["InvitationUrl"], Is.EqualTo(values["InvitationUrl"]));
    }

    [Test]
    public async Task Worker_rejects_tampered_ciphertext_without_sending()
    {
        var protector = (IStudentPortalEmailProtector)new StudentGatewaySecretProtector(new TestKeyResolver());
        var encrypted = await protector.ProtectAsync(7, "{\"InvitationUrl\":\"https://example.test\"}", CancellationToken.None);
        var parts = encrypted.Split('.');
        var ciphertext = Convert.FromBase64String(parts[3]);
        ciphertext[0] ^= 0x40;
        parts[3] = Convert.ToBase64String(ciphertext);
        var queue = new TestQueue(new EmailQueue
        {
            Id = 1,
            InstitutionId = 7,
            TemplateCode = StudentConstants.PortalInvitationTemplateCode,
            ToEmail = "guardian@example.test",
            PlaceholdersJson = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                [StudentConstants.PortalEmailProtectedPayloadKey] = string.Join('.', parts)
            })
        });
        var mail = new TestEmailService();
        using var provider = new ServiceCollection()
            .AddSingleton<IEmailQueueRepository>(queue)
            .AddSingleton<IEmailService>(mail)
            .AddSingleton(protector)
            .BuildServiceProvider();
        var worker = new EmailQueueWorker(provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<EmailQueueWorker>.Instance);
        await worker.StartAsync(CancellationToken.None);
        try
        {
            await queue.Completed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }

        Assert.That(queue.Sent, Is.False);
        Assert.That(queue.Failed, Is.True);
        Assert.That(mail.Received, Is.Null);
    }

    private sealed class TestKeyResolver : IInstitutionKeyResolver
    {
        public Task<string> ResolveAsync(long institutionId) => Task.FromResult("synthetic-test-key");
    }

    private sealed class TestQueue(EmailQueue item) : IEmailQueueRepository
    {
        private bool claimed;
        public bool Sent { get; private set; }
        public bool Failed { get; private set; }
        public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<int?> EnqueueAsync(string templateCode, string toEmail, long? institutionId, string placeholdersJson, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public async Task<EmailQueue?> ClaimNextAsync(int maximumRetries, DateTime staleBeforeUtc, CancellationToken cancellationToken = default)
        {
            if (!claimed)
            {
                claimed = true;
                return item;
            }

            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return null;
        }

        public Task MarkSentAsync(int id, DateTime sentUtc, CancellationToken cancellationToken = default)
        {
            Sent = true;
            Completed.TrySetResult();
            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(int id, string errorMessage, CancellationToken cancellationToken = default)
        {
            Failed = true;
            Completed.TrySetResult();
            return Task.CompletedTask;
        }
    }

    private sealed class TestEmailService : IEmailService
    {
        public Dictionary<string, string>? Received { get; private set; }

        public Task<bool> SendTemplatedEmailAsync(string templateCode, string toEmail, long? institutionId, Dictionary<string, string> placeholders)
        {
            Received = placeholders;
            return Task.FromResult(true);
        }

        public Task<bool> SendOtpEmailAsync(string toEmail, string subject, string body, long? institutionId, string otp) =>
            throw new NotSupportedException();

        public Task<bool> SendTemplatedEmailUsingHostConfigAsync(string templateCode, string toEmail, long? institutionId, Dictionary<string, string> placeholders) =>
            throw new NotSupportedException();

        public Task<int?> QueueTemplatedEmailAsync(string templateCode, string toEmail, long? institutionId, Dictionary<string, string> placeholders) =>
            throw new NotSupportedException();
    }
}
