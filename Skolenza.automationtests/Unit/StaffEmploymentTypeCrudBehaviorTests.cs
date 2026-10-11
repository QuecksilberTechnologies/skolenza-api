// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Verifies StaffEmploymentType CRUD authorization surface and deletion safety rules.
// ================================================================

using System.Reflection;
using skolenza.application.Common.Models.Security;
using skolenza.application.DTOS.Staff.Type;
using skolenza.application.Exceptions;
using skolenza.application.Features.StaffEmploymentTypeCmd.Handlers;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

/// <summary>
/// Covers the StaffEmploymentType manual CRUD behavior that does not require a database fixture.
/// </summary>
[TestFixture]
[Category("StaffEmploymentTypeCrud")]
public sealed class StaffEmploymentTypeCrudBehaviorTests
{
    #region Delete behavior

    /// <summary>
    /// Verifies that a repository-reported protected dependency prevents soft deletion.
    /// </summary>
    [Test]
    public void Delete_blocks_when_a_protected_dependency_exists()
    {
        var staffEmploymentType = CreateStaffEmploymentType();
        var softDeleteCalled = false;
        var handler = CreateDeleteHandler(staffEmploymentType, hasDependencies: true, () => softDeleteCalled = true);

        var error = Assert.ThrowsAsync<ConflictException>(async () => await handler.Handle(
            new DeleteStaffEmploymentTypeCommand(new DeleteStaffEmploymentTypeRequestDTO
            {
                Id = staffEmploymentType.Id,
                ModuleId = 31,
                OperationId = 3
            }),
            CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(error!.Message, Does.Contain("cannot be deleted"));
            Assert.That(softDeleteCalled, Is.False);
            Assert.That(staffEmploymentType.IsSoftDeleted, Is.False);
        });
    }

