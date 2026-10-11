// ================================================================
// Purpose : Defines safe request and response contracts for Institution SMTP configuration.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOS.Configruations;

/// <summary>
/// Carries the encrypted Host-selected Institution identifier and permission
/// metadata. Institution StaffMember callers are always restricted to the Institution in
/// their authenticated token, so their submitted InstitutionId is ignored.
/// </summary>
public class InstitutionEmailConfigAccessRequestDTO : PermissionRequestDTO
{
    public string? InstitutionId { get; set; }
}

/// <summary>Supplies the SMTP settings to create for one Institution.</summary>
public class CreateInstitutionEmailConfigRequestDTO : InstitutionEmailConfigAccessRequestDTO
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string SmtpUsername { get; set; } = string.Empty;

    /// <summary>Write-only SMTP secret. It is never returned by this API.</summary>
    public string SmtpPassword { get; set; } = string.Empty;

    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

/// <summary>Supplies updated SMTP settings for an existing Institution configuration.</summary>
public sealed class UpdateInstitutionEmailConfigRequestDTO : InstitutionEmailConfigAccessRequestDTO
{
    public int Id { get; set; }
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string SmtpUsername { get; set; } = string.Empty;

    /// <summary>When omitted, the existing SMTP secret is retained.</summary>
    public string? SmtpPassword { get; set; }

    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>Safe Institution SMTP configuration representation; it excludes all SMTP secret material.</summary>
public sealed class InstitutionEmailConfigResponseDTO
{
    public int Id { get; init; }
    public string InstitutionId { get; set; } = string.Empty;
    public string? InstitutionName { get; init; }
    public string? SmtpHost { get; init; }
    public int? SmtpPort { get; init; }
    public string? SmtpUsername { get; init; }
    public string? FromEmail { get; init; }
    public string? FromName { get; init; }
    public bool IsActive { get; init; }
    public bool HasSmtpPassword { get; init; }
}
