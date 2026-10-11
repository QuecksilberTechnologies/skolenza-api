using skolenza.application.DTOS.InstitutionConfiguration;
using skolenza.application.Features.StaffMemberCmd.StaffMemberWorkInfo;
using skolenza.domain.Entity;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StaffMemberWorkArrangement")]
[Category("StaffMemberWorkLocation")]
public sealed class StaffMemberWorkLocationArrangementValidationTests
{
    [TestCase(WorkMode.ClientSite, CampusType.ClientSite, true)]
    [TestCase(WorkMode.ClientSite, CampusType.HeadOffice, false)]
    [TestCase(WorkMode.Office, CampusType.HeadOffice, true)]
    [TestCase(WorkMode.Office, CampusType.ClientSite, false)]
    [TestCase(WorkMode.Hybrid, CampusType.Branch, true)]
    [TestCase(WorkMode.Field, CampusType.ProjectSite, true)]
    [TestCase(WorkMode.WorkFromHome, CampusType.RemoteOffice, false)]
    public void Work_mode_accepts_only_compatible_primary_location_types(WorkMode workMode, CampusType locationType, bool expected)
    {
        Assert.That(StaffMemberWorkConfigurationRules.IsLocationTypeCompatible(workMode, locationType), Is.EqualTo(expected));
    }

    [Test]
    public void Shared_work_mode_rules_keep_arrangement_pattern_and_override_requirements_consistent()
    {
        Assert.Multiple(() =>
        {
            Assert.That(StaffMemberWorkConfigurationRules.RequiresPrimaryLocation(WorkMode.Hybrid), Is.True);
            Assert.That(StaffMemberWorkConfigurationRules.RequiresPrimaryLocation(WorkMode.Field), Is.False);
            Assert.That(StaffMemberWorkConfigurationRules.RequiresExecutionLocation(WorkMode.Field), Is.True);
            Assert.That(StaffMemberWorkConfigurationRules.RequiresExecutionLocation(WorkMode.Hybrid), Is.False);
            Assert.That(StaffMemberWorkConfigurationRules.DisallowsLocation(WorkMode.WorkFromHome), Is.True);
            Assert.That(StaffMemberWorkConfigurationRules.IsConcreteWorkMode(WorkMode.Hybrid), Is.False);
            Assert.That(StaffMemberWorkConfigurationRules.IsConcreteWorkMode(WorkMode.ClientSite), Is.True);
            Assert.That(StaffMemberWorkConfigurationRules.SupportsWeekdayPattern(
                WorkMode.Hybrid,
                (short)HybridType.Flexible), Is.False);
            Assert.That(StaffMemberWorkConfigurationRules.SupportsWeekdayPattern(
                WorkMode.Hybrid,
                (short)HybridType.Fixed), Is.True);
            Assert.That(StaffMemberWorkConfigurationRules.IsPatternModeAllowed(
                WorkMode.ClientSite,
                WorkMode.Office), Is.False);
            Assert.That(StaffMemberWorkConfigurationRules.IsPatternModeAllowed(
                WorkMode.Hybrid,
                WorkMode.Office), Is.True);
        });
    }

