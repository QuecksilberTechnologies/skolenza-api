using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Staff;
using skolenza.application.Wrappers;
using skolenza.application.Exceptions;
using skolenza.persistance.Repositories;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using skolenza.api.Controllers.Staff;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StaffAcademicStructure")]
public sealed class StaffAcademicStructureTests
{
    [Test]
    public void Persistence_model_maps_academic_structure_and_assignment_tables()
    {
        using var context = new SkolenzaDbContext(
            new DbContextOptionsBuilder<SkolenzaDbContext>()
                .UseNpgsql("Host=localhost;Database=skolenza_model_validation;Username=postgres;Password=not-used")
                .Options);

        Assert.Multiple(() =>
        {
            Assert.That(context.Model.FindEntityType(typeof(AcademicSession))?.GetTableName(), Is.EqualTo("AcademicSessionMaster"));
            Assert.That(context.Model.FindEntityType(typeof(AcademicProgramme))?.GetTableName(), Is.EqualTo("AcademicProgrammeMaster"));
            Assert.That(context.Model.FindEntityType(typeof(AcademicGroup))?.GetTableName(), Is.EqualTo("AcademicGroupMaster"));
            Assert.That(context.Model.FindEntityType(typeof(ClassSection))?.GetTableName(), Is.EqualTo("ClassSectionMaster"));
            Assert.That(context.Model.FindEntityType(typeof(Subject))?.GetTableName(), Is.EqualTo("SubjectMaster"));
            Assert.That(context.Model.FindEntityType(typeof(CourseOffering))?.GetTableName(), Is.EqualTo("CourseOfferingMaster"));
            Assert.That(context.Model.FindEntityType(typeof(TeachingAssignmentTypeMaster))?.GetTableName(), Is.EqualTo("TeachingAssignmentTypeMaster"));
            Assert.That(context.Model.FindEntityType(typeof(StaffTeachingAssignment))?.GetTableName(), Is.EqualTo("StaffTeachingAssignment"));
            Assert.That(context.Model.FindEntityType(typeof(ClassTeacherAssignment))?.GetTableName(), Is.EqualTo("ClassTeacherAssignment"));
        });
    }

    [Test]
    public void Controller_exposes_catalogue_profile_and_assignment_endpoints()
    {
        var methods = typeof(StaffAcademicStructureController).GetMethods().Select(method => method.Name).ToHashSet();
        Assert.Multiple(() =>
        {
            Assert.That(methods, Does.Contain("GetMasters"));
            Assert.That(methods, Does.Contain("GetTeachingAssignmentTypes"));
            Assert.That(methods, Does.Contain("CreateMaster"));
            Assert.That(methods, Does.Contain("UpdateMaster"));
            Assert.That(methods, Does.Contain("DeleteMaster"));
            Assert.That(methods, Does.Contain("GetStaffProfile"));
            Assert.That(methods, Does.Contain("CreateTeachingAssignment"));
            Assert.That(methods, Does.Contain("CreateClassTeacherAssignment"));
        });
    }

