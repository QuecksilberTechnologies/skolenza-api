namespace skolenza.application.Interfaces;

/// <summary>Protects institution-owned Student portal email placeholders at rest.</summary>
public interface IStudentPortalEmailProtector
{
    Task<string> ProtectAsync(long institutionId, string plainText, CancellationToken cancellationToken);

    Task<string> UnprotectAsync(long institutionId, string protectedValue, CancellationToken cancellationToken);
}
