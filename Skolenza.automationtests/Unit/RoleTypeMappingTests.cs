// ================================================================
// Purpose : Locks Institution role-type display mapping across Role and
//           NewLogin response contracts.
// ================================================================

using AutoMapper;
using System.Reflection;
using skolenza.api.Controllers.Role;
using skolenza.application.Constants;
using skolenza.application.DTOS.Role;
using skolenza.application.Features.RoleCmd;
using skolenza.application.Features.RoleCmd.Handlers;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.DTOs.Role;
using skolenza.application.DTOs.UserLogin;
using skolenza.application.Mappings;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("RoleTypeMapping")]
public sealed class RoleTypeMappingTests
{
    private IMapper _mapper = null!;

    [OneTimeSetUp]
    public void SetUpMapper()
    {
        _mapper = new MapperConfiguration(configuration =>
            configuration.AddProfile<MappingProfile>()).CreateMapper();
    }

    [TestCase(1, "Institution Administrator")]
    [TestCase(2, "Workforce User")]
    [TestCase(3, "People Manager")]
    [TestCase(4, "External User")]
    [TestCase(99, "Unknown")]
    public void Role_response_mappings_return_the_central_role_type_display_name(
        int roleType,
        string expectedDisplayName)
    {
        var role = new Role
        {
            Id = 10,
            RoleName = "Test Role",
            RoleType = roleType,
            IsActive = true
        };

        var roleResponse = _mapper.Map<GetRoleResponseDTO>(role);
        var loginResponse = _mapper.Map<NewLoginRoleDTO>(role);

        Assert.Multiple(() =>
        {
            Assert.That(
                ConstantValues.GetRoleTypeDisplayName(roleType),
                Is.EqualTo(expectedDisplayName));
            Assert.That(roleResponse.RoleType, Is.EqualTo(roleType));
            Assert.That(roleResponse.RoleTypeName, Is.EqualTo(expectedDisplayName));
            Assert.That(loginResponse.RoleTypeId, Is.EqualTo(roleType));
            Assert.That(loginResponse.RoleTypeName, Is.EqualTo(expectedDisplayName));
        });
    }

    [TestCase(1, "Institution Administrator", "Company workspace, users, roles and settings administration.")]
    [TestCase(2, "Workforce Member", "StaffMember self-service, attendance, leave, documents and assigned work.")]
    [TestCase(3, "People Manager", "Team and supervisor management according to assigned permissions.")]
    [TestCase(4, "External Collaborator", "Client, consultant, vendor and external portal access.")]
    [TestCase(99, null, "This is an auto-generated Admin account by AI for the initial setup of the institution.")]
    public void Default_role_name_and_remark_are_resolved_from_the_stable_role_type(
        int roleType,
        string? expectedRoleName,
        string expectedRemark)
    {
        var roleName = roleType switch
        {
            var value when value == ConstantValues.RoleTypeAdmin => ConstantValues.InstitutionAdminRoleName,
            var value when value == ConstantValues.RoleTypeStaffMember => ConstantValues.InstitutionStaffMemberRoleName,
            var value when value == ConstantValues.RoleTypeManager => ConstantValues.InstitutionManagerRoleName,
            var value when value == ConstantValues.RoleTypeClient => ConstantValues.InstitutionExternalRoleName,
            _ => null
        };

        Assert.Multiple(() =>
        {
            Assert.That(roleName, Is.EqualTo(expectedRoleName));
            Assert.That(ConstantValues.GetRoleTypeRemark(roleType), Is.EqualTo(expectedRemark));
        });
    }

