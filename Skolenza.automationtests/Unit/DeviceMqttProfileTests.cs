using skolenza.domain.Entity;
using skolenza.infrastructure.DeviceCommunication.Mqtt;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

/// <summary>Protects the rule that plain MQTT and MQTTS never share a connection profile.</summary>
[TestFixture]
public sealed class DeviceMqttProfileTests
{
    [Test]
    public void Explicit_profiles_keep_mqtt_and_mqtts_isolated()
    {
        var options = new SkolenzaMqttOptions
        {
            Mqtt = new SkolenzaMqttTransportOptions
            {
                Enabled = true,
                Host = "mqtt.example.test",
                Port = 1883,
                ClientId = "gateway-mqtt"
            },
            Mqtts = new SkolenzaMqttTransportOptions
            {
                Enabled = true,
                Host = "mqtts.example.test",
                Port = 8883,
                ClientId = "gateway-mqtts"
            }
        };

        var profiles = options.ResolveEnabledProfiles();

        Assert.Multiple(() =>
        {
            Assert.That(profiles.Keys, Is.EquivalentTo(new[]
            {
                DeviceCommunicationProtocol.Mqtt,
                DeviceCommunicationProtocol.Mqtts
            }));
            Assert.That(profiles[DeviceCommunicationProtocol.Mqtt].Port, Is.EqualTo(1883));
            Assert.That(profiles[DeviceCommunicationProtocol.Mqtts].Port, Is.EqualTo(8883));
            Assert.That(profiles[DeviceCommunicationProtocol.Mqtt].ClientId,
                Is.Not.EqualTo(profiles[DeviceCommunicationProtocol.Mqtts].ClientId));
        });
    }

    [TestCase(false, DeviceCommunicationProtocol.Mqtt, 1883)]
    [TestCase(true, DeviceCommunicationProtocol.Mqtts, 8883)]
    public void Legacy_profile_maps_to_exactly_one_transport(bool useTls, DeviceCommunicationProtocol expectedTransport, int expectedPort)
    {
#pragma warning disable CS0618
        var options = new SkolenzaMqttOptions
        {
            Enabled = true,
            UseTls = useTls,
            Host = "broker.example.test",
            Port = expectedPort,
            ClientId = "legacy-gateway"
        };
#pragma warning restore CS0618

        var profiles = options.ResolveEnabledProfiles();

        Assert.That(profiles.Keys, Is.EquivalentTo(new[] { expectedTransport }));
    }

    [Test]
    public void Vendor_topic_suffix_is_applied_to_command_and_response_topics()
    {
        var profile = new SkolenzaMqttTransportOptions { TopicSuffix = "stellar" };

        Assert.Multiple(() =>
        {
            Assert.That(profile.BuildDeviceCommandTopic("AYUC24030780"), Is.EqualTo("aiface/AYUC24030780/pub/stellar"));
            Assert.That(profile.BuildDeviceResponseSubscriptionTopic(), Is.EqualTo("aiface/+/sub/stellar"));
            Assert.That(profile.TryGetDeviceResponseSerialNumber("aiface/AYUC24030780/sub/stellar", out var serialNumber), Is.True);
            Assert.That(serialNumber, Is.EqualTo("AYUC24030780"));
            Assert.That(profile.TryGetDeviceResponseSerialNumber("aiface/AYUC24030780/sub", out _), Is.False);
        });
    }

    [TestCase("stellar/legacy")]
    [TestCase("stellar topic")]
    [TestCase("+")]
    [TestCase("#")]
    public void Topic_suffix_must_be_one_static_mqtt_level(string suffix)
    {
        Assert.That(new SkolenzaMqttTransportOptions { TopicSuffix = suffix }.HasValidTopicSuffix(), Is.False);
    }
}
