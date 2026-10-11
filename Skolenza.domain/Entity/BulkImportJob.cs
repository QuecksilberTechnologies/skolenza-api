namespace skolenza.domain.Entity;

/// <summary>Durable import snapshot and cursor. Business inserts and cursor updates commit together.</summary>
public sealed class BulkImportJob
{
    public Guid Id { get; set; }
    public long InstitutionId { get; set; }
    public long ActorId { get; set; }
    public int RoleId { get; set; }
    public int ModuleId { get; set; }
    public int OperationId { get; set; }
    public int Master { get; set; }
    public int Status { get; set; }
    public string PreviewJson { get; set; } = "{}";
    public string InputHash { get; set; } = string.Empty;
    public int NextRow { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? ScheduledAtUtc { get; set; }
    public string? Error { get; set; }
}
