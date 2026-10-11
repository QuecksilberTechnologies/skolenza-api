using System.Text.Json;
using Skolenza.AutomationTests.Infrastructure;
using NUnit.Framework;

namespace Skolenza.AutomationTests.Api;

[TestFixture]
[Category("API")]
public sealed class SwaggerDocumentTests : ApiTestBase
{
    [Test]
    public async Task Swagger_document_is_available_and_contains_routes()
    {
        var response = await Api.GetAsync("/swagger/v1/swagger.json");

        Assert.That(response.Status, Is.EqualTo(200), "Start the API before running this test.");
        Assert.That(response.Headers["content-type"], Does.Contain("application/json"));

        var payload = await response.JsonAsync();
        Assert.That(payload, Is.Not.Null);

        var document = payload!.Value;
        Assert.That(document.TryGetProperty("paths", out var paths), Is.True);
        Assert.That(paths.ValueKind, Is.EqualTo(JsonValueKind.Object));
        Assert.That(paths.EnumerateObject(), Is.Not.Empty);
    }
}
