// ================================================================
// Purpose : Durable command, response, raw-message audit, and encrypted
//           device-credential records for the transport-neutral device stack.
// ================================================================

namespace skolenza.domain.Entity;

/// <summary>Represents one server-side command in the strict per-device queue.</summary>
public partial class DeviceCommand
{
    public long Id { get; set; }
    public Guid InternalTrackingId { get; set; }
    public long InstitutionId { get; set; }
    public long InstitutionDeviceId { get; set; }
    public long CampusId { get; set; }
    public string DeviceSerialNumber { get; set; } = null!;
    public string CommandName { get; set; } = null!;
    public string RequestPayload { get; set; } = null!;
    /// <summary>True when RequestPayload contains an encrypted wrapper rather than vendor JSON.</summary>
    public bool IsSensitivePayload { get; set; }
    public string MatchCriteria { get; set; } = "{}";
    public short Status { get; set; }
    public short ResponseMode { get; set; }
    public short AccessLevel { get; set; }
    public int AttemptCount { get; set; }
    public int MaxAttempts { get; set; }
    public DateTime? NextAttemptDateTime { get; set; }
    public DateTime? PublishedDateTime { get; set; }
    public DateTime? ResponseDeadlineDateTime { get; set; }
    public DateTime? CompletedDateTime { get; set; }
    public string? FailureReason { get; set; }
    public long? RequestedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public virtual Institution Institution { get; set; } = null!;
    public virtual InstitutionDevice InstitutionDevice { get; set; } = null!;
    public virtual Campus Campus { get; set; } = null!;
    public virtual ICollection<DeviceCommandResponse> DeviceCommandResponses { get; set; } = new List<DeviceCommandResponse>();
}

/// <summary>Preserves a parsed device acknowledgement or result without relying on invented protocol identifiers.</summary>
public partial class DeviceCommandResponse
{
    public long Id { get; set; }
    public long? DeviceCommandId { get; set; }
    public long? InstitutionId { get; set; }
    public long? InstitutionDeviceId { get; set; }
    public string DeviceSerialNumber { get; set; } = null!;
    public string ResponseCommandName { get; set; } = null!;
    public bool? Result { get; set; }
    public string? FailureReason { get; set; }
    public string ResponsePayload { get; set; } = null!;
    public DateTime ReceivedDateTime { get; set; }
    public virtual DeviceCommand? DeviceCommand { get; set; }
    public virtual Institution? Institution { get; set; }
    public virtual InstitutionDevice? InstitutionDevice { get; set; }
}

/// <summary>Stores every raw inbound or outbound device payload for diagnostic audit.</summary>
public partial class DeviceMessageLog
{
    public long Id { get; set; }
    public long? InstitutionId { get; set; }
    public long? InstitutionDeviceId { get; set; }
    public string DeviceSerialNumber { get; set; } = null!;
    public string Topic { get; set; } = null!;
    public short Direction { get; set; }
    public int QualityOfService { get; set; }
    public bool IsDuplicateDelivery { get; set; }
    public string PayloadHash { get; set; } = null!;
    public string RawPayload { get; set; } = null!;
    public DateTime OccurredDateTime { get; set; }
    public DateTime AddedDateTime { get; set; }
    public virtual Institution? Institution { get; set; }
    public virtual InstitutionDevice? InstitutionDevice { get; set; }
}

/// <summary>Stores a device-specific broker credential only in encrypted form.</summary>
public partial class DeviceCredential
{
    public long Id { get; set; }
    public long InstitutionDeviceId { get; set; }
    public short CredentialType { get; set; }
    public string? UserName { get; set; }
    public string? SecretEncrypted { get; set; }
    public string? CertificateReference { get; set; }
    public DateTime? ExpiresDateTime { get; set; }
    public bool IsActive { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public virtual InstitutionDevice InstitutionDevice { get; set; } = null!;
}
