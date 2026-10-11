// ================================================================
// Purpose : Regression coverage for the device configuration, staffMember
//           enrollment, work setup, card inventory, and DDL API surface.
//           These tests intentionally use controller metadata and constants,
//           so they do not require a device, a broker, or a live database.
// ================================================================

using System.Reflection;
using skolenza.api.Controllers.HostDevice;
using skolenza.api.Controllers.InstitutionConfiguration;
using skolenza.application.Constants;
using skolenza.application.Features.StaffMemberCmd;
using skolenza.application.Features.StaffMemberCmd.StaffMemberDeviceEnrollment.Handlers;
using skolenza.application.Features.StaffMemberCmd.StaffMemberWorkInfo.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

/// <summary>
/// Locks down every recently added Device and StaffMember endpoint. A route,
/// verb, authorization attribute, DDL section, or server-owned permission
/// module cannot drift without causing an explicit automated-test failure.
/// </summary>
[TestFixture]
[Category("RecentDeviceStaffMember")]
public sealed class RecentDeviceStaffMemberApiContractTests
{
    public sealed record EndpointContract(Type ControllerType, string HttpMethod, string Template);

    public sealed record DdlContract(string ActionName, string Section, params string[] FieldKeys);

    public static IEnumerable<TestCaseData> EndpointContracts() =>
        BuildEndpointContracts().Select(contract => new TestCaseData(contract)
            .SetName($"{contract.ControllerType.Name}_{contract.HttpMethod}_{contract.Template.Replace('/', '_')}"));

    public static IEnumerable<TestCaseData> DdlContracts() =>
        new[]
        {
            new DdlContract(nameof(DeviceDdlOptionsController.GetTime), DeviceDdl.Time, "timeFormat", "dateFormat", "networkTimeEnabled", "timeZone"),
            new DdlContract(nameof(DeviceDdlOptionsController.GetBell), DeviceDdl.Bell, "ringStyle", "bellOutput"),
            new DdlContract(nameof(DeviceDdlOptionsController.GetDeviceSetup), DeviceDdl.DeviceSetup, "language", "screenWakeUpMethod", "resultDisplayStyle", "faceRecognitionDistance"),
            new DdlContract(nameof(DeviceDdlOptionsController.GetAdvanced), DeviceDdl.Advanced, "verificationMode", "qrCodeMode", "fillLightMode"),
            new DdlContract(nameof(DeviceDdlOptionsController.GetLock), DeviceDdl.Lock, "doorSensorMode", "antiPassbackMode", "wiegandOutput", "wiegandFormat", "cardDisplayFormat"),
            new DdlContract(nameof(DeviceDdlOptionsController.GetSerial), DeviceDdl.Serial, "baudRate", "serialFunction"),
            new DdlContract(nameof(DeviceDdlOptionsController.GetEthernet), DeviceDdl.Ethernet, "dhcpEnabled"),
            new DdlContract(nameof(DeviceDdlOptionsController.GetWifi), DeviceDdl.Wifi, "dhcpEnabled"),
            new DdlContract(nameof(DeviceDdlOptionsController.GetAppNotification), DeviceDdl.AppNotification, "appNotificationEnabled", "notificationType"),
            new DdlContract(nameof(DeviceDdlOptionsController.GetStaffMemberDeviceCredentials), DeviceDdl.StaffMemberDeviceCredentials, "credentialType"),
            new DdlContract(nameof(DeviceDdlOptionsController.GetStaffMemberDeviceAccessWindows), DeviceDdl.StaffMemberDeviceAccessWindows, "dayOfWeek"),
            new DdlContract(nameof(DeviceDdlOptionsController.GetInstitutionCardInventory), DeviceDdl.InstitutionCardInventory, "taxTreatment", "cardStatus")
        }.Select(contract => new TestCaseData(contract).SetName($"DDL_{contract.Section}"));

    public static IEnumerable<TestCaseData> StaffMemberPermissionModules() =>
        new[]
        {
            new { Request = typeof(GetStaffMemberDeviceEnrollmentsQuery), ExpectedModule = "STAFF_DEVICE_ENROLMENTS" },
            new { Request = typeof(GetStaffMemberLocationAssignmentsQuery), ExpectedModule = "STAFF_CAMPUS_ASSIGNMENTS" },
            new { Request = typeof(GetStaffMemberWorkArrangementsQuery), ExpectedModule = "STAFF_WORK_ARRANGEMENTS" },
            new { Request = typeof(GetStaffMemberWorkPatternsQuery), ExpectedModule = "STAFF_WORK_PATTERNS" },
            new { Request = typeof(GetStaffMemberWorkModeOverridesQuery), ExpectedModule = "STAFF_WORK_MODE_OVERRIDES" }
        }.Select(test => new TestCaseData(test.Request, test.ExpectedModule)
            .SetName($"{test.Request.Name}_requires_{test.ExpectedModule}"));

