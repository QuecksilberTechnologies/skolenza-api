using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using skolenza.api.Controllers.EmailTemplate;
using skolenza.api.Controllers.HostDevice;
using skolenza.api.Controllers.Policies;
using skolenza.api.Controllers.InstitutionEmailConfig;
using skolenza.api.Controllers.InstitutionUserAccess;
using skolenza.api.Controllers.Staff;
using skolenza.api.Controllers.StaffEmploymentType;
using skolenza.api.Controllers.ManagerMapping;
using skolenza.api.Controllers.InstitutionConfiguration;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("InstitutionCompatibilityRoutes")]
public sealed class InstitutionCompatibilityRouteTests
{
    public static IEnumerable<TestCaseData> StaffRoutes()
    {
        yield return StaffRouteCase(typeof(StaffController), "api/Staff");
        yield return StaffRouteCase(typeof(BankController), "api/Staff/[controller]");
        yield return StaffRouteCase(typeof(ContactController), "api/Staff/[controller]");
        yield return StaffRouteCase(typeof(DependentController), "api/Staff/[controller]");
        yield return StaffRouteCase(typeof(EducationController), "api/Staff/[controller]");
        yield return StaffRouteCase(typeof(ExperienceController), "api/Staff/[controller]");
        yield return StaffRouteCase(typeof(SensitiveController), "api/Staff/[controller]");
        yield return StaffRouteCase(typeof(StaffMonitoringController), "api/Staff/Monitoring");
        yield return StaffRouteCase(typeof(StaffEmploymentTypeController), "api/StaffEmploymentType");
        yield return StaffRouteCase(typeof(StaffReportingMappingController), "api/StaffReportingMapping");
        yield return StaffRouteCase(typeof(StaffCampusAssignmentController), "api/StaffCampusAssignment");
        yield return StaffRouteCase(typeof(StaffDeviceEnrollmentController), "api/StaffDeviceEnrollment");
        yield return StaffRouteCase(typeof(StaffWorkArrangementController), "api/StaffWorkArrangement");
        yield return StaffRouteCase(typeof(StaffWorkPatternController), "api/StaffWorkPattern");
        yield return StaffRouteCase(typeof(StaffWorkModeOverrideController), "api/StaffWorkModeOverride");
    }

    [TestCaseSource(nameof(StaffRoutes))]
    public void Staff_endpoint_does_not_publish_staffMember_alias(Type controllerType, string staffRoute)
    {
        var routes = controllerType
            .GetCustomAttributes(typeof(RouteAttribute), inherit: true)
            .Cast<RouteAttribute>()
            .Select(attribute => attribute.Template)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(routes, Does.Contain(staffRoute));
            Assert.That(routes, Has.None.Contains("StaffMember"));
        });
    }

    public static IEnumerable<TestCaseData> InstitutionRoutes()
    {
        yield return RouteCase(
            typeof(InstitutionUserAccessController),
            "api/InstitutionUserAccess",
            "api/InstitutionUserAccess");
        yield return RouteCase(
            typeof(InstitutionPolicyController),
            "api/InstitutionPolicy",
            "api/InstitutionPolicy");
        yield return RouteCase(
            typeof(InstitutionEmailTemplateController),
            "api/InstitutionEmailTemplate",
            "api/InstitutionEmailTemplate");
        yield return RouteCase(
            typeof(InstitutionEmailConfigController),
            "api/InstitutionEmailConfig",
            "api/InstitutionEmailConfig");
        yield return RouteCase(
            typeof(InstitutionCardMasterController),
            "api/InstitutionCardMaster",
            "api/InstitutionCardMaster");
        yield return RouteCase(
            typeof(InstitutionCardBulkImportController),
            "api/InstitutionCardMaster/import",
            "api/InstitutionCardMaster/import");
    }

    [TestCaseSource(nameof(InstitutionRoutes))]
    public void Institution_endpoint_keeps_legacy_institution_alias(
        Type controllerType,
        string institutionRoute,
        string legacyRoute)
    {
        var routes = controllerType
            .GetCustomAttributes(typeof(RouteAttribute), inherit: true)
            .Cast<RouteAttribute>()
            .Select(attribute => attribute.Template)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(routes, Does.Contain(institutionRoute));
            Assert.That(routes, Does.Contain(legacyRoute));
        });
    }

    private static TestCaseData RouteCase(
        Type controllerType,
        string institutionRoute,
        string legacyRoute) =>
        new TestCaseData(controllerType, institutionRoute, legacyRoute)
            .SetName($"{controllerType.Name}_publishes_Institution_and_legacy_Institution_routes");

    private static TestCaseData StaffRouteCase(Type controllerType, string staffRoute) =>
        new TestCaseData(controllerType, staffRoute)
            .SetName($"{controllerType.Name}_publishes_only_Staff_route");
}
