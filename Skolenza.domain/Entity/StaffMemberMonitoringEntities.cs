// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Represents institution-scoped staffMember screenshot monitoring state.
// ================================================================

namespace skolenza.domain.Entity;

public enum StaffMemberMonitoringConnectionStatus
{
    NeverConnected = 1,
    Online = 2,
    UploadFailure = 3,
    Delayed = 4,
    Unreachable = 5,
    Inactive = 6
}

/// <summary>Defines one Institution's configurable staffMember monitoring policy.</summary>
public sealed class StaffMemberMonitoringPolicy
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public int MinimumCaptureIntervalSeconds { get; set; }
    public int MaximumCaptureIntervalSeconds { get; set; }
    public int HeartbeatIntervalSeconds { get; set; }
    public int DelayedAfterSeconds { get; set; }
    public int UnreachableAfterSeconds { get; set; }
    public int OfflineRetentionDays { get; set; }
    public long MaximumOfflineBytes { get; set; }
    public int ImageQuality { get; set; }
    public bool CaptureAllMonitors { get; set; }
    public bool IsActive { get; set; }
    public bool IsSoftDeleted { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

/// <summary>Represents one installed staffMember PC monitoring agent.</summary>
public sealed class StaffMemberMonitoringAgent
{
    public long Id { get; set; }
    public Guid AgentInstanceId { get; set; }
    public long InstitutionId { get; set; }
    public long StaffMemberId { get; set; }
    public long PolicyId { get; set; }
    public string DeviceName { get; set; } = null!;
    public string CredentialHash { get; set; } = null!;
    public string? AgentVersion { get; set; }
    public DateTime? LastHeartbeatDateTime { get; set; }
    public DateTime? LastCaptureDateTime { get; set; }
    public DateTime? LastSuccessfulUploadDateTime { get; set; }
    public int PendingCaptureCount { get; set; }
    public string? LastErrorCode { get; set; }
    public bool IsActive { get; set; }
    public bool IsSoftDeleted { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public StaffMemberMonitoringPolicy Policy { get; set; } = null!;
}

/// <summary>Stores institution-resolved metadata for one uploaded screenshot.</summary>
public sealed class StaffMemberScreenCapture
{
    public long Id { get; set; }
    public Guid CaptureId { get; set; }
    public long InstitutionId { get; set; }
    public long StaffMemberId { get; set; }
    public long MonitoringAgentId { get; set; }
    public DateTime CapturedAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public int MonitorNumber { get; set; }
    public string ContentType { get; set; } = null!;
    public long ContentLength { get; set; }
    public string ChecksumSha256 { get; set; } = null!;
    public string StorageObjectKey { get; set; } = null!;
    public StaffMemberMonitoringAgent MonitoringAgent { get; set; } = null!;
}
