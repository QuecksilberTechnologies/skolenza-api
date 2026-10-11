using skolenza.domain.Entity;

namespace skolenza.infrastructure.DeviceCommunication.Mqtt;

/// <summary>Publishes through the isolated central MQTT or MQTTS broker profile.</summary>
public interface ISkolenzaMqttPublisher
{
    /// <summary>Returns the profiles intentionally enabled by deployment configuration.</summary>
    IReadOnlyCollection<DeviceCommunicationProtocol> EnabledTransports { get; }

    /// <summary>Returns whether the requested transport has an active broker connection.</summary>
    bool IsConnected(DeviceCommunicationProtocol transport);

    /// <summary>Builds the configured device command topic for exactly one broker transport.</summary>
    string BuildDeviceCommandTopic(DeviceCommunicationProtocol transport, string deviceSerialNumber);

    /// <summary>Publishes through exactly the requested transport. No protocol fallback occurs.</summary>
    Task PublishAsync(
        DeviceCommunicationProtocol transport,
        string topic,
        string payload,
        CancellationToken cancellationToken = default);
}
