using NUnit.Framework;
using Microsoft.EntityFrameworkCore;
using skolenza.api.Controllers.Staff;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using skolenza.application.DTOS.Staff;
using skolenza.persistance.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StaffProfessionalStructure")]
public sealed class StaffProfessionalStructureTests
{
    [Test]
    [Category("PositionCutoverDatabase")]
    public async Task New_staff_creation_materializes_without_a_Position_then_resolves_its_assignment()
    {
        var settingsPath = Environment.GetEnvironmentVariable("SKOLENZA_HOST_DB_SETTINGS");
        if (string.IsNullOrWhiteSpace(settingsPath))
        {
            Assert.Ignore("Opt-in real database rollback test.");
        }
        var configuration = new ConfigurationBuilder().AddJsonFile(settingsPath!).Build();
        await using var db = new SkolenzaDbContext(new DbContextOptionsBuilder<SkolenzaDbContext>()
            .UseNpgsql(configuration.GetConnectionString("DefaultConnection")).Options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var template = await db.StaffMembers.FirstAsync(item => item.IsActive && !item.IsSoftDeleted);
            var staff = (StaffMember)db.Entry(template).CurrentValues.ToObject();
            staff.Id = 0;
            staff.FirstName = "Position";
            staff.LastName = "Rollback Probe";
            staff.OfficialEmail = "position-" + Guid.NewGuid().ToString("N") + "@example.test";
            staff.EmployementCode = "PROBE-" + Guid.NewGuid().ToString("N")[..12];
            staff.DateOfOnBoarding = DateTime.UtcNow;
            var roleId = await db.Roles.Where(item => item.InstitutionId == staff.InstitutionId && item.IsActive)
                .Select(item => item.Id).FirstAsync();
            var repository = new BaseStaffMemberRepository(db, null!, NullLogger<BaseStaffMemberRepository>.Instance, null!, null!);
            var result = await repository.CreateStaffMemberAsync(staff,
                new LoginCredential { InstitutionId = staff.InstitutionId, LoginId = staff.OfficialEmail, IsActive = true, StaffMember = staff },
                new UserRole { StaffMember = staff, RoleId = roleId, RoleStartDate = DateTime.UtcNow, IsActive = true, IsPrimaryRole = true, AddedById = template.Id, AddedDateTime = DateTime.UtcNow });
            Assert.That(result.StaffPositionMasterId, Is.Zero);

            var position = await db.StaffPositionMasters.FirstAsync(item => item.InstitutionId == staff.InstitutionId &&
                item.InstitutionStaffCategoryMasterId == staff.InstitutionStaffCategoryMasterId && item.IsActive && !item.IsSoftDeleted);
            var professional = new StaffProfessionalStructureRepository(db);
            await professional.AssignPrimaryPositionAsync(staff.InstitutionId!.Value, staff.Id, position.Id,
                DateOnly.FromDateTime(DateTime.UtcNow), template.Id);
            var profile = await professional.GetStaffProfileAsync(staff.InstitutionId.Value, staff.Id);
            Assert.That(profile?.StaffPositionMasterId, Is.EqualTo(position.Id));
            Assert.That(profile?.PositionName, Is.EqualTo(position.Name));
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    [Test]
    [Category("PositionCutoverDatabase")]
    public async Task Scheduling_a_future_Position_keeps_the_current_Position_effective_until_the_change_date()
    {
        var settingsPath = Environment.GetEnvironmentVariable("SKOLENZA_HOST_DB_SETTINGS");
        if (string.IsNullOrWhiteSpace(settingsPath))
        {
            Assert.Ignore("Opt-in real database rollback test.");
        }
        var configuration = new ConfigurationBuilder().AddJsonFile(settingsPath!).Build();
        await using var db = new SkolenzaDbContext(new DbContextOptionsBuilder<SkolenzaDbContext>()
            .UseNpgsql(configuration.GetConnectionString("DefaultConnection")).Options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var target = await (from staff in db.StaffMembers
                            join position in db.StaffPositionMasters on staff.InstitutionStaffCategoryMasterId equals position.InstitutionStaffCategoryMasterId
                            where staff.InstitutionId == position.InstitutionId && !staff.IsSoftDeleted &&
                                position.IsActive && !position.IsSoftDeleted && !position.DepartmentId.HasValue
                            select new { StaffId = staff.Id, InstitutionId = position.InstitutionId!.Value, Position = position })
            .FirstAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var repository = new StaffProfessionalStructureRepository(db);
        var current = await repository.GetStaffProfileAsync(target.InstitutionId, target.StaffId);
        if (current is null)
        {
            await repository.AssignPrimaryPositionAsync(target.InstitutionId, target.StaffId,
                target.Position.Id, today.AddDays(-1), target.StaffId);
            current = await repository.GetStaffProfileAsync(target.InstitutionId, target.StaffId);
        }
        var future = await repository.SavePositionAsync(new StaffPositionMasterRequestDTO
        {
            Code = "FUTURE_PROBE_" + Guid.NewGuid().ToString("N"),
            Name = "Future Position rollback probe",
            StaffJobFamilyMasterId = target.Position.StaffJobFamilyMasterId,
            InstitutionStaffCategoryMasterId = target.Position.InstitutionStaffCategoryMasterId!.Value,
            IsActive = true
        }, target.InstitutionId, target.StaffId);
        var changeDate = today.AddDays(30);
        await repository.ReplacePrimaryPositionAsync(target.InstitutionId, target.StaffId,
            future.Id, changeDate, target.StaffId);
        var stillCurrent = await repository.GetStaffProfileAsync(target.InstitutionId, target.StaffId);
        var atChangeDate = await StaffProfessionalStructureRepository.CurrentPositionIdentities(db, changeDate)
            .Where(position => position.StaffMemberId == target.StaffId).SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(stillCurrent?.StaffPositionMasterId, Is.EqualTo(current!.StaffPositionMasterId));
            Assert.That(atChangeDate.StaffPositionMasterId, Is.EqualTo(future.Id));
        });
        await transaction.RollbackAsync();
    }

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
