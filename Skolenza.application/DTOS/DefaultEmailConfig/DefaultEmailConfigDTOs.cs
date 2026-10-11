using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOs.DefaultEmailConfig;

/// <summary>
/// Request contract used by Host users to create the SMTP configuration copied into a newly registered Institution.
/// </summary>
public sealed class CreateDefaultEmailConfigRequestDTO
{
    public string ConfigName { get; set; } = string.Empty;
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string SmtpUsername { get; set; } = string.Empty;

    /// <summary>Write-only SMTP secret. It is never returned by this API.</summary>
    public string SmtpPassword { get; set; } = string.Empty;

    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public PermissionRequestDTO? PermissionRequest { get; set; }
}

/// <summary>
/// Request contract used by Host users to change a default SMTP configuration.
/// An omitted SMTP password keeps the current stored secret unchanged.
/// </summary>
public sealed class UpdateDefaultEmailConfigRequestDTO
{
    public int Id { get; set; }
    public string ConfigName { get; set; } = string.Empty;
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string SmtpUsername { get; set; } = string.Empty;

    /// <summary>Optional write-only SMTP secret. It is never returned by this API.</summary>
    public string? SmtpPassword { get; set; }

    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public PermissionRequestDTO? PermissionRequest { get; set; }
}

/// <summary>
/// Safe Host-facing representation of a default SMTP configuration. SMTP credentials are intentionally excluded.
/// </summary>
public sealed class DefaultEmailConfigResponseDTO
{
    public int Id { get; init; }
    public string ConfigName { get; init; } = string.Empty;
    public string SmtpHost { get; init; } = string.Empty;
    public int SmtpPort { get; init; }
    public string SmtpUsername { get; init; } = string.Empty;
    public string FromEmail { get; init; } = string.Empty;
    public string FromName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsDefault { get; init; }
    public bool HasSmtpPassword { get; init; }
    public DateTime CreatedDateTime { get; init; }
    public DateTime? UpdatedDateTime { get; init; }
}
