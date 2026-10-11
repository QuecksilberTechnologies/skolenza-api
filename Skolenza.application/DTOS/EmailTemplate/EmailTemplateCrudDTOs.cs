using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOs.EmailTemplate;

/// <summary>
/// Data required to create a centrally managed email template.
/// HTML is stored as template content and is rendered only by the server-side mail flow.
/// </summary>
public sealed class CreateEmailTemplateRequestDTO
{
    public string TemplateName { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
    public string? CcEmail { get; set; }
    public string? BccEmail { get; set; }
    public string? Category { get; set; }
    public string? LanguageCode { get; set; }
    public bool IsActive { get; set; } = true;
    public PermissionRequestDTO? PermissionRequest { get; set; }
}

/// <summary>Data required to update a centrally managed email template.</summary>
public sealed class UpdateEmailTemplateRequestDTO
{
    public int Id { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
    public string? CcEmail { get; set; }
    public string? BccEmail { get; set; }
    public string? Category { get; set; }
    public string? LanguageCode { get; set; }
    public bool IsActive { get; set; }
    public PermissionRequestDTO? PermissionRequest { get; set; }
}

/// <summary>Changes only the active state of a centrally managed email template.</summary>
public sealed class UpdateEmailTemplateStatusRequestDTO
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
    public PermissionRequestDTO? PermissionRequest { get; set; }
}

/// <summary>Filters the Host email-template list and supplies its runtime permission context.</summary>
public sealed class EmailTemplateListRequestDTO : PermissionRequestDTO
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    public string? LanguageCode { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>Safe Host-facing representation of an email template and its audit metadata.</summary>
public sealed class EmailTemplateResponseDTO
{
    public int Id { get; init; }
    public string TemplateName { get; init; } = string.Empty;
    public string TemplateCode { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public string? FromEmail { get; init; }
    public string? FromName { get; init; }
    public string? CcEmail { get; init; }
    public string? BccEmail { get; init; }
    public string? Category { get; init; }
    public string? LanguageCode { get; init; }
    public bool IsActive { get; init; }
    public long AddedById { get; init; }
    public DateTime AddedDateTime { get; init; }
    public long? UpdatedById { get; init; }
    public DateTime? UpdatedDateTime { get; init; }
}
