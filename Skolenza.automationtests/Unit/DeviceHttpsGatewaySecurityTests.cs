using skolenza.application.Constants;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

/// <summary>Exercises the token rules used by both normal and initial device gateway routes.</summary>
[TestFixture]
public sealed class DeviceHttpsGatewaySecurityTests
{
    [Test]
    public void Generated_ingress_token_is_fixed_length_lowercase_hex_and_hashes_deterministically()
    {
        var token = DeviceHttpsGatewaySecurity.GenerateIngressToken();

        Assert.Multiple(() =>
        {
            Assert.That(DeviceHttpsGatewaySecurity.IsValidIngressToken(token), Is.True);
            Assert.That(token, Has.Length.EqualTo(64));
            Assert.That(token, Is.EqualTo(token.ToLowerInvariant()));
            Assert.That(DeviceHttpsGatewaySecurity.HashIngressToken(token), Has.Length.EqualTo(64));
            Assert.That(DeviceHttpsGatewaySecurity.HashIngressToken(token),
                Is.EqualTo(DeviceHttpsGatewaySecurity.HashIngressToken(token)));
        });
    }

    [TestCase("")]
    [TestCase("not-a-token")]
    [TestCase("0123456789abcdef")]
    [TestCase("GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG")]
    public void Nonconforming_ingress_tokens_are_rejected(string token) =>
        Assert.That(DeviceHttpsGatewaySecurity.IsValidIngressToken(token), Is.False);
}
