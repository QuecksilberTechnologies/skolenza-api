// ================================================================
// Purpose : Central dispatcher. It never creates a institution-specific worker and
//           uses the durable database queue to preserve device ordering.
// ================================================================

using skolenza.application.Interfaces.IDeviceCommunication;
using skolenza.domain.Entity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace skolenza.infrastructure.DeviceCommunication.Mqtt;

/// <summary>Publishes the next eligible command per physical device through the central MQTT client.</summary>
public sealed class DeviceCommandDispatcherWorker(
    IServiceScopeFactory scopeFactory,
    ISkolenzaMqttPublisher mqttPublisher,
    ILogger<DeviceCommandDispatcherWorker> logger)
    : BackgroundService
{
    private static readonly DeviceCommunicationProtocol[] SupportedTransports =
    [
        DeviceCommunicationProtocol.Mqtt,
        DeviceCommunicationProtocol.Mqtts
    ];

    private int _transportCursor;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var queueStore = scope.ServiceProvider.GetRequiredService<IDeviceCommandDispatchStore>();
                await queueStore.RecoverExpiredResponseDeadlinesAsync(stoppingToken);
                foreach (var transport in GetDispatchOrder())
                {
                    if (!mqttPublisher.IsConnected(transport))
                    {
                        continue;
                    }

                    var dispatch = await queueStore.TryAcquireNextAsync(
                        new[] { transport },
                        cancellationToken: stoppingToken);
                    if (dispatch is null)
                    {
                        continue;
                    }

                    try
                    {
                        var topic = mqttPublisher.BuildDeviceCommandTopic(transport, dispatch.DeviceSerialNumber);
                        await mqttPublisher.PublishAsync(transport, topic, dispatch.Payload, stoppingToken);
                        await queueStore.MarkPublishedAsync(dispatch, topic, qualityOfService: 1, DateTime.UtcNow, stoppingToken);
                    }
                    catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                    {
                        logger.LogWarning(
                            exception,
                            "{Transport} publish failed for DeviceCommand {DeviceCommandId} on serial {DeviceSerialNumber}.",
                            transport,
                            dispatch.DeviceCommandId,
                            dispatch.DeviceSerialNumber);
                        await queueStore.ScheduleRetryOrFailAsync(dispatch, exception.Message, DateTime.UtcNow, stoppingToken);
                    }

                    // One command per timer tick preserves the established database queue pacing.
                    break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "The central device command dispatcher iteration failed.");
            }
        }
    }

    private IEnumerable<DeviceCommunicationProtocol> GetDispatchOrder()
    {
        var start = Math.Abs(Interlocked.Increment(ref _transportCursor)) % SupportedTransports.Length;
        for (var offset = 0; offset < SupportedTransports.Length; offset++)
        {
            yield return SupportedTransports[(start + offset) % SupportedTransports.Length];
        }
    }
}
