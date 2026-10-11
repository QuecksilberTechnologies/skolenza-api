using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOs.EmailTemplate;

public class CreateInstitutionEmailTemplateRequestDTO
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

public sealed class UpdateInstitutionEmailTemplateRequestDTO : CreateInstitutionEmailTemplateRequestDTO
{
    public int Id { get; set; }
}

public sealed class UpdateInstitutionEmailTemplateStatusRequestDTO
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
    public PermissionRequestDTO? PermissionRequest { get; set; }
}

public sealed class SyncInstitutionEmailTemplatesRequestDTO
{
    public PermissionRequestDTO? PermissionRequest { get; set; }
}

public sealed class SyncInstitutionEmailTemplatesResponseDTO
{
    public int ActiveDefaultTemplateCount { get; init; }
    public int ExistingInstitutionTemplateCount { get; init; }
    public int InsertedTemplateCount { get; init; }
    public IReadOnlyList<string> InsertedTemplateCodes { get; init; } = [];
}

public sealed class InstitutionEmailTemplateListRequestDTO : PermissionRequestDTO
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    public string? LanguageCode { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public sealed class InstitutionEmailTemplateResponseDTO
{
    public int Id { get; init; }
    public long InstitutionId { get; init; }
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
