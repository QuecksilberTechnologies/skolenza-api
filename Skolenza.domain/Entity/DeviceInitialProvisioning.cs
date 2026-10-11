// ================================================================
// Purpose : Holds the short-lived, Host-issued bootstrap identity used before
//           a physical device is assigned to a Institution.
// ================================================================

namespace skolenza.domain.Entity;

/// <summary>
/// Represents a revocable, short-lived bootstrap route for one physical device.
/// The raw route token is never persisted; only its SHA-256 hash is retained.
/// </summary>
public class DeviceInitialProvisioning
{
    public long Id { get; set; }
    public long DeviceMasterId { get; set; }
    public string IngressTokenHash { get; set; } = null!;
    public int HeartbeatIntervalSeconds { get; set; }
    public DateTime ExpiresDateTime { get; set; }
    public DateTime? FirstConnectedDateTime { get; set; }
    public DateTime? LastConnectedDateTime { get; set; }
    public DateTime? RevokedDateTime { get; set; }
    public long IssuedById { get; set; }
    public DateTime IssuedDateTime { get; set; }
    public long? RevokedById { get; set; }
    public virtual DeviceMaster DeviceMaster { get; set; } = null!;
}
