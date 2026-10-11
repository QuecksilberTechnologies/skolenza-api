using skolenza.application.Common.Enums;
using skolenza.application.DTOs.BaseDTO;
using Microsoft.AspNetCore.Http;

namespace skolenza.application.DTOS.Common;

/// <summary>One uploaded workbook/CSV or pasted table, with optional explicit column mapping.</summary>
public class BulkImportPreviewRequestDTO : PermissionRequestDTO
{
    /// <summary>Optional client-generated identifier for retrying the same preview request safely.</summary>
    public Guid? RequestId { get; set; }
    public IFormFile? File { get; set; }
    public string? PastedText { get; set; }
    public string? SheetName { get; set; }

    /// <summary>JSON object of target field to source column, for example {"DepartmentName":"Dept"}.</summary>
    public string? ColumnMappingJson { get; set; }
}

/// <summary>A bounded source table. Row numbers preserve source locations for correction.</summary>
public sealed class BulkImportTableDTO
{
    public bool IsExcel { get; set; }
    public bool Uses1904DateSystem { get; set; }
    public List<string> Columns { get; set; } = new();
    public List<BulkImportSourceRowDTO> Rows { get; set; } = new();
}

public sealed class BulkImportSourceRowDTO
{
    public int RowNumber { get; set; }
    public List<string> Values { get; set; } = new();
}

/// <summary>A saved draft preview; master records are created only after explicit confirmation.</summary>
public sealed class BulkImportPreviewResponseDTO
{
    /// <summary>StaffMember pattern snapshot; confirmation rejects changed patterns or counters.</summary>
    public string? StaffMemberPatternHash { get; set; }
    public int? ReservedStaffMemberSequence { get; set; }
    public Guid? JobId { get; set; }
    public BulkImportMaster Master { get; set; }
    public List<string> SourceColumns { get; set; } = new();
    public Dictionary<string, string> ColumnMapping { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<BulkImportPreviewRowDTO> Rows { get; set; } = new();
    public int ReadyCount => Rows.Count(row => row.Status == BulkImportRowStatus.Ready);
    public int ExistingCount => Rows.Count(row => row.Status == BulkImportRowStatus.Existing);
    public int InvalidCount => Rows.Count(row => row.Status == BulkImportRowStatus.Invalid);
    public bool IsValid => Errors.Count == 0 && Rows.Count > 0 && InvalidCount == 0;
    public bool ConfirmationAvailable { get; set; }
    public bool CanCommit => JobId.HasValue && ConfirmationAvailable && IsValid;
}

/// <summary>Acts only on saved server-side rows; confirmation accepts no replacement data.</summary>
public class BulkImportJobRequestDTO : PermissionRequestDTO
{
    /// <summary>Optional StaffMember invitation row selection. At most 100 per dispatch request.</summary>
    public List<int>? RowNumbers { get; set; }
    public Guid JobId { get; set; }
    /// <summary>Null queues now. Future UTC time queues for that time.</summary>
    public DateTimeOffset? ScheduledAtUtc { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class BulkImportJobResponseDTO
{
    public Guid JobId { get; set; }
    public BulkImportMaster Master { get; set; }
    public BulkImportJobStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? ScheduledAtUtc { get; set; }
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int CreatedCount { get; set; }
    public int ReactivatedCount { get; set; }
    public int ExistingCount { get; set; }
    public int FailedCount { get; set; }
    public string? Error { get; set; }
    public BulkImportPreviewResponseDTO? Preview { get; set; }
}

public sealed class BulkImportPreviewRowDTO
{
    /// <summary>Persistence only; removed from public previews and reports.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? CardCiphertext { get; set; }
    /// <summary>Persistence only; removed from public previews and reports.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? CardLookupHash { get; set; }
    public long? HostRecordId { get; set; }
    public string? ProposedStaffMemberCode { get; set; }
    public BulkImportInvitationStatus? InvitationStatus { get; set; }
    public string? InvitationError { get; set; }
    public DateTime? InvitationAttemptedAtUtc { get; set; }
    /// <summary>Persistence only. Public response projection removes this identity.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public long? ImportedStaffMemberId { get; set; }
    /// <summary>Persistence only. Prevents stale send attempts from overwriting newer delivery results.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public Guid? InvitationAttemptId { get; set; }
    public bool Processed { get; set; }
    public bool WillReactivate { get; set; }
    public bool WasReactivated { get; set; }
    public int RowNumber { get; set; }
    public Dictionary<string, string> Values { get; set; } = new();
    public BulkImportRowStatus Status { get; set; }
    public int? ExistingId { get; set; }
    public int? DepartmentId { get; set; }
    public long? PolicyVersionId { get; set; }
    public long? TargetStaffMemberId { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>Server-side claimed invitation; never returned from the API.</summary>
public sealed record StaffMemberImportInvitationDTO(int RowNumber, Guid AttemptId, long StaffMemberId, string Email, string FullName);
