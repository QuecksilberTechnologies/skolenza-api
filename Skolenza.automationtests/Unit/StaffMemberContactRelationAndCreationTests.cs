using System.Reflection;
using skolenza.api.Controllers.Staff;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.Common.Models.Security;
using skolenza.application.DTOS.Staff.Contact;
using skolenza.application.Features.StaffMemberCmd;
using skolenza.application.Features.StaffMemberCmd.Contact.Handlers;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StaffMemberContactRelation")]
public sealed class StaffMemberContactRelationAndCreationTests
{
    [Test]
    public async Task Relation_options_publish_the_complete_enum_catalogue_in_stable_order()
    {
        var handler = new GetContactRelationOptionsQueryHandler();

        var response = await handler.Handle(
            new GetContactRelationOptionsQuery(),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(response.IsSucceeded, Is.True);
            Assert.That(
                response.Data.Select(option => option.Id),
                Is.EqualTo(Enumerable.Range(1, 16).Append(99)));
            Assert.That(
                response.Data.Single(option => option.Id == (int)EmergencyContactRelation.Owner).Code,
                Is.EqualTo("OWNER"));
            Assert.That(
                response.Data.Single(option => option.Id == (int)EmergencyContactRelation.Owner).Label,
                Is.EqualTo("Owner"));
            Assert.That(
                response.Data.Single(option => option.Id == (int)EmergencyContactRelation.Other).Label,
                Is.EqualTo("Other"));
        });
    }

    [Test]
    public void Relation_options_endpoint_is_bearer_authenticated_and_has_no_permission_inputs()
    {
        var method = typeof(ContactController).GetMethod(
            nameof(ContactController.GetRelationOptions),
            BindingFlags.Instance | BindingFlags.Public);
        var route = method?.GetCustomAttribute<HttpMethodAttribute>();

        Assert.Multiple(() =>
        {
            Assert.That(method, Is.Not.Null);
            Assert.That(method?.GetCustomAttribute<AuthorizeAttribute>(), Is.Not.Null);
            Assert.That(route?.HttpMethods, Is.EqualTo(new[] { "GET" }));
            Assert.That(route?.Template, Is.EqualTo("relation-options"));
            Assert.That(
                method?.GetParameters().Select(parameter => parameter.ParameterType),
                Is.EqualTo(new[] { typeof(CancellationToken) }));
        });
    }

    [Test]
    public async Task Authenticated_relation_lookup_bypasses_module_permission_persistence()
    {
        var commonRequestService = Proxy<ICommonRequestService>((method, _) =>
            throw new AssertionException($"Token-only catalogue must not call institution persistence: {method.Name}"));
        var unitOfWork = Proxy<IUnitOfWork>((method, _) =>
            throw new AssertionException($"Permission persistence must not be called: {method.Name}"));
        var idEncoderService = Proxy<IIdEncoderService>((method, _) =>
            throw new AssertionException($"Identifier decoding must not be called: {method.Name}"));
        var behavior = new StaffMemberInstitutionPermissionBehavior<
            GetContactRelationOptionsQuery,
            ApiResponse<IReadOnlyList<ContactRelationOptionResponseDTO>>>(
                unitOfWork,
                commonRequestService,
                idEncoderService,
                NullLogger<StaffMemberInstitutionPermissionBehavior<
                    GetContactRelationOptionsQuery,
                    ApiResponse<IReadOnlyList<ContactRelationOptionResponseDTO>>>>.Instance);
        var nextCalled = false;

        Task<ApiResponse<IReadOnlyList<ContactRelationOptionResponseDTO>>> Next(CancellationToken _)
        {
            nextCalled = true;
            return Task.FromResult(
                ApiResponse<IReadOnlyList<ContactRelationOptionResponseDTO>>.Success([]));
        }

        var response = await behavior.Handle(
            new GetContactRelationOptionsQuery(),
            Next,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(response.IsSucceeded, Is.True);
            Assert.That(nextCalled, Is.True);
        });
    }

    [Test]
    public void Relation_lookup_does_not_allow_anonymous_access()
    {
        var method = typeof(ContactController).GetMethod(nameof(ContactController.GetRelationOptions));
        Assert.That(method!.GetCustomAttribute<AuthorizeAttribute>(), Is.Not.Null);
        Assert.That(method.GetCustomAttribute<AllowAnonymousAttribute>(), Is.Null);
        Assert.That(typeof(ContactController).GetCustomAttribute<AllowAnonymousAttribute>(), Is.Null);
    }

    [Test]
    public void StaffMember_creation_placeholder_copies_only_name_country_and_required_metadata()
    {
        var staffMember = new StaffMember
        {
            FirstName = "Jane",
            MiddleName = "Q",
            LastName = "StaffMember",
            CountryId = 10
        };
        var addedAt = new DateTime(2026, 10, 1, 8, 30, 0, DateTimeKind.Utc);

        var contact = StaffMemberContactInfoMapperHelper.CreateInitialContact(
            staffMember,
            25,
            addedAt);

        Assert.Multiple(() =>
        {
            Assert.That(contact.StaffMember, Is.SameAs(staffMember));
            Assert.That(contact.ContactName, Is.EqualTo("Jane Q StaffMember"));
            Assert.That(contact.CountryId, Is.EqualTo(10));
            Assert.That(contact.Relation, Is.Null);
            Assert.That(contact.ContactType, Is.Null);
            Assert.That(contact.ContactNumber, Is.Null);
            Assert.That(contact.AlternateNumber, Is.Null);
            Assert.That(contact.Email, Is.Null);
            Assert.That(contact.StateId, Is.Null);
            Assert.That(contact.DistrictId, Is.Null);
            Assert.That(contact.LocalityId, Is.Null);
            Assert.That(contact.HouseNo, Is.Null);
            Assert.That(contact.LandMark, Is.Null);
            Assert.That(contact.Street, Is.Null);
            Assert.That(contact.Address, Is.Null);
            Assert.That(contact.Remark, Is.Null);
            Assert.That(contact.Description, Is.Null);
            Assert.That(contact.IsPrimary, Is.False);
            Assert.That(contact.IsActive, Is.True);
            Assert.That(contact.IsSoftDeleted, Is.False);
            Assert.That(contact.IsEditAllowed, Is.True);
            Assert.That(contact.IsInfoVerified, Is.False);
            Assert.That(contact.AddedById, Is.EqualTo(25));
            Assert.That(contact.AddedDateTime, Is.EqualTo(addedAt));
        });
    }

    [Test]
    public void Contact_schema_change_is_additive_and_keeps_existing_rows()
    {
        var script = ReadRepositoryFile(
            "database-scripts",
            "AddStaffMemberContactDefaultAndLocality.sql");

        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain("ALTER COLUMN \"ContactNumber\" DROP NOT NULL"));
            Assert.That(script, Does.Contain("ALTER COLUMN \"ContactName\" TYPE varchar(302)"));
            Assert.That(script, Does.Contain("ADD COLUMN IF NOT EXISTS \"LocalityId\" integer NULL"));
            Assert.That(script, Does.Contain("FK_StaffMemberContact_Locality"));
            Assert.That(script, Does.Contain("ON DELETE SET NULL"));
            Assert.That(script, Does.Not.Contain("DELETE FROM"));
            Assert.That(script, Does.Not.Contain("UPDATE skolenza.\"StaffMemberContact\""));
        });
    }

    [TestCase("9876543210", true)]
    [TestCase("+971501234567", true)]
    [TestCase("+919876543210", true)]
    [TestCase("+12025550123", true)]
    [TestCase("+0123456789", false)]
    [TestCase("+971CALLME", false)]
    [TestCase("+1234567890123456", false)]
    [TestCase("123", false)]
    public void Contact_phone_accepts_international_and_preserves_legacy_format(string number, bool valid)
    {
        if (valid)
        {
            Assert.DoesNotThrow(() => StaffMemberProfileValidationHelper.ValidateContactNumber(number, "contact number"));
        }
        else
        {
            Assert.Throws<ValidationErrorException>(() =>
                StaffMemberProfileValidationHelper.ValidateContactNumber(number, "contact number"));
        }
    }

    [TestCase(true, true, true)]
    [TestCase(false, false, true)]
    [TestCase(false, true, false)]
    public void Contact_update_preserves_verified_edit_and_ownership_guards(
        bool verified, bool editable, bool owner)
    {
        var existing = new StaffMemberContact
        {
            Id = 100,
            StaffMemberId = owner ? 25 : 26,
            IsEditAllowed = editable,
            IsInfoVerified = verified
        };
        var repository = Proxy<IStaffMemberContactRepository>((method, _) =>
            method.Name == nameof(IStaffMemberContactRepository.GetSingleRecordAsync)
                ? Task.FromResult<StaffMemberContact?>(existing)
                : throw new AssertionException("Guard must not save: " + method.Name));
        var unitOfWork = Proxy<IUnitOfWork>((method, _) => method.Name switch
        {
            "get_StaffMemberContactRepository" => repository,
            "RollbackTransactionAsync" => Task.CompletedTask,
            _ => throw new AssertionException("Guard must not start transaction: " + method.Name)
        });
        var common = Proxy<ICommonRequestService>((_, _) => Task.FromResult(new CommonDecodedResult
        {
            Success = true,
            UserStaffMemberId = 25
        }));
        var handler = new UpdateContactInfoCommandHandler(
            unitOfWork, NullLogger<UpdateContactInfoCommandHandler>.Instance, common);

        Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new UpdateStaffMemberContactCommand(new UpdateContactRequestDTO { Id = 100 }), default));
    }

    [Test]
    public void Uae_catalogue_uses_source_hierarchy_without_rewriting_existing_rows()
    {
        var script = ReadRepositoryFile("database-scripts", "SeedUaeLocations.sql");
        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain("GeoNames, CC BY 4.0"));
            Assert.That(script, Does.Contain("7 states, 35 districts, 2776 localities"));
            Assert.That(script, Does.Contain("Unassigned District"));
            Assert.That(script, Does.Contain("c.\"CountryCode\" = 'AE'"));
            Assert.That(script, Does.Not.Contain("DELETE FROM"));
            Assert.That(script, Does.Not.Contain("UPDATE skolenza."));
            Assert.That(script, Does.Contain("pg_advisory_xact_lock"));
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

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> invoke)
        where T : class
    {
        var proxy = DispatchProxy.Create<T, BulkImportPermissionTests.TestProxy>();
        ((BulkImportPermissionTests.TestProxy)(object)proxy).InvokeMethod = invoke;
        return proxy;
    }
}

