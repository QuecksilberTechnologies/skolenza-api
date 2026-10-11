using Skolenza.AutomationTests.Infrastructure;
using NUnit.Framework;
using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http.Headers;

namespace Skolenza.AutomationTests.Api;

[TestFixture]
[Category("API")]
public sealed class AuthenticationTests : ApiTestBase
{
    [Test]
    [Category("HostLive")]
    public async Task Institution_device_live_api_supports_the_complete_assignment_lifecycle()
    {
        using var client = await CreateAuthenticatedHostClientAsync();
        var modules = await GetHostMenuModulesAsync(client);
        var institutionDeviceModule = Module(modules, "INSTITUTION_DEVICES");
        var institutionListModule = Module(modules, "HOST_INSTITUTION_LIST");
        var campusModule = Module(modules, "HOST_CAMPUS_LIST");
        var deviceCatalogueModule = Module(modules, "HOST_DEVICE_SETUP");

        var institutions = await GetSuccessfulPayloadAsync(
            client,
            $"/api/Institution/get-all-institutions?PageNumber=1&PageSize=10&ModuleId={Property(institutionListModule, "id")}&OperationId={Operation(institutionListModule, "View")}");
        var institutionId = Property(Property(institutions.RootElement, "data").EnumerateArray().First(), "id").GetString()!;

        var locations = await GetSuccessfulPayloadAsync(
            client,
            $"/api/Campus/get-all?IsActive=true&PageNumber=1&PageSize=10&InstitutionId={institutionId}&ModuleId={Property(campusModule, "id")}&OperationId={Operation(campusModule, "View")}");
        var campusId = Property(Property(locations.RootElement, "data").EnumerateArray().First(), "id").GetInt64();

        var catalogue = await GetSuccessfulPayloadAsync(
            client,
            $"/api/DeviceMaster/get-all?IsActive=true&PageNumber=1&PageSize=10&ModuleId={Property(deviceCatalogueModule, "id")}&OperationId={Operation(deviceCatalogueModule, "View")}");
        var deviceMasterId = Property(Property(catalogue.RootElement, "data").EnumerateArray().First(), "id").GetInt64();

        var code = $"API-TD-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        string? institutionDeviceId = null;

        try
        {
            var created = await PostSuccessfulPayloadAsync(client, "/api/InstitutionDevice/create", new
            {
                institutionId,
                campusId,
                deviceMasterId,
                deviceCode = code,
                deviceName = "Automated InstitutionDevice lifecycle",
                installedDateTime = DateTime.UtcNow,
                installationRemark = "Disposable HostLive API test",
                isAttendanceDevice = true,
                description = "Created by automated API verification",
                remark = "Removed by test cleanup",
                isActive = true,
                moduleId = Property(institutionDeviceModule, "id").GetInt32(),
                operationId = Operation(institutionDeviceModule, "Assign")
            });
            institutionDeviceId = Property(Property(created.RootElement, "data"), "id").GetString();

            using var listed = await GetSuccessfulPayloadAsync(
                client,
                $"/api/InstitutionDevice/get-all?Search={Uri.EscapeDataString(code)}&PageNumber=1&PageSize=10&ModuleId={Property(institutionDeviceModule, "id")}&OperationId={Operation(institutionDeviceModule, "View")}");
            Assert.That(Property(listed.RootElement, "totalRecords").GetInt32(), Is.EqualTo(1));

            using var read = await GetSuccessfulPayloadAsync(
                client,
                $"/api/InstitutionDevice/get-by-id/{institutionDeviceId}?InstitutionId={institutionId}&ModuleId={Property(institutionDeviceModule, "id")}&OperationId={Operation(institutionDeviceModule, "View")}");
            Assert.That(Property(Property(read.RootElement, "data"), "deviceCode").GetString(), Is.EqualTo(code));

            await PostSuccessfulPayloadAsync(client, "/api/InstitutionDevice/update", new
            {
                id = institutionDeviceId,
                institutionId,
                campusId,
                deviceMasterId,
                deviceCode = code,
                deviceName = "Automated InstitutionDevice lifecycle updated",
                isAttendanceDevice = true,
                isActive = true,
                moduleId = Property(institutionDeviceModule, "id").GetInt32(),
                operationId = Operation(institutionDeviceModule, "Update")
            });

            await PostSuccessfulPayloadAsync(client, "/api/InstitutionDevice/update-status", new
            {
                id = institutionDeviceId,
                institutionId,
                isActive = false,
                moduleId = Property(institutionDeviceModule, "id").GetInt32(),
                operationId = Operation(institutionDeviceModule, "Inactive")
            });

            await PostSuccessfulPayloadAsync(client, "/api/InstitutionDevice/update-status", new
            {
                id = institutionDeviceId,
                institutionId,
                isActive = true,
                moduleId = Property(institutionDeviceModule, "id").GetInt32(),
                operationId = Operation(institutionDeviceModule, "Active")
            });
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(institutionDeviceId))
            {
                using var removed = await client.DeleteAsync(
                    $"/api/InstitutionDevice/delete/{institutionDeviceId}?InstitutionId={institutionId}&ModuleId={Property(institutionDeviceModule, "id")}&OperationId={Operation(institutionDeviceModule, "Remove")}");
                Assert.That(removed.IsSuccessStatusCode, Is.True, await removed.Content.ReadAsStringAsync());
            }
        }
    }

    [Test]
    [Category("HostLive")]
    public async Task Refresh_apis_rotate_a_fresh_host_token_and_return_the_expected_payloads()
    {
        var loginId = RequiredEnvironment("SKOLENZA_HOST_LOGIN_ID");
        var password = RequiredEnvironment("SKOLENZA_HOST_LOGIN_PASSWORD");
        using var client = new HttpClient
        {
            BaseAddress = new Uri(TestSettings.ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(60)
        };
        using var loginResponse = await client.PostAsJsonAsync("/api/NewLogin/login", LoginBody(loginId, password));
        Assert.That(loginResponse.IsSuccessStatusCode, Is.True, await loginResponse.Content.ReadAsStringAsync());
        using var login = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var originalRefreshToken = Property(Property(login.RootElement, "data"), "refreshToken").GetString()!;

        using var legacyResponse = await client.PostAsJsonAsync("/api/Auth/refresh-token", new
        {
            refreshToken = originalRefreshToken,
            ipAddress = "127.0.0.1"
        });
        Assert.That((int)legacyResponse.StatusCode, Is.EqualTo(200), await legacyResponse.Content.ReadAsStringAsync());
        using var legacy = JsonDocument.Parse(await legacyResponse.Content.ReadAsStringAsync());
        Assert.That(Property(legacy.RootElement, "isSucceeded").GetBoolean(), Is.True);
        var legacyReplacement = Property(Property(legacy.RootElement, "data"), "refreshToken").GetString()!;

        using var reusedLegacy = await client.PostAsJsonAsync("/api/Auth/refresh-token", new
        {
            refreshToken = originalRefreshToken,
            ipAddress = "127.0.0.1"
        });
        Assert.That((int)reusedLegacy.StatusCode, Is.EqualTo(401));

        using var v2Response = await client.PostAsJsonAsync("/api/Auth/refresh-token-v2", new
        {
            refreshToken = legacyReplacement,
            ipAddress = "127.0.0.1"
        });
        Assert.That((int)v2Response.StatusCode, Is.EqualTo(200), await v2Response.Content.ReadAsStringAsync());
        using var v2 = JsonDocument.Parse(await v2Response.Content.ReadAsStringAsync());
        Assert.That(Property(v2.RootElement, "isSucceeded").GetBoolean(), Is.True);
        var v2Data = Property(v2.RootElement, "data");
        Assert.That(
            v2Data.EnumerateObject().Select(property => property.Name).OrderBy(name => name),
            Is.EqualTo(new[] { "refreshToken", "refreshTokenExpiresAtUtc", "token", "tokenExpiry" }.OrderBy(name => name)));

        using var reusedV2 = await client.PostAsJsonAsync("/api/Auth/refresh-token-v2", new
        {
            refreshToken = legacyReplacement,
            ipAddress = "127.0.0.1"
        });
        Assert.That((int)reusedV2.StatusCode, Is.EqualTo(401));
    }

    [TestCase("InstitutionDevice", "SKOLENZA_HOST_DEVICE_MODULE_ID")]
    [TestCase("InstitutionDeviceConfiguration", "SKOLENZA_HOST_CONFIGURATION_MODULE_ID")]
    [Category("HostLive")]
    public async Task Authenticated_host_device_list_returns_a_successful_paged_response(string controller, string moduleVariable)
    {
        var loginId = Environment.GetEnvironmentVariable("SKOLENZA_HOST_LOGIN_ID");
        var password = Environment.GetEnvironmentVariable("SKOLENZA_HOST_LOGIN_PASSWORD");
        var moduleId = Environment.GetEnvironmentVariable(moduleVariable);
        var operationId = Environment.GetEnvironmentVariable("SKOLENZA_HOST_VIEW_OPERATION_ID");
        if (new[] { loginId, password, moduleId, operationId }.Any(string.IsNullOrWhiteSpace))
            Assert.Ignore("HostLive requires Host credentials and module/View operation IDs from my-menu in environment variables.");
        using var client = new HttpClient { BaseAddress = new Uri(TestSettings.ApiBaseUrl), Timeout = TimeSpan.FromSeconds(60) };
        using var loginResponse = await client.PostAsJsonAsync("/api/NewLogin/login", new
        {
            LoginId = loginId, Password = password, IpAddressLocal = "127.0.0.1", IpAddressPublic = "",
            MacAddress = "", LoginDevice = 1, Latitude = 0, Longitude = 0
        });
        Assert.That(loginResponse.IsSuccessStatusCode, Is.True, $"Host login returned {(int)loginResponse.StatusCode}");
        using var login = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var data = Property(login.RootElement, "data");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Property(data, "accessToken").GetString());
        using var response = await client.GetAsync($"/api/{controller}/get-all?PageNumber=1&PageSize=10&ModuleId={moduleId}&OperationId={operationId}");
        Assert.That((int)response.StatusCode, Is.EqualTo(200), $"Authenticated {controller}/get-all returned {(int)response.StatusCode}");
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(Property(payload.RootElement, "isSucceeded").GetBoolean(), Is.True);
        Assert.That(Property(payload.RootElement, "data").ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(Property(payload.RootElement, "pageNumber").GetInt32(), Is.EqualTo(1));
        Assert.That(Property(payload.RootElement, "totalRecords").GetInt32(), Is.GreaterThanOrEqualTo(0));
    }

    private static JsonElement Property(JsonElement element, string name) =>
        element.EnumerateObject().First(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).Value;

    private static string RequiredEnvironment(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value
            : throw new IgnoreException($"HostLive requires {name}.");

    private static object LoginBody(string loginId, string password) => new
    {
        LoginId = loginId,
        Password = password,
        IpAddressLocal = "127.0.0.1",
        IpAddressPublic = string.Empty,
        MacAddress = string.Empty,
        LoginDevice = 1,
        Latitude = 0,
        Longitude = 0
    };

    private static async Task<HttpClient> CreateAuthenticatedHostClientAsync()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(TestSettings.ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(60)
        };
        using var response = await client.PostAsJsonAsync(
            "/api/NewLogin/login",
            LoginBody(RequiredEnvironment("SKOLENZA_HOST_LOGIN_ID"), RequiredEnvironment("SKOLENZA_HOST_LOGIN_PASSWORD")));
        Assert.That(response.IsSuccessStatusCode, Is.True, await response.Content.ReadAsStringAsync());
        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            Property(Property(payload.RootElement, "data"), "accessToken").GetString());
        return client;
    }

    private static async Task<List<JsonElement>> GetHostMenuModulesAsync(HttpClient client)
    {
        using var payload = await GetSuccessfulPayloadAsync(client, "/api/Navigation/my-menu");
        return FlattenModules(Property(Property(payload.RootElement, "data"), "items"))
            .Select(module => module.Clone())
            .ToList();
    }

    private static JsonElement Module(IEnumerable<JsonElement> modules, string code) =>
        modules.First(module => string.Equals(Property(module, "moduleCode").GetString(), code, StringComparison.OrdinalIgnoreCase));

    private static int Operation(JsonElement module, string name) =>
        Property(module, "operations").EnumerateArray()
            .First(operation => string.Equals(Property(operation, "name").GetString(), name, StringComparison.OrdinalIgnoreCase))
            .GetProperty("id")
            .GetInt32();

    private static async Task<JsonDocument> GetSuccessfulPayloadAsync(HttpClient client, string url)
    {
        using var response = await client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(response.IsSuccessStatusCode, Is.True, content);
        var payload = JsonDocument.Parse(content);
        Assert.That(Property(payload.RootElement, "isSucceeded").GetBoolean(), Is.True, content);
        return payload;
    }

    private static async Task<JsonDocument> PostSuccessfulPayloadAsync(HttpClient client, string url, object body)
    {
        using var response = await client.PostAsJsonAsync(url, body);
        var content = await response.Content.ReadAsStringAsync();
        Assert.That(response.IsSuccessStatusCode, Is.True, content);
        var payload = JsonDocument.Parse(content);
        Assert.That(Property(payload.RootElement, "isSucceeded").GetBoolean(), Is.True, content);
        return payload;
    }

    [Test]
    [Category("HostLive")]
    public async Task Host_my_menu_operational_modules_have_a_live_read_contract()
    {
        var loginId = Environment.GetEnvironmentVariable("SKOLENZA_HOST_LOGIN_ID");
        var password = Environment.GetEnvironmentVariable("SKOLENZA_HOST_LOGIN_PASSWORD");
        if (string.IsNullOrWhiteSpace(loginId) || string.IsNullOrWhiteSpace(password))
            Assert.Ignore("HostLive requires SKOLENZA_HOST_LOGIN_ID and SKOLENZA_HOST_LOGIN_PASSWORD.");
        using var client = new HttpClient { BaseAddress = new Uri(TestSettings.ApiBaseUrl), Timeout = TimeSpan.FromSeconds(60) };
        using var loginResponse = await client.PostAsJsonAsync("/api/NewLogin/login", new { LoginId = loginId, Password = password, IpAddressLocal = "127.0.0.1", IpAddressPublic = "", MacAddress = "", LoginDevice = 1, Latitude = 0, Longitude = 0 });
        Assert.That(loginResponse.IsSuccessStatusCode, Is.True);
        using var login = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Property(Property(login.RootElement, "data"), "accessToken").GetString());
        using var menuResponse = await client.GetAsync("/api/Navigation/my-menu");
        Assert.That(menuResponse.IsSuccessStatusCode, Is.True);
        using var menu = JsonDocument.Parse(await menuResponse.Content.ReadAsStringAsync());
        var modules = FlattenModules(Property(Property(menu.RootElement, "data"), "items"));
        var institutionListModule = modules.First(x => Property(x, "moduleCode").GetString() == "HOST_INSTITUTION_LIST");
        using var institutionList = await client.GetAsync($"/api/Institution/get-all-institutions?PageNumber=1&PageSize=10&ModuleId={Property(institutionListModule,"id")}&OperationId={ViewOperation(institutionListModule)}");
        Assert.That(institutionList.IsSuccessStatusCode, Is.True);
        using var institutionPayload = JsonDocument.Parse(await institutionList.Content.ReadAsStringAsync());
        var institutionId = Property(institutionPayload.RootElement, "data").EnumerateArray().FirstOrDefault();
        var institutionKey = institutionId.ValueKind == JsonValueKind.Undefined ? null : Property(institutionId, "id").GetString();
        var routes = new Dictionary<string, Func<JsonElement, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["HOST_DEVICE_INVENTORY"] = m => $"/api/DeviceMaster/get-all?PageNumber=1&PageSize=10&ModuleId={Property(m,"id")}&OperationId={ViewOperation(m)}",
            ["INSTITUTION_DEVICE_CONFIG"] = m => $"/api/InstitutionDeviceConfiguration/get-all?PageNumber=1&PageSize=10&ModuleId={Property(m,"id")}&OperationId={ViewOperation(m)}",
            ["INSTITUTION_DEVICES"] = m => $"/api/InstitutionDevice/get-all?PageNumber=1&PageSize=10&ModuleId={Property(m,"id")}&OperationId={ViewOperation(m)}",
            ["HOST_INSTITUTION_LIST"] = m => $"/api/Institution/get-all-institutions?PageNumber=1&PageSize=10&ModuleId={Property(m,"id")}&OperationId={ViewOperation(m)}",
            ["HOST_CAMPUS_LIST"] = m => $"/api/Campus/get-all?IsActive=true&PageNumber=1&PageSize=500&InstitutionId={institutionKey}&ModuleId={Property(m,"id")}&OperationId={ViewOperation(m)}",
            ["HOST_USERS"] = m => $"/api/Host/get-all-host-users?PageNumber=1&PageSize=10&ModuleId={Property(m,"id")}&OperationId={ViewOperation(m)}",
            ["HOST_ROLES"] = m => $"/api/Host/get-all-host-roles?PageNumber=1&PageSize=10&ModuleId={Property(m,"id")}&OperationId={ViewOperation(m)}",
            ["HOST_MODULES"] = m => $"/api/Host/get-host-modules?ModuleId={Property(m,"id")}&OperationId={ViewOperation(m)}",
            ["HOST_OPERATIONS"] = m => $"/api/OperationsMaster/get-all-operations?ModuleId={Property(m,"id")}&OperationId={ViewOperation(m)}",
            ["HOST_MODULE_OPERATIONS"] = m => $"/api/ModuleOperation/get-all?PageNumber=1&PageSize=10&ModuleId={Property(m,"id")}&OperationId={ViewOperation(m)}",
            ["HOST_DEFAULT_EMAIL_CONFIG"] = m => $"/api/DefaultEmailConfig/get-all?PageNumber=1&PageSize=10&ModuleId={Property(m,"id")}&OperationId={ViewOperation(m)}",
            ["HOST_EMAIL_TEMPLATE"] = m => $"/api/EmailTemplate/get-all?PageNumber=1&PageSize=10&ModuleId={Property(m,"id")}&OperationId={ViewOperation(m)}",
            ["HOST_DEVICE_SETUP"] = m => $"/api/DeviceMaster/get-all?PageNumber=1&PageSize=10&ModuleId={Property(m,"id")}&OperationId={ViewOperation(m)}",
            ["HOST_SUBSCRIPTIONS"] = m => "/api/Subscription/get-all-host-subscription-plans",
            ["HOST_INSTITUTION_CARD_INVENTORY"] = m => $"/api/InstitutionCardMaster/get-all?PageNumber=1&PageSize=10&InstitutionId={institutionKey}&ModuleId={Property(m,"id")}&OperationId={ViewOperation(m)}",
            ["HOST_INSTITUTION_EMAIL_CONFIG"] = m => $"/api/InstitutionEmailConfig/get-all?PageNumber=1&PageSize=10&InstitutionId={institutionKey}&ModuleId={Property(m,"id")}&OperationId={ViewOperation(m)}"
        };
        var failures = new List<string>();
        foreach (var module in modules)
        {
            var code = Property(module, "moduleCode").GetString()!;
            if (!routes.TryGetValue(code, out var route)) continue;
            using var response = code.Equals("HOST_SUBSCRIPTIONS", StringComparison.OrdinalIgnoreCase)
                ? await client.PostAsJsonAsync(route(module), new { PageNumber = 1, PageSize = 10 })
                : await client.GetAsync(route(module));
            if (!response.IsSuccessStatusCode) failures.Add($"{code}: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        }
        Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
    }

    private static List<JsonElement> FlattenModules(JsonElement items)
    {
        var result = new List<JsonElement>();
        foreach (var item in items.EnumerateArray())
        {
            result.Add(item);
            if (item.TryGetProperty("children", out var children)) result.AddRange(FlattenModules(children));
        }
        return result;
    }

    private static int ViewOperation(JsonElement module) => module.GetProperty("operations").EnumerateArray()
        .First(x => string.Equals(Property(x, "name").GetString(), "View", StringComparison.OrdinalIgnoreCase)).GetProperty("id").GetInt32();

    [Test]
    public async Task Protected_navigation_endpoint_rejects_an_anonymous_request()
    {
        var response = await Api.GetAsync("/api/Navigation/my-menu");

        Assert.That(response.Status, Is.EqualTo(401));
    }
}
