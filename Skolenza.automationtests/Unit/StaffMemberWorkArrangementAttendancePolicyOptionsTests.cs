using System.Reflection;
using skolenza.api.Controllers.InstitutionConfiguration;
using skolenza.application.Common.Models.Security;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.InstitutionConfiguration;
using skolenza.application.Features.StaffMemberCmd;
using skolenza.application.Features.StaffMemberCmd.StaffMemberWorkInfo.Handlers;
using skolenza.application.Constants;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StaffMemberWorkArrangement")]
public sealed class StaffMemberWorkArrangementAttendancePolicyOptionsTests
{
    [Test]
    public void Endpoint_is_authorized_token_only_and_does_not_request_module_permission_ids()
    {
        var controllerType = typeof(StaffWorkArrangementController);
        var method = controllerType.GetMethod("GetAttendancePolicyOptions", BindingFlags.Instance | BindingFlags.Public);
        var route = method?.GetCustomAttribute<HttpMethodAttribute>();

        Assert.Multiple(() =>
        {
            Assert.That(controllerType.BaseType?.GetCustomAttribute<AuthorizeAttribute>(), Is.Not.Null);
            Assert.That(route?.HttpMethods, Is.EqualTo(new[] { "GET" }));
            Assert.That(route?.Template, Is.EqualTo("attendance-policy-options"));
            Assert.That(typeof(PermissionRequestDTO).IsAssignableFrom(typeof(AttendancePolicyOptionRequestDTO)), Is.False);
            Assert.That(typeof(AttendancePolicyOptionRequestDTO).GetProperty("ModuleId"), Is.Null);
            Assert.That(typeof(AttendancePolicyOptionRequestDTO).GetProperty("OperationId"), Is.Null);
        });
    }

