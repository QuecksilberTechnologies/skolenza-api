using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using NUnit.Framework;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IRepositories;
using skolenza.infrastructure.CacheMemory;
using skolenza.infrastructure.EncryptionService;

namespace skolenza.automationtests.Unit;

/// <summary>Characterizes existing compatibility hazards; it does not authorize or implement key rotation.</summary>
[TestFixture]
[Category("StudentInstitutionKeyRotationCharacterization")]
public sealed class StudentInstitutionKeyRotationCharacterizationTests
{
    [Test]
    public async Task Existing_portal_and_gateway_envelopes_require_the_original_key()
    {
        var resolver = new MutableKeyResolver { Key = "synthetic-old-key" };
        var implementation = new StudentGatewaySecretProtector(resolver);
        var email = (IStudentPortalEmailProtector)implementation;
        var gatewayValue = await implementation.ProtectAsync(11, "synthetic-gateway-value", CancellationToken.None);
        var emailValue = await email.ProtectAsync(11, "synthetic-email-placeholder", CancellationToken.None);

        resolver.Key = "synthetic-new-key";
        Assert.CatchAsync<CryptographicException>(async () =>
            await implementation.UnprotectAsync(11, gatewayValue, CancellationToken.None));
        Assert.CatchAsync<CryptographicException>(async () =>
            await email.UnprotectAsync(11, emailValue, CancellationToken.None));

        resolver.Key = "synthetic-old-key";
        Assert.That(await implementation.UnprotectAsync(11, gatewayValue, CancellationToken.None), Is.EqualTo("synthetic-gateway-value"));
        Assert.That(await email.UnprotectAsync(11, emailValue, CancellationToken.None), Is.EqualTo("synthetic-email-placeholder"));
    }

    [Test]
    public void Legacy_bank_ciphertext_has_no_authentication_and_cannot_safely_select_a_key_by_decrypt_success()
    {
        var encryption = new AesEncryptionService();
        const string original = "501234567890123456";
        var encrypted = encryption.Encrypt(original, "synthetic-bank-key");
        var bytes = Convert.FromBase64String(encrypted);
        bytes[0] ^= 1;

        var modified = encryption.Decrypt(Convert.ToBase64String(bytes), "synthetic-bank-key");
        Assert.That(modified, Is.Not.EqualTo(original));
        Assert.That(modified.Length, Is.EqualTo(original.Length));
        Assert.That(encryption.Decrypt(encrypted, "synthetic-bank-key"), Is.EqualTo(original));
    }

    [Test]
    public void Numeric_identifiers_remain_stable_across_institution_keys_in_current_implementation()
    {
        var encoder = new IdEncoderService();
        var first = encoder.EncodeId_long(1234, "synthetic-old-key");
        var second = encoder.EncodeId_long(1234, "synthetic-new-key");

        Assert.That(second, Is.EqualTo(first));
        Assert.That(encoder.DecodeId_long(first, "synthetic-new-key"), Is.EqualTo(1234));
    }

    [Test]
    public void Removing_a_cached_key_in_one_process_does_not_invalidate_another_process()
    {
        using var firstMemory = new MemoryCache(new MemoryCacheOptions());
        using var secondMemory = new MemoryCache(new MemoryCacheOptions());
        var first = new InstitutionKeyMemoryCache(firstMemory);
        var second = new InstitutionKeyMemoryCache(secondMemory);
        first.Set(11, "synthetic-old-key");
        second.Set(11, "synthetic-old-key");

        first.Remove(11);

        Assert.That(first.Get(11), Is.Null);
        Assert.That(second.Get(11), Is.EqualTo("synthetic-old-key"));
    }

    private sealed class MutableKeyResolver : IInstitutionKeyResolver
    {
        public string Key { get; set; } = string.Empty;

        public Task<string> ResolveAsync(long institutionId)
        {
            return Task.FromResult(Key);
        }
    }
}
