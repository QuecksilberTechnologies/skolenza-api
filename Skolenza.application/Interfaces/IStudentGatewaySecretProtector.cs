namespace skolenza.application.Interfaces;

/// <summary>Protects institution-owned payment gateway secrets with authenticated encryption.</summary>
public interface IStudentGatewaySecretProtector
{
    Task<string> ProtectAsync(long institutionId, string plainText, CancellationToken cancellationToken);

    Task<string> UnprotectAsync(long institutionId, string protectedValue, CancellationToken cancellationToken);
}
