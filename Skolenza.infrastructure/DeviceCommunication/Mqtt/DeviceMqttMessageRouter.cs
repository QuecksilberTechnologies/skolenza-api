// ================================================================
// Purpose : Validates vendor MQTT topic/payload identity then delegates institution
//           resolution and idempotent response matching to persistence.
// ================================================================

using System.Text.Json;
using skolenza.application.Interfaces.IDeviceCommunication;
using skolenza.domain.Entity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace skolenza.infrastructure.DeviceCommunication.Mqtt;

/// <summary>Routes inbound messages received through the configured AiFace response topic.</summary>
public sealed class DeviceMqttMessageRouter(
    IServiceScopeFactory scopeFactory,
    ILogger<DeviceMqttMessageRouter> logger)
{
    /// <summary>Audits and processes an inbound device publication without trusting client institution data.</summary>
    public async Task RouteAsync(
        DeviceCommunicationProtocol transport,
        SkolenzaMqttTransportOptions profile,
        string topic,
        string payload,
        int qualityOfService,
        bool isDuplicateDelivery,
        CancellationToken cancellationToken)
    {
        if (transport is not DeviceCommunicationProtocol.Mqtt and not DeviceCommunicationProtocol.Mqtts)
        {
            throw new ArgumentOutOfRangeException(nameof(transport), transport, "Only MQTT and MQTTS messages can use this router.");
        }

        if (!profile.TryGetDeviceResponseSerialNumber(topic, out var serialNumber))
        {
            logger.LogWarning("Rejected MQTT topic outside the configured device response route: {Topic}", topic);
            return;
        }

        var identityValid = IsPayloadSerialCompatible(payload, serialNumber);
        using var scope = scopeFactory.CreateScope();
        var queueStore = scope.ServiceProvider.GetRequiredService<IDeviceCommandDispatchStore>();
        await queueStore.RecordInboundAsync(
            new DeviceMqttInboundMessage(
                transport,
                topic,
                serialNumber,
                payload,
                qualityOfService,
                isDuplicateDelivery,
                identityValid,
                DateTime.UtcNow),
            cancellationToken);
    }

    private static bool IsPayloadSerialCompatible(string payload, string topicSerialNumber)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            if (!document.RootElement.TryGetProperty("sn", out var serialProperty))
            {
                // Older vendor payload shapes do not always include sn; the topic remains authoritative.
                return true;
            }

            return serialProperty.ValueKind == JsonValueKind.String &&
                   string.Equals(serialProperty.GetString()?.Trim(), topicSerialNumber, StringComparison.Ordinal);
        }
        catch (JsonException)
        {
            // Keep and audit malformed raw payloads, but never route them to a command completion.
            return false;
        }
    }
}
