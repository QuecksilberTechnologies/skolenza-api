using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Pagination;
using skolenza.application.Wrappers;
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

    #region Professional List Pagination

    [TestCase("GetCategories")]
    [TestCase("GetMasters")]
    [TestCase("GetPositions")]
    public void Professional_list_actions_keep_authorization_and_bind_central_paging_defaults(string name)
    {
        var action = typeof(StaffProfessionalStructureController).GetMethod(name)!;
        var dto = action.GetParameters().Single(item => item.Name == "dto");
        var request = new StaffProfessionalPagedRequestDTO();
        Assert.Multiple(() =>
        {
            Assert.That(dto.ParameterType, Is.EqualTo(typeof(StaffProfessionalPagedRequestDTO)));
            Assert.That(dto.GetCustomAttribute<FromQueryAttribute>(), Is.Not.Null);
            Assert.That(request, Is.InstanceOf<PermissionRequestDTO>());
            Assert.That(request.PageNumber, Is.EqualTo(1));
            Assert.That(request.PageSize, Is.EqualTo(10));
            Assert.That(typeof(StaffProfessionalStructureController).GetCustomAttribute<AuthorizeAttribute>(), Is.Not.Null);
        });
    }

    [Test]
    public void Professional_list_envelope_uses_root_totals_without_profile_completion_flags()
    {
        var result = ApiResponse<List<StaffProfessionalMasterResponseDTO>>.SuccessPaginated([], 2, 10, 23, 3);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var root = json.RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("data").ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(root.GetProperty("totalRecords").GetInt32(), Is.EqualTo(23));
            Assert.That(root.GetProperty("totalPages").GetInt32(), Is.EqualTo(3));
            Assert.That(root.TryGetProperty("completionPercentage", out _), Is.False);
            Assert.That(root.TryGetProperty("isPrimaryMarked", out _), Is.False);
            Assert.That(root.TryGetProperty("hasAllDocUploaded", out _), Is.False);
        });
    }

    [Test]
    [Category("ProfessionalPaginationDatabase")]
    public async Task Five_professional_catalogues_page_with_filters_full_dependencies_and_beyond_first_page_saves()
    {
        var settings = Environment.GetEnvironmentVariable("SKOLENZA_HOST_DB_SETTINGS");
        if (string.IsNullOrWhiteSpace(settings))
        {
            Assert.Ignore("Opt-in PostgreSQL rollback test.");
        }
        var configuration = new ConfigurationBuilder().AddJsonFile(settings!).Build();
        await using var db = new SkolenzaDbContext(new DbContextOptionsBuilder<SkolenzaDbContext>()
            .UseNpgsql(configuration.GetConnectionString("DefaultConnection")).Options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var template = await db.StaffPositionMasters.AsNoTracking().FirstAsync(item => item.InstitutionId.HasValue && !item.IsSoftDeleted);
            var institutionId = template.InstitutionId!.Value;
            var actorId = await db.StaffMembers.Where(item => item.InstitutionId == institutionId && !item.IsSoftDeleted).Select(item => item.Id).FirstAsync();
            var categories = await CreatePagingCatalogueAsync(db, db.InstitutionStaffCategoryMasters, institutionId);
            var families = await CreatePagingCatalogueAsync(db, db.StaffJobFamilyMasters, institutionId);
            var capabilities = await CreatePagingCatalogueAsync(db, db.StaffCapabilityMasters, institutionId);
            var duties = await CreatePagingCatalogueAsync(db, db.StaffDutyDefinitionMasters, institutionId);
            var positions = await CreatePagingCatalogueAsync(db, db.StaffPositionMasters, institutionId, item =>
            {
                item.InstitutionStaffCategoryMasterId = categories[0].Id;
                item.StaffCategoryId = categories[0].StaffCategoryTypeId;
                item.StaffJobFamilyMasterId = families[0].Id;
                item.DepartmentId = null;
            });
            db.StaffCategoryJobFamilies.AddRange(families.Select(item => new StaffCategoryJobFamily
            {
                InstitutionId = institutionId,
                InstitutionStaffCategoryMasterId = categories[0].Id,
                StaffJobFamilyMasterId = item.Id,
                IsActive = true,
                AddedById = actorId,
                AddedDateTime = DateTime.UtcNow
            }));
            db.StaffPositionCapabilities.AddRange(positions.SelectMany(position => capabilities.Select(item => new StaffPositionCapability
            {
                InstitutionId = institutionId,
                StaffPositionMasterId = position.Id,
                StaffCapabilityMasterId = item.Id,
                IsMandatory = true,
                AddedById = actorId,
                AddedDateTime = DateTime.UtcNow
            })));
            db.StaffPositionDuties.AddRange(positions.SelectMany(position => duties.Select(item => new StaffPositionDuty
            {
                InstitutionId = institutionId,
                StaffPositionMasterId = position.Id,
                StaffDutyDefinitionMasterId = item.Id,
                IsMandatory = false,
                AddedById = actorId,
                AddedDateTime = DateTime.UtcNow
            })));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var repository = new StaffProfessionalStructureRepository(db);
            foreach (var activeOnly in new[] { false, true })
            {
                foreach (var type in Enum.GetValues<StaffProfessionalMasterType>())
                {
                    var all = (await repository.GetMastersAsync(type, institutionId, activeOnly))
                        .OrderBy(item => item.DisplayOrder).ThenBy(item => item.Name).ThenBy(item => item.Id).ToList();
                    await AssertCataloguePagesAsync(all, (page, size) => repository.GetMastersAsync(type, institutionId, activeOnly, page, size));
                }
                var allCategories = (await repository.GetInstitutionCategoriesAsync(institutionId, activeOnly))
                    .OrderBy(item => item.DisplayOrder).ThenBy(item => item.Name).ThenBy(item => item.Id).ToList();
                await AssertCataloguePagesAsync(allCategories,
                    (page, size) => repository.GetInstitutionCategoriesAsync(institutionId, activeOnly, page, size));
                foreach (int? categoryId in new int?[] { null, categories[0].Id })
                {
                    var allPositions = (await repository.GetPositionsAsync(institutionId, activeOnly, categoryId))
                        .OrderBy(item => item.DisplayOrder).ThenBy(item => item.Name).ThenBy(item => item.Id).ToList();
                    await AssertCataloguePagesAsync(allPositions,
                        (page, size) => repository.GetPositionsAsync(institutionId, activeOnly, categoryId, page, size));
                    var probePositions = allPositions.Where(item => positions.Select(position => position.Id).Contains(item.Id)).ToList();
                    Assert.That(probePositions, Is.Not.Empty);
                    Assert.That(probePositions.All(item => item.Capabilities.Count == 23 && item.Duties.Count == 23), Is.True);
                    Assert.That(probePositions.All(item => item.Capabilities.All(capability => capability.IsMandatory) &&
                        item.Duties.All(duty => !duty.IsMandatory)), Is.True);
                }
                Assert.That(allCategories.Single(item => item.Id == categories[0].Id).StaffJobFamilyMasterIds, Has.Count.EqualTo(23));
            }
            var savedCategory = await repository.SaveInstitutionCategoryAsync(new InstitutionStaffCategoryRequestDTO
            {
                Id = categories[^2].Id,
                StaffCategoryTypeId = categories[^2].StaffCategoryTypeId,
                Code = categories[^2].Code,
                Name = categories[^2].Name,
                DisplayOrder = categories[^2].DisplayOrder
            }, institutionId, actorId);
            var savedPosition = await repository.SavePositionAsync(new StaffPositionMasterRequestDTO
            {
                Id = positions[^2].Id,
                InstitutionStaffCategoryMasterId = categories[0].Id,
                StaffJobFamilyMasterId = families[0].Id,
                Code = positions[^2].Code,
                Name = positions[^2].Name,
                DisplayOrder = positions[^2].DisplayOrder,
                Capabilities = [new StaffPositionRequirementRequestDTO { Id = capabilities[0].Id }],
                Duties = [new StaffPositionRequirementRequestDTO { Id = duties[0].Id }]
            }, institutionId, actorId);
            Assert.That(savedCategory.Id, Is.EqualTo(categories[^2].Id));
            Assert.That(savedPosition.Id, Is.EqualTo(positions[^2].Id));
            Assert.That(savedPosition.Capabilities, Has.Count.EqualTo(1));
            foreach (var type in Enum.GetValues<StaffProfessionalMasterType>())
            {
                Assert.That((await repository.GetMastersAsync(type, long.MaxValue, false, 1, 10)).TotalCount, Is.Zero);
            }
            Assert.That((await repository.GetInstitutionCategoriesAsync(long.MaxValue, false, 1, 10)).Data, Is.Empty);
            Assert.That((await repository.GetPositionsAsync(long.MaxValue, false, categories[0].Id, 1, 10)).Data, Is.Empty);
            Assert.That((await repository.GetPositionsAsync(institutionId, false, int.MaxValue, 1, 10)).Data, Is.Empty);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    private static async Task<List<T>> CreatePagingCatalogueAsync<T>(
        SkolenzaDbContext db,
        DbSet<T> set,
        long institutionId,
        Action<T>? configure = null)
        where T : InstitutionMasterEntity
    {
        var template = await set.AsNoTracking().FirstAsync(item => item.InstitutionId == institutionId && !item.IsSoftDeleted);
        var code = "PAGE_" + Guid.NewGuid().ToString("N")[..12];
        var records = new List<T>();
        for (var index = 0; index < 23; index++)
        {
            var item = (T)db.Entry(template).CurrentValues.ToObject();
            item.Id = 0;
            item.Code = code + index;
            item.Name = code;
            item.DisplayOrder = 32000;
            item.IsActive = index % 2 == 0;
            item.IsSoftDeleted = index == 22;
            item.AddedDateTime = DateTime.UtcNow;
            item.UpdatedDateTime = null;
            item.DeletedDateTime = null;
            configure?.Invoke(item);
            records.Add(item);
        }
        set.AddRange(records);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return records;
    }

    private static async Task AssertCataloguePagesAsync<T>(List<T> all, Func<int, int, Task<PagedResponseDTO<T>>> read)
    {
        Assert.That(all.Count, Is.GreaterThan(10), "Rollback fixture must span multiple pages.");
        for (var pageNumber = 1; pageNumber <= (int)Math.Ceiling(all.Count / 10d) + 1; pageNumber++)
        {
            var page = await read(pageNumber, 10);
            Assert.Multiple(() =>
            {
                Assert.That(JsonSerializer.Serialize(page.Data), Is.EqualTo(JsonSerializer.Serialize(all.Skip((pageNumber - 1) * 10).Take(10).ToList())));
                Assert.That(page.TotalCount, Is.EqualTo(all.Count));
                Assert.That(page.TotalPages, Is.EqualTo((int)Math.Ceiling(all.Count / 10d)));
                Assert.That(page.PageNumber, Is.EqualTo(pageNumber));
                Assert.That(page.PageSize, Is.EqualTo(10));
            });
        }
        var custom = await read(2, 3);
        Assert.That(JsonSerializer.Serialize(custom.Data), Is.EqualTo(JsonSerializer.Serialize(all.Skip(3).Take(3).ToList())));
        var normalized = await read(0, -1);
        Assert.That(normalized.PageNumber, Is.EqualTo(1));
        Assert.That(normalized.PageSize, Is.EqualTo(10));
    }

    #endregion
}
