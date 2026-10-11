namespace skolenza.application.DTOs.Registration;

/// <summary>
/// Public result payload for institution self-registration.
/// </summary>
public sealed class InstitutionCreateResponseDTO
{
    public bool Success { get; set; }

    public bool? EmailSent { get; set; }

    public string Message { get; set; } = string.Empty;
}
