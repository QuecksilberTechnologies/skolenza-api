using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Skolenza.AutomationTests.Infrastructure;
using NUnit.Framework;

namespace Skolenza.AutomationTests.Api;

/// <summary>
/// Exercises the Institution SMTP configuration API with an isolated inactive test record.
/// Credentials are supplied only through process environment variables; no secret is
/// stored in source control or in automationsettings.json.
/// </summary>
[TestFixture]
[Category("API")]
[Category("InstitutionEmailConfig")]
public sealed class InstitutionEmailConfigCrudTests
{
    private const string LoginIdEnvironmentVariable = "SKOLENZA_TEST_LOGIN_ID";
    private const string PasswordEnvironmentVariable = "SKOLENZA_TEST_LOGIN_PASSWORD";
    private const string InstitutionEmailConfigModuleCode = "INSTITUTION_EMAIL_CONFIG";

    private HttpClient _authenticatedApi = null!;
    private int _moduleId;
    private int _createOperationId;
    private int _readOperationId;
    private int _updateOperationId;
    private int _deleteOperationId;

    [SetUp]
    public async Task AuthenticateAndResolveInstitutionEmailConfigPermissions()
    {
        var loginId = Environment.GetEnvironmentVariable(LoginIdEnvironmentVariable);
        var password = Environment.GetEnvironmentVariable(PasswordEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(loginId) || string.IsNullOrWhiteSpace(password))
        {
            Assert.Ignore(
                $"Set '{LoginIdEnvironmentVariable}' and '{PasswordEnvironmentVariable}' for the dedicated test account to run this CRUD test.");
        }

        using var anonymousApi = new HttpClient
        {
            BaseAddress = new Uri(TestSettings.ApiBaseUrl)
        };

        using var loginResponse = await PostJsonAsync(anonymousApi, "/api/NewLogin/login", new
        {
            LoginId = loginId,
            Password = password,
            MacAddress = string.Empty,
            IpAddressPublic = string.Empty,
            IpAddressLocal = "127.0.0.1",
            Latitude = 0,
            Longitude = 0,
            LoginDevice = 0
        });

        using var loginJson = await ReadJsonAsync(loginResponse, "login");
        AssertSucceeded(loginJson.RootElement, "login");

        var accessToken = GetRequiredString(
            GetRequiredProperty(loginJson.RootElement, "Data"),
            "AccessToken");

        _authenticatedApi = new HttpClient
        {
            BaseAddress = new Uri(TestSettings.ApiBaseUrl)
        };
        _authenticatedApi.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        _authenticatedApi.DefaultRequestHeaders.UserAgent.ParseAdd("Skolenza.AutomationTests");

        using var menuResponse = await _authenticatedApi.GetAsync("/api/Navigation/my-menu");
        using var menuJson = await ReadJsonAsync(menuResponse, "navigation menu");
        AssertSucceeded(menuJson.RootElement, "navigation menu");

        var institutionEmailConfigModule = FindModule(
            GetRequiredProperty(GetRequiredProperty(menuJson.RootElement, "Data"), "Items"),
            InstitutionEmailConfigModuleCode);

        Assert.That(
            institutionEmailConfigModule.HasValue,
            Is.True,
            $"The test account's institution does not expose the '{InstitutionEmailConfigModuleCode}' module. Apply database-scripts/complete seed data/Skolenza_New_Production_Module_Operation_Seed.sql, enable the module and its CRUD operations for the institution through plan-entitlement synchronization, and assign the operations to the account's Institution role.");

        var module = institutionEmailConfigModule!.Value;
        _moduleId = GetRequiredProperty(module, "Id").GetInt32();
        var operations = GetRequiredProperty(module, "Operations");
        _createOperationId = GetOperationId(operations, "Create", "Add");
        _readOperationId = GetOperationId(operations, "View", "Read");
        _updateOperationId = GetOperationId(operations, "Update", "Edit");
        _deleteOperationId = GetOperationId(operations, "Delete");
    }

    [TearDown]
    public async Task DisposeAuthenticatedApi()
    {
        if (_authenticatedApi is not null)
        {
            _authenticatedApi.Dispose();
        }
    }

