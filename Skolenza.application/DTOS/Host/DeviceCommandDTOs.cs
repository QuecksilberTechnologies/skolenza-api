// ================================================================
// Purpose : API contracts for MQTT device command submission. The payload is
//           vendor JSON and must not contain Skolenza tracking identifiers.
// ================================================================

using System.Text.Json.Serialization;

namespace skolenza.application.DTOS.Host;

/// <summary>Submits one supported vendor command to a Institution device.</summary>
public sealed class SubmitDeviceCommandRequestDTO : InstitutionDeviceAccessRequestDTO
{
    public long InstitutionDeviceId { get; set; }
    public string CommandName { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
}

/// <summary>Returns server-side tracking information for a newly durable command.</summary>
public sealed class DeviceCommandSubmissionResponseDTO
{
    public long DeviceCommandId { get; set; }
    public Guid InternalTrackingId { get; set; }
    /// <summary>Host-only physical device identity. Omitted from Institution responses.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DeviceSerialNumber { get; set; }
    public string Status { get; set; } = string.Empty;
}
