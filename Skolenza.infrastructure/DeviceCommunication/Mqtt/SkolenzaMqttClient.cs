// ================================================================
// Purpose : Owns isolated, secure central MQTT and MQTTS broker clients.
// ================================================================

using System.Buffers;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using skolenza.domain.Entity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;

namespace skolenza.infrastructure.DeviceCommunication.Mqtt;

/// <summary>
/// Provides one client per enabled transport profile. A command configured for
/// MQTT can only leave through MQTT; a command configured for MQTTS can only
/// leave through the independently authenticated TLS connection.
/// </summary>
public sealed class SkolenzaMqttClient : ISkolenzaMqttPublisher, IAsyncDisposable
{
    private readonly DeviceMqttMessageRouter _messageRouter;
    private readonly ILogger<SkolenzaMqttClient> _logger;
    private readonly IReadOnlyDictionary<DeviceCommunicationProtocol, BrokerConnection> _connections;

    /// <summary>Creates and validates all deployment-configured broker profiles.</summary>
    public SkolenzaMqttClient(
        IOptions<SkolenzaMqttOptions> options,
        DeviceMqttMessageRouter messageRouter,
        ILogger<SkolenzaMqttClient> logger)
    {
        _messageRouter = messageRouter;
        _logger = logger;

        var configuredProfiles = options.Value.ResolveEnabledProfiles();
        ValidateConfiguredProfiles(configuredProfiles);
        _connections = configuredProfiles.ToDictionary(
            pair => pair.Key,
            pair => CreateConnection(pair.Key, pair.Value));
    }

    #region Connection State

    /// <inheritdoc />
    public IReadOnlyCollection<DeviceCommunicationProtocol> EnabledTransports => _connections.Keys.ToArray();

    /// <inheritdoc />
    public bool IsConnected(DeviceCommunicationProtocol transport) =>
        _connections.TryGetValue(transport, out var connection) && connection.Client.IsConnected;

