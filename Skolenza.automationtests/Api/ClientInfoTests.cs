using Skolenza.AutomationTests.Infrastructure;
using NUnit.Framework;

namespace Skolenza.AutomationTests.Api;

[TestFixture]
[Category("API")]
public sealed class ClientInfoTests : ApiTestBase
{
    [Test]
    public async Task Detect_device_returns_the_automation_client_context()
    {
        var response = await Api.GetAsync("/api/ClientInfo/detect-device");

        Assert.That(response.Status, Is.EqualTo(200), "The public device-info endpoint should be reachable.");

        var payload = await response.JsonAsync();
        Assert.That(payload, Is.Not.Null);

        var device = payload!.Value;
        Assert.Multiple(() =>
        {
            Assert.That(device.GetProperty("deviceType").GetInt32(), Is.EqualTo(1));
            Assert.That(device.GetProperty("userAgent").GetString(), Does.Contain("Skolenza.AutomationTests"));
        });
    }
}
