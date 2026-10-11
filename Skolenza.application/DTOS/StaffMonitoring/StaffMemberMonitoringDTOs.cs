using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOS.StaffMonitoring;

public sealed class UpsertStaffMemberMonitoringPolicyRequestDTO : PermissionRequestDTO
{
    public int MinimumCaptureIntervalSeconds { get; set; }
    public int MaximumCaptureIntervalSeconds { get; set; }
    public int HeartbeatIntervalSeconds { get; set; }
    public int DelayedAfterSeconds { get; set; }
    public int UnreachableAfterSeconds { get; set; }
    public int OfflineRetentionDays { get; set; }
    public long MaximumOfflineBytes { get; set; }
    public int ImageQuality { get; set; }
    public bool CaptureAllMonitors { get; set; }
}

public sealed class RegisterStaffMemberMonitoringAgentRequestDTO : PermissionRequestDTO
{
    public string StaffMemberId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
}

public sealed class StaffMemberMonitoringPermissionRequestDTO : PermissionRequestDTO;

public sealed record StaffMemberMonitoringPolicyResponseDTO(
    int MinimumCaptureIntervalSeconds,
    int MaximumCaptureIntervalSeconds,
    int HeartbeatIntervalSeconds,
    int DelayedAfterSeconds,
    int UnreachableAfterSeconds,
    int OfflineRetentionDays,
    long MaximumOfflineBytes,
    int ImageQuality,
    bool CaptureAllMonitors);

public sealed record StaffMemberMonitoringAgentRegistrationResponseDTO(
    Guid AgentInstanceId,
    string AgentCredential,
    StaffMemberMonitoringPolicyResponseDTO Policy);

public sealed record StaffMemberMonitoringAgentStatusResponseDTO(
    Guid AgentInstanceId,
    string StaffMemberId,
    string DeviceName,
    string ConnectionStatus,
    DateTime? LastHeartbeatDateTime,
    DateTime? LastCaptureDateTime,
    DateTime? LastSuccessfulUploadDateTime,
    int PendingCaptureCount,
    string? LastErrorCode,
    string? AgentVersion);

public sealed record AgentHeartbeatRequestDTO(
    string? AgentVersion,
    int PendingCaptureCount,
    DateTime? LastCaptureDateTime,
    DateTime? LastSuccessfulUploadDateTime,
    string? LastErrorCode);
