using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.DTOs.Department;
using skolenza.application.DTOs.Role;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Policy;
using skolenza.application.Exceptions;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("PositionRetirement")]
public sealed class PositionRetirementTests
{
    [Test]
    public void Retired_master_and_foreign_keys_are_absent_from_the_runtime_model()
    {
        using var db = new SkolenzaDbContext(new DbContextOptionsBuilder<SkolenzaDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only").Options);
        Assert.Multiple(() =>
        {
            Assert.That(db.Model.GetEntityTypes().Any(entity => entity.GetTableName() == "Designation"), Is.False);
            Assert.That(db.Model.GetEntityTypes().SelectMany(entity => entity.GetProperties())
                .Any(property => property.Name.Contains("Designation", StringComparison.OrdinalIgnoreCase)), Is.False);
            Assert.That(typeof(StaffMember).GetProperty("DesignationId"), Is.Null);
            Assert.That(typeof(StaffMemberManagerMapping).GetProperty("StaffPositionMasterId"), Is.Not.Null);
            Assert.That(typeof(StaffMemberExperience).GetProperty("PreviousPositionTitle"), Is.Not.Null);
            Assert.That(typeof(StaffMemberExperience).GetProperty("ColleaguePositionTitle"), Is.Not.Null);
            Assert.That(typeof(StaffMemberWorkHistory).GetProperty("PreviousPositionTitle"), Is.Not.Null);
        });
    }

    [TestCase("{\"DesignationId\":42}")]
    [TestCase("{\"DesignationId\":42,\"StaffPositionMasterId\":7}")]
    public void Obsolete_policy_target_cannot_silently_become_an_unrestricted_scope(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PolicyApplicabilityInputDTO>(json));
    }

    [Test]
    public void Retired_import_slot_cannot_be_reused_or_previewed_as_another_master()
    {
        Assert.That((int)BulkImportMaster.RetiredWorkTitle, Is.EqualTo(2));
        Assert.Throws<ValidationErrorException>(() => BulkImportPreviewService.Build(
            BulkImportMaster.RetiredWorkTitle,
            null,
            new BulkImportTableDTO(),
            Array.Empty<GetDepartmentResponseDTO>(),
            Array.Empty<GetRoleResponseDTO>()));
    }

    [Test]
    public void Retired_master_controller_is_not_exposed()
    {
        var assembly = typeof(skolenza.api.Controllers.Staff.StaffController).Assembly;
        Assert.That(assembly.GetTypes().Any(type => type.Name == "DesignationController"), Is.False);
    }

    [Test]
    public void Active_production_seeds_do_not_recreate_the_retired_module()
    {
        var repositoryRoot = FindRepositoryRoot();
        var productionSeed = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "database-scripts",
            "production-seed",
            "04-access-and-host",
            "001-modules-operations-two-host-admins.sql"));
        var consolidatedSeed = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "database-scripts",
            "complete seed data",
            "Skolenza_New_Production_Module_Operation_Seed.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(productionSeed, Does.Not.Contain("'DESIGNATION'"));
            Assert.That(productionSeed, Does.Not.Contain("'INSTITUTION_DESIGNATION'"));
            Assert.That(consolidatedSeed, Does.Not.Contain("'DESIGNATION'"));
            Assert.That(consolidatedSeed, Does.Not.Contain("'INSTITUTION_DESIGNATION'"));
        });
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Skolenza.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root containing Skolenza.sln was not found.");
    }
}
