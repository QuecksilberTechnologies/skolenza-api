using System.Security.Cryptography;
using System.Text;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IRepositories;

namespace skolenza.infrastructure.EncryptionService;

/// <summary>Uses the institution's existing active encryption key with AES-GCM for gateway secrets.</summary>
public sealed class StudentGatewaySecretProtector(IInstitutionKeyResolver institutionKeyResolver)
    : IStudentGatewaySecretProtector, IStudentPortalEmailProtector
{
    private const string FormatPrefix = "v1";
    private const string GatewaySecretPurpose = "student-gateway-secret";
    private const string PortalEmailPurpose = "student-portal-email";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public async Task<string> ProtectAsync(long institutionId, string plainText, CancellationToken cancellationToken)
        => await ProtectCoreAsync(institutionId, plainText, GatewaySecretPurpose, cancellationToken);

    async Task<string> IStudentPortalEmailProtector.ProtectAsync(long institutionId, string plainText, CancellationToken cancellationToken)
        => await ProtectCoreAsync(institutionId, plainText, PortalEmailPurpose, cancellationToken);

    private async Task<string> ProtectCoreAsync(long institutionId, string plainText, string purpose, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);
        var key = await ResolveKeyAsync(institutionId);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[TagSize];
        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag, BuildAssociatedData(institutionId, purpose));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plainBytes);
        }

        return string.Join('.', FormatPrefix,
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(tag),
            Convert.ToBase64String(cipherBytes));
    }

    public async Task<string> UnprotectAsync(long institutionId, string protectedValue, CancellationToken cancellationToken)
        => await UnprotectCoreAsync(institutionId, protectedValue, GatewaySecretPurpose, cancellationToken);

    async Task<string> IStudentPortalEmailProtector.UnprotectAsync(long institutionId, string protectedValue, CancellationToken cancellationToken)
        => await UnprotectCoreAsync(institutionId, protectedValue, PortalEmailPurpose, cancellationToken);

    private async Task<string> UnprotectCoreAsync(long institutionId, string protectedValue, string purpose, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedValue);
        var parts = protectedValue.Split('.');
        if (parts.Length != 4 || !string.Equals(parts[0], FormatPrefix, StringComparison.Ordinal))
        {
            throw new CryptographicException("The gateway secret has an unsupported protection format.");
        }

        byte[] nonce;
        byte[] tag;
        byte[] cipherBytes;
        try
        {
            nonce = Convert.FromBase64String(parts[1]);
            tag = Convert.FromBase64String(parts[2]);
            cipherBytes = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException exception)
        {
            throw new CryptographicException("The gateway secret is malformed.", exception);
        }

        if (nonce.Length != NonceSize || tag.Length != TagSize)
        {
            throw new CryptographicException("The gateway secret is malformed.");
        }

        var key = await ResolveKeyAsync(institutionId);
        var plainBytes = new byte[cipherBytes.Length];
        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes, BuildAssociatedData(institutionId, purpose));
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (AuthenticationTagMismatchException exception)
        {
            throw new CryptographicException("The gateway secret could not be authenticated.", exception);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plainBytes);
        }
    }

    private async Task<byte[]> ResolveKeyAsync(long institutionId)
    {
        if (institutionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(institutionId));
        }

        var institutionKey = await institutionKeyResolver.ResolveAsync(institutionId);
        if (string.IsNullOrWhiteSpace(institutionKey))
        {
            throw new CryptographicException("An active institution encryption key is required.");
        }

        return SHA256.HashData(Encoding.UTF8.GetBytes(institutionKey));
    }

    private static byte[] BuildAssociatedData(long institutionId, string purpose) =>
        Encoding.UTF8.GetBytes($"skolenza:{purpose}:{institutionId}:v1");
}
