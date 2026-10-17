using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using skolenza.api.Controllers.Staff;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Staff;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using skolenza.persistance.Repositories;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StaffCurriculumDelivery")]
[Category("CurriculumPagination")]
public sealed class CurriculumPaginationTests
{
    #region HTTP Contract

    [TestCase("GetFrameworks")]
    [TestCase("GetUnits")]
    [TestCase("GetTopics")]
    [TestCase("GetLessonPlans")]
    [TestCase("GetLessonDeliveries")]
    [TestCase("GetSyllabusProgress")]
    public void List_actions_bind_paging_without_losing_permission_context(string methodName)
    {
        var action = typeof(StaffCurriculumDeliveryController).GetMethod(methodName)!;
        var parameter = action.GetParameters().Single(item => item.Name == "dto");
        var dto = new CurriculumDeliveryPagedRequestDTO();
        Assert.Multiple(() =>
        {
            Assert.That(parameter.ParameterType, Is.EqualTo(typeof(CurriculumDeliveryPagedRequestDTO)));
            Assert.That(parameter.GetCustomAttribute<FromQueryAttribute>(), Is.Not.Null);
            Assert.That(dto, Is.InstanceOf<PermissionRequestDTO>());
            Assert.That(dto.PageNumber, Is.EqualTo(1));
            Assert.That(dto.PageSize, Is.EqualTo(10));
            Assert.That(typeof(StaffCurriculumDeliveryController).GetCustomAttribute<AuthorizeAttribute>(), Is.Not.Null);
        });
    }

