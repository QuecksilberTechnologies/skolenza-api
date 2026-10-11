namespace Skolenza.StaffMemberAgent;

internal static class AgentConstants
{
    public const string ProductFolder = "Skolenza.StaffMemberAgent";
    public const string SettingsFileName = "agent.settings.json";
    public const string CredentialFileName = "agent.credential";
    public const string EncryptionKeyFileName = "queue.key";
    public const string QueueDatabaseFileName = "queue.db";
    public const string QueueFolderName = "queue";
    public const string AutoStartRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string AutoStartValueName = "SkolenzaStaffMemberAgent";
    public const string AgentCredentialHeader = "X-Agent-Credential";
    public const string ConfigurationRoute = "api/StaffMember/Monitoring/runtime/configuration";
    public const string HeartbeatRoute = "api/StaffMember/Monitoring/runtime/heartbeat";
    public const string CaptureRoute = "api/StaffMember/Monitoring/runtime/captures";
    public const int SyncProbeSeconds = 15;
    public const int UploadBatchSize = 10;
    public const string UploadFailedErrorCode = "UPLOAD_FAILED";
    public const string LocalQueueLimitErrorCode = "LOCAL_QUEUE_LIMIT";
}

internal sealed record AgentSettings(Uri ApiBaseUri, Guid AgentInstanceId);

internal sealed record MonitoringPolicy(
    int MinimumCaptureIntervalSeconds,
    int MaximumCaptureIntervalSeconds,
    int HeartbeatIntervalSeconds,
    int DelayedAfterSeconds,
    int UnreachableAfterSeconds,
    int OfflineRetentionDays,
    long MaximumOfflineBytes,
    int ImageQuality,
    bool CaptureAllMonitors);

internal sealed record ApiEnvelope<T>(bool IsSuccess, T? Data, string? Message);

internal sealed record QueueCapture(
    Guid CaptureId,
    DateTime CapturedAtUtc,
    int MonitorNumber,
    string EncryptedFilePath,
    string ContentType,
    long ContentLength,
    string ChecksumSha256,
    int RetryCount);
