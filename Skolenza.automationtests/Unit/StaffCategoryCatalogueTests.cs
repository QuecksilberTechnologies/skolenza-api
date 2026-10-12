using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using NUnit.Framework;
using skolenza.api.Controllers.Staff;
using skolenza.application.Constants;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StaffCategoryCatalogue")]
public sealed class StaffCategoryCatalogueTests
{
    [Test]
    public void Proposed_category_ids_remain_stable()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AppConstants.StaffCategories.Administrative, Is.EqualTo(1));
            Assert.That(AppConstants.StaffCategories.Teaching, Is.EqualTo(2));
            Assert.That(AppConstants.StaffCategories.NonTeaching, Is.EqualTo(3));
            Assert.That(AppConstants.StaffCategories.Support, Is.EqualTo(4));
            Assert.That(AppConstants.StaffCategories.Specialist, Is.EqualTo(5));
        });
    }

    [Test]
    public void Options_endpoint_is_authenticated_get_without_permission_identifiers()
    {
        var action = typeof(StaffCategoryController).GetMethod(nameof(StaffCategoryController.GetOptions));

        Assert.Multiple(() =>
        {
            Assert.That(action, Is.Not.Null);
            Assert.That(typeof(StaffCategoryController).GetCustomAttribute<AuthorizeAttribute>(), Is.Null);
            Assert.That(action!.GetCustomAttribute<AuthorizeAttribute>(), Is.Not.Null);
            Assert.That(action.GetCustomAttribute<HttpMethodAttribute>()?.HttpMethods, Does.Contain("GET"));
            Assert.That(action.GetParameters().Select(parameter => parameter.Name),
                Does.Not.Contain("moduleId").And.Not.Contain("operationId"));
        });
    }

    [Test]
    public void Migration_targets_staffMember_and_contains_the_complete_mapping()
    {
        var root = TestContext.CurrentContext.TestDirectory;
        var repositoryRoot = Path.GetFullPath(Path.Combine(root, "..", "..", "..", ".."));
        var script = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "database-scripts",
            "migrations",
            "20261011_AddStaffCategory.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain("skolenza.\"StaffMember\""));
            Assert.That(script, Does.Not.Contain("skolenza.\"Employee\""));
            Assert.That(script, Does.Contain("'ADMINISTRATIVE'"));
            Assert.That(script, Does.Contain("'SPECIALIST'"));
            Assert.That(script, Does.Contain("WHEN 3 THEN 5"));
            Assert.That(script, Does.Contain("FK_StaffMember_StaffCategory"));
        });
    }

    [Test]
    public void StaffMember_create_detail_and_list_projections_return_the_persisted_category()
    {
        var root = TestContext.CurrentContext.TestDirectory;
        var repositoryRoot = Path.GetFullPath(Path.Combine(root, "..", "..", "..", ".."));
        var source = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "Skolenza.persistance",
            "Repositories",
            "BaseStaffMemberRepository.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("StaffCategoryId = x.StaffMember.StaffCategoryId"));
            Assert.That(source, Does.Contain("StaffCategoryId = x.emp.StaffCategoryId"));
            Assert.That(
                source.Split("StaffCategoryId = emp.StaffCategoryId").Length - 1,
                Is.GreaterThanOrEqualTo(2),
                "Both staff creation response projections must return the persisted category.");
        });
    }
}
