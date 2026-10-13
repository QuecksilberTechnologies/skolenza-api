using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using skolenza.api.Controllers.Staff;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StaffProfessionalStructure")]
public sealed class StaffProfessionalStructureTests
{
    [Test]
    public void Persistence_model_builds_and_maps_each_professional_master_to_its_own_table()
    {
        using var context = new SkolenzaDbContext(
            new DbContextOptionsBuilder<SkolenzaDbContext>()
                .UseNpgsql("Host=localhost;Database=skolenza_model_validation;Username=postgres;Password=not-used")
                .Options);

        Assert.Multiple(() =>
        {
            Assert.That(context.Model.FindEntityType(typeof(InstitutionMasterEntity)), Is.Not.Null);
            Assert.That(context.Model.FindEntityType(typeof(StaffJobFamilyMaster))?.GetTableName(), Is.EqualTo("StaffJobFamilyMaster"));
            Assert.That(context.Model.FindEntityType(typeof(StaffCapabilityMaster))?.GetTableName(), Is.EqualTo("StaffCapabilityMaster"));
            Assert.That(context.Model.FindEntityType(typeof(StaffDutyDefinitionMaster))?.GetTableName(), Is.EqualTo("StaffDutyDefinitionMaster"));
            Assert.That(context.Model.FindEntityType(typeof(StaffPositionMaster))?.GetTableName(), Is.EqualTo("StaffPositionMaster"));
            Assert.That(context.Model.FindEntityType(typeof(InstitutionStaffCategoryMaster))?.GetTableName(), Is.EqualTo("InstitutionStaffCategoryMaster"));
            Assert.That(context.Model.FindEntityType(typeof(StaffCategoryJobFamily))?.GetTableName(), Is.EqualTo("StaffCategoryJobFamily"));
        });
    }

    [Test]
    public void Staff_create_and_official_update_expose_position_contract()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(CreateBaseStaffMemberRequestDTO).GetProperty("StaffPositionMasterId"), Is.Not.Null);
            Assert.That(typeof(CreateBaseStaffMemberRequestDTO).GetProperty("InstitutionStaffCategoryMasterId"), Is.Not.Null);
            Assert.That(typeof(UpdateStaffMemberRequestOfficialDTO).GetProperty("StaffPositionMasterId"), Is.Not.Null);
            Assert.That(typeof(UpdateStaffMemberRequestOfficialDTO).GetProperty("InstitutionStaffCategoryMasterId"), Is.Not.Null);
            Assert.That(typeof(GetBaseStaffMemberResponseDTO).GetProperty("ProfessionalProfile"), Is.Not.Null);
        });
    }

    [Test]
    public void Controller_exposes_master_position_profile_and_approval_endpoints()
    {
        var methods = typeof(StaffProfessionalStructureController).GetMethods().Select(method => method.Name).ToHashSet();
        Assert.Multiple(() =>
        {
            Assert.That(methods, Does.Contain("GetMasters"));
            Assert.That(methods, Does.Contain("GetCategories"));
            Assert.That(methods, Does.Contain("SetCategoryJobFamilies"));
            Assert.That(methods, Does.Contain("SetCategoryActive"));
            Assert.That(methods, Does.Contain("CreateMaster"));
            Assert.That(methods, Does.Contain("UpdateMaster"));
            Assert.That(methods, Does.Contain("DeleteMaster"));
            Assert.That(methods, Does.Contain("GetPositions"));
            Assert.That(methods, Does.Contain("GetStaffProfile"));
            Assert.That(methods, Does.Contain("VerifyCapability"));
        });
    }

    [Test]
    public void Migration_uses_master_suffix_and_enforces_primary_position_overlap()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        var script = File.ReadAllText(Path.Combine(repositoryRoot, "database-scripts", "migrations", "20261012_AddStaffProfessionalMastersAndAssignments.sql"));
        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain("\"StaffJobFamilyMaster\""));
            Assert.That(script, Does.Contain("\"StaffPositionMaster\""));
            Assert.That(script, Does.Contain("\"StaffCapabilityMaster\""));
            Assert.That(script, Does.Contain("\"StaffDutyDefinitionMaster\""));
            Assert.That(script, Does.Contain("ValidatePrimaryStaffPositionAssignment"));
            Assert.That(script, Does.Contain("SeedInstitutionStaffProfessionalMasters"));
        });
    }

    [Test]
    public void Category_hierarchy_migration_is_idempotent_and_backfills_staff_and_positions()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        var script = File.ReadAllText(Path.Combine(repositoryRoot, "database-scripts", "migrations", "20261013_AddInstitutionStaffCategoryHierarchy.sql"));
        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain("CREATE TABLE IF NOT EXISTS skolenza.\"InstitutionStaffCategoryMaster\""));
            Assert.That(script, Does.Contain("CREATE TABLE IF NOT EXISTS skolenza.\"StaffCategoryJobFamily\""));
            Assert.That(script, Does.Contain("SeedInstitutionStaffCategoryHierarchy"));
            Assert.That(script, Does.Contain("UPDATE skolenza.\"StaffPositionMaster\""));
            Assert.That(script, Does.Contain("UPDATE skolenza.\"StaffMember\""));
            Assert.That(script, Does.Contain("pg_constraint"));
        });
    }
}
