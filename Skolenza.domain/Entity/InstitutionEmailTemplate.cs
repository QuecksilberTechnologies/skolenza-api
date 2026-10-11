namespace skolenza.domain.Entity;

public sealed class InstitutionEmailTemplate
{
    public int Id { get; set; }
    public long InstitutionId { get; set; }
    public string TemplateName { get; set; } = null!;
    public string? TemplateCode { get; set; }
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
    public string? CcEmail { get; set; }
    public string? BccEmail { get; set; }
    public string? Category { get; set; }
    public string? LanguageCode { get; set; }
    public bool IsActive { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public string? AddedFromIp { get; set; }
    public string? UpdatedFromIp { get; set; }
    public Institution Institution { get; set; } = null!;
}
