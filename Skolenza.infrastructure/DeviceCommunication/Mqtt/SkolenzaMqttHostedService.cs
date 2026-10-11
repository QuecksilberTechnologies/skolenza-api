// ================================================================
// Purpose : Maintains the single central connection. It never creates a
//           per-institution client, subscription, or hosted service.
// ================================================================

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace skolenza.infrastructure.DeviceCommunication.Mqtt;

/// <summary>Keeps the central MQTT/MQTTS client connected with bounded reconnect attempts.</summary>
public sealed class SkolenzaMqttHostedService(
    SkolenzaMqttClient mqttClient,
    ILogger<SkolenzaMqttHostedService> logger)
    : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabledTransports = mqttClient.EnabledTransports;
        if (enabledTransports.Count == 0)
        {
            logger.LogWarning("MQTT/MQTTS device communication is disabled. No device command will be published until an explicit DeviceMqtt transport profile is enabled.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (var transport in enabledTransports)
                {
                    await mqttClient.ConnectAsync(transport, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "An Skolenza MQTT/MQTTS connection attempt failed; retrying without creating institution-specific clients.");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }
}