    [Test]
    public void Migration_enforces_tenant_catalogues_and_primary_class_teacher()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        var script = File.ReadAllText(Path.Combine(repositoryRoot, "database-scripts", "20261013_AddAcademicStructureAndTeachingAssignments.sql"));
        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain("UQ_AcademicSession_Institution_Code"));
            Assert.That(script, Does.Contain("UQ_CourseOffering_Scope"));
            Assert.That(script, Does.Contain("UX_ClassTeacherAssignment_CurrentPrimary"));
            Assert.That(script, Does.Contain("TeachingAssignmentTypeMaster"));
            Assert.That(script, Does.Contain("FK_StaffTeachingAssignment_AssignmentType"));
            Assert.That(script, Does.Contain("\"AcademicSessionMaster\""));
            Assert.That(script, Does.Contain("\"SubjectMaster\""));
            Assert.That(script, Does.Contain("StaffTeachingAssignment"));
        });
    }

    [Test]
    public void Repository_enforces_tenant_ownership_and_overlapping_assignment_guards()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        var source = File.ReadAllText(Path.Combine(repositoryRoot, "Skolenza.persistance", "Repositories", "StaffAcademicStructureRepository.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("EnsureStaffMemberAsync(staffMemberId, institutionId"));
            Assert.That(source, Does.Contain("ValidateCampusAsync(request.CampusId, institutionId"));
            Assert.That(source, Does.Contain("item.InstitutionId == institutionId && !item.IsSoftDeleted").Or.Contain("item.InstitutionId == institutionId && item.IsActive"));
            Assert.That(source, Does.Contain("item.EffectiveFrom <= (request.EffectiveTo ?? DateOnly.MaxValue)"));
            Assert.That(source, Does.Contain("The selected teaching assignment type is unavailable."));
        });
    }

    #region Academic Catalogue Pagination

    [Test]
    public void Academic_master_list_binds_paging_with_existing_auth_and_defaults()
    {
        var action = typeof(StaffAcademicStructureController).GetMethod("GetMasters")!;
        var dto = action.GetParameters().Single(item => item.Name == "dto");
        var request = new AcademicStructurePagedRequestDTO();
        Assert.Multiple(() =>
        {
            Assert.That(dto.ParameterType, Is.EqualTo(typeof(AcademicStructurePagedRequestDTO)));
            Assert.That(dto.GetCustomAttribute<FromQueryAttribute>(), Is.Not.Null);
            Assert.That(request, Is.InstanceOf<PermissionRequestDTO>());
            Assert.That(request.PageNumber, Is.EqualTo(1));
            Assert.That(request.PageSize, Is.EqualTo(10));
            Assert.That(typeof(StaffAcademicStructureController).GetCustomAttribute<AuthorizeAttribute>(), Is.Not.Null);
        });
    }

    [Test]
    public void Academic_paging_envelope_keeps_root_array_and_no_profile_completion_metadata()
    {
        var response = ApiResponse<List<AcademicMasterResponseDTO>>.SuccessPaginated([], 2, 10, 23, 3);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        Assert.Multiple(() =>
        {
            Assert.That(json.RootElement.GetProperty("data").ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(json.RootElement.GetProperty("pageNumber").GetInt32(), Is.EqualTo(2));
            Assert.That(json.RootElement.GetProperty("totalRecords").GetInt32(), Is.EqualTo(23));
            Assert.That(json.RootElement.GetProperty("totalPages").GetInt32(), Is.EqualTo(3));
            Assert.That(json.RootElement.TryGetProperty("completionPercentage", out _), Is.False);
            Assert.That(json.RootElement.TryGetProperty("hasAllDocUploaded", out _), Is.False);
            Assert.That(json.RootElement.TryGetProperty("isPrimaryMarked", out _), Is.False);
        });
    }

    [Test]
    [Category("AcademicPaginationDatabase")]
    public async Task Six_academic_catalogues_page_in_database_preserve_parent_fields_and_save_after_page_one()
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
            var institutionId = await db.CourseOfferings.AsNoTracking().Where(item => !item.IsSoftDeleted).Select(item => item.InstitutionId).FirstAsync();
            var actorId = await db.StaffMembers.Where(item => item.InstitutionId == institutionId && !item.IsSoftDeleted).Select(item => item.Id).FirstAsync();
            var existingSubjectId = await db.Subjects.Where(item => item.InstitutionId == institutionId && item.IsActive && !item.IsSoftDeleted).Select(item => item.Id).FirstAsync();
            var repository = new StaffAcademicStructureRepository(db);
            var before = new Dictionary<(AcademicMasterType, bool), int>();
            foreach (var type in Enum.GetValues<AcademicMasterType>())
            {
                foreach (var activeOnly in new[] { false, true })
                {
                    before[(type, activeOnly)] = (await repository.GetMastersAsync(type, institutionId, activeOnly)).Count;
                }
            }
            var sessions = await CreateAcademicPageRowsAsync(db, db.AcademicSessions, institutionId, item =>
            {
                item.ParentAcademicSessionId = null;
                item.CampusId = null;
            });
            var programmes = await CreateAcademicPageRowsAsync(db, db.AcademicProgrammes, institutionId);
            var groups = await CreateAcademicPageRowsAsync(db, db.AcademicGroups, institutionId, item =>
            {
                item.AcademicSessionId = sessions[0].Id;
                item.AcademicProgrammeId = programmes[0].Id;
                item.ParentAcademicGroupId = null;
            });
            var sections = await CreateAcademicPageRowsAsync(db, db.ClassSections, institutionId, item =>
            {
                item.AcademicGroupId = groups[0].Id;
                item.CampusId = null;
            });
            var subjects = await CreateAcademicPageRowsAsync(db, db.Subjects, institutionId);
            var offeringIndex = 0;
            var offerings = await CreateAcademicPageRowsAsync(db, db.CourseOfferings, institutionId, item =>
            {
                item.AcademicSessionId = sessions[0].Id;
                item.AcademicGroupId = groups[0].Id;
                item.SubjectId = subjects[offeringIndex++].Id;
            });
            var probeIds = new Dictionary<AcademicMasterType, List<long>>
            {
                [AcademicMasterType.Session] = sessions.Select(item => item.Id).ToList(),
                [AcademicMasterType.Programme] = programmes.Select(item => item.Id).ToList(),
                [AcademicMasterType.Group] = groups.Select(item => item.Id).ToList(),
                [AcademicMasterType.Section] = sections.Select(item => item.Id).ToList(),
                [AcademicMasterType.Subject] = subjects.Select(item => item.Id).ToList(),
                [AcademicMasterType.CourseOffering] = offerings.Select(item => item.Id).ToList()
            };
            foreach (var type in Enum.GetValues<AcademicMasterType>())
            {
                foreach (var activeOnly in new[] { false, true })
                {
                    var all = (await repository.GetMastersAsync(type, institutionId, activeOnly))
                        .OrderBy(item => item.Name).ThenBy(item => item.Id).ToList();
                    Assert.That(all.Count, Is.EqualTo(before[(type, activeOnly)] + (activeOnly ? 11 : 22)));
                    Assert.That(all.Select(item => item.Id), Does.Not.Contain(probeIds[type][^1]));
                    Assert.That(!activeOnly || all.All(item => item.IsActive), Is.True);
                    for (var pageNumber = 1; pageNumber <= (int)Math.Ceiling(all.Count / 10d) + 1; pageNumber++)
                    {
                        var page = await repository.GetMastersAsync(type, institutionId, activeOnly, pageNumber, 10);
                        Assert.Multiple(() =>
                        {
                            Assert.That(JsonSerializer.Serialize(page.Data), Is.EqualTo(JsonSerializer.Serialize(all.Skip((pageNumber - 1) * 10).Take(10).ToList())));
                            Assert.That(page.TotalCount, Is.EqualTo(all.Count));
                            Assert.That(page.TotalPages, Is.EqualTo((int)Math.Ceiling(all.Count / 10d)));
                            Assert.That(page.PageNumber, Is.EqualTo(pageNumber));
                            Assert.That(page.PageSize, Is.EqualTo(10));
                        });
                    }
                    var custom = await repository.GetMastersAsync(type, institutionId, activeOnly, 2, 3);
                    Assert.That(JsonSerializer.Serialize(custom.Data), Is.EqualTo(JsonSerializer.Serialize(all.Skip(3).Take(3).ToList())));
                }
                var normalized = await repository.GetMastersAsync(type, institutionId, false, 0, -1);
                Assert.That(normalized.PageNumber, Is.EqualTo(1));
                Assert.That(normalized.PageSize, Is.EqualTo(10));
                Assert.That((await repository.GetMastersAsync(type, long.MaxValue, false, 1, 10)).TotalCount, Is.Zero);
                var late = (await repository.GetMastersAsync(type, institutionId, false)).Single(item => item.Id == probeIds[type][^2]);
                var saved = await repository.SaveMasterAsync(type, new AcademicMasterRequestDTO
                {
                    Id = late.Id,
                    Code = late.Code,
                    Name = late.Name,
                    Description = late.Description,
                    IsActive = true,
                    CampusId = late.CampusId,
                    ParentId = late.ParentId,
                    AcademicSessionId = late.AcademicSessionId,
                    AcademicProgrammeId = late.AcademicProgrammeId,
                    AcademicGroupId = late.AcademicGroupId,
                    SubjectId = type == AcademicMasterType.CourseOffering ? existingSubjectId : late.SubjectId,
                    Type = late.Type,
                    StartDate = late.StartDate,
                    EndDate = late.EndDate,
                    Level = late.Level,
                    Capacity = late.Capacity,
                    CurriculumVersion = late.CurriculumVersion,
                    RequiredPeriodsPerWeek = late.RequiredPeriodsPerWeek
                }, institutionId, actorId);
                Assert.That(saved.Id, Is.EqualTo(late.Id));
                db.ChangeTracker.Clear();
            }
            var group = (await repository.GetMastersAsync(AcademicMasterType.Group, institutionId, false)).Single(item => item.Id == groups[0].Id);
            var offering = (await repository.GetMastersAsync(AcademicMasterType.CourseOffering, institutionId, false)).Single(item => item.Id == offerings[0].Id);
            Assert.Multiple(() =>
            {
                Assert.That(group.AcademicSessionName, Is.EqualTo(sessions[0].Name));
                Assert.That(group.AcademicProgrammeName, Is.EqualTo(programmes[0].Name));
                Assert.That(offering.AcademicGroupName, Is.EqualTo(groups[0].Name));
                Assert.That(offering.SubjectName, Is.EqualTo(subjects[0].Name));
            });
            Assert.ThrowsAsync<ValidationErrorException>(async () =>
                await repository.GetMastersAsync((AcademicMasterType)int.MaxValue, institutionId, false, 1, 10));
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    private static async Task<List<T>> CreateAcademicPageRowsAsync<T>(
        SkolenzaDbContext db,
        DbSet<T> set,
        long institutionId,
        Action<T>? configure = null)
        where T : InstitutionAcademicEntity
    {
        var template = await set.AsNoTracking().FirstAsync(item => item.InstitutionId == institutionId && !item.IsSoftDeleted);
        var code = "PAGE_" + Guid.NewGuid().ToString("N")[..12];
        var rows = new List<T>();
        for (var index = 0; index < 23; index++)
        {
            var row = (T)db.Entry(template).CurrentValues.ToObject();
            row.Id = 0;
            row.Code = code + index;
            row.Name = code;
            row.IsActive = index % 2 == 0;
            row.IsSoftDeleted = index == 22;
            row.AddedDateTime = DateTime.UtcNow;
            row.UpdatedDateTime = null;
            configure?.Invoke(row);
            rows.Add(row);
        }
        set.AddRange(rows);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return rows;
    }

    #endregion
}
