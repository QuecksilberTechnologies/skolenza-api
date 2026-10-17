using System.Reflection;
using skolenza.api.Controllers.Dashboard;
using skolenza.application.DTOS.Dashboard;
using skolenza.application.Wrappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using skolenza.persistance.Data.Context;
using skolenza.persistance.Repositories;

namespace skolenza.automationtests.Unit;

[TestFixture]
public sealed class SuperAdminDashboardApiContractTests
{
    [Test]
    [Category("DashboardDatabase")]
    public async Task Department_headcount_executes_in_PostgreSQL_and_preserves_tenant_counts_and_order()
    {
        var settingsPath = Environment.GetEnvironmentVariable("SKOLENZA_HOST_DB_SETTINGS");
        if (string.IsNullOrWhiteSpace(settingsPath))
        {
            Assert.Ignore("Opt-in local PostgreSQL read-only probe.");
        }

        var configuration = new ConfigurationBuilder().AddJsonFile(settingsPath!).Build();
        await using var db = new SkolenzaDbContext(new DbContextOptionsBuilder<SkolenzaDbContext>()
            .UseNpgsql(configuration.GetConnectionString("DefaultConnection")).Options);
        var institutionId = await db.Departments
            .Where(x => x.IsActive && !x.IsSoftDeleted && x.InstitutionId.HasValue)
            .Select(x => x.InstitutionId!.Value)
            .FirstAsync();
        var departments = await db.Departments.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted)
            .ToListAsync();
        var staff = await db.StaffMembers.AsNoTracking()
            .Where(x => x.InstitutionId == institutionId && x.IsActive && !x.IsSoftDeleted)
            .Select(x => x.DepartmentId)
            .ToListAsync();
        var repository = new SuperAdminDashboardRepository(db, null!);