    [Test]
    public async Task Institution_email_config_can_be_created_read_updated_and_deleted()
    {
        var runId = Guid.NewGuid().ToString("N")[..12];
        var email = $"institution-email-crud-{runId}@example.test";
        var createdId = 0;

        try
        {
            using var createResponse = await PostJsonAsync(_authenticatedApi, "/api/InstitutionEmailConfig/create", new
            {
                ModuleId = _moduleId,
                OperationId = _createOperationId,
                SmtpHost = "smtp.example.com",
                SmtpPort = 2525,
                SmtpUsername = email,
                SmtpPassword = "Test-only-password-123!",
                FromEmail = email,
                FromName = $"Institution Email CRUD {runId}",
                // Inactive test data cannot replace a institution's active SMTP configuration.
                IsActive = false
            });
            using (var createJson = await ReadJsonAsync(createResponse, "create"))
            {
                AssertSucceeded(createJson.RootElement, "create");
                createdId = GetRequiredProperty(GetRequiredProperty(createJson.RootElement, "Data"), "Id").GetInt32();
            }

            var readUrl = GetAccessUrl($"/api/InstitutionEmailConfig/get-by-id/{createdId}", _readOperationId);
            using var readResponse = await _authenticatedApi.GetAsync(readUrl);
            using (var readJson = await ReadJsonAsync(readResponse, "read"))
            {
                AssertSucceeded(readJson.RootElement, "read");
                var readData = GetRequiredProperty(readJson.RootElement, "Data");
                Assert.That(GetRequiredProperty(readData, "Id").GetInt32(), Is.EqualTo(createdId));
                Assert.That(GetRequiredString(readData, "FromEmail"), Is.EqualTo(email));
                Assert.That(HasProperty(readData, "SmtpPassword"), Is.False, "SMTP passwords must never be returned by the API.");
            }

            using var listResponse = await _authenticatedApi.GetAsync(GetAccessUrl("/api/InstitutionEmailConfig/get-all", _readOperationId));
            using (var listJson = await ReadJsonAsync(listResponse, "list"))
            {
                AssertSucceeded(listJson.RootElement, "list");
                var listData = GetRequiredProperty(listJson.RootElement, "Data");
                Assert.That(
                    listData.EnumerateArray().Any(item => GetRequiredProperty(item, "Id").GetInt32() == createdId),
                    Is.True,
                    "The created SMTP configuration was not returned by get-all.");
            }

            var updatedName = $"Institution Email CRUD Updated {runId}";
            using var updateResponse = await PostJsonAsync(_authenticatedApi, "/api/InstitutionEmailConfig/update", new
            {
                Id = createdId,
                ModuleId = _moduleId,
                OperationId = _updateOperationId,
                SmtpHost = "smtp.example.com",
                SmtpPort = 2526,
                SmtpUsername = email,
                // An omitted password must retain the secret set at creation time.
                SmtpPassword = (string?)null,
                FromEmail = email,
                FromName = updatedName,
                IsActive = false
            });
            using (var updateJson = await ReadJsonAsync(updateResponse, "update"))
            {
                AssertSucceeded(updateJson.RootElement, "update");
                var updateData = GetRequiredProperty(updateJson.RootElement, "Data");
                Assert.That(GetRequiredString(updateData, "FromName"), Is.EqualTo(updatedName));
                Assert.That(GetRequiredProperty(updateData, "SmtpPort").GetInt32(), Is.EqualTo(2526));
                Assert.That(GetRequiredProperty(updateData, "IsActive").GetBoolean(), Is.False);
                Assert.That(GetRequiredProperty(updateData, "HasSmtpPassword").GetBoolean(), Is.True);
            }

            await DeleteTestRecordAsync(createdId, "delete");
            createdId = 0;
        }
        finally
        {
            if (createdId > 0)
            {
                await DeleteTestRecordAsync(createdId, "cleanup delete");
            }
        }
    }

    private async Task DeleteTestRecordAsync(int id, string operationName)
    {
        using var deleteResponse = await _authenticatedApi.DeleteAsync(
            GetAccessUrl($"/api/InstitutionEmailConfig/delete/{id}", _deleteOperationId));
        using var deleteJson = await ReadJsonAsync(deleteResponse, operationName);
        AssertSucceeded(deleteJson.RootElement, operationName);
    }

    private string GetAccessUrl(string path, int operationId) =>
        $"{path}?ModuleId={_moduleId}&OperationId={operationId}";

    private static async Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string path, object data) =>
        await client.PostAsync(
            path,
            new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json"));

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, string operationName)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.That((int)response.StatusCode, Is.EqualTo(200), $"Institution Email Config {operationName} returned HTTP {(int)response.StatusCode}.");
        return JsonDocument.Parse(body);
    }

    private static void AssertSucceeded(JsonElement response, string operationName)
    {
        var isSucceeded = GetRequiredProperty(response, "IsSucceeded").GetBoolean();
        Assert.That(
            isSucceeded,
            Is.True,
            $"Institution Email Config {operationName} failed: {GetOptionalString(response, "Message") ?? "No error message was returned."}");
    }

    private static JsonElement? FindModule(JsonElement items, string moduleCode)
    {
        foreach (var item in items.EnumerateArray())
        {
            if (string.Equals(GetOptionalString(item, "ModuleCode"), moduleCode, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }

            var children = GetRequiredProperty(item, "Children");
            var childMatch = FindModule(children, moduleCode);
            if (childMatch.HasValue)
            {
                return childMatch;
            }
        }

        return null;
    }

    private static int GetOperationId(JsonElement operations, params string[] acceptedNames)
    {
        foreach (var operation in operations.EnumerateArray())
        {
            if (acceptedNames.Any(name => string.Equals(
                    GetOptionalString(operation, "Name"),
                    name,
                    StringComparison.OrdinalIgnoreCase)))
            {
                return GetRequiredProperty(operation, "Id").GetInt32();
            }
        }

        Assert.Fail($"The '{InstitutionEmailConfigModuleCode}' module does not grant a required '{string.Join("' or '", acceptedNames)}' operation.");
        return 0;
    }

    private static JsonElement GetRequiredProperty(JsonElement element, string propertyName)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        Assert.Fail($"The API response did not contain the expected '{propertyName}' property.");
        return default;
    }

    private static string GetRequiredString(JsonElement element, string propertyName)
    {
        var value = GetOptionalString(element, propertyName);
        Assert.That(value, Is.Not.Null.And.Not.Empty, $"The API response did not contain a value for '{propertyName}'.");
        return value!;
    }

    private static string? GetOptionalString(JsonElement element, string propertyName)
    {
        var value = GetRequiredProperty(element, propertyName);
        return value.ValueKind == JsonValueKind.Null ? null : value.GetString();
    }

    private static bool HasProperty(JsonElement element, string propertyName) =>
        element.EnumerateObject().Any(property =>
            string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase));
}
