// ================================================================
// Purpose : Explicit, institution-authorized MQTTS delivery of one already queued
//           command. It accepts no vendor JSON and preserves queue ordering.
// ================================================================

using skolenza.application.Interfaces.IDeviceCommunication;
using skolenza.domain.Entity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace skolenza.infrastructure.DeviceCommunication.Mqtt;

/// <summary>Delivers the next existing MQTTS queue item on an explicit UI request.</summary>
public sealed class DeviceCommandManualDispatcher(
    IServiceScopeFactory scopeFactory,
    ISkolenzaMqttPublisher mqttPublisher,
    ILogger<DeviceCommandManualDispatcher> logger)
    : IDeviceCommandManualDispatcher
{
    /// <inheritdoc />
    public async Task<ManualDeviceCommandDispatchResult> DispatchNextMqttsAsync(
        long institutionDeviceId,
        CancellationToken cancellationToken = default)
    {
        if (!mqttPublisher.IsConnected(DeviceCommunicationProtocol.Mqtts))
        {
            return new ManualDeviceCommandDispatchResult(false, "The secure MQTT broker is not connected. The queued command was not changed.");
        }

        using var scope = scopeFactory.CreateScope();
        var queueStore = scope.ServiceProvider.GetRequiredService<IDeviceCommandDispatchStore>();
        await queueStore.RecoverExpiredResponseDeadlinesAsync(cancellationToken);
        var dispatch = await queueStore.TryAcquireNextAsync(
            new[] { DeviceCommunicationProtocol.Mqtts },
            institutionDeviceId,
            cancellationToken);
        if (dispatch is null)
        {
            return new ManualDeviceCommandDispatchResult(false, "There is no eligible queued MQTTS command for this device.");
        }

        try
        {
            var topic = mqttPublisher.BuildDeviceCommandTopic(DeviceCommunicationProtocol.Mqtts, dispatch.DeviceSerialNumber);
            await mqttPublisher.PublishAsync(DeviceCommunicationProtocol.Mqtts, topic, dispatch.Payload, cancellationToken);
            await queueStore.MarkPublishedAsync(dispatch, topic, qualityOfService: 1, DateTime.UtcNow, cancellationToken);
            logger.LogInformation("Manually dispatched MQTTS DeviceCommand {DeviceCommandId}.", dispatch.DeviceCommandId);
            return new ManualDeviceCommandDispatchResult(true, "The next queued command has been published through secure MQTT.");
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            await queueStore.ScheduleRetryOrFailAsync(dispatch, exception.Message, DateTime.UtcNow, cancellationToken);
            logger.LogWarning(exception, "Manual MQTTS dispatch failed for DeviceCommand {DeviceCommandId}.", dispatch.DeviceCommandId);
            return new ManualDeviceCommandDispatchResult(false, "The secure MQTT publish failed. The command has been retained for retry.");
        }
    }
}
