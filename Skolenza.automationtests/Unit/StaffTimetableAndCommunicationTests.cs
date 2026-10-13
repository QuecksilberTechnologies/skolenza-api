using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using skolenza.api.Controllers.Staff;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StaffTimetableAndCommunication")]
public sealed class StaffTimetableAndCommunicationTests
{
    [Test]
    public void Persistence_model_maps_phase_four_and_five_tables()
    {
        using var context = new SkolenzaDbContext(new DbContextOptionsBuilder<SkolenzaDbContext>()
            .UseNpgsql("Host=localhost;Database=skolenza_model_validation;Username=postgres;Password=not-used").Options);
        var types = new Dictionary<Type, string>
        {
            [typeof(TimetablePeriodMaster)] = "TimetablePeriodMaster", [typeof(TimetableEntry)] = "TimetableEntry",
            [typeof(TimetableWorkflowMaster)] = "TimetableWorkflowMaster", [typeof(TimetableWorkflowStep)] = "TimetableWorkflowStep",
            [typeof(TimetableApprovalHistory)] = "TimetableApprovalHistory",
            [typeof(SubstituteWorkflowMaster)] = "SubstituteWorkflowMaster", [typeof(SubstituteWorkflowStep)] = "SubstituteWorkflowStep",
            [typeof(StaffAbsenceImpact)] = "StaffAbsenceImpact", [typeof(SubstituteAssignment)] = "SubstituteAssignment",
            [typeof(InstitutionCommunicationSetting)] = "InstitutionCommunicationSetting", [typeof(StaffQueryCategoryMaster)] = "StaffQueryCategoryMaster",
            [typeof(StaffConversation)] = "StaffConversation", [typeof(StaffConversationParticipant)] = "StaffConversationParticipant",
            [typeof(StaffConversationMessage)] = "StaffConversationMessage", [typeof(StaffBroadcastAudience)] = "StaffBroadcastAudience"
        };
        Assert.Multiple(() => { foreach (var item in types) Assert.That(context.Model.FindEntityType(item.Key)?.GetTableName(), Is.EqualTo(item.Value)); });
    }

    [Test]
    public void Controller_exposes_timetable_workflow_and_communication_contracts()
    {
        var methods = typeof(StaffTimetableAndCommunicationController).GetMethods().Select(x => x.Name).ToHashSet();
        Assert.Multiple(() =>
        {
            Assert.That(methods, Does.Contain("GetOptions"));
            Assert.That(methods, Does.Contain("GetPeriods")); Assert.That(methods, Does.Contain("CreateEntry"));
            Assert.That(methods, Does.Contain("GetTimetableWorkflows"));
            Assert.That(methods, Does.Contain("CreateTimetableWorkflow"));
            Assert.That(methods, Does.Contain("SubmitTimetableEntry"));
            Assert.That(methods, Does.Contain("ActOnTimetableEntry"));
            Assert.That(methods, Does.Contain("CreateTimetableRevision"));
            Assert.That(methods, Does.Contain("GetTimetableHistory"));
            Assert.That(methods, Does.Contain("GetWorkflows"));
            Assert.That(methods, Does.Contain("GetAbsenceImpacts"));
            Assert.That(methods, Does.Contain("SynchronizeLeaveImpacts"));
            Assert.That(methods, Does.Contain("AssignSubstitute"));
            Assert.That(methods, Does.Contain("ActOnSubstitute"));
            Assert.That(methods, Does.Contain("SaveCommunicationSetting"));
            Assert.That(methods, Does.Contain("GetQueryCategories"));
            Assert.That(methods, Does.Contain("CreateQueryCategory"));
            Assert.That(methods, Does.Contain("UpdateQueryCategory"));
            Assert.That(methods, Does.Contain("DeactivateQueryCategory"));
            Assert.That(methods, Does.Contain("CreateConversation"));
            Assert.That(methods, Does.Contain("GetConversation"));
            Assert.That(methods, Does.Contain("AddMessage"));
            Assert.That(methods, Does.Contain("MarkConversationRead"));
            Assert.That(methods, Does.Contain("CloseConversation"));
        });
    }

    [Test]
    public void Timetable_approval_migration_is_idempotent_and_dynamic()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        var migration = File.ReadAllText(Path.Combine(root, "database-scripts", "20261014_AddDynamicTimetableApproval.sql"));
        var seed = File.ReadAllText(Path.Combine(root, "database-scripts", "20261014_SeedDynamicTimetableApproval.sql"));
        Assert.Multiple(() =>
        {
            Assert.That(migration, Does.Contain("CREATE TABLE IF NOT EXISTS"));
            Assert.That(migration, Does.Contain("TimetableWorkflowMaster"));
            Assert.That(migration, Does.Contain("TimetableApprovalHistory"));
            Assert.That(migration, Does.Contain("MANAGER"));
            Assert.That(migration, Does.Contain("ACCESS_ROLE"));
            Assert.That(migration, Does.Contain("POSITION"));
            Assert.That(migration, Does.Contain("STAFF_MEMBER"));
            Assert.That(seed, Does.Contain("ON CONFLICT"));
            Assert.That(seed, Does.Contain("AllowTeacherDraftProposal"));
        });
    }

    [Test]
    public void Migration_enforces_institution_ownership_and_configurable_workflow()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        var script = File.ReadAllText(Path.Combine(root, "database-scripts", "20261014_AddTimetableSubstituteAndCommunication.sql"));
        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain("SubstituteWorkflowStep"));
            Assert.That(script, Does.Contain("ApproverTypeCode"));
            Assert.That(script, Does.Contain("InstitutionCommunicationSetting"));
            Assert.That(script, Does.Contain("StaffBroadcastAudience"));
            Assert.That(script, Does.Contain("InstitutionId"));
        });
    }

    [Test]
    public void Seed_is_idempotent_and_provides_complete_phase_four_and_five_fixture()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        var script = File.ReadAllText(Path.Combine(
            root,
            "database-scripts",
            "20261014_SeedRedRoseTimetableAndCommunication.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain("ON CONFLICT"));
            Assert.That(script, Does.Contain("TimetablePeriodMaster"));
            Assert.That(script, Does.Contain("StaffTeachingAssignment"));
            Assert.That(script, Does.Contain("SubstituteWorkflowStep"));
            Assert.That(script, Does.Contain("StaffQueryCategoryMaster"));
            Assert.That(script, Does.Contain("StaffConversationParticipant"));
            Assert.That(script, Does.Contain("StaffConversationMessage"));
        });
    }
}
