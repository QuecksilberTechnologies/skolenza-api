namespace skolenza.application.DTOS.Token;

/// <summary>Trusted server-side identity used to issue an isolated Student portal JWT.</summary>
public sealed class StudentPortalTokenInfoDTO
{
    public long AccountId { get; init; }
    public long InstitutionId { get; init; }
    public string NormalizedEmail { get; init; } = string.Empty;
}
