using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using MediatR;
using NUnit.Framework;
using skolenza.api.Controllers.Student;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Models.Security;
using skolenza.application.Constants;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.RoleModulePermission;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;
using skolenza.application.Features.StudentCmd;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using System.Reflection;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StudentRegistry")]
public sealed class StudentRegistryApiTests
{
    [Test]
    public void Every_staff_student_request_carries_permission_metadata()
    {
        var requests = typeof(GetStudentAdmissionQuery).Assembly.GetTypes()
            .Where(type => type.Namespace == typeof(GetStudentAdmissionQuery).Namespace)
            .Where(type => !type.IsAbstract && type.GetInterfaces().Any(contract =>
                contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequest<>)))
            .ToArray();

        var missing = requests.Where(type => !type.GetProperties().Any(property =>
                typeof(PermissionRequestDTO).IsAssignableFrom(property.PropertyType)))
            .Select(type => type.Name)
            .ToArray();

        Assert.That(requests, Is.Not.Empty);
        Assert.That(missing, Is.Empty, "Student staff requests must reach the existing permission behavior with their permission DTO.");
    }

    [Test]
    public async Task Admission_detail_controller_forwards_query_permissions_to_mediator()
    {
        GetStudentAdmissionQuery? captured = null;
        var mediator = Proxy<IMediator>((method, args) =>
        {
            Assert.That(method.Name, Is.EqualTo("Send"));
            captured = args![0] as GetStudentAdmissionQuery;
            return Task.FromResult(ApiResponse<StudentAdmissionResponseDTO>.Success(new StudentAdmissionResponseDTO()));
        });
        var request = new PermissionRequestDTO { ModuleId = 101, OperationId = 3 };

        var result = await new StudentAdmissionsController(mediator).GetAdmission(123, request, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            Assert.That(captured?.Id, Is.EqualTo(123));
            Assert.That(captured?.DTO, Is.SameAs(request));
            var permissionParameter = typeof(StudentAdmissionsController)
                .GetMethod(nameof(StudentAdmissionsController.GetAdmission))!
                .GetParameters()
                .Single(parameter => parameter.ParameterType == typeof(PermissionRequestDTO));
            Assert.That(permissionParameter.GetCustomAttribute<FromQueryAttribute>(), Is.Not.Null);
        });
    }

    [Test]
    public void Admission_detail_rejects_missing_permission_ids_before_invoking_handler()
    {
        var behavior = new StudentPermissionBehavior<GetStudentAdmissionQuery, ApiResponse<StudentAdmissionResponseDTO>>(
            null!, null!);
        var invoked = false;
        Task<ApiResponse<StudentAdmissionResponseDTO>> Next(CancellationToken _)
        {
            invoked = true;
            return Task.FromResult(ApiResponse<StudentAdmissionResponseDTO>.Success(new StudentAdmissionResponseDTO()));
        }

        Assert.ThrowsAsync<ValidationErrorException>(async () =>
            await behavior.Handle(new GetStudentAdmissionQuery(123, new PermissionRequestDTO()), Next, CancellationToken.None));
        Assert.That(invoked, Is.False);
    }

    [TestCase(StudentConstants.AdmissionsModuleCode, "View", 1, true)]
    [TestCase("UNRELATED_MODULE", "View", 1, false)]
    [TestCase(StudentConstants.AdmissionsModuleCode, "Add", 1, false)]
    [TestCase(StudentConstants.AdmissionsModuleCode, "View", 0, false)]
    public async Task Admission_detail_requires_the_existing_view_permission(
        string moduleCode, string operationName, int permissionResult, bool allowed)
    {
        var common = Proxy<ICommonRequestService>((method, _) => method.Name switch
        {
            nameof(ICommonRequestService.GetModuleCodeAsync) => Task.FromResult<string?>(moduleCode),
            nameof(ICommonRequestService.ValidateInstitutionUserRequestAsync) => Task.FromResult(new CommonDecodedResult
            {
                Success = true,
                InstitutionId = 8,
                LoggedInStaffMemberId = 4,
                RoleId = 2
            }),
            _ => throw new AssertionException($"Unexpected request: {method.Name}")
        });
        var operations = Proxy<IOperationRepository>((method, _) => method.Name switch
        {
            nameof(IOperationRepository.GetAllOperationAsync) => Task.FromResult(new List<Operation>
            {
                new() { Id = 3, OperationName = operationName, IsActive = true }
            }),
            _ => throw new AssertionException($"Unexpected operation request: {method.Name}")
        });
        var permissions = Proxy<IStoreProcedureRepository>((method, _) => method.Name switch
        {
            nameof(IStoreProcedureRepository.CheckInstitutionStaffMemberPermissionAsync) => Task.FromResult(
                new InstitutionsUserPermissionCheckResponseDTO { ResultCode = permissionResult }),
            _ => throw new AssertionException($"Unexpected permission request: {method.Name}")
        });
        var unit = Proxy<IUnitOfWork>((method, _) => method.Name switch
        {
            "get_OperationRepository" => operations,
            "get_StoreProcedureRepository" => permissions,
            _ => throw new AssertionException($"Unexpected unit-of-work request: {method.Name}")
        });
        var behavior = new StudentPermissionBehavior<GetStudentAdmissionQuery, ApiResponse<StudentAdmissionResponseDTO>>(unit, common);
        var invoked = false;
        Task<ApiResponse<StudentAdmissionResponseDTO>> Next(CancellationToken _)
        {
            invoked = true;
            return Task.FromResult(ApiResponse<StudentAdmissionResponseDTO>.Success(new StudentAdmissionResponseDTO()));
        }

        var request = new GetStudentAdmissionQuery(123, new PermissionRequestDTO { ModuleId = 101, OperationId = 3 });
        if (allowed)
        {
            await behavior.Handle(request, Next, CancellationToken.None);
        }
        else
        {
            Assert.ThrowsAsync<ForbiddenAccessException>(async () =>
                await behavior.Handle(request, Next, CancellationToken.None));
        }

        Assert.That(invoked, Is.EqualTo(allowed));
    }

    [TestCase("Update", true)]
    [TestCase("View", false)]
    public async Task Cashfree_reconciliation_requires_update_permission(string operationName, bool allowed)
    {
        var common = Proxy<ICommonRequestService>((method, _) => method.Name switch
        {
            nameof(ICommonRequestService.GetModuleCodeAsync) => Task.FromResult<string?>(StudentConstants.FeesModuleCode),
            nameof(ICommonRequestService.ValidateInstitutionUserRequestAsync) => Task.FromResult(new CommonDecodedResult
            {
                Success = true,
                InstitutionId = 8,
                LoggedInStaffMemberId = 4,
                RoleId = 2
            }),
            _ => throw new AssertionException($"Unexpected request: {method.Name}")
        });
        var operations = Proxy<IOperationRepository>((method, _) => method.Name switch
        {
            nameof(IOperationRepository.GetAllOperationAsync) => Task.FromResult(new List<Operation>
            {
                new() { Id = 3, OperationName = operationName, IsActive = true }
            }),
            _ => throw new AssertionException($"Unexpected operation request: {method.Name}")
        });
        var permissions = Proxy<IStoreProcedureRepository>((method, _) => method.Name switch
        {
            nameof(IStoreProcedureRepository.CheckInstitutionStaffMemberPermissionAsync) => Task.FromResult(
                new InstitutionsUserPermissionCheckResponseDTO { ResultCode = 1 }),
            _ => throw new AssertionException($"Unexpected permission request: {method.Name}")
        });
        var unit = Proxy<IUnitOfWork>((method, _) => method.Name switch
        {
            "get_OperationRepository" => operations,
            "get_StoreProcedureRepository" => permissions,
            _ => throw new AssertionException($"Unexpected unit-of-work request: {method.Name}")
        });
        var behavior = new StudentPermissionBehavior<ReconcileStudentCashfreePaymentCommand, ApiResponse<StudentCashfreePaymentStatusResponseDTO>>(unit, common);
        var invoked = false;
        Task<ApiResponse<StudentCashfreePaymentStatusResponseDTO>> Next(CancellationToken _)
        {
            invoked = true;
            return Task.FromResult(ApiResponse<StudentCashfreePaymentStatusResponseDTO>.Success(new StudentCashfreePaymentStatusResponseDTO()));
        }

        var request = new ReconcileStudentCashfreePaymentCommand(
            123, new StudentFeePaymentStatusRequestDTO { ModuleId = 101, OperationId = 3 });
        if (allowed)
        {
            await behavior.Handle(request, Next, CancellationToken.None);
        }
        else
        {
            Assert.ThrowsAsync<ForbiddenAccessException>(async () =>
                await behavior.Handle(request, Next, CancellationToken.None));
        }

        Assert.That(invoked, Is.EqualTo(allowed));
    }

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> invoke) where T : class
    {
        var proxy = DispatchProxy.Create<T, BulkImportPermissionTests.TestProxy>();
        ((BulkImportPermissionTests.TestProxy)(object)proxy).InvokeMethod = invoke;
        return proxy;
    }

    [Test]
    public void Student_records_and_admissions_map_to_bigint_owned_tables()
    {
        using var context = new SkolenzaDbContext(
            new DbContextOptionsBuilder<SkolenzaDbContext>()
                .UseNpgsql("Host=localhost;Database=skolenza_model_validation;Username=postgres;Password=not-used")
                .Options);

        var ownedEntities = new[]
        {
            typeof(Student),
            typeof(StudentAdmission),
            typeof(StudentAdmissionStatusHistory),
            typeof(StudentAdmissionGuardian),
            typeof(StudentEnrollment),
            typeof(Guardian),
            typeof(StudentGuardian),
            typeof(StudentExitHistory),
            typeof(StudentEducationIdentifierMaster),
            typeof(StudentEducationIdentifier)
        };

        foreach (var entityType in ownedEntities)
        {
            var entity = context.Model.FindEntityType(entityType);
            Assert.That(entity, Is.Not.Null, entityType.Name);
            Assert.That(entity?.GetSchema(), Is.EqualTo("skolenza"), entityType.Name);
            Assert.That(entity?.GetTableName(), Is.Not.Empty, entityType.Name);
            Assert.That(entity?.FindProperty("Id")?.ClrType, Is.EqualTo(typeof(long)), $"{entityType.Name}.Id");
        }

        Assert.That(context.Model.FindEntityType(typeof(StudentEnrollment))?.GetIndexes().Any(index => index.IsUnique), Is.True);
        Assert.That(context.Model.FindEntityType(typeof(Guardian))?.GetIndexes().Any(index =>
            index.IsUnique && index.GetDatabaseName() == "UX_Guardian_Institution_Email"), Is.True);
        Assert.That(context.Model.FindEntityType(typeof(StudentGuardian))?.GetIndexes().Any(index =>
            index.IsUnique && index.GetDatabaseName() == "UX_StudentGuardian_OnePrimary"), Is.True);
        Assert.That(context.Model.FindEntityType(typeof(StudentAdmissionGuardian))?.GetIndexes().Any(index =>
            index.IsUnique && index.GetDatabaseName() == "UX_StudentAdmissionGuardian_ActiveLink"), Is.True);
        Assert.That(context.Model.FindEntityType(typeof(StudentAdmissionGuardian))?.GetIndexes().Any(index =>
            index.IsUnique && index.GetDatabaseName() == "UX_StudentAdmissionGuardian_OnePrimary"), Is.True);
    }

    [Test]
    public void Admission_controller_is_authenticated_and_routes_are_long_id_safe()
    {
        var controller = typeof(StudentAdmissionsController);
        var routes = controller.GetMethods().SelectMany(method => method.GetCustomAttributes(typeof(HttpMethodAttribute), true)
            .Cast<HttpMethodAttribute>().Select(attribute => attribute.Template)).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(controller.GetCustomAttributes(typeof(AuthorizeAttribute), true), Is.Not.Empty);
            Assert.That(routes, Does.Contain("admissions"));
            Assert.That(routes, Does.Contain("records"));
            Assert.That(routes, Does.Contain("admissions/{id:long}"));
            Assert.That(routes, Does.Contain("admissions/{id:long}/review"));
            Assert.That(routes, Does.Contain("admissions/{id:long}/guardians"));
            Assert.That(routes, Does.Contain("admissions/{id:long}/guardians/{linkId:long}"));
            Assert.That(routes, Does.Contain("records/{id:long}"));
            Assert.That(routes, Does.Contain("records/{id:long}/exit-history"));
            Assert.That(routes, Does.Contain("options"));
            Assert.That(routes, Does.Contain("guardians"));
            Assert.That(routes, Does.Contain("guardians/{id:long}"));
            Assert.That(routes, Does.Contain("records/{id:long}/guardians"));
            Assert.That(routes, Does.Contain("records/{id:long}/guardians/{linkId:long}"));
        });
    }

    [Test]
    public async Task Student_options_api_returns_values_from_central_enums()
    {
        var handler = new GetStudentOptionsQueryHandler();
        var result = await handler.Handle(
            new GetStudentOptionsQuery(new StudentOptionsRequestDTO()),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSucceeded, Is.True);
            Assert.That(result.Data.AdmissionStatuses.Select(item => item.Name),
                Is.EquivalentTo(Enum.GetNames<StudentAdmissionStatus>()));
            Assert.That(result.Data.DocumentPersonScopes.Select(item => item.Value),
                Is.EquivalentTo(Enum.GetValues<StudentDocumentPersonScope>().Select(value => Convert.ToInt16(value))));
            Assert.That(result.Data.FeeChargeNatures.Select(item => item.Name),
                Is.EquivalentTo(Enum.GetNames<StudentFeeComponentChargeNature>()));
            Assert.That(result.Data.ExitReasons.Select(item => item.Value),
                Is.EquivalentTo(Enum.GetValues<StudentExitReason>().Select(value => Convert.ToInt16(value))));
            Assert.That(result.Data.GuardianRelationships.Select(item => item.Name),
                Is.EquivalentTo(Enum.GetNames<StudentGuardianRelationshipType>()));
        });
    }

    [Test]
    public void Admission_statuses_and_exit_reasons_remain_centrally_defined()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Enum.GetValues<StudentAdmissionStatus>(), Has.Length.EqualTo(7));
            Assert.That((short)StudentAdmissionStatus.Draft, Is.EqualTo(1));
            Assert.That((short)StudentAdmissionStatus.ReturnedForCorrection, Is.EqualTo(6));
            Assert.That((short)StudentAdmissionStatus.Cancelled, Is.EqualTo(7));
            Assert.That(Enum.GetValues<StudentExitReason>(), Has.Length.EqualTo(4));
            Assert.That(typeof(StudentExitRequestDTO).GetProperty(nameof(StudentExitRequestDTO.DestinationSchoolCountryId)), Is.Not.Null);
            Assert.That(typeof(StudentEducationIdentifier).GetProperty(nameof(StudentEducationIdentifier.EncryptedValue)), Is.Not.Null);
            Assert.That(typeof(StudentEducationIdentifier).GetProperty(nameof(StudentEducationIdentifier.MaskedValue)), Is.Not.Null);
        });
    }

    [Test]
    public void Additive_registry_schema_preserves_exit_history_and_issued_identifier_values()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        var script = File.ReadAllText(Path.Combine(root, "database-scripts", "CreateStudentAdmissionsAndRecords.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain("StudentExitHistory"));
            Assert.That(script, Does.Contain("StudentEducationIdentifierMaster"));
            Assert.That(script, Does.Contain("EncryptedValue"));
            Assert.That(script, Does.Contain("StudentAdmissionStatusHistory"));
            Assert.That(script, Does.Contain("StudentAdmissionGuardian"));
            Assert.That(script, Does.Contain("UX_StudentAdmissionGuardian_OnePrimary"));
            Assert.That(script, Does.Contain("REFERENCES skolenza.\"Student\"(\"Id\") ON DELETE RESTRICT"));
            Assert.That(script, Does.Not.Contain("DROP TABLE"));
        });
    }

    [Test]
    public void Admission_approval_validates_guardian_documents_and_saved_fee_schedule_before_student_creation()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        var source = File.ReadAllText(Path.Combine(root, "Skolenza.persistance", "Repositories", "StudentRegistryRepository.cs"));
        var reviewStart = source.IndexOf("ReviewAdmissionAsync(", StringComparison.Ordinal);
        var transactionStart = source.IndexOf("BeginTransactionAsync(cancellationToken)", reviewStart, StringComparison.Ordinal);
        var readinessCheck = source.IndexOf("ValidateAdmissionReadyForApprovalAsync(institutionId, entity, cancellationToken)", reviewStart, StringComparison.Ordinal);
        var studentCreate = source.IndexOf("context.Students.Add(student)", reviewStart, StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(transactionStart, Is.GreaterThanOrEqualTo(0));
            Assert.That(readinessCheck, Is.GreaterThan(transactionStart));
            Assert.That(studentCreate, Is.GreaterThan(readinessCheck));
            Assert.That(source, Does.Contain("exactly one active primary Guardian link"));
            Assert.That(source, Does.Contain("HasCurrentApprovedDocumentAsync"));
            Assert.That(source, Does.Contain("A fee quote and installment schedule are required"));
            Assert.That(source, Does.Contain("StudentFeeInstallments.AsNoTracking()"));
        });
    }
}
