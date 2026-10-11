// ================================================================
// Purpose : Defines the durable device communication lifecycle.
// ================================================================

namespace skolenza.domain.Entity;

/// <summary>Identifies the transport that carries a Institution device command.</summary>
public enum DeviceCommunicationProtocol : short
{
    /// <summary>MQTT over TCP.</summary>
    Mqtt = 1,

    /// <summary>MQTT over TLS.</summary>
    Mqtts = 2,

    /// <summary>Device-initiated HTTP polling. It is retained for LAN-only legacy devices.</summary>
    Http = 3,

    /// <summary>
    /// Device-initiated HTTPS polling. The device opens an outbound TLS connection
    /// and receives at most one queued command in the HTTP response.
    /// </summary>
    Https = 4,

    /// <summary>Unencrypted WebSocket. It is retained for LAN-only legacy devices.</summary>
    WebSocket = 5,

    /// <summary>WebSocket over TLS for an authenticated long-lived device connection.</summary>
    WebSocketSecure = 6
}

/// <summary>Represents the durable server-side state of a device command.</summary>
public enum DeviceCommandStatus : short
{
    Queued = 1,
    Publishing = 2,
    AwaitingResponse = 3,
    Completed = 4,
    Failed = 5,
    RetryScheduled = 6,
    Cancelled = 7
}

/// <summary>Defines whether a command expects a vendor-protocol response.</summary>
public enum DeviceCommandResponseMode : short
{
    Required = 1,
    Streamed = 2,
    PublishOnly = 3
}

/// <summary>Classifies the authorization risk of a vendor command.</summary>
public enum DeviceCommandAccessLevel : short
{
    InstitutionPermission = 1,
    InstitutionAccessControlPermission = 2,
    HostOnly = 3
}

/// <summary>Identifies the direction of a raw device-protocol message.</summary>
public enum DeviceMessageDirection : short
{
    Inbound = 1,
    Outbound = 2
}

/// <summary>Describes why a device credential is retained.</summary>
public enum DeviceCredentialType : short
{
    Mqtt = 1,
    Mqtts = 2,
    ClientCertificate = 3,

    /// <summary>Local device WebServer password, retained only encrypted when explicitly supported.</summary>
    WebServerPassword = 4
}