    [Test]
    public void Inclusive_effective_windows_detect_overlap_and_allow_adjacent_history()
    {
        Assert.Multiple(() =>
        {
            Assert.That(StaffMemberWorkConfigurationRules.EffectiveWindowsOverlap(
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 31),
                new DateOnly(2026, 1, 31),
                new DateOnly(2026, 2, 15)), Is.True);
            Assert.That(StaffMemberWorkConfigurationRules.EffectiveWindowsOverlap(
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 1, 31),
                new DateOnly(2026, 2, 1),
                null), Is.False);
            Assert.That(StaffMemberWorkConfigurationRules.EffectiveWindowsOverlap(
                new DateOnly(2026, 1, 1),
                null,
                new DateOnly(2030, 1, 1),
                null), Is.True);
        });
    }

    [TestCase("2026-01-01", "2026-01-31", "2025-12-01", "2025-12-31", false)]
    [TestCase("2026-01-01", "2026-01-31", "2026-01-31", "2026-02-15", true)]
    [TestCase("2026-01-01", "2026-01-31", "2026-02-01", null, false)]
    [TestCase("2026-01-01", null, "2030-01-01", "2030-01-31", true)]
    [TestCase("2026-01-10", "2026-01-20", "2026-01-01", null, true)]
    public void Effective_window_overlap_covers_past_boundary_future_and_open_ended_cases(
        string firstFrom,
        string? firstTo,
        string secondFrom,
        string? secondTo,
        bool expected)
    {
        Assert.That(
            StaffMemberWorkConfigurationRules.EffectiveWindowsOverlap(
                DateOnly.Parse(firstFrom),
                firstTo is null ? null : DateOnly.Parse(firstTo),
                DateOnly.Parse(secondFrom),
                secondTo is null ? null : DateOnly.Parse(secondTo)),
            Is.EqualTo(expected));
    }

    [Test]
    public void Assignment_must_cover_the_complete_arrangement_window()
    {
        Assert.Multiple(() =>
        {
            Assert.That(StaffMemberWorkConfigurationRules.AssignmentCoversArrangement(
                new DateOnly(2026, 1, 1), null, new DateOnly(2026, 2, 1), null), Is.True);
            Assert.That(StaffMemberWorkConfigurationRules.AssignmentCoversArrangement(
                new DateOnly(2026, 2, 2), null, new DateOnly(2026, 2, 1), null), Is.False);
            Assert.That(StaffMemberWorkConfigurationRules.AssignmentCoversArrangement(
                new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), new DateOnly(2026, 2, 1), null), Is.False);
            Assert.That(StaffMemberWorkConfigurationRules.AssignmentCoversArrangement(
                new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), new DateOnly(2026, 2, 1), new DateOnly(2026, 11, 30)), Is.True);
        });
    }

    [Test]
    public void Location_assignment_response_exposes_only_the_encoded_staffMember_identifier()
    {
        var response = new StaffMemberLocationAssignmentResponseDTO
        {
            StaffMemberId = "encoded-42"
        };
        var root = FindRepositoryRoot();
        var handler = File.ReadAllText(Path.Combine(
            root,
            "skolenza.application",
            "Features",
            "StaffMemberCmd",
            "StaffMemberWorkInfo",
            "Handlers",
            "StaffMemberLocationAssignmentHandler.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(response.StaffMemberId, Is.EqualTo("encoded-42"));
            Assert.That(typeof(StaffMemberLocationAssignmentResponseDTO).GetProperty("StaffMemberId")!.PropertyType, Is.EqualTo(typeof(string)));
            Assert.That(handler, Does.Contain("response.StaffMemberId = encodeStaffMemberId(entity.StaffMemberId)"));
            Assert.That(handler, Does.Contain("UpdateStaffMemberLocationAssignmentStatusCommandHandler"));
            Assert.That(handler, Does.Contain("GetStaffMemberLocationAssignmentByIdQueryHandler"));
            Assert.That(handler, Does.Contain("GetStaffMemberLocationAssignmentsQueryHandler"));
        });
    }

    [Test]
    public void Attendance_policy_configuration_controls_physical_and_wfh_arrangements_without_device_enrollment()
    {
        var configuration = new AttendancePolicyVersionConfiguration
        {
            AttendanceLocationScope = (short)AttendanceLocationScope.AssignedLocations,
            AllowMobile = true,
            AllowWorkFromHome = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(StaffMemberWorkConfigurationRules.IsAllowedByAttendancePolicy(
                WorkMode.Office, 10, configuration), Is.True);
            Assert.That(StaffMemberWorkConfigurationRules.IsAllowedByAttendancePolicy(
                WorkMode.WorkFromHome, null, configuration), Is.True);
            Assert.That(StaffMemberWorkConfigurationRules.IsAllowedByAttendancePolicy(
                WorkMode.WorkFromHome, 10, configuration), Is.False);

            configuration.AttendanceLocationScope = (short)AttendanceLocationScope.RemoteAnywhere;
            Assert.That(StaffMemberWorkConfigurationRules.IsAllowedByAttendancePolicy(
                WorkMode.Office, 10, configuration), Is.False);

            configuration.AllowWorkFromHome = false;
            Assert.That(StaffMemberWorkConfigurationRules.IsAllowedByAttendancePolicy(
                WorkMode.WorkFromHome, null, configuration), Is.False);
        });
    }

    [Test]
    public void Typed_attendance_policy_version_schema_is_one_to_one_and_backfills_existing_versions()
    {
        var root = FindRepositoryRoot();
        var context = File.ReadAllText(Path.Combine(root, "skolenza.persistance", "Data", "Context", "SkolenzaDbContext.PolicyFramework.cs"));
        var repository = File.ReadAllText(Path.Combine(root, "skolenza.persistance", "Repositories", "GenericPolicyRepository.cs"));
        var script = File.ReadAllText(Path.Combine(root, "database-scripts", "AddAttendancePolicyVersionConfiguration.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(context, Does.Contain("WithOne(e => e.AttendanceConfiguration)"));
            Assert.That(context, Does.Contain("HasIndex(e => e.PolicyVersionId).IsUnique()"));
            Assert.That(repository, Does.Contain("ValidateAttendanceConfigurationAsync"));
            Assert.That(repository, Does.Contain("UpsertAttendanceConfigurationAsync"));
            Assert.That(repository, Does.Contain("ToDto(attendanceConfiguration)"));
            Assert.That(script, Does.Contain("CK_AttendancePolicyVersionConfiguration_Channel"));
            Assert.That(script, Does.Contain("upper(category.\"CategoryCode\") = 'ATTENDANCE'"));
            Assert.That(script, Does.Contain("NOT EXISTS"));
        });
    }

    [Test]
    public void Repository_and_handlers_enforce_cross_record_and_date_window_rules()
    {
        var root = FindRepositoryRoot();
        var repository = File.ReadAllText(Path.Combine(root, "skolenza.persistance", "Repositories", "InstitutionConfigurationRepositories.cs"));
        var arrangementHandler = File.ReadAllText(Path.Combine(root, "skolenza.application", "Features", "StaffMemberCmd", "StaffMemberWorkInfo", "Handlers", "StaffMemberWorkArrangementHandler.cs"));
        var assignmentHandler = File.ReadAllText(Path.Combine(root, "skolenza.application", "Features", "StaffMemberCmd", "StaffMemberWorkInfo", "Handlers", "StaffMemberLocationAssignmentHandler.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(repository, Does.Contain("HasCoveringPrimaryLocationAssignmentAsync"));
            Assert.That(repository, Does.Contain("GetOverlappingArrangementAsync"));
            Assert.That(repository, Does.Contain("&& x.IsActive"));
            Assert.That(repository, Does.Contain("GetLocationAssignmentsForValidationAsync"));
            Assert.That(repository, Does.Contain("&& !x.IsSoftDeleted"));
            Assert.That(repository, Does.Contain("x.IsPrimary"));
            Assert.That(repository, Does.Contain("x.IsAttendanceAllowed"));
            Assert.That(repository, Does.Contain("x.EffectiveFrom <= effectiveFrom"));
            Assert.That(repository, Does.Contain("x.EffectiveTo.Value >= effectiveTo.Value"));
            Assert.That(arrangementHandler, Does.Contain("StaffMemberWorkConfigurationRules.IsLocationTypeCompatible"));
            Assert.That(arrangementHandler, Does.Contain("WorkArrangementStaffMemberLocationStartsLate"));
            Assert.That(arrangementHandler, Does.Contain("WorkArrangementStaffMemberLocationEndsEarly"));
            Assert.That(arrangementHandler, Does.Contain("WorkArrangementStaffMemberLocationMustBeOpenEnded"));
            Assert.That(arrangementHandler, Does.Contain("WorkArrangementStaffMemberLocationAttendanceDisabled"));
            Assert.That(arrangementHandler, Does.Contain("WorkArrangementStaffMemberLocationNotPrimary"));
            Assert.That(arrangementHandler, Does.Contain("ValidateNoDateOverlapAsync"));
            Assert.That(arrangementHandler, Does.Contain("StaffMemberWorkArrangementDateOverlap"));
            Assert.That(arrangementHandler, Does.Contain("StaffMemberWorkArrangementOpenEndedDateOverlap"));
            Assert.That(arrangementHandler, Does.Contain("dto.EffectiveFrom,"));
            Assert.That(arrangementHandler, Does.Contain("dto.EffectiveTo,"));
            Assert.That(arrangementHandler, Does.Contain("excludeId,"));
            Assert.That(assignmentHandler, Does.Contain("WouldInvalidatePrimaryWorkArrangementAsync"));
            Assert.That(assignmentHandler, Does.Contain("dto.EffectiveFrom, dto.EffectiveTo, excludeId"));
        });
    }

    [Test]
    public void Work_pattern_and_override_handlers_enforce_dependencies_approval_and_overlap_rules()
    {
        var root = FindRepositoryRoot();
        var repository = File.ReadAllText(Path.Combine(root, "skolenza.persistance", "Repositories", "InstitutionConfigurationRepositories.cs"));
        var patternHandler = File.ReadAllText(Path.Combine(root, "skolenza.application", "Features", "StaffMemberCmd", "StaffMemberWorkInfo", "Handlers", "StaffMemberWorkPatternHandler.cs"));
        var overrideHandler = File.ReadAllText(Path.Combine(root, "skolenza.application", "Features", "StaffMemberCmd", "StaffMemberWorkInfo", "Handlers", "StaffMemberWorkModeOverrideHandler.cs"));
        var controller = File.ReadAllText(Path.Combine(root, "skolenza.api", "Controllers", "InstitutionConfiguration", "InstitutionConfigurationControllers.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(patternHandler, Does.Contain("HasCoveringAttendanceLocationAssignmentAsync"));
            Assert.That(patternHandler, Does.Contain("IsLocationTypeCompatible"));
            Assert.That(patternHandler, Does.Contain("IsAllowedByAttendancePolicy"));
            Assert.That(patternHandler, Does.Contain("PatternDayExistsAsync"));
            Assert.That(patternHandler, Does.Contain("FlexibleHybridPatternNotAllowed"));
            Assert.That(patternHandler, Does.Contain("WorkPatternModeMustMatchArrangement"));
            Assert.That(overrideHandler, Does.Contain("GetOverlappingOverrideAsync"));
            Assert.That(overrideHandler, Does.Contain("WorkModeOverrideDateOverlap"));
            Assert.That(overrideHandler, Does.Contain("GetActiveOperationNameAsync"));
            Assert.That(overrideHandler, Does.Contain("expectedOperation"));
            Assert.That(repository, Does.Contain("WorkModeOverrideApprovalStatus.Pending"));
            Assert.That(repository, Does.Contain("WorkModeOverrideApprovalStatus.Approved"));
            Assert.That(controller, Does.Contain("HttpPost(\"approve\")"));
            Assert.That(controller, Does.Contain("HttpPost(\"reject\")"));
        });
    }

    [Test]
    public void Work_mode_override_response_exposes_encoded_staffMember_identifier_only()
    {
        Assert.That(
            typeof(StaffMemberWorkModeOverrideResponseDTO).GetProperty("StaffMemberId")!.PropertyType,
            Is.EqualTo(typeof(string)));
    }

    [Test]
    public void Work_pattern_response_exposes_encoded_staffMember_identity()
    {
        var root = FindRepositoryRoot();
        var handler = File.ReadAllText(Path.Combine(root, "skolenza.application", "Features", "StaffMemberCmd", "StaffMemberWorkInfo", "Handlers", "StaffMemberWorkPatternHandler.cs"));
        var repository = File.ReadAllText(Path.Combine(root, "skolenza.persistance", "Repositories", "InstitutionConfigurationRepositories.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(
                typeof(StaffMemberWorkPatternResponseDTO).GetProperty("StaffMemberId")!.PropertyType,
                Is.EqualTo(typeof(string)));
            Assert.That(
                typeof(StaffMemberWorkPatternResponseDTO).GetProperty("StaffMemberName")!.PropertyType,
                Is.EqualTo(typeof(string)));
            Assert.That(handler, Does.Contain("StaffMemberWorkPatternResponseMapper.Map"));
            Assert.That(handler, Does.Contain("EncodeStaffMemberId(id, validation)"));
            Assert.That(repository, Does.Contain("ThenInclude(x => x.StaffMember)"));
        });
    }

    [Test]
    public void Work_arrangement_response_exposes_encoded_staffMember_identity()
    {
        var root = FindRepositoryRoot();
        var handler = File.ReadAllText(Path.Combine(root, "skolenza.application", "Features", "StaffMemberCmd", "StaffMemberWorkInfo", "Handlers", "StaffMemberWorkArrangementHandler.cs"));
        var mappingProfile = File.ReadAllText(Path.Combine(root, "skolenza.application", "Mappings", "MappingProfile.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(
                typeof(StaffMemberWorkArrangementResponseDTO).GetProperty("StaffMemberId")!.PropertyType,
                Is.EqualTo(typeof(string)));
            Assert.That(handler, Does.Contain("internal static class StaffMemberWorkArrangementResponseMapper"));
            Assert.That(handler, Does.Contain("response.StaffMemberId = encodeStaffMemberId(entity.StaffMemberId)"));
            Assert.That(handler, Does.Contain("id => EncodeStaffMemberId(id, validation)"));
            Assert.That(mappingProfile, Does.Contain("CreateMap<StaffMemberWorkArrangement, StaffMemberWorkArrangementResponseDTO>()"));
            Assert.That(mappingProfile, Does.Contain(".ForMember(d => d.StaffMemberId, o => o.Ignore())"));
        });
    }

    [Test]
    public void Database_model_and_deployment_script_allow_non_overlapping_schedules()
    {
        var root = FindRepositoryRoot();
        var context = File.ReadAllText(Path.Combine(root, "skolenza.persistance", "Data", "Context", "SkolenzaDbContext.cs"));
        var script = File.ReadAllText(Path.Combine(root, "database-scripts", "AlignStaffMemberWorkLocationAndArrangementValidation.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(context, Does.Not.Contain("UX_StaffMemberWorkArrangement_Current"));
            Assert.That(context, Does.Not.Contain("UX_StaffMemberLocationAssignment_Primary"));
            Assert.That(script, Does.Contain("DROP INDEX IF EXISTS skolenza.\"UX_StaffMemberWorkArrangement_Current\""));
            Assert.That(script, Does.Contain("IX_StaffMemberWorkArrangement_StaffMember_EffectiveFrom"));
            Assert.That(script, Does.Contain("IX_StaffMemberLocationAssignment_Primary_EffectiveFrom"));
            Assert.That(script, Does.Contain("CREATE EXTENSION IF NOT EXISTS btree_gist"));
            Assert.That(script, Does.Contain("EX_StaffMemberLocationAssignment_StaffMember_Location_Window"));
            Assert.That(script, Does.Contain("EX_StaffMemberLocationAssignment_Primary_Window"));
            Assert.That(script, Does.Contain("EX_StaffMemberWorkArrangement_StaffMember_Window"));
            Assert.That(script, Does.Contain("WITH &&"));
        });
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Skolenza.sln")))
        {
            directory = directory.Parent;
        }

        Assert.That(directory, Is.Not.Null, "Repository root containing Skolenza.sln was not found.");
        return directory!.FullName;
    }
}
