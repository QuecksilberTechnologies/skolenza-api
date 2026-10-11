using skolenza.application.Features.AttendanceCmd;
using skolenza.domain.Entity;
using Microsoft.AspNetCore.Authorization;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("Attendance")]
public sealed class AttendancePunchFlowTests
{
    [TestCase(null, AttendancePunchAction.CheckIn, true)]
    [TestCase(null, AttendancePunchAction.CheckOut, false)]
    [TestCase(AttendancePunchAction.CheckIn, AttendancePunchAction.CheckIn, false)]
    [TestCase(AttendancePunchAction.CheckIn, AttendancePunchAction.CheckOut, true)]
    [TestCase(AttendancePunchAction.CheckOut, AttendancePunchAction.CheckIn, true)]
    [TestCase(AttendancePunchAction.CheckOut, AttendancePunchAction.CheckOut, false)]
    public void Punch_state_machine_rejects_duplicate_or_out_of_order_actions(
        AttendancePunchAction? previous, AttendancePunchAction requested, bool expected)
    {
        Assert.That(AttendancePunchRules.IsAllowedTransition(previous, requested), Is.EqualTo(expected));
    }

    [Test]
    public void Attendance_controller_requires_authentication_and_exposes_mark_and_today_routes()
    {
        var root = FindRepositoryRoot();
        var controller = File.ReadAllText(Path.Combine(root, "skolenza.api", "Controllers",
            "Attendance", "AttendanceController.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(controller, Does.Contain("[Authorize]"));
            Assert.That(controller, Does.Contain("[HttpPost(\"mark-attendance\")]"));
            Assert.That(controller, Does.Contain("[HttpGet(\"today\")]"));
            Assert.That(controller, Does.Contain("[HttpGet(\"device-types\")]"));
            Assert.That(controller, Does.Contain("new MarkAttendanceCommand"));
            Assert.That(controller, Does.Contain("new GetTodayAttendanceQuery"));
        });
    }

    [Test]
    public void Punch_contract_uses_token_staffMember_identity_and_database_idempotency()
    {
        var root = FindRepositoryRoot();
        var handler = File.ReadAllText(Path.Combine(root, "skolenza.application", "Features",
            "AttendanceCmd", "AttendancePunchHandler.cs"));
        var repository = File.ReadAllText(Path.Combine(root, "skolenza.persistance", "Repositories",
            "AttendanceRepository.cs"));
        var script = File.ReadAllText(Path.Combine(root, "database-scripts", "AddStaffMemberAttendancePunch.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(handler, Does.Contain("ValidateInstitutionUserRequestAsync"));
            Assert.That(handler, Does.Contain("LoggedInStaffMemberId"));
            Assert.That(handler, Does.Not.Contain("ModuleId"));
            Assert.That(repository, Does.Contain("pg_advisory_xact_lock"));
            Assert.That(repository, Does.Contain("RequireGeoFenceForOffice"));
            Assert.That(repository, Does.Contain("IsAttendanceAllowed"));
            Assert.That(script, Does.Contain("UX_StaffMemberAttendancePunch_Idempotency"));
            Assert.That(script, Does.Contain("IdempotencyKey"));
            Assert.That(script, Does.Contain("AttendanceDeviceTypeId"));
            Assert.That(script, Does.Contain("DeviceTypeCode"));
            Assert.That(script, Does.Contain("'MOBILE'"));
            Assert.That(script, Does.Contain("'WEB'"));
            Assert.That(script, Does.Contain("'BIOMETRIC'"));
            Assert.That(script, Does.Contain("'MANUAL'"));
            Assert.That(script, Does.Not.Contain("\"Channel\" smallint"));
        });
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Skolenza.sln")))
            directory = directory.Parent;

        Assert.That(directory, Is.Not.Null, "Repository root containing Skolenza.sln was not found.");
        return directory!.FullName;
    }
}
