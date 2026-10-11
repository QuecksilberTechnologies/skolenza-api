// ================================================================
// Purpose : Application contracts for the durable device-command pipeline.
// ================================================================

using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IDeviceCommunication;

/// <summary>Contains the caller-owned data required to submit a device command.</summary>
public sealed record DeviceCommandSubmission(
    long InstitutionId,
    long InstitutionDeviceId,
    string CommandName,
    string Payload,
    long? RequestedById,
    int MaxAttempts = 3,
    bool ProtectPayload = false);

/// <summary>Returns only Skolenza internal tracking data; it is not sent to a device.</summary>
public sealed record DeviceCommandSubmissionResult(
    long DeviceCommandId,
    Guid InternalTrackingId,
    string DeviceSerialNumber,
    DeviceCommandStatus Status);

/// <summary>Represents a command acquired by the central dispatcher.</summary>
public sealed record DeviceCommandDispatch(
    long DeviceCommandId,
    long InstitutionId,
    long InstitutionDeviceId,
    string DeviceSerialNumber,
    string CommandName,
    string Payload,
    DeviceCommandResponseMode ResponseMode,
    int AttemptCount);

/// <summary>Represents one raw inbound device publication or polling message.</summary>
public sealed record DeviceMqttInboundMessage(
    DeviceCommunicationProtocol Transport,
    string Topic,
    string DeviceSerialNumber,
    string Payload,
    int QualityOfService,
    bool IsDuplicateDelivery,
    bool IsProtocolIdentityValid,
    DateTime ReceivedDateTime);

/// <summary>
/// Carries a raw device-initiated HTTPS poll. The bearer token lives only in the
/// device URL; the payload serial is still verified against its server mapping.
/// </summary>
public sealed record DeviceHttpsPollingRequest(
    string IngressToken,
    string Payload,
    DateTime ReceivedDateTime);

/// <summary>Represents the raw JSON response returned to a validated HTTPS device poll.</summary>
public sealed record DeviceHttpsPollingResponse(bool IsAccepted, string ResponsePayload)
{
    /// <summary>Returns the deliberately non-descriptive result used for rejected device traffic.</summary>
    public static DeviceHttpsPollingResponse Rejected { get; } = new(false, string.Empty);
}

/// <summary>Submits a validated command into the durable per-device queue.</summary>
public interface IDeviceCommandSubmissionService
{
    Task<DeviceCommandSubmissionResult> SubmitAsync(
        DeviceCommandSubmission submission,
        CancellationToken cancellationToken = default);
}

/// <summary>Coordinates generic transport queue acquisition, audit, retry, and protocol-aware response handling.</summary>
public interface IDeviceCommandDispatchStore
{
    /// <summary>
    /// Acquires one command only when its configured transport belongs to the
    /// supplied adapter set. This prevents one transport worker from dispatching
    /// another transport's command.
    /// </summary>
    Task<DeviceCommandDispatch?> TryAcquireNextAsync(
        IReadOnlyCollection<DeviceCommunicationProtocol> transports,
        long? institutionDeviceId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Moves expired response-waiting commands to their next durable retry or final failure state.</summary>
    Task RecoverExpiredResponseDeadlinesAsync(CancellationToken cancellationToken = default);

    Task MarkPublishedAsync(
        DeviceCommandDispatch dispatch,
        string topic,
        int qualityOfService,
        DateTime publishedDateTime,
        CancellationToken cancellationToken = default);

    Task ScheduleRetryOrFailAsync(
        DeviceCommandDispatch dispatch,
        string failureReason,
        DateTime failedDateTime,
        CancellationToken cancellationToken = default);

    Task RecordInboundAsync(DeviceMqttInboundMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// Publishes the next already-queued MQTTS command for one device only when an
/// authorized user explicitly requests it. It never accepts a raw command
/// payload and preserves the durable per-device queue ordering.
/// </summary>
public interface IDeviceCommandManualDispatcher
{
    Task<ManualDeviceCommandDispatchResult> DispatchNextMqttsAsync(
        long institutionDeviceId,
        CancellationToken cancellationToken = default);
}

/// <summary>Contains only non-sensitive manual-dispatch outcome data.</summary>
public sealed record ManualDeviceCommandDispatchResult(bool WasDispatched, string Message);

/// <summary>
/// Handles the HTTPS polling transport. It never opens a connection to a
/// device IP address; the physical device always initiates the request.
/// </summary>
public interface IDeviceHttpsPollingService
{
    Task<DeviceHttpsPollingResponse> ProcessAsync(
        DeviceHttpsPollingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a device poll authenticated by the short-lived Host bootstrap
    /// route. This is used only until the device has been moved to its normal
    /// Institution gateway URL.
    /// </summary>
    Task<DeviceHttpsPollingResponse> ProcessInitialAsync(
        long deviceMasterId,
        string expectedSerialNumber,
        DeviceHttpsPollingRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>Issues, validates, records, and revokes short-lived initial device gateway identities.</summary>
public interface IDeviceInitialProvisioningService
{
    /// <summary>Creates a short-lived initial gateway URL for an unassigned HTTPS-capable physical device.</summary>
    Task<DeviceInitialProvisioningIssue> IssueAsync(
        long deviceMasterId,
        int lifetimeMinutes,
        long issuedById,
        CancellationToken cancellationToken = default);

    /// <summary>Validates the serial-plus-token identity used by the initial device gateway.</summary>
    Task<DeviceInitialProvisioningValidation?> ValidateAsync(
        string deviceSerialNumber,
        string ingressToken,
        CancellationToken cancellationToken = default);

    /// <summary>Records a successful initial device connection without disclosing the token.</summary>
    Task RecordConnectionAsync(
        long provisioningId,
        DateTime connectedDateTime,
        CancellationToken cancellationToken = default);

    /// <summary>Revokes all still-active initial URLs for a physical device after normal gateway activation.</summary>
    Task RevokeForDeviceMasterAsync(
        long deviceMasterId,
        DateTime revokedDateTime,
        CancellationToken cancellationToken = default);
}

/// <summary>Contains the raw one-time URL material returned only to the Host provisioning caller.</summary>
public sealed record DeviceInitialProvisioningIssue(
    string DeviceSerialNumber,
    string InitialGatewayUrl,
    int HeartbeatIntervalSeconds,
    DateTime ExpiresDateTime);

/// <summary>Contains validated server-side initial provisioning identity data without exposing the raw token.</summary>
public sealed record DeviceInitialProvisioningValidation(
    long ProvisioningId,
    long DeviceMasterId,
    string DeviceSerialNumber,
    int HeartbeatIntervalSeconds);
