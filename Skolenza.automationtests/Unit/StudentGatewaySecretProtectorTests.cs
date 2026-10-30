using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IRepositories;
using skolenza.infrastructure.EncryptionService;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StudentGatewaySecrets")]
public sealed class StudentGatewaySecretProtectorTests
{
    [Test]
    public async Task Protect_and_unprotect_round_trip_uses_versioned_authenticated_ciphertext()
    {
        var protector = CreateProtector("institution-key-a");

        var protectedValue = await protector.ProtectAsync(12, "secret-value", CancellationToken.None);
        var plainText = await protector.UnprotectAsync(12, protectedValue, CancellationToken.None);

        Assert.That(protectedValue, Does.StartWith("v1."));
        Assert.That(protectedValue, Does.Not.Contain("secret-value"));
        Assert.That(plainText, Is.EqualTo("secret-value"));
    }

    [Test]
    public async Task Unprotect_rejects_modified_ciphertext()
    {
        var protector = CreateProtector("institution-key-a");
        var parts = (await protector.ProtectAsync(12, "secret-value", CancellationToken.None)).Split('.');
        var cipher = Convert.FromBase64String(parts[3]);
        cipher[0] ^= 0x40;
        parts[3] = Convert.ToBase64String(cipher);

        Assert.CatchAsync<CryptographicException>(async () => await protector.UnprotectAsync(
            12, string.Join('.', parts), CancellationToken.None));
    }

    [Test]
    public async Task Unprotect_rejects_ciphertext_for_another_institution_key()
    {
        var protectedValue = await CreateProtector("institution-key-a")
            .ProtectAsync(12, "secret-value", CancellationToken.None);

        Assert.CatchAsync<CryptographicException>(async () => await CreateProtector("institution-key-b")
            .UnprotectAsync(12, protectedValue, CancellationToken.None));
    }

    [Test]
    public async Task Unprotect_rejects_ciphertext_replayed_for_another_institution_id()
    {
        var protector = CreateProtector("institution-key-a");
        var protectedValue = await protector.ProtectAsync(12, "secret-value", CancellationToken.None);

        Assert.CatchAsync<CryptographicException>(async () => await protector.UnprotectAsync(
            13, protectedValue, CancellationToken.None));
    }

    [Test]
    public async Task Portal_email_ciphertext_is_authenticated_and_separate_from_gateway_secrets()
    {
        var implementation = new StudentGatewaySecretProtector(new StaticInstitutionKeyResolver("institution-key-a"));
        var emailProtector = (IStudentPortalEmailProtector)implementation;
        var ciphertext = await emailProtector.ProtectAsync(12, "private-placeholder", CancellationToken.None);

        Assert.That(ciphertext, Does.Not.Contain("private-placeholder"));
        Assert.That(await emailProtector.UnprotectAsync(12, ciphertext, CancellationToken.None), Is.EqualTo("private-placeholder"));
        Assert.CatchAsync<CryptographicException>(async () => await implementation.UnprotectAsync(12, ciphertext, CancellationToken.None));
        Assert.CatchAsync<CryptographicException>(async () => await emailProtector.UnprotectAsync(13, ciphertext, CancellationToken.None));
    }

    [Test]
    public async Task Existing_gateway_ciphertext_remains_readable()
    {
        var key = SHA256.HashData(Encoding.UTF8.GetBytes("institution-key-a"));
        var nonce = new byte[12];
        var plaintext = Encoding.UTF8.GetBytes("existing-gateway-secret");
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(key, tag.Length))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag, Encoding.UTF8.GetBytes("skolenza:student-gateway-secret:12:v1"));
        }

        var legacyValue = string.Join('.', "v1", Convert.ToBase64String(nonce), Convert.ToBase64String(tag), Convert.ToBase64String(ciphertext));
        Assert.That(await CreateProtector("institution-key-a").UnprotectAsync(12, legacyValue, CancellationToken.None), Is.EqualTo("existing-gateway-secret"));
    }

    private static IStudentGatewaySecretProtector CreateProtector(string key) =>
        new StudentGatewaySecretProtector(new StaticInstitutionKeyResolver(key));

    private sealed class StaticInstitutionKeyResolver(string key) : IInstitutionKeyResolver
    {
        public Task<string> ResolveAsync(long institutionId) => Task.FromResult(key);
    }

}
