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
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using skolenza.api.Middlewares;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.infrastructure.CommonRequest;

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

    [Test]
    public async Task Student_document_review_controller_forwards_the_permission_and_document_context()
    {
        ReviewStudentRecordDocumentCommand? captured = null;
        var mediator = Proxy<IMediator>((method, args) =>
        {
            Assert.That(method.Name, Is.EqualTo("Send"));
            captured = args![0] as ReviewStudentRecordDocumentCommand;
            return Task.FromResult(ApiResponse<StudentRecordDocumentResponseDTO>.Success(new StudentRecordDocumentResponseDTO()));
        });
        var request = new StudentAdmissionDocumentReviewDTO
        {
            ModuleId = 101,
            OperationId = 3,
            Status = StudentDocumentReviewStatus.Approved
        };

        var result = await new StudentAdmissionsController(mediator)
            .ReviewStudentRecordDocument(123, 456, request, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            Assert.That(captured?.StudentId, Is.EqualTo(123));
            Assert.That(captured?.DocumentId, Is.EqualTo(456));
            Assert.That(captured?.DTO, Is.SameAs(request));
        });
    }

    [TestCase(StudentConstants.AdmissionsModuleCode, "Update", true, 1, true)]
    [TestCase(StudentConstants.AdmissionsModuleCode, "View", true, 1, false)]
    [TestCase(StudentConstants.FeesModuleCode, "Update", true, 1, false)]
    [TestCase(StudentConstants.AdmissionsModuleCode, "Update", false, 1, false)]
    [TestCase(StudentConstants.AdmissionsModuleCode, "Update", true, 0, false)]
    public async Task Student_document_review_requires_active_admissions_update_permission(
        string moduleCode, string operationName, bool operationActive, int permissionResult, bool allowed)
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
                new() { Id = 3, OperationName = operationName, IsActive = operationActive }
            }),
            _ => throw new AssertionException($"Unexpected operation request: {method.Name}")
        });
        var permissions = Proxy<IStoreProcedureRepository>((method, args) => method.Name switch
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
        var behavior = new StudentPermissionBehavior<ReviewStudentRecordDocumentCommand, ApiResponse<StudentRecordDocumentResponseDTO>>(unit, common);
        var invoked = false;
        Task<ApiResponse<StudentRecordDocumentResponseDTO>> Next(CancellationToken _)
        {
            invoked = true;
            return Task.FromResult(ApiResponse<StudentRecordDocumentResponseDTO>.Success(new StudentRecordDocumentResponseDTO()));
        }

        var request = new ReviewStudentRecordDocumentCommand(123, 456,
            new StudentAdmissionDocumentReviewDTO { ModuleId = 101, OperationId = 3 });
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

    [Test]
    public async Task Student_document_review_http_requires_bearer_and_existing_update_permission()
    {
        const string signingKey = "synthetic-document-review-key-at-least-32-bytes";
        var allowed = true;
        var reviewed = 0;
        var staffLookups = 0;
        var modules = Proxy<IModuleRepository>((method, _) => method.Name switch
        {
            nameof(IModuleRepository.GetModuleByIdIncludingInactiveAsync) => Task.FromResult<skolenza.domain.Entity.Module?>(
                new skolenza.domain.Entity.Module { ModuleCode = StudentConstants.AdmissionsModuleCode }),
            _ => throw new AssertionException($"Unexpected module request: {method.Name}")
        });
        var operations = Proxy<IOperationRepository>((method, _) => method.Name switch
        {
            nameof(IOperationRepository.GetAllOperationAsync) => Task.FromResult(new List<Operation>
            {
                new() { Id = 3, OperationName = "Update", IsActive = true },
                new() { Id = 4, OperationName = "View", IsActive = true }
            }),
            _ => throw new AssertionException($"Unexpected operation request: {method.Name}")
        });
        var permissions = Proxy<IStoreProcedureRepository>((method, _) => method.Name switch
        {
            nameof(IStoreProcedureRepository.CheckInstitutionStaffMemberPermissionAsync) => Task.FromResult(
                new InstitutionsUserPermissionCheckResponseDTO { ResultCode = allowed ? 1 : 0 }),
            nameof(IStoreProcedureRepository.ValidateActiveUserLoginOnlyAsync) => ActiveStaff(),
            _ => throw new AssertionException($"Unexpected permission request: {method.Name}")
        });
        Task<long> ActiveStaff()
        {
            staffLookups++;
            return Task.FromResult(4L);
        }
        var unit = Proxy<IUnitOfWork>((method, _) => method.Name switch
        {
            "get_ModuleRepository" => modules,
            "get_OperationRepository" => operations,
            "get_StoreProcedureRepository" => permissions,
            _ => throw new AssertionException($"Unexpected unit-of-work request: {method.Name}")
        });
        var encoder = Proxy<IIdEncoderService>((method, _) => method.Name == nameof(IIdEncoderService.DecodeId_long)
            ? 8L : throw new AssertionException($"Unexpected encoder request: {method.Name}"));
        var documents = Proxy<IStudentPortalDocumentRepository>((method, args) =>
        {
            Assert.That(method.Name, Is.EqualTo(nameof(IStudentPortalDocumentRepository.ReviewAsync)));
            Assert.That(args![0], Is.EqualTo(8L));
            Assert.That(args[1], Is.EqualTo(4L));
            Assert.That(args[2], Is.EqualTo(123L));
            Assert.That(args[3], Is.EqualTo(456L));
            reviewed++;
            return Task.FromResult(new StudentRecordDocumentResponseDTO());
        });

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["JWTSettings:Secret"] = signingKey });
        builder.Services.AddMediatR(settings => settings.RegisterServicesFromAssembly(typeof(ReviewStudentRecordDocumentCommand).Assembly));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(StudentPermissionBehavior<,>));
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICommonRequestService, CommonRequestService>();
        builder.Services.AddSingleton(encoder);
        builder.Services.AddSingleton(unit);
        builder.Services.AddSingleton(documents);
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(settings =>
        {
            settings.RequireHttpsMetadata = false;
            settings.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                ClockSkew = TimeSpan.Zero
            };
        });
        builder.Services.AddAuthorization();
        builder.Services.AddControllers().AddApplicationPart(typeof(StudentAdmissionsController).Assembly);
        await using var app = builder.Build();
        app.UseMiddleware<ErrorHandlerMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        const string path = "/api/Student/records/123/documents/456/review";
        var updateRequest = new { moduleId = 101, operationId = 3, status = (short)StudentDocumentReviewStatus.Approved };

        using var anonymous = await client.PostAsJsonAsync(path, updateRequest);
        Assert.That(anonymous.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        string Token(string? userType)
        {
            var claims = new List<Claim>
            {
                new("UserId", "synthetic-staff"),
                new("InstitutionId", "synthetic-institution"),
                new("InstitutionEncriptionKey", "synthetic-key"),
                new("RoleId", "2"),
                new("RoleTypeId", "2")
            };
            if (userType is not null)
            {
                claims.Add(new Claim(AppConstants.UserTypeClaim, userType));
            }
            return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256)));
        }
        var token = Token(AppConstants.InstitutionUserType);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var approved = await client.PostAsJsonAsync(path, updateRequest);
        Assert.That(approved.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        using var wrongOperation = await client.PostAsJsonAsync(path,
            new { moduleId = 101, operationId = 4, status = (short)StudentDocumentReviewStatus.Approved });
        Assert.That(wrongOperation.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        allowed = false;
        using var denied = await client.PostAsJsonAsync(path, updateRequest);
        Assert.That(denied.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        allowed = true;
        var lookupsBeforePortal = staffLookups;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(StudentConstants.PortalUserType));
        using var portal = await client.PostAsJsonAsync(path, updateRequest);
        Assert.That(portal.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        Assert.That(staffLookups, Is.EqualTo(lookupsBeforePortal));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(null));
        using var legacy = await client.PostAsJsonAsync(path, updateRequest);
        Assert.That(legacy.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(reviewed, Is.EqualTo(2));
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
            Assert.That(result.Data.FeeArrearsPolicies.Select(item => item.Value),
                Is.EquivalentTo(Enum.GetValues<StudentFeeArrearsPolicy>().Select(value => Convert.ToInt16(value))));
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
