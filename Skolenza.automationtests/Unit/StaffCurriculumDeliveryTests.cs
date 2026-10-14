using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using skolenza.api.Controllers.Staff;
using skolenza.application.DTOS.Staff;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StaffCurriculumDelivery")]
public sealed class StaffCurriculumDeliveryTests
{
    [Test]
    public void Persistence_model_maps_curriculum_planning_and_delivery_tables()
    {
        using var context = new SkolenzaDbContext(
            new DbContextOptionsBuilder<SkolenzaDbContext>()
                .UseNpgsql("Host=localhost;Database=skolenza_model_validation;Username=postgres;Password=not-used")
                .Options);

        Assert.Multiple(() =>
        {
            Assert.That(context.Model.FindEntityType(typeof(AcademicWorkflowStatusMaster))?.GetTableName(), Is.EqualTo("AcademicWorkflowStatusMaster"));
            Assert.That(context.Model.FindEntityType(typeof(CurriculumFrameworkMaster))?.GetTableName(), Is.EqualTo("CurriculumFrameworkMaster"));
            Assert.That(context.Model.FindEntityType(typeof(CurriculumUnit))?.GetTableName(), Is.EqualTo("CurriculumUnit"));
            Assert.That(context.Model.FindEntityType(typeof(CurriculumTopic))?.GetTableName(), Is.EqualTo("CurriculumTopic"));
            Assert.That(context.Model.FindEntityType(typeof(LessonPlan))?.GetTableName(), Is.EqualTo("LessonPlan"));
            Assert.That(context.Model.FindEntityType(typeof(LessonDeliveryLog))?.GetTableName(), Is.EqualTo("LessonDeliveryLog"));
            Assert.That(context.Model.FindEntityType(typeof(LessonDeliveryCoverage))?.GetTableName(), Is.EqualTo("LessonDeliveryCoverage"));
        });
    }

    [Test]
    public void Controller_exposes_complete_curriculum_approval_delivery_and_progress_contract()
    {
        var methods = typeof(StaffCurriculumDeliveryController).GetMethods().Select(method => method.Name).ToHashSet();

        Assert.Multiple(() =>
        {
            Assert.That(methods, Does.Contain("GetWorkflowStatuses"));
            Assert.That(methods, Does.Contain("GetFrameworks"));
            Assert.That(methods, Does.Contain("GetUnits"));
            Assert.That(methods, Does.Contain("GetTeachingAssignments"));
            Assert.That(methods, Does.Contain("CreateLessonPlan"));
            Assert.That(methods, Does.Contain("ReviewLessonPlan"));
            Assert.That(methods, Does.Contain("CreateLessonDelivery"));
            Assert.That(methods, Does.Contain("ReviewLessonDelivery"));
            Assert.That(methods, Does.Contain("GetSyllabusProgress"));
            Assert.That(methods, Does.Contain("DeleteFramework"));
            Assert.That(methods, Does.Contain("DeleteUnit"));
            Assert.That(methods, Does.Contain("DeleteTopic"));
            Assert.That(methods, Does.Contain("DeleteLessonPlan"));
            Assert.That(methods, Does.Contain("DeleteLessonDelivery"));
        });
    }

    [Test]
    public void Teaching_assignment_option_identifies_the_assigned_staff_member()
    {
        Assert.That(
            typeof(StaffTeachingAssignmentResponseDTO).GetProperty(nameof(StaffTeachingAssignmentResponseDTO.StaffMemberName)),
            Is.Not.Null);
    }

    [Test]
    public void Migration_centralizes_statuses_and_enforces_relational_coverage()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        var script = File.ReadAllText(Path.Combine(root, "database-scripts", "20261013_AddCurriculumLessonDelivery.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain("AcademicWorkflowStatusMaster"));
            Assert.That(script, Does.Contain("FK_LessonPlan_Assignment"));
            Assert.That(script, Does.Contain("FK_LessonPlanTopic_Topic"));
            Assert.That(script, Does.Contain("FK_LessonCoverage_Log"));
            Assert.That(script, Does.Contain("CK_LessonCoverage_Percent"));
        });
    }

    [Test]
    public void Repository_derives_progress_from_approved_delivery_and_enforces_tenant_ownership()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        var source = File.ReadAllText(Path.Combine(root, "Skolenza.persistance", "Repositories", "StaffCurriculumDeliveryRepository.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("log.WorkflowStatusId == approved"));
            Assert.That(source, Does.Contain("item.InstitutionId == institutionId"));
            Assert.That(source, Does.Contain("Coverage contains a topic outside the approved lesson plan."));
            Assert.That(source, Does.Contain("Delivery can be recorded only for an approved lesson plan."));
            Assert.That(source, Does.Not.Contain("SyllabusProgressMaster"));
            Assert.That(source, Does.Contain("Remove child units and topics before deleting this curriculum unit."));
            Assert.That(source, Does.Contain("Only draft or rejected records can be edited."));
        });
    }
}