        var actual = await repository.GetDepartmentHeadcountAsync(institutionId, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(actual.Select(x => x.DepartmentId), Is.EquivalentTo(departments.Select(x => x.Id)));
            Assert.That(actual.Select(x => x.Headcount), Is.Ordered.Descending);
            foreach (var row in actual)
            {
                Assert.That(row.Headcount, Is.EqualTo(staff.Count(id => id == row.DepartmentId)));
                Assert.That(row.DepartmentName, Is.EqualTo(departments.Single(x => x.Id == row.DepartmentId).DepartmentName));
            }
        });
        var anotherInstitution = await repository.GetDepartmentHeadcountAsync(long.MaxValue, CancellationToken.None);
        Assert.That(anotherInstitution, Is.Empty);
    }

    [Test]
    public void Dashboard_controller_exposes_legacy_and_generic_data_routes()
    {
        var routes = typeof(DashboardController).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>())
            .Select(attribute => $"{attribute.HttpMethods.Single()}:{attribute.Template}")
            .ToArray();

        Assert.That(routes, Is.EquivalentTo(new[]
        {
            "GET:{roleTypeCode}",
            "GET:SuperAdmin/Summary",
            "GET:SuperAdmin/StaffMemberOverview",
            "GET:SuperAdmin/Birthdays",
            "GET:SuperAdmin/Onboarding",
            "GET:SuperAdmin/DepartmentHeadcount",
            "GET:SuperAdmin/CurrentlyOnLeave",
            "GET:SuperAdmin/Locations",
            "GET:SuperAdmin/Storage",
            "GET:SuperAdmin/HiringPipeline"
        }));
    }

    [Test]
    public void Generic_dashboard_contract_exposes_one_role_specific_data_envelope()
    {
        var action = typeof(DashboardController).GetMethod(nameof(DashboardController.Data));
        var successResponse = action?.GetCustomAttributes<ProducesResponseTypeAttribute>()
            .SingleOrDefault(attribute => attribute.StatusCode == 200);

        Assert.Multiple(() =>
        {
            Assert.That(successResponse?.Type, Is.EqualTo(typeof(ApiResponse<DashboardDataDTO>)));
            Assert.That(typeof(DashboardDataDTO).GetProperty(nameof(DashboardDataDTO.RoleTypeCode)), Is.Not.Null);
            Assert.That(typeof(DashboardDataDTO).GetProperty(nameof(DashboardDataDTO.InstitutionAdministrator)), Is.Not.Null);
            Assert.That(typeof(DashboardDataDTO).GetProperty(nameof(DashboardDataDTO.StaffManager)), Is.Not.Null);
            Assert.That(typeof(DashboardDataDTO).GetProperty(nameof(DashboardDataDTO.StaffMember)), Is.Not.Null);
            Assert.That(typeof(DashboardDataDTO).GetProperty(nameof(DashboardDataDTO.ExternalUser)), Is.Not.Null);
            Assert.That(typeof(DashboardSectionDTO<>).GetProperty(nameof(DashboardSectionDTO<object>.IsPlaceholder)), Is.Not.Null);
            Assert.That(typeof(DashboardSectionDTO<>).GetProperty(nameof(DashboardSectionDTO<object>.Source)), Is.Not.Null);
        });
    }

    [Test]
    public void Generic_dashboard_uses_trusted_role_context_and_non_blocking_async_calls()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRoot(),
            "skolenza.application",
            "Features",
            "DashboardCmd",
            "DashboardDataHandler.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("ValidateInstitutionPermissionAsync("));
            Assert.That(source, Does.Contain("request.Permission"));
            Assert.That(source, Does.Contain("request.RoleTypeCode"));
            Assert.That(source, Does.Contain("StringComparison.OrdinalIgnoreCase"));
            Assert.That(source, Does.Contain("GetRoleTypeCode(context.RoleTypeId)"));
            Assert.That(source, Does.Contain("RoleTypeManagerCode"));
            Assert.That(source, Does.Contain("RoleTypeStaffMemberCode"));
            Assert.That(source, Does.Contain("RoleTypeClientCode"));
            Assert.That(source, Does.Contain("TEMPORARY_STATIC"));
            Assert.That(source, Does.Contain("generatedAtUtc.Date.AddDays(-30)"));
            Assert.That(source, Does.Contain("generatedAtUtc.Date.AddDays(-7)"));
            Assert.That(source, Does.Not.Contain(".Result"));
            Assert.That(source, Does.Not.Contain(".Wait()"));
        });
    }

    [Test]
    public void Legacy_onboarding_uses_the_same_thirty_day_window()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRoot(),
            "skolenza.application",
            "Features",
            "DashboardCmd",
            "SuperAdminDashboardHandlers.cs"));

        Assert.That(source, Does.Contain("DateTime.UtcNow.Date.AddDays(-30)"));
    }

    [Test]
    public void Birthday_contract_carries_staffMember_context_without_numeric_staffMember_identifier()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(DashboardStaffMemberDTO).GetProperty(nameof(DashboardStaffMemberDTO.StaffMemberId))?.PropertyType, Is.EqualTo(typeof(string)));
            Assert.That(typeof(DashboardStaffMemberDTO).GetProperty(nameof(DashboardStaffMemberDTO.DepartmentName)), Is.Not.Null);
            Assert.That(typeof(DashboardStaffMemberDTO).GetProperty(nameof(DashboardStaffMemberDTO.PositionName)), Is.Not.Null);
            Assert.That(typeof(DashboardStaffMemberDTO).GetProperty("DesignationName"), Is.Null);
            Assert.That(typeof(DashboardStaffMemberDTO).GetProperty(nameof(DashboardStaffMemberDTO.RoleName)), Is.Not.Null);
        });
    }

    [Test]
    public void Dashboard_handlers_enforce_permission_pipeline_and_super_admin_role()
    {
        var source = File.ReadAllText(Path.Combine(FindRoot(), "skolenza.application", "Features", "DashboardCmd", "SuperAdminDashboardHandlers.cs"));
        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("ValidateInstitutionPermissionAsync(permission, cancellationToken)"));
            Assert.That(source, Does.Contain("context.RoleTypeId != ConstantValues.RoleTypeAdmin"));
            Assert.That(source, Does.Contain("ForbiddenAccessException"));
        });
    }

    private static string FindRoot()
    {
        var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Skolenza.sln"))) current = current.Parent;
        return current?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