    /// <summary>Connects one enabled transport profile. Repeated calls are safe.</summary>
    public async Task ConnectAsync(DeviceCommunicationProtocol transport, CancellationToken cancellationToken)
    {
        if (!_connections.TryGetValue(transport, out var connection) || connection.Client.IsConnected)
        {
            return;
        }

        await connection.ConnectionLock.WaitAsync(cancellationToken);
        try
        {
            if (connection.Client.IsConnected)
            {
                return;
            }

            await connection.Client.ConnectAsync(BuildClientOptions(connection), cancellationToken);
            _logger.LogInformation(
                "Connected Skolenza {Transport} broker client to {Host}:{Port}.",
                transport,
                connection.Profile.Host,
                connection.Profile.Port);
        }
        finally
        {
            connection.ConnectionLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task PublishAsync(
        DeviceCommunicationProtocol transport,
        string topic,
        string payload,
        CancellationToken cancellationToken = default)
    {
        if (!_connections.TryGetValue(transport, out var connection))
        {
            throw new InvalidOperationException($"The {transport} broker profile is not enabled.");
        }

        if (!connection.Client.IsConnected)
        {
            throw new InvalidOperationException($"The {transport} broker profile is not connected.");
        }

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(false)
            .Build();
        await connection.Client.PublishAsync(message, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var connection in _connections.Values)
        {
            try
            {
                if (connection.Client.IsConnected)
                {
                    await connection.Client.DisconnectAsync();
                }
            }
            finally
            {
                connection.Client.Dispose();
                connection.ConnectionLock.Dispose();
            }
        }
    }

    #endregion

    #region Client Setup and TLS

    private BrokerConnection CreateConnection(
        DeviceCommunicationProtocol transport,
        SkolenzaMqttTransportOptions profile)
    {
        var client = new MqttClientFactory().CreateMqttClient();
        var connection = new BrokerConnection(transport, profile, client);
        client.ApplicationMessageReceivedAsync += arguments => HandleMessageAsync(connection, arguments);
        client.ConnectedAsync += arguments => SubscribeToDeviceResponsesAsync(connection, arguments);
        client.DisconnectedAsync += arguments =>
        {
            _logger.LogWarning(
                "Skolenza {Transport} broker client disconnected. Reason: {Reason}",
                transport,
                arguments.Reason);
            return Task.CompletedTask;
        };
        return connection;
    }

    private static MqttClientOptions BuildClientOptions(BrokerConnection connection)
    {
        var builder = new MqttClientOptionsBuilder()
            .WithClientId(connection.Profile.ClientId)
            .WithTcpServer(connection.Profile.Host, connection.Profile.Port)
            .WithProtocolVersion(MqttProtocolVersion.V311)
            .WithCleanSession(false);

        if (!string.IsNullOrWhiteSpace(connection.Profile.UserName))
        {
            builder.WithCredentials(connection.Profile.UserName, connection.Profile.Password);
        }

        if (connection.Transport == DeviceCommunicationProtocol.Mqtts)
        {
            ConfigureTls(builder, connection.Profile);
        }

        return builder.Build();
    }

    private static void ConfigureTls(MqttClientOptionsBuilder builder, SkolenzaMqttTransportOptions profile)
    {
        builder.WithTlsOptions(tls =>
        {
            tls.UseTls()
                .WithSslProtocols(SslProtocols.Tls12 | SslProtocols.Tls13)
                .WithAllowUntrustedCertificates(false)
                .WithIgnoreCertificateChainErrors(false)
                .WithIgnoreCertificateRevocationErrors(false)
                .WithRevocationMode(X509RevocationMode.Online)
                .WithTargetHost(string.IsNullOrWhiteSpace(profile.TlsServerName) ? profile.Host : profile.TlsServerName);

            if (!string.IsNullOrWhiteSpace(profile.TrustedServerCertificatePath))
            {
                tls.WithTrustChain(new X509Certificate2Collection(
                    LoadCertificate(profile.TrustedServerCertificatePath, password: null)));
            }

            if (!string.IsNullOrWhiteSpace(profile.ClientCertificatePath))
            {
                tls.WithClientCertificates(new X509Certificate2Collection(
                    LoadCertificate(profile.ClientCertificatePath, profile.ClientCertificatePassword)));
            }
        });
    }

    private static X509Certificate2 LoadCertificate(string certificatePath, string? password)
    {
        if (!File.Exists(certificatePath))
        {
            throw new InvalidOperationException("The configured MQTTS certificate file does not exist.");
        }

        if (string.Equals(Path.GetExtension(certificatePath), ".pem", StringComparison.OrdinalIgnoreCase))
        {
            return X509Certificate2.CreateFromPemFile(certificatePath);
        }

        return string.Equals(Path.GetExtension(certificatePath), ".pfx", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(Path.GetExtension(certificatePath), ".p12", StringComparison.OrdinalIgnoreCase)
            ? X509CertificateLoader.LoadPkcs12FromFile(
                certificatePath,
                password ?? string.Empty,
                X509KeyStorageFlags.EphemeralKeySet,
                Pkcs12LoaderLimits.Defaults)
            : X509CertificateLoader.LoadCertificateFromFile(certificatePath);
    }

    private static void ValidateConfiguredProfiles(
        IReadOnlyDictionary<DeviceCommunicationProtocol, SkolenzaMqttTransportOptions> profiles)
    {
        foreach (var (transport, profile) in profiles)
        {
            if (transport is not DeviceCommunicationProtocol.Mqtt and not DeviceCommunicationProtocol.Mqtts ||
                string.IsNullOrWhiteSpace(profile.Host) ||
                profile.Port is < 1 or > 65535 ||
                string.IsNullOrWhiteSpace(profile.ClientId))
            {
                throw new InvalidOperationException(
                    $"DeviceMqtt:{transport} requires a host, a port from 1 to 65535, and a client ID.");
            }

            if (string.IsNullOrWhiteSpace(profile.UserName) && !string.IsNullOrWhiteSpace(profile.Password))
            {
                throw new InvalidOperationException($"DeviceMqtt:{transport} cannot provide a password without a user name.");
            }

            if (!profile.HasValidTopicSuffix())
            {
                throw new InvalidOperationException(
                    $"DeviceMqtt:{transport}:TopicSuffix must be one static MQTT topic level without spaces, '/', '+', or '#'.");
            }

            if (transport == DeviceCommunicationProtocol.Mqtt &&
                (!string.IsNullOrWhiteSpace(profile.TlsServerName) ||
                 !string.IsNullOrWhiteSpace(profile.TrustedServerCertificatePath) ||
                 !string.IsNullOrWhiteSpace(profile.ClientCertificatePath)))
            {
                throw new InvalidOperationException("TLS certificate settings are valid only for the DeviceMqtt:Mqtts profile.");
            }

            if (string.IsNullOrWhiteSpace(profile.ClientCertificatePath) &&
                !string.IsNullOrWhiteSpace(profile.ClientCertificatePassword))
            {
                throw new InvalidOperationException($"DeviceMqtt:{transport} cannot provide a client certificate password without a client certificate path.");
            }
        }

        var duplicateIdentity = profiles
            .GroupBy(profile => new
            {
                Host = profile.Value.Host.Trim().ToUpperInvariant(),
                profile.Value.Port,
                ClientId = profile.Value.ClientId.Trim()
            })
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateIdentity is not null)
        {
            throw new InvalidOperationException("MQTT and MQTTS broker profiles cannot reuse the same broker host, port, and client ID.");
        }
    }

    #endregion

    #region Device Routing

    private async Task SubscribeToDeviceResponsesAsync(
        BrokerConnection connection,
        MqttClientConnectedEventArgs arguments)
    {
        var topic = connection.Profile.BuildDeviceResponseSubscriptionTopic();
        var options = new MqttClientSubscribeOptionsBuilder()
            .WithTopicFilter(filter => filter.WithTopic(topic).WithAtLeastOnceQoS())
            .Build();
        await connection.Client.SubscribeAsync(options, CancellationToken.None);
        _logger.LogInformation("Subscribed Skolenza {Transport} client to {Topic}.", connection.Transport, topic);
    }

    private async Task HandleMessageAsync(
        BrokerConnection connection,
        MqttApplicationMessageReceivedEventArgs arguments)
    {
        var payload = Encoding.UTF8.GetString(arguments.ApplicationMessage.Payload.ToArray());
        await _messageRouter.RouteAsync(
            connection.Transport,
            connection.Profile,
            arguments.ApplicationMessage.Topic,
            payload,
            (int)arguments.ApplicationMessage.QualityOfServiceLevel,
            arguments.ApplicationMessage.Dup,
            CancellationToken.None);
    }

    /// <inheritdoc />
    public string BuildDeviceCommandTopic(DeviceCommunicationProtocol transport, string deviceSerialNumber)
    {
        if (!_connections.TryGetValue(transport, out var connection))
        {
            throw new InvalidOperationException($"The {transport} broker profile is not enabled.");
        }

        return connection.Profile.BuildDeviceCommandTopic(deviceSerialNumber);
    }

    #endregion

    /// <summary>Retains transport-specific state without duplicating client implementations.</summary>
    private sealed class BrokerConnection(
        DeviceCommunicationProtocol transport,
        SkolenzaMqttTransportOptions profile,
        IMqttClient client)
    {
        public DeviceCommunicationProtocol Transport { get; } = transport;
        public SkolenzaMqttTransportOptions Profile { get; } = profile;
        public IMqttClient Client { get; } = client;
        public SemaphoreSlim ConnectionLock { get; } = new(1, 1);
    }
}