    [TestCaseSource(nameof(EndpointContracts))]
    public void Recent_endpoint_keeps_its_expected_route_verb_and_authorization(EndpointContract contract)
    {
        var routes = contract.ControllerType.GetCustomAttributes<RouteAttribute>(inherit: true).ToArray();
        var endpointExists = contract.ControllerType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>(inherit: true))
            .Any(attribute =>
                string.Equals(attribute.Template, contract.Template, StringComparison.Ordinal)
                && attribute.HttpMethods.Contains(contract.HttpMethod, StringComparer.OrdinalIgnoreCase));

        Assert.Multiple(() =>
        {
            Assert.That(routes.Select(route => route.Template), Has.Some.Not.Null.And.Not.Empty,
                $"{contract.ControllerType.Name} must declare an API route.");
            Assert.That(contract.ControllerType.IsDefined(typeof(AuthorizeAttribute), inherit: true), Is.True,
                $"{contract.ControllerType.Name} must remain authenticated.");
            Assert.That(endpointExists, Is.True,
                $"Missing {contract.HttpMethod} {contract.Template} on {contract.ControllerType.Name}.");
        });
    }

    [TestCaseSource(nameof(DdlContracts))]
    public void Every_ddl_endpoint_returns_only_its_owned_complete_constant_section(DdlContract contract)
    {
        var controller = new DeviceDdlOptionsController();
        var action = typeof(DeviceDdlOptionsController).GetMethod(contract.ActionName, BindingFlags.Instance | BindingFlags.Public)
            ?? throw new AssertionException($"DDL action {contract.ActionName} was not found.");
        var result = action.Invoke(controller, null) as OkObjectResult;
        var response = result?.Value as skolenza.application.Wrappers.ApiResponse<IReadOnlyList<DeviceDdlField>>;

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(response, Is.Not.Null);
            Assert.That(response!.IsSucceeded, Is.True);
            Assert.That(response.Data.Select(field => field.Key), Is.EquivalentTo(contract.FieldKeys));
            Assert.That(response.Data.SelectMany(field => field.Options), Is.Not.Empty);
            Assert.That(response.Data.SelectMany(field => field.Options).All(option =>
                !string.IsNullOrWhiteSpace(option.Value) && !string.IsNullOrWhiteSpace(option.Label)), Is.True);
            Assert.That(DeviceDdl.GetSection(contract.Section).Select(field => field.Key), Is.EquivalentTo(contract.FieldKeys));
        });
    }

    [Test]
    public void Device_ddl_catalog_contains_every_section_exposed_by_the_controller()
    {
        var sections = DeviceDdl.GetSectionNames();

        Assert.That(sections, Is.EquivalentTo(new[]
        {
            DeviceDdl.Time,
            DeviceDdl.Bell,
            DeviceDdl.DeviceSetup,
            DeviceDdl.Advanced,
            DeviceDdl.Lock,
            DeviceDdl.Serial,
            DeviceDdl.Ethernet,
            DeviceDdl.Wifi,
            DeviceDdl.AppNotification,
            DeviceDdl.StaffMemberDeviceCredentials,
            DeviceDdl.StaffMemberDeviceAccessWindows,
            DeviceDdl.InstitutionCardInventory
        }));
    }

    [TestCaseSource(nameof(StaffMemberPermissionModules))]
    public void StaffMember_request_family_is_bound_to_its_server_owned_permission_module(Type requestType, string expectedModuleCode)
    {
        var behaviorType = typeof(StaffMemberInstitutionPermissionBehavior<,>).MakeGenericType(requestType, typeof(object));
        var resolver = behaviorType.GetMethod("ResolveExpectedModuleCode", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new AssertionException("StaffMember permission module resolver was not found.");

        var actualModuleCode = resolver.Invoke(null, null) as string;

        Assert.That(actualModuleCode, Is.EqualTo(expectedModuleCode));
    }

    [Test]
    public void StaffMember_device_contract_keeps_credentials_and_physical_enroll_id_out_of_the_ui_response()
    {
        var responseType = typeof(skolenza.application.DTOS.InstitutionConfiguration.StaffMemberDeviceEnrollmentResponseDTO);

        Assert.Multiple(() =>
        {
            Assert.That(responseType.GetProperty("Pin"), Is.Null);
            Assert.That(responseType.GetProperty("CardNumber"), Is.Null);
            Assert.That(responseType.GetProperty("FaceImage"), Is.Null);
            Assert.That(responseType.GetProperty("DeviceEnrollId"), Is.Null);
            Assert.That(responseType.GetProperty("FaceCommandStatus"), Is.Not.Null);
            Assert.That(responseType.GetProperty("CardCommandStatus"), Is.Not.Null);
            Assert.That(responseType.GetProperty("PinCommandStatus"), Is.Not.Null);
            Assert.That(responseType.GetProperty("UserActivationCommandStatus"), Is.Not.Null);
        });
    }

    private static IEnumerable<EndpointContract> BuildEndpointContracts()
    {
        // Institution Device lifecycle APIs.
        foreach (var contract in Crud(typeof(InstitutionDeviceController), "{id}"))
            yield return contract;
        yield return Endpoint(typeof(InstitutionDeviceController), "POST", "update-location");

        // Host provisioning + Institution device connectivity and runtime APIs.
        foreach (var template in new[]
        {
            "issue-bootstrap-url", "apply-runtime-configuration", "reboot",
            "settings/time", "settings/time/sync", "settings/bell", "settings/device-setup",
            "settings/advanced", "settings/lock", "settings/serial", "settings/ethernet",
            "settings/wifi", "settings/app-notification", "settings/web-access", "settings/screen-menu-pin",
            "replace-https-gateway-url", "dispatch-mqtts-now", "create", "update", "rotate-https-ingress-token"
        })
            yield return Endpoint(typeof(InstitutionDeviceConfigurationController), "POST", template);
        yield return Endpoint(typeof(InstitutionDeviceConfigurationController), "GET", "gateway-address/{institutionDeviceId}");
        yield return Endpoint(typeof(InstitutionDeviceConfigurationController), "GET", "get-by-id/{id}");
        yield return Endpoint(typeof(InstitutionDeviceConfigurationController), "GET", "get-all");
        yield return Endpoint(typeof(InstitutionDeviceConfigurationController), "DELETE", "delete/{id}");

        // Dropdown API: one endpoint per UI section; no Angular hard-coded values.
        foreach (var template in new[]
        {
            "time", "bell", "device-setup", "advanced", "lock", "serial", "ethernet", "wifi",
            "app-notification", "staff-member-device-credentials", "staff-member-device-access-windows", "institution-card-inventory"
        })
            yield return Endpoint(typeof(DeviceDdlOptionsController), "GET", template);

        // Institution location, attendance, staffMember work, and Host card lifecycle APIs.
        foreach (var contract in Crud(typeof(CampusController), "{id:long}"))
            yield return contract;
        // The legacy AttendancePolicy controller is intentionally disabled. Generic institution
        // policies are covered by the dedicated InstitutionPolicy contract tests.
        foreach (var contract in Crud(typeof(StaffCampusAssignmentController), "{id:long}"))
            yield return contract;
        foreach (var contract in Crud(typeof(StaffWorkArrangementController), "{id:long}"))
            yield return contract;
        foreach (var contract in Crud(typeof(StaffWorkPatternController), "{id:long}"))
            yield return contract;
        foreach (var contract in Crud(typeof(StaffWorkModeOverrideController), "{id:long}"))
            yield return contract;
        yield return Endpoint(typeof(StaffWorkModeOverrideController), "POST", "approve");
        yield return Endpoint(typeof(StaffWorkModeOverrideController), "POST", "reject");
        foreach (var contract in Crud(typeof(InstitutionCardMasterController), "{id}"))
            yield return contract;

        // StaffMember device enrollment owns credentials in addition to standard lifecycle calls.
        foreach (var contract in Crud(typeof(StaffDeviceEnrollmentController), "{id}"))
            yield return contract;
        foreach (var template in new[] { "face/upsert", "pin/upsert", "card/bind", "credential/remove" })
            yield return Endpoint(typeof(StaffDeviceEnrollmentController), "POST", template);
    }

    private static IEnumerable<EndpointContract> Crud(Type controllerType, string idTemplate)
    {
        yield return Endpoint(controllerType, "POST", "create");
        yield return Endpoint(controllerType, "GET", $"get-by-id/{idTemplate}");
        yield return Endpoint(controllerType, "GET", "get-all");
        yield return Endpoint(controllerType, "POST", "update");
        yield return Endpoint(controllerType, "POST", "update-status");
        yield return Endpoint(controllerType, "DELETE", $"delete/{idTemplate}");
    }

    private static EndpointContract Endpoint(Type controllerType, string httpMethod, string template) =>
        new(controllerType, httpMethod, template);
}