    [Test]
    public void Unit_headers_omit_topics_and_paging_envelope_omits_profile_completion_metadata()
    {
        var response = ApiResponse<List<CurriculumUnitResponseDTO>>.SuccessPaginated(
            [new CurriculumUnitResponseDTO { Id = 1, Topics = null }], 2, 10, 23, 3);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var root = json.RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(root.GetProperty("data").ValueKind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(root.GetProperty("pageNumber").GetInt32(), Is.EqualTo(2));
            Assert.That(root.GetProperty("pageSize").GetInt32(), Is.EqualTo(10));
            Assert.That(root.GetProperty("totalRecords").GetInt32(), Is.EqualTo(23));
            Assert.That(root.GetProperty("totalPages").GetInt32(), Is.EqualTo(3));
            Assert.That(root.TryGetProperty("completionPercentage", out _), Is.False);
            Assert.That(root.TryGetProperty("hasAllDocUploaded", out _), Is.False);
            Assert.That(root.TryGetProperty("isPrimaryMarked", out _), Is.False);
            Assert.That(root.GetProperty("data")[0].TryGetProperty("topics", out _), Is.False);
            Assert.That(typeof(StaffCurriculumDeliveryController).GetMethod("GetTopics")!
                .GetCustomAttribute<HttpGetAttribute>()!.Template, Is.EqualTo("units/{unitId:long}/topics"));
        });
    }

    #endregion

    #region PostgreSQL Rollback Acceptance

    private static SkolenzaDbContext OpenDatabase()
    {
        var settings = Environment.GetEnvironmentVariable("SKOLENZA_HOST_DB_SETTINGS");
        if (string.IsNullOrWhiteSpace(settings))
        {
            Assert.Ignore("Opt-in PostgreSQL rollback acceptance; configure SKOLENZA_HOST_DB_SETTINGS.");
        }
        var configuration = new ConfigurationBuilder().AddJsonFile(settings!).Build();
        return new SkolenzaDbContext(new DbContextOptionsBuilder<SkolenzaDbContext>()
            .UseNpgsql(configuration.GetConnectionString("DefaultConnection")).Options);
    }

    [Test]
    public async Task Central_query_paging_handles_defaults_last_empty_overflow_and_cancellation()
    {
        await using var db = OpenDatabase();
        var query = db.CurriculumTopics.AsNoTracking().OrderBy(item => item.Id).Select(item => item.Id).Take(23);
        var expected = await query.ToListAsync();
        Assert.That(expected, Has.Count.EqualTo(23), "Seeded topic catalogue is a prerequisite.");
        var first = await query.ToPageAsync();
        var normalized = await query.ToPageAsync(-1, 0);
        var second = await query.ToPageAsync(2, 10);
        var last = await query.ToPageAsync(3, 10);
        var overflow = await query.ToPageAsync(int.MaxValue, int.MaxValue);
        var empty = await query.Where(item => item < 0).ToPageAsync();
        Assert.Multiple(() =>
        {
            Assert.That(first.Data, Is.EqualTo(expected.Take(10)));
            Assert.That(normalized.Data, Is.EqualTo(first.Data));
            Assert.That(normalized.PageNumber, Is.EqualTo(1));
            Assert.That(normalized.PageSize, Is.EqualTo(10));
            Assert.That(second.Data, Is.EqualTo(expected.Skip(10).Take(10)));
            Assert.That(last.Data, Is.EqualTo(expected.Skip(20)));
            Assert.That(last.TotalCount, Is.EqualTo(23));
            Assert.That(last.TotalPages, Is.EqualTo(3));
            Assert.That(last.HasNext, Is.False);
            Assert.That(last.HasPrevious, Is.True);
            Assert.That(overflow.Data, Is.Empty);
            Assert.That(overflow.TotalCount, Is.EqualTo(23));
            Assert.That(empty.Data, Is.Empty);
            Assert.That(empty.TotalPages, Is.Zero);
        });
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Assert.CatchAsync<OperationCanceledException>(async () => await query.ToPageAsync(cancellationToken: cancelled.Token));
    }

    [Test]
    public async Task All_curriculum_lists_page_in_SQL_and_preserve_full_plan_coverage_and_write_responses()
    {
        await using var db = OpenDatabase();
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var planTemplate = await db.LessonPlans.AsNoTracking().FirstAsync(item => !item.IsSoftDeleted);
            var institutionId = planTemplate.InstitutionId;
            var frameworkTemplate = await db.CurriculumFrameworks.AsNoTracking().FirstAsync(item => item.InstitutionId == institutionId && !item.IsSoftDeleted);
            var unitTemplate = await db.CurriculumUnits.AsNoTracking().FirstAsync(item => item.InstitutionId == institutionId && !item.IsSoftDeleted);
            var topicTemplate = await db.CurriculumTopics.AsNoTracking().FirstAsync(item => item.InstitutionId == institutionId && !item.IsSoftDeleted);
            var deliveryTemplate = await db.LessonDeliveryLogs.AsNoTracking().FirstAsync(item => item.InstitutionId == institutionId && !item.IsSoftDeleted);
            var approved = await db.AcademicWorkflowStatuses.Where(item => item.Code == "APPROVED").Select(item => item.Id).SingleAsync();
            var offeringId = await db.StaffTeachingAssignments.Where(item => item.Id == planTemplate.StaffTeachingAssignmentId)
                .Select(item => item.CourseOfferingId).SingleAsync();
            var probe = "PAGE_" + Guid.NewGuid().ToString("N")[..12];
            var frameworks = new List<CurriculumFrameworkMaster>();
            for (var index = 0; index < 23; index++)
            {
                var item = (CurriculumFrameworkMaster)db.Entry(frameworkTemplate).CurrentValues.ToObject();
                item.Id = 0;
                item.AddedDateTime = DateTime.UtcNow;
                item.UpdatedDateTime = null;
                item.Code = probe + index;
                item.Name = probe;
                frameworks.Add(item);
            }
            db.CurriculumFrameworks.AddRange(frameworks);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var units = new List<CurriculumUnit>();
            for (var index = 0; index < 23; index++)
            {
                var item = (CurriculumUnit)db.Entry(unitTemplate).CurrentValues.ToObject();
                item.Id = 0;
                item.AddedDateTime = DateTime.UtcNow;
                item.UpdatedDateTime = null;
                item.CurriculumFrameworkId = frameworks[0].Id;
                item.CourseOfferingId = offeringId;
                item.ParentCurriculumUnitId = null;
                item.Code = probe + index;
                item.Name = probe;
                item.Sequence = 1;
                units.Add(item);
            }
            db.CurriculumUnits.AddRange(units);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var topics = new List<CurriculumTopic>();
            for (var index = 0; index < 23; index++)
            {
                var item = (CurriculumTopic)db.Entry(topicTemplate).CurrentValues.ToObject();
                item.Id = 0;
                item.AddedDateTime = DateTime.UtcNow;
                item.UpdatedDateTime = null;
                item.CurriculumUnitId = units[0].Id;
                item.Code = probe + index;
                item.Name = probe;
                item.Sequence = 1;
                item.IsActive = true;
                topics.Add(item);
            }
            db.CurriculumTopics.AddRange(topics);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var plans = new List<LessonPlan>();
            for (var index = 0; index < 23; index++)
            {
                var item = (LessonPlan)db.Entry(planTemplate).CurrentValues.ToObject();
                item.Id = 0;
                item.AddedDateTime = DateTime.UtcNow;
                item.UpdatedDateTime = null;
                item.ReviewedDateTime = null;
                item.WorkflowStatusId = approved;
                item.PlannedDate = new DateOnly(2028, 7, 1);
                plans.Add(item);
            }
            db.LessonPlans.AddRange(plans);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            db.LessonPlanCurriculumTopics.AddRange(plans.SelectMany(plan => topics.Select(topic =>
                new LessonPlanCurriculumTopic { LessonPlanId = plan.Id, CurriculumTopicId = topic.Id })));
            var deliveries = new List<LessonDeliveryLog>();
            foreach (var plan in plans)
            {
                var item = (LessonDeliveryLog)db.Entry(deliveryTemplate).CurrentValues.ToObject();
                item.Id = 0;
                item.AddedDateTime = DateTime.UtcNow;
                item.UpdatedDateTime = null;
                item.LessonPlanId = plan.Id;
                item.ReviewedDateTime = null;
                item.WorkflowStatusId = approved;
                item.DeliveredDate = plan.PlannedDate;
                deliveries.Add(item);
            }
            db.LessonDeliveryLogs.AddRange(deliveries);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            db.LessonDeliveryCoverages.AddRange(deliveries.SelectMany(log => topics.Select(topic =>
                new LessonDeliveryCoverage { LessonDeliveryLogId = log.Id, CurriculumTopicId = topic.Id, CompletionPercent = 50 })));
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();

            var repository = new StaffAcademicStructureRepository(db);
            var allFrameworks = (await repository.GetCurriculumFrameworksAsync(institutionId)).OrderBy(item => item.Name).ThenBy(item => item.Id).ToList();
            var frameworkPage = await repository.GetCurriculumFrameworksAsync(institutionId, 2, 10);
            var unitPage = await repository.GetCurriculumUnitsAsync(frameworks[0].Id, institutionId, 2, 10);
            var topicPage = await repository.GetCurriculumTopicsAsync(units[0].Id, institutionId, 2, 10);
            var topicLast = await repository.GetCurriculumTopicsAsync(units[0].Id, institutionId, 3, 10);
            var topicEmpty = await repository.GetCurriculumTopicsAsync(units[0].Id, institutionId, 4, 10);
            var allPlans = (await repository.GetLessonPlansAsync(institutionId)).OrderByDescending(item => item.PlannedDate).ThenByDescending(item => item.Id).ToList();
            var planPage = await repository.GetLessonPlansAsync(institutionId, 2, 10);
            var allDeliveries = (await repository.GetLessonDeliveriesAsync(institutionId)).OrderByDescending(item => item.DeliveredDate).ThenByDescending(item => item.Id).ToList();
            var deliveryPage = await repository.GetLessonDeliveriesAsync(institutionId, 2, 10);
            var allProgress = await repository.GetSyllabusProgressAsync(institutionId, offeringId);
            var progressPage = await repository.GetSyllabusProgressAsync(institutionId, offeringId, 2, 10);
            var progressUnit = allProgress.Single(item => item.CurriculumUnitId == units[0].Id);
            Assert.Multiple(() =>
            {
                Assert.That(frameworkPage.Data.Select(item => item.Id), Is.EqualTo(allFrameworks.Skip(10).Take(10).Select(item => item.Id)));
                Assert.That(frameworkPage.TotalCount, Is.EqualTo(allFrameworks.Count));
                Assert.That(unitPage.Data.Select(item => item.Id), Is.EqualTo(units.Skip(10).Take(10).Select(item => item.Id)));
                Assert.That(unitPage.TotalCount, Is.EqualTo(23));
                Assert.That(unitPage.Data.All(item => item.Topics is null), Is.True);
                Assert.That(topicPage.Data.Select(item => item.Id), Is.EqualTo(topics.Skip(10).Take(10).Select(item => item.Id)));
                Assert.That(topicLast.Data, Has.Count.EqualTo(3));
                Assert.That(topicLast.TotalPages, Is.EqualTo(3));
                Assert.That(topicEmpty.Data, Is.Empty);
                Assert.That(topicEmpty.TotalCount, Is.EqualTo(23));
                Assert.That(planPage.Data.Select(item => item.Id), Is.EqualTo(allPlans.Skip(10).Take(10).Select(item => item.Id)));
                Assert.That(planPage.TotalCount, Is.EqualTo(allPlans.Count));
                Assert.That(planPage.Data.Where(item => plans.Select(plan => plan.Id).Contains(item.Id)).All(item => item.Topics.Count == 23), Is.True);
                Assert.That(deliveryPage.Data.Select(item => item.Id), Is.EqualTo(allDeliveries.Skip(10).Take(10).Select(item => item.Id)));
                Assert.That(deliveryPage.TotalCount, Is.EqualTo(allDeliveries.Count));
                Assert.That(deliveryPage.Data.Where(item => deliveries.Select(log => log.Id).Contains(item.Id)).All(item => item.Coverage.Count == 23), Is.True);
                Assert.That(progressPage.Data.Select(item => item.CurriculumUnitId), Is.EqualTo(allProgress.Skip(10).Take(10).Select(item => item.CurriculumUnitId)));
                Assert.That(progressPage.TotalCount, Is.EqualTo(allProgress.Count));
                Assert.That(progressUnit.TopicCount, Is.EqualTo(23));
                Assert.That(progressUnit.CompletionPercent, Is.EqualTo(50));
            });
            var unitSaved = await repository.SaveCurriculumUnitAsync(new CurriculumUnitRequestDTO
            {
                Id = units[^1].Id,
                CurriculumFrameworkId = frameworks[0].Id,
                CourseOfferingId = offeringId,
                Code = units[^1].Code,
                Name = probe,
                Sequence = 1,
                PlannedPeriods = 1
            }, institutionId, planTemplate.AddedById);
            var deliverySaved = await repository.SaveLessonDeliveryAsync(new LessonDeliveryRequestDTO
            {
                LessonPlanId = plans[0].Id,
                DeliveredDate = new DateOnly(2020, 1, 1),
                CoverageNotes = "Paging rollback write response probe",
                Coverage = [new LessonDeliveryCoverageRequestDTO { CurriculumTopicId = topics[0].Id, CompletionPercent = 25 }]
            }, institutionId, planTemplate.AddedById);
            Assert.That(unitSaved.Id, Is.EqualTo(units[^1].Id));
            Assert.That(deliverySaved.Coverage, Has.Count.EqualTo(1));
            Assert.That((await repository.GetLessonDeliveriesAsync(institutionId, 1, 10)).Data.Select(item => item.Id), Does.Not.Contain(deliverySaved.Id));
            Assert.That((await repository.GetCurriculumFrameworksAsync(long.MaxValue, 1, 10)).Data, Is.Empty);
            Assert.That((await repository.GetLessonPlansAsync(long.MaxValue, 1, 10)).Data, Is.Empty);
            Assert.That((await repository.GetLessonDeliveriesAsync(long.MaxValue, 1, 10)).Data, Is.Empty);
            Assert.That((await repository.GetSyllabusProgressAsync(long.MaxValue, null, 1, 10)).Data, Is.Empty);
            Assert.ThrowsAsync<KeyNotFoundException>(async () => await repository.GetCurriculumUnitsAsync(frameworks[0].Id, long.MaxValue, 1, 10));
            Assert.ThrowsAsync<KeyNotFoundException>(async () => await repository.GetCurriculumTopicsAsync(units[0].Id, long.MaxValue, 1, 10));
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    #endregion
}