    [Test]
    public void Role_response_replaces_only_the_legacy_shared_remark()
    {
        var legacyRole = new Role
        {
            RoleType = ConstantValues.RoleTypeClient,
            Remark = ConstantValues.InstitutionAllRoleRemark
        };
        var customizedRole = new Role
        {
            RoleType = ConstantValues.RoleTypeClient,
            Remark = "Customer portal reviewer"
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                _mapper.Map<GetRoleResponseDTO>(legacyRole).Remark,
                Is.EqualTo(ConstantValues.InstitutionExternalRoleRemark));
            Assert.That(
                _mapper.Map<GetRoleResponseDTO>(customizedRole).Remark,
                Is.EqualTo("Customer portal reviewer"));
        });
    }

    [Test]
    public async Task Role_type_options_publish_all_centralized_values_in_stable_order()
    {
        var handler = new GetRoleTypeOptionsQueryHandler();

        var response = await handler.Handle(
            new GetRoleTypeOptionsQuery(),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(response.IsSucceeded, Is.True);
            Assert.That(response.Data.Select(option => option.Id), Is.EqualTo(new[] { 1, 2, 3, 4 }));
            Assert.That(response.Data.Select(option => option.Code),
                Is.EqualTo(new[]
                {
                    "INSTITUTION_ADMIN",
                    "WORKFORCE_USER",
                    "PEOPLE_MANAGER",
                    "EXTERNAL_USER"
                }));
            Assert.That(response.Data.Select(option => option.Name),
                Is.EqualTo(new[]
                {
                    "Institution Administrator",
                    "Workforce User",
                    "People Manager",
                    "External User"
                }));
            Assert.That(
                response.Data.Select(option => option.Description),
                Is.EqualTo(new[]
                {
                    "Full company workspace administration, including users, roles, settings, and institution-level configuration.",
                    "StaffMember self-service access to personal information, attendance, leave, documents, and assigned work features.",
                    "Team management access for supervisors and managers, subject to assigned role permissions.",
                    "Limited portal access for clients, consultants, vendors, and other external users, subject to assigned role permissions."
                }));
            Assert.That(
                response.Data.Single(option => option.Id == 4).Name,
                Is.EqualTo("External User"));
        });
    }

    [TestCase(1, "INSTITUTION_ADMIN")]
    [TestCase(2, "WORKFORCE_USER")]
    [TestCase(3, "PEOPLE_MANAGER")]
    [TestCase(4, "EXTERNAL_USER")]
    [TestCase(99, "")]
    public void Role_type_code_is_resolved_from_the_stable_numeric_value(
        int roleType,
        string expectedCode)
    {
        Assert.That(ConstantValues.GetRoleTypeCode(roleType), Is.EqualTo(expectedCode));
    }

    [Test]
    public async Task Role_type_options_bypass_module_permission_lookup_and_reach_handler()
    {
        var unitOfWork = Proxy<IUnitOfWork>((method, _) =>
            throw new AssertionException($"Permission persistence must not be called: {method.Name}"));
        var commonRequestService = Proxy<ICommonRequestService>((method, _) =>
            throw new AssertionException($"Common request service must not be called: {method.Name}"));
        var behavior = new RolePermissionBehavior<
            GetRoleTypeOptionsQuery,
            ApiResponse<IReadOnlyList<RoleTypeOptionResponseDTO>>>(
                unitOfWork,
                commonRequestService,
                NullLogger<RolePermissionBehavior<
                    GetRoleTypeOptionsQuery,
                    ApiResponse<IReadOnlyList<RoleTypeOptionResponseDTO>>>>.Instance);
        var nextCalled = false;

        Task<ApiResponse<IReadOnlyList<RoleTypeOptionResponseDTO>>> Next(CancellationToken _)
        {
            nextCalled = true;
            return Task.FromResult(ApiResponse<IReadOnlyList<RoleTypeOptionResponseDTO>>.Success([]));
        }

        var response = await behavior.Handle(
            new GetRoleTypeOptionsQuery(),
            Next,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(nextCalled, Is.True);
            Assert.That(response.IsSucceeded, Is.True);
        });
    }

    [Test]
    public void Role_type_options_endpoint_is_authenticated_and_has_no_permission_inputs()
    {
        var method = typeof(RoleController).GetMethod(
            nameof(RoleController.GetRoleTypeOptions),
            BindingFlags.Instance | BindingFlags.Public);
        var route = method?.GetCustomAttribute<HttpMethodAttribute>();

        Assert.Multiple(() =>
        {
            Assert.That(method, Is.Not.Null);
            Assert.That(method?.GetCustomAttribute<AuthorizeAttribute>(), Is.Not.Null);
            Assert.That(route?.HttpMethods, Is.EqualTo(new[] { "GET" }));
            Assert.That(route?.Template, Is.EqualTo("type-options"));
            Assert.That(method?.GetParameters().Select(parameter => parameter.ParameterType),
                Is.EqualTo(new[] { typeof(CancellationToken) }));
        });
    }

    [Test]
    public void Existing_role_queries_still_require_module_and_operation_permissions()
    {
        var unitOfWork = Proxy<IUnitOfWork>((method, _) =>
            throw new AssertionException($"Permission persistence must not run for invalid input: {method.Name}"));
        var commonRequestService = Proxy<ICommonRequestService>((method, _) =>
            throw new AssertionException($"Common request service must not run for invalid input: {method.Name}"));
        var behavior = new RolePermissionBehavior<
            GetRoleOptionQuery,
            ApiResponse<List<GetRoleOptionResponseDTO>>>(
                unitOfWork,
                commonRequestService,
                NullLogger<RolePermissionBehavior<
                    GetRoleOptionQuery,
                    ApiResponse<List<GetRoleOptionResponseDTO>>>>.Instance);
        var nextCalled = false;

        Task<ApiResponse<List<GetRoleOptionResponseDTO>>> Next(CancellationToken _)
        {
            nextCalled = true;
            return Task.FromResult(ApiResponse<List<GetRoleOptionResponseDTO>>.Success([]));
        }

        Assert.ThrowsAsync<ValidationErrorException>(async () => await behavior.Handle(
            new GetRoleOptionQuery(new GetRoleOptionRequestDTO()),
            Next,
            CancellationToken.None));
        Assert.That(nextCalled, Is.False);
    }

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> invoke)
        where T : class
    {
        var proxy = DispatchProxy.Create<T, TestProxy>();
        ((TestProxy)(object)proxy).InvokeHandler = invoke;
        return proxy;
    }

    private class TestProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> InvokeHandler { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return InvokeHandler(targetMethod!, args);
        }
    }
}