    /// <summary>
    /// Verifies that an unused StaffEmploymentType is soft deleted and retains the trusted audit actor.
    /// </summary>
    [Test]
    public async Task Delete_soft_deletes_an_unused_staffMember_type_with_audit_values()
    {
        var staffEmploymentType = CreateStaffEmploymentType();
        var softDeleteCalled = false;
        var handler = CreateDeleteHandler(staffEmploymentType, hasDependencies: false, () => softDeleteCalled = true);

        var result = await handler.Handle(
            new DeleteStaffEmploymentTypeCommand(new DeleteStaffEmploymentTypeRequestDTO
            {
                Id = staffEmploymentType.Id,
                ModuleId = 31,
                OperationId = 3
            }),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSucceeded, Is.True);
            Assert.That(softDeleteCalled, Is.True);
            Assert.That(staffEmploymentType.SoftDeletedById, Is.EqualTo(44));
            Assert.That(staffEmploymentType.UpdatedById, Is.EqualTo(44));
            Assert.That(staffEmploymentType.IsSoftDeleted, Is.True);
            Assert.That(staffEmploymentType.IsActive, Is.False);
        });
    }

    #endregion

    #region API surface and seed contract

    /// <summary>
    /// Verifies institution-scoped paging preserves inactive master records and returns an empty page beyond the result set.
    /// </summary>
    [TestCase(1, 1)]
    [TestCase(2, 0)]
    public async Task List_preserves_repository_response_and_page_metadata(int pageNumber, int expectedCount)
    {
        var rows = new List<skolenza.application.DTOs.StaffType.GetStaffEmploymentTypeResponseDTO>
        {
            new() { Id = 12, TypeName = "Inactive type", IsActive = false }
        };
        var repository = Proxy<IStaffEmploymentTypeRepository>((method, args) =>
        {
            Assert.That(method.Name, Is.EqualTo(nameof(IStaffEmploymentTypeRepository.GetAllAsync)));
            Assert.That(args![0], Is.EqualTo(9L));
            return Task.FromResult(rows);
        });
        var unitOfWork = Proxy<IUnitOfWork>((method, _) => method.Name == "get_StaffEmploymentTypeRepository"
            ? repository
            : throw new InvalidOperationException("Unexpected UnitOfWork call: " + method.Name));
        var common = Proxy<ICommonRequestService>((method, _) =>
            method.Name == nameof(ICommonRequestService.ValidateInstitutionUserRequestAsync)
                ? Task.FromResult(new CommonDecodedResult
                {
                    Success = true,
                    InstitutionId = 9,
                    LoggedInStaffMemberId = 44,
                    RoleId = 7
                })
                : throw new InvalidOperationException("Unexpected context call: " + method.Name));

        var response = await new GetStaffEmploymentTypesQueryHandler(unitOfWork, common).Handle(
            new GetStaffEmploymentTypesQuery(new GetStaffEmploymentTypeRequestDTO
            {
                PageNumber = pageNumber,
                PageSize = 1
            }), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(response.IsSucceeded, Is.True);
            Assert.That(response.Data, Has.Count.EqualTo(expectedCount));
            Assert.That(response.TotalRecords, Is.EqualTo(1));
            Assert.That(response.TotalPages, Is.EqualTo(1));
            if (expectedCount == 1)
            {
                Assert.That(response.Data![0].Id, Is.EqualTo(12));
                Assert.That(response.Data[0].IsActive, Is.False);
            }
        });
    }

    /// <summary>
    /// Verifies the explicit authenticated update and delete endpoints required by the UI.
    /// </summary>
    [Test]
    public void Update_and_delete_routes_are_authenticated_and_explicit()
    {
        var controller = typeof(skolenza.api.Controllers.StaffEmploymentType.StaffEmploymentTypeController);
        var update = controller.GetMethod("Update")!;
        var delete = controller.GetMethod("Delete")!;

        Assert.Multiple(() =>
        {
            Assert.That(update.GetCustomAttribute<AuthorizeAttribute>(), Is.Not.Null);
            Assert.That(update.GetCustomAttribute<HttpPutAttribute>()!.Template, Is.EqualTo("update"));
            Assert.That(delete.GetCustomAttribute<AuthorizeAttribute>(), Is.Not.Null);
            Assert.That(delete.GetCustomAttribute<HttpDeleteAttribute>()!.Template, Is.EqualTo("delete"));
        });
    }

    /// <summary>
    /// Verifies the StaffEmploymentType leaf is under StaffMember Management with CRUD and bulk operations seeded.
    /// </summary>
    [Test]
    public void Seed_script_declares_staffMember_management_parent_and_crud_bulk_operations()
    {
        var repositoryRoot = FindRepositoryRoot();
        var script = File.ReadAllText(Path.Combine(repositoryRoot, "database-scripts", "SeedInstitutionStaffEmploymentTypeModule.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain("STAFF_MEMBER_TYPE"));
            Assert.That(script, Does.Contain("EMP_MGMT"));
            Assert.That(script, Does.Contain("EMP_LIST"));
            Assert.That(script, Does.Contain("IN ('add','update','delete','view')"));
            Assert.That(script, Does.Contain("'import' AND op.\"OperationType\"=12"));
            Assert.That(script, Does.Contain("'export' AND op.\"OperationType\"=11"));
            Assert.That(script, Does.Not.Contain("DELETE FROM skolenza.\"InstitutionEnabledOperation\""));
            Assert.That(script, Does.Not.Contain("DELETE FROM skolenza.\"ModuleOperationMapping\""));
        });
    }

    #endregion

    #region Test helpers

    private static DeleteStaffEmploymentTypeCommandHandler CreateDeleteHandler(
        StaffEmploymentType staffEmploymentType,
        bool hasDependencies,
        Action onSoftDelete)
    {
        var repository = Proxy<IStaffEmploymentTypeRepository>((method, _) => method.Name switch
        {
            nameof(IStaffEmploymentTypeRepository.GetForUpdateAsync) => Task.FromResult<StaffEmploymentType?>(staffEmploymentType),
            nameof(IStaffEmploymentTypeRepository.HasDeletionDependenciesAsync) => Task.FromResult(hasDependencies),
            nameof(IStaffEmploymentTypeRepository.SoftDeleteAsync) => SoftDeleteAsync(staffEmploymentType, onSoftDelete),
            _ => throw new InvalidOperationException("Unexpected StaffEmploymentType repository call: " + method.Name)
        });
        var unitOfWork = Proxy<IUnitOfWork>((method, _) => method.Name == "get_StaffEmploymentTypeRepository"
            ? repository
            : throw new InvalidOperationException("Unexpected UnitOfWork call: " + method.Name));
        var commonRequestService = Proxy<ICommonRequestService>((method, _) =>
            method.Name == nameof(ICommonRequestService.ValidateInstitutionUserRequestAsync)
                ? Task.FromResult(new CommonDecodedResult
                {
                    Success = true,
                    InstitutionId = staffEmploymentType.InstitutionId!.Value,
                    LoggedInStaffMemberId = 44,
                    RoleId = 7
                })
                : throw new InvalidOperationException("Unexpected common request call: " + method.Name));

        return new DeleteStaffEmploymentTypeCommandHandler(
            unitOfWork,
            commonRequestService,
            NullLogger<DeleteStaffEmploymentTypeCommandHandler>.Instance);
    }

    private static Task<bool> SoftDeleteAsync(StaffEmploymentType staffEmploymentType, Action onSoftDelete)
    {
        onSoftDelete();
        staffEmploymentType.IsSoftDeleted = true;
        staffEmploymentType.IsActive = false;
        return Task.FromResult(true);
    }

    private static StaffEmploymentType CreateStaffEmploymentType()
    {
        return new StaffEmploymentType
        {
            Id = 12,
            InstitutionId = 9,
            TypeName = "Permanent",
            IsActive = true,
            IsSoftDeleted = false
        };
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Skolenza.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the repository root for seed script verification.");
    }

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> invoke)
        where T : class
    {
        var proxy = DispatchProxy.Create<T, BulkImportPermissionTests.TestProxy>();
        ((BulkImportPermissionTests.TestProxy)(object)proxy).InvokeMethod = invoke;
        return proxy;
    }

    #endregion
}

