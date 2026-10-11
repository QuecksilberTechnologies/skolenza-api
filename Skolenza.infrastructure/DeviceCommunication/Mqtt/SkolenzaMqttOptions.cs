// ================================================================
// Purpose : Defines isolated, secure broker profiles for MQTT and MQTTS.
// ================================================================

using skolenza.domain.Entity;

namespace skolenza.infrastructure.DeviceCommunication.Mqtt;

/// <summary>
/// Configuration for the Skolenza device brokers. MQTT and MQTTS are deliberately
/// separate profiles: a device configured for one transport can never be silently
/// delivered through the other transport.
/// </summary>
public sealed class SkolenzaMqttOptions
{
    public const string SectionName = "DeviceMqtt";

    #region Explicit Transport Profiles

    /// <summary>Plain MQTT broker profile. It always uses MQTT over TCP.</summary>
    public SkolenzaMqttTransportOptions Mqtt { get; init; } = new()
    {
        ClientId = "skolenza-device-gateway-mqtt",
        Port = 1883
    };

    /// <summary>MQTT over TLS broker profile. It always uses MQTTS.</summary>
    public SkolenzaMqttTransportOptions Mqtts { get; init; } = new()
    {
        ClientId = "skolenza-device-gateway-mqtts",
        Port = 8883
    };

    #endregion

    #region Legacy Single-Profile Compatibility

    /// <summary>
    /// Previous single-profile setting. When present, it enables exactly one
    /// profile: <c>UseTls=true</c> becomes MQTTS; otherwise it becomes MQTT.
    /// New deployments must use <see cref="Mqtt"/> or <see cref="Mqtts"/>.
    /// </summary>
    [Obsolete("Configure DeviceMqtt:Mqtt or DeviceMqtt:Mqtts instead.")]
    public bool? Enabled { get; init; }

    /// <inheritdoc cref="SkolenzaMqttTransportOptions.Host"/>
    [Obsolete("Configure DeviceMqtt:Mqtt or DeviceMqtt:Mqtts instead.")]
    public string? Host { get; init; }

    /// <inheritdoc cref="SkolenzaMqttTransportOptions.Port"/>
    [Obsolete("Configure DeviceMqtt:Mqtt or DeviceMqtt:Mqtts instead.")]
    public int? Port { get; init; }

    /// <summary>Determines which secure profile a legacy configuration represents.</summary>
    [Obsolete("Configure DeviceMqtt:Mqtt or DeviceMqtt:Mqtts instead.")]
    public bool? UseTls { get; init; }

    /// <inheritdoc cref="SkolenzaMqttTransportOptions.ClientId"/>
    [Obsolete("Configure DeviceMqtt:Mqtt or DeviceMqtt:Mqtts instead.")]
    public string? ClientId { get; init; }

    /// <inheritdoc cref="SkolenzaMqttTransportOptions.UserName"/>
    [Obsolete("Configure DeviceMqtt:Mqtt or DeviceMqtt:Mqtts instead.")]
    public string? UserName { get; init; }

    /// <inheritdoc cref="SkolenzaMqttTransportOptions.Password"/>
    [Obsolete("Configure DeviceMqtt:Mqtt or DeviceMqtt:Mqtts instead.")]
    public string? Password { get; init; }

    /// <inheritdoc cref="SkolenzaMqttTransportOptions.TopicSuffix"/>
    [Obsolete("Configure DeviceMqtt:Mqtt or DeviceMqtt:Mqtts instead.")]
    public string? TopicSuffix { get; init; }

    #endregion

    #region Profile Resolution

    /// <summary>
    /// Resolves only explicitly enabled profiles. New profile configuration takes
    /// precedence over the old single-profile values to avoid accidental fallback.
    /// </summary>
    public IReadOnlyDictionary<DeviceCommunicationProtocol, SkolenzaMqttTransportOptions> ResolveEnabledProfiles()
    {
        var hasExplicitProfileConfiguration = Mqtt.HasExplicitConfiguration || Mqtts.HasExplicitConfiguration;
        if (hasExplicitProfileConfiguration)
        {
            return BuildProfileMap(
                Mqtt.Enabled ? Mqtt : null,
                Mqtts.Enabled ? Mqtts : null);
        }

#pragma warning disable CS0618
        if (Enabled != true)
#pragma warning restore CS0618
        {
            return new Dictionary<DeviceCommunicationProtocol, SkolenzaMqttTransportOptions>();
        }

#pragma warning disable CS0618
        var legacyUsesTls = UseTls == true;
        var legacyProfile = new SkolenzaMqttTransportOptions
        {
            Enabled = true,
            Host = Host ?? string.Empty,
            Port = Port ?? (legacyUsesTls ? 8883 : 1883),
            ClientId = ClientId ?? (legacyUsesTls ? "skolenza-device-gateway-mqtts" : "skolenza-device-gateway-mqtt"),
            UserName = UserName,
            Password = Password,
            TopicSuffix = TopicSuffix
        };
#pragma warning restore CS0618

        return legacyUsesTls
            ? BuildProfileMap(null, legacyProfile)
            : BuildProfileMap(legacyProfile, null);
    }