    [Test]
    public async Task Authenticated_dropdown_query_bypasses_module_permission_lookup_and_reaches_handler()
    {
        var validationCalls = 0;
        var commonRequestService = Proxy<ICommonRequestService>((method, _) =>
        {
            if (method.Name != nameof(ICommonRequestService.ValidateInstitutionUserRequestAsync))
            {
                throw new AssertionException($"Unexpected common-request call: {method.Name}");
            }

            validationCalls++;
            return Task.FromResult(new CommonDecodedResult
            {
                Success = true,
                InstitutionId = 8,
                LoggedInStaffMemberId = 1,
                RoleId = 22
            });
        });
        var unitOfWork = Proxy<IUnitOfWork>((method, _) =>
            throw new AssertionException($"Permission persistence must not be called: {method.Name}"));
        var idEncoderService = Proxy<IIdEncoderService>((method, _) =>
            throw new AssertionException($"Identifier decoding must not be called: {method.Name}"));
        var behavior = new StaffMemberInstitutionPermissionBehavior<GetAttendancePolicyOptionsQuery, bool>(
            unitOfWork,
            commonRequestService,
            idEncoderService,
            NullLogger<StaffMemberInstitutionPermissionBehavior<GetAttendancePolicyOptionsQuery, bool>>.Instance);
        var nextCalled = false;

        Task<bool> Next(CancellationToken _)
        {
            nextCalled = true;
            return Task.FromResult(true);
        }

        var result = await behavior.Handle(
            new GetAttendancePolicyOptionsQuery(new AttendancePolicyOptionRequestDTO()),
            Next,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(nextCalled, Is.True);
            Assert.That(validationCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void Unauthenticated_dropdown_query_is_rejected_before_handler()
    {
        var commonRequestService = Proxy<ICommonRequestService>((method, _) =>
            method.Name == nameof(ICommonRequestService.ValidateInstitutionUserRequestAsync)
                ? Task.FromResult(new CommonDecodedResult
                {
                    Success = false,
                    ErrorMessage = "The request is not authenticated."
                })
                : throw new AssertionException($"Unexpected common-request call: {method.Name}"));
        var unitOfWork = Proxy<IUnitOfWork>((method, _) =>
            throw new AssertionException($"Permission persistence must not be called: {method.Name}"));
        var idEncoderService = Proxy<IIdEncoderService>((method, _) =>
            throw new AssertionException($"Identifier decoding must not be called: {method.Name}"));
        var behavior = new StaffMemberInstitutionPermissionBehavior<GetAttendancePolicyOptionsQuery, bool>(
            unitOfWork,
            commonRequestService,
            idEncoderService,
            NullLogger<StaffMemberInstitutionPermissionBehavior<GetAttendancePolicyOptionsQuery, bool>>.Instance);
        var nextCalled = false;

        Task<bool> Next(CancellationToken _)
        {
            nextCalled = true;
            return Task.FromResult(true);
        }

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await behavior.Handle(
            new GetAttendancePolicyOptionsQuery(new AttendancePolicyOptionRequestDTO()),
            Next,
            CancellationToken.None));
        Assert.That(nextCalled, Is.False);
    }

    [Test]
    public void Query_supports_optional_effective_date_and_returns_policy_and_exact_version_identifiers()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(AttendancePolicyOptionRequestDTO).GetProperty(nameof(AttendancePolicyOptionRequestDTO.EffectiveOn)), Is.Not.Null);
            Assert.That(Nullable.GetUnderlyingType(typeof(AttendancePolicyOptionRequestDTO).GetProperty(nameof(AttendancePolicyOptionRequestDTO.EffectiveOn))!.PropertyType), Is.EqualTo(typeof(DateOnly)));
            Assert.That(typeof(AttendancePolicyOptionResponseDTO).GetProperty(nameof(AttendancePolicyOptionResponseDTO.PolicyId)), Is.Not.Null);
            Assert.That(typeof(AttendancePolicyOptionResponseDTO).GetProperty(nameof(AttendancePolicyOptionResponseDTO.PolicyVersionId)), Is.Not.Null);
            Assert.That(typeof(AttendancePolicyOptionResponseDTO).GetProperty(nameof(AttendancePolicyOptionResponseDTO.VersionNumber)), Is.Not.Null);
            Assert.That(typeof(AttendancePolicyOptionResponseDTO).GetProperty(nameof(AttendancePolicyOptionResponseDTO.DisplayName)), Is.Not.Null);
        });
    }

    [Test]
    public void Repository_filters_stable_attendance_category_institution_status_activity_and_effective_window()
    {
        var source = ReadRepositoryFile(
            "skolenza.persistance",
            "Repositories",
            "InstitutionConfigurationRepositories.cs");

        Assert.Multiple(() =>
        {
            Assert.That(AppConstants.PolicyCategoryCodes.Attendance, Is.EqualTo("ATTENDANCE"));
            Assert.That(AppConstants.PolicyStatusCodes.Published, Is.EqualTo("PUBLISHED"));
            Assert.That(source, Does.Contain("AppConstants.PolicyCategoryCodes.Attendance"));
            Assert.That(source, Does.Contain("AppConstants.PolicyStatusCodes.Published"));
            Assert.That(source, Does.Contain("policyType.InstitutionId == institutionId"));
            Assert.That(source, Does.Contain("policyType.IsActive == true"));
            Assert.That(source, Does.Contain("policyType.IsSoftDelete != true"));
            Assert.That(source, Does.Contain("policy.IsActive"));
            Assert.That(source, Does.Contain("!policy.IsSoftDeleted"));
            Assert.That(source, Does.Contain("version.IsActive"));
            Assert.That(source, Does.Contain("version.EffectiveFrom <= effectiveOn"));
            Assert.That(source, Does.Contain("version.EffectiveTo.Value >= effectiveOn"));
            Assert.That(source, Does.Contain("OrderByDescending(x => x.VersionNumber)"));
        });
    }

    [Test]
    public void Handler_defaults_effective_date_returns_actionable_empty_state_and_validates_authenticated_institution()
    {
        var source = ReadRepositoryFile(
            "skolenza.application",
            "Features",
            "StaffMemberCmd",
            "StaffMemberWorkInfo",
            "Handlers",
            "StaffMemberWorkArrangementHandler.cs");

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("request.Request.EffectiveOn"));
            Assert.That(source, Does.Contain("DateOnly.FromDateTime(DateTime.UtcNow)"));
            Assert.That(source, Does.Contain("await ValidateInstitutionAsync()"));
            Assert.That(source, Does.Not.Contain("ValidateInstitutionPermissionAsync(request.Request"));
            Assert.That(source, Does.Contain("Please create and publish an Attendance policy first."));
        });
    }

    [Test]
    public void Work_arrangement_write_contract_persists_the_selected_policy_version()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(CreateStaffMemberWorkArrangementRequestDTO).GetProperty(nameof(CreateStaffMemberWorkArrangementRequestDTO.PolicyVersionId)), Is.Not.Null);
            Assert.That(typeof(CreateStaffMemberWorkArrangementRequestDTO).GetProperty("AttendancePolicyId"), Is.Null);
            Assert.That(typeof(StaffMemberWorkArrangementResponseDTO).GetProperty(nameof(StaffMemberWorkArrangementResponseDTO.PolicyVersionId)), Is.Not.Null);
            Assert.That(typeof(StaffMemberWorkArrangementResponseDTO).GetProperty(nameof(StaffMemberWorkArrangementResponseDTO.PolicyId)), Is.Not.Null);
        });
    }

    [Test]
    public void Work_arrangement_write_validation_uses_attendance_published_status_and_effective_window()
    {
        var repositorySource = ReadRepositoryFile(
            "skolenza.persistance",
            "Repositories",
            "InstitutionConfigurationRepositories.cs");
        var handlerSource = ReadRepositoryFile(
            "skolenza.application",
            "Features",
            "StaffMemberCmd",
            "StaffMemberWorkInfo",
            "Handlers",
            "StaffMemberWorkArrangementHandler.cs");

        Assert.Multiple(() =>
        {
            Assert.That(repositorySource, Does.Contain("IsEligibleAttendancePolicyVersionAsync"));
            Assert.That(repositorySource, Does.Contain("version.Id == policyVersionId"));
            Assert.That(repositorySource, Does.Contain("AppConstants.PolicyCategoryCodes.Attendance"));
            Assert.That(repositorySource, Does.Contain("AppConstants.PolicyStatusCodes.Published"));
            Assert.That(repositorySource, Does.Contain("version.EffectiveFrom <= effectiveOn"));
            Assert.That(handlerSource, Does.Contain("dto.PolicyVersionId, dto.EffectiveFrom"));
            Assert.That(handlerSource, Does.Contain("InvalidAttendancePolicyVersion"));
        });
    }

    [Test]
    public void Work_arrangement_schema_migration_is_additive_and_does_not_guess_legacy_mapping()
    {
        var source = ReadRepositoryFile(
            "database-scripts",
            "AddPolicyVersionToStaffMemberWorkArrangement.sql");

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("ADD COLUMN IF NOT EXISTS \"PolicyVersionId\" bigint"));
            Assert.That(source, Does.Contain("ALTER COLUMN \"AttendancePolicyId\" DROP NOT NULL"));
            Assert.That(source, Does.Contain("FK_StaffMemberWorkArrangement_PolicyVersion"));
            Assert.That(source, Does.Not.Contain("UPDATE skolenza.\"StaffMemberWorkArrangement\""));
        });
    }

    private static string ReadRepositoryFile(params string[] pathParts)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Skolenza.sln")))
        {
            directory = directory.Parent;
        }

        Assert.That(directory, Is.Not.Null, "Repository root containing Skolenza.sln was not found.");
        return File.ReadAllText(Path.Combine(new[] { directory!.FullName }.Concat(pathParts).ToArray()));
    }

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> invoke) where T : class
    {
        var proxy = DispatchProxy.Create<T, BulkImportPermissionTests.TestProxy>();
        ((BulkImportPermissionTests.TestProxy)(object)proxy).InvokeMethod = invoke;
        return proxy;
    }
}

