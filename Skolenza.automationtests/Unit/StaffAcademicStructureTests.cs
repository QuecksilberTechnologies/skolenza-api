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
}
