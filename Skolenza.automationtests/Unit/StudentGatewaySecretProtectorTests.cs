using System.Security.Cryptography;
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

    private static IStudentGatewaySecretProtector CreateProtector(string key) =>
        new StudentGatewaySecretProtector(new StaticInstitutionKeyResolver(key));

    private sealed class StaticInstitutionKeyResolver(string key) : IInstitutionKeyResolver
    {
        public Task<string> ResolveAsync(long institutionId) => Task.FromResult(key);
    }
}