    private static IReadOnlyDictionary<DeviceCommunicationProtocol, SkolenzaMqttTransportOptions> BuildProfileMap(
        SkolenzaMqttTransportOptions? mqtt,
        SkolenzaMqttTransportOptions? mqtts)
    {
        var profiles = new Dictionary<DeviceCommunicationProtocol, SkolenzaMqttTransportOptions>();
        if (mqtt is not null)
        {
            profiles.Add(DeviceCommunicationProtocol.Mqtt, mqtt);
        }

        if (mqtts is not null)
        {
            profiles.Add(DeviceCommunicationProtocol.Mqtts, mqtts);
        }

        return profiles;
    }

    #endregion
}

/// <summary>Contains one broker connection profile. Secrets must come from deployment secret configuration.</summary>
public sealed class SkolenzaMqttTransportOptions
{
    private const string TopicRoot = "aiface";

    /// <summary>Enables this transport profile.</summary>
    public bool Enabled { get; init; }

    /// <summary>Broker DNS name or IP address.</summary>
    public string Host { get; init; } = string.Empty;

    /// <summary>Broker TCP port: normally 1883 for MQTT and 8883 for MQTTS.</summary>
    public int Port { get; init; }

    /// <summary>Stable server-side broker client identifier; it must be unique per broker profile.</summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>Broker user name, supplied from secret configuration when required by the broker.</summary>
    public string? UserName { get; init; }

    /// <summary>Broker password, supplied only from secret configuration and never logged.</summary>
    public string? Password { get; init; }

    /// <summary>
    /// Optional static final topic segment required by a vendor device family, for example
    /// <c>stellar</c>. Empty retains the documented <c>aiface/{SN}/pub</c> and
    /// <c>aiface/{SN}/sub</c> topic convention.
    /// </summary>
    public string? TopicSuffix { get; init; }

    /// <summary>Optional TLS server-name indication value. Empty uses <see cref="Host"/>.</summary>
    public string? TlsServerName { get; init; }

    /// <summary>Optional PEM/DER/PFX path containing the trusted broker CA or server certificate.</summary>
    public string? TrustedServerCertificatePath { get; init; }

    /// <summary>Optional PFX path when the MQTTS broker requires mutual TLS.</summary>
    public string? ClientCertificatePath { get; init; }

    /// <summary>Optional client-certificate password, supplied only through secret configuration.</summary>
    public string? ClientCertificatePassword { get; init; }

    /// <summary>Determines whether the new-profile object was actually configured.</summary>
    internal bool HasExplicitConfiguration =>
        Enabled ||
        !string.IsNullOrWhiteSpace(Host) ||
        !string.IsNullOrWhiteSpace(UserName) ||
        !string.IsNullOrWhiteSpace(Password) ||
        !string.IsNullOrWhiteSpace(TopicSuffix) ||
        !string.IsNullOrWhiteSpace(TlsServerName) ||
        !string.IsNullOrWhiteSpace(TrustedServerCertificatePath) ||
        !string.IsNullOrWhiteSpace(ClientCertificatePath) ||
        !string.IsNullOrWhiteSpace(ClientCertificatePassword);

    #region Device Topic Convention

    /// <summary>Builds the command topic that the device listens to.</summary>
    public string BuildDeviceCommandTopic(string deviceSerialNumber) =>
        BuildTopic(deviceSerialNumber, "pub");

    /// <summary>Builds the wildcard filter for device responses and device-originated data.</summary>
    public string BuildDeviceResponseSubscriptionTopic() =>
        BuildTopic("+", "sub");

    /// <summary>
    /// Validates and extracts a serial number from an inbound device response topic.
    /// The configured static suffix, when present, must match exactly.
    /// </summary>
    public bool TryGetDeviceResponseSerialNumber(string topic, out string serialNumber)
    {
        serialNumber = string.Empty;
        var segments = topic.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var suffix = GetNormalizedTopicSuffix();
        var expectedLength = suffix is null ? 3 : 4;
        if (segments.Length != expectedLength ||
            !string.Equals(segments[0], TopicRoot, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(segments[2], "sub", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(segments[1]) ||
            (suffix is not null && !string.Equals(segments[3], suffix, StringComparison.Ordinal)))
        {
            return false;
        }

        serialNumber = segments[1];
        return true;
    }

    /// <summary>Returns whether the optional static topic suffix is safe for a single MQTT topic level.</summary>
    public bool HasValidTopicSuffix()
    {
        var suffix = GetNormalizedTopicSuffix();
        return suffix is null ||
               suffix.IndexOfAny(['/', '\\', '+', '#']) < 0 &&
               !suffix.Any(char.IsWhiteSpace);
    }

    private string BuildTopic(string deviceSerialNumber, string direction)
    {
        if (string.IsNullOrWhiteSpace(deviceSerialNumber))
        {
            throw new ArgumentException("A device serial number is required to build an MQTT topic.", nameof(deviceSerialNumber));
        }

        var suffix = GetNormalizedTopicSuffix();
        return suffix is null
            ? $"{TopicRoot}/{deviceSerialNumber.Trim()}/{direction}"
            : $"{TopicRoot}/{deviceSerialNumber.Trim()}/{direction}/{suffix}";
    }

    private string? GetNormalizedTopicSuffix() =>
        string.IsNullOrWhiteSpace(TopicSuffix) ? null : TopicSuffix.Trim();

    #endregion
}
