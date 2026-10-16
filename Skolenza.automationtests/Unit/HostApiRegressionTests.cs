// ================================================================
// Purpose : Regression coverage for authenticated Host administration APIs,
//           the Institution onboarding verification state, and the deliberate
//           Host/Institution device-configuration ownership boundary.
// ================================================================

using System.Reflection;
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using skolenza.persistance.Data.Context;
using skolenza.persistance.Repositories;
using skolenza.application.DTOs.Institution;
using System.Text.RegularExpressions;
using skolenza.api.Controllers.DefaultEmailConfig;
using skolenza.api.Controllers.EmailTemplate;
using skolenza.api.Controllers.Host;
using skolenza.api.Controllers.HostDevice;
using skolenza.api.Controllers.Institution;
using skolenza.api.Controllers.InstitutionEmailConfig;
using skolenza.api.Middlewares;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.Common.Models.Security;
using skolenza.application.Constants;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Host;
using skolenza.application.DTOS.Navigation;
using skolenza.application.Exceptions;
using skolenza.application.Features.HostCmd.Handler;
using skolenza.application.Features.HostDeviceCmd;
using skolenza.application.Features.HostDeviceCmd.Handlers;
using skolenza.application.Features.NavigationCmd.Handlers;
using skolenza.application.Features.InstitutionManagementCmd.Commands;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEmail;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Interfaces.ITokenService;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

/// <summary>
/// Covers the Host pages shown in the administration UI. These tests use the
/// existing controller, behavior, mapping, and constant contracts; they do
/// not require a browser, device, broker, or live database.
/// </summary>
[TestFixture]
[Category("HostApi")]
public sealed class HostApiRegressionTests
{
    [Test]
    public void Institution_device_create_payload_without_operation_is_rejected_before_database_write()
    {
        var dto = System.Text.Json.JsonSerializer.Deserialize<CreateInstitutionDeviceRequestDTO>(
            """
            {"institutionId":"78N5XZW2","campusId":5,"deviceMasterId":2,
             "deviceCode":"QT-AI-06-AYUC24030780","deviceName":"QT-AI-82000",
             "moduleId":35,"isActive":true,"isAttendanceDevice":true}
            """, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Assert.That(dto.OperationId, Is.Zero);
        var stored = CreateProxy<IStoreProcedureRepository>((_, _) =>
            throw new AssertionException("Missing OperationId must be rejected before querying a grant."));
        var unit = CreateProxy<IUnitOfWork>((method, _) => method.Name == "get_StoreProcedureRepository"
            ? stored : throw new AssertionException("Invalid request must not write device data."));
        var handler = new skolenza.application.Features.HostDeviceCmd.Handlers.CreateInstitutionDeviceCommandHandler(
            unit, null!, CreateHostCommonRequestService(), null!,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<skolenza.application.Features.InstitutionConfigurationCmd.Handlers.InstitutionConfigurationHandlerBase>.Instance,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<skolenza.application.Features.HostDeviceCmd.Handlers.CreateInstitutionDeviceCommandHandler>.Instance);
        Assert.ThrowsAsync<ValidationErrorException>(async () => await handler.Handle(
            new skolenza.application.Features.HostDeviceCmd.Handlers.CreateInstitutionDeviceCommand(dto), CancellationToken.None));
    }

    [TestCase(1, "HOST_CAMPUS_LIST", true)]
    [TestCase(0, "HOST_CAMPUS_LIST", false)]
    [TestCase(-1, "HOST_CAMPUS_LIST", false)]
    [TestCase(-2, "HOST_CAMPUS_LIST", false)]
    [TestCase(1, "INSTITUTION_LOCATIONS", false)]
    [TestCase(1, "HOST_INSTITUTION_LIST", false)]
    public async Task Host_location_list_pipeline_enforces_grant_and_module(
        int resultCode, string moduleCode, bool allowed)
    {
        var permissionCalls = 0;
        var stored = CreateProxy<IStoreProcedureRepository>((method, args) =>
        {
            Assert.That(method.Name, Is.EqualTo("CheckHostUserPermissionAsync"));
            Assert.That(args![0], Is.EqualTo(1L));
            Assert.That(args[1], Is.EqualTo(1L));
            Assert.That(args[2], Is.EqualTo(78));
            Assert.That(args[3], Is.EqualTo(4));
            permissionCalls++;
            return Task.FromResult(new HostUserPermissionCheckResponseDTO { ResultCode = resultCode });
        });
        var unit = CreateProxy<IUnitOfWork>((method, _) => method.Name == "get_StoreProcedureRepository"
            ? stored : throw new AssertionException("Unexpected repository access."));
        var common = CreateHostCommonRequestService(1, moduleCode);
        var behavior = new skolenza.application.Features.InstitutionConfigurationCmd.InstitutionLocationPermissionBehavior<
            skolenza.application.Features.InstitutionConfigurationCmd.Handlers.GetCampussQuery, bool>(
            unit, common, Microsoft.Extensions.Logging.Abstractions.NullLogger<
                skolenza.application.Features.InstitutionConfigurationCmd.InstitutionLocationPermissionBehavior<
                    skolenza.application.Features.InstitutionConfigurationCmd.Handlers.GetCampussQuery, bool>>.Instance);
        var request = new skolenza.application.Features.InstitutionConfigurationCmd.Handlers.GetCampussQuery(
            new skolenza.application.DTOS.InstitutionConfiguration.CampusFilterRequestDTO
            {
                ModuleId = 78, OperationId = 4, InstitutionId = "78N5XZW2", PageNumber = 1, PageSize = 500
            });
        var reachedHandler = false;
        Task<bool> Next(CancellationToken _)
        {
            reachedHandler = true;
            return Task.FromResult(true);
        }
        if (allowed)
        {
            Assert.That(await behavior.Handle(request, Next, CancellationToken.None), Is.True);
        }
        else if (resultCode < 0)
        {
            Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await behavior.Handle(request, Next, CancellationToken.None));
        }
        else
        {
            Assert.ThrowsAsync<ForbiddenAccessException>(async () => await behavior.Handle(request, Next, CancellationToken.None));
        }
        Assert.That(permissionCalls, Is.EqualTo(1));
        Assert.That(reachedHandler, Is.EqualTo(allowed));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Institution_creation_real_database_rollback_probe(bool host)
    {
        var path = Environment.GetEnvironmentVariable("SKOLENZA_HOST_DB_SETTINGS");
        if (string.IsNullOrWhiteSpace(path)) Assert.Ignore("Opt-in real database rollback test.");
        var config = new ConfigurationBuilder().AddJsonFile(path!).Build();
        await using var db = new SkolenzaDbContext(new DbContextOptionsBuilder<SkolenzaDbContext>()
            .UseNpgsql(config.GetConnectionString("DefaultConnection")).Options);
        var mapper = new AutoMapper.MapperConfiguration(c => c.AddProfile<skolenza.application.Mappings.MappingProfile>()).CreateMapper();
        var encryption = new skolenza.infrastructure.EncryptionService.AesEncryptionService();
        var encoder = new skolenza.infrastructure.EncryptionService.IdEncoderService();
        var password = new skolenza.infrastructure.Security.HashedService.PasswordService(NullLogger<skolenza.infrastructure.Security.HashedService.PasswordService>.Instance);
        var ctor = typeof(global::UnitOfWork).GetConstructors().Single();
        var values = ctor.GetParameters().Select(p =>
        {
            object? value = p.ParameterType == typeof(SkolenzaDbContext) ? db :
                p.ParameterType == typeof(Microsoft.Extensions.Logging.ILoggerFactory) ? NullLoggerFactory.Instance :
                p.ParameterType == typeof(AutoMapper.IMapper) ? mapper :
                p.ParameterType == typeof(IConfiguration) ? config :
                p.ParameterType == typeof(IEncryptionService) ? encryption :
                p.ParameterType == typeof(IIdEncoderService) ? encoder :
                p.ParameterType == typeof(skolenza.application.Interfaces.IHashed.IPasswordService) ? password : null;
            if (value != null) return value;
            var proxy = DispatchProxy.Create(p.ParameterType, typeof(InterfaceProxy));
            ((InterfaceProxy)proxy).Handler = (m, _) => throw new InvalidOperationException("Unexpected dependency: " + m.Name);
            return proxy;
        }).ToArray();
        using var real = (global::UnitOfWork)ctor.Invoke(values);
        var reachedCommit = false;
        var wrapper = CreateProxy<IUnitOfWork>((m, a) =>
        {
            if (m.Name == "CommitTransactionAsync") { reachedCommit = true; return real.RollbackTransactionAsync(); }
            return m.Invoke(real, a);
        });
        var logger = new CreationProbeLogger();
        var handler = new skolenza.application.Features.RegistrationCmd.Handlers.CreateInstitutionCommandHandler(
            CreateProxy<ITokenService>((_, _) => Task.FromResult("probe-token")), mapper, wrapper, logger,
            CreateProxy<IEmailService>((_, _) => Task.FromResult(true)),
            CreateProxy<IStoreProcedureRepository>((_, _) => Task.FromResult(new HostUserPermissionCheckResponseDTO { ResultCode = 1 })),
            password, encryption, encoder, config, CreateHostCommonRequestService());
        var dto = new skolenza.application.DTOs.Registration.InstitutionCreateRequestDTO
        {
            SubscriptionPlanId = 1, InstitutionTypeId = 1, CompanyName = "Rollback Probe", InstitutionCode = "QTPROBE",
            CompanyEmailDomain = "example.test", InstitutionEmail = "probe-" + Guid.NewGuid().ToString("N") + "@example.test",
            ContactPersonName = "Test Admin", ContactNumber = "+919827177773", CountryId = 1, GenderId = 1, Prefix = "QT", IncludeMonth = false
        };
        ApiResponse<skolenza.application.DTOs.Registration.InstitutionCreateResponseDTO> result;
        if (host)
        {
            var sender = CreateProxy<ISender>((_, a) => handler.Handle(
                (skolenza.application.Features.RegistrationCmd.Handlers.CreateInstitutionCommand)a![0]!, (CancellationToken)a[1]!));
            result = await new CreateNewInstitutionCommandHandler(sender).Handle(new(new NewInstitutionCreationRequestDTO
            {
                ModuleId = 34, OperationId = 1, SubscriptionPlanId = 1, InstitutionTypeId = 1,
                CompanyName = dto.CompanyName, InstitutionCode = dto.InstitutionCode, InstitutionEmail = dto.InstitutionEmail,
                CompanyEmailDomain = dto.CompanyEmailDomain, ContactPersonName = dto.ContactPersonName,
                ContactNumber = dto.ContactNumber, CountryId = 1, GenderId = 1,
                InitialLocation = new() { LocationCode = "HQ-54-001", LocationName = "Main Campus", Address = "Registration probe address" },
                Profile = new() { LogoUrl = "https://www.quecksilber.org/office-place.webp", ThemeColor = "#b4cafa" },
                StaffMemberCodePattern = new() { Prefix = "QT", IncludeMonth = false }
            }), default);
        }
        else result = await handler.Handle(new(dto), default);
        Assert.That(result.IsSucceeded && reachedCommit, Is.True, result.Message + "\n" + logger.Error);
    }

    private sealed class CreationProbeLogger : Microsoft.Extensions.Logging.ILogger<skolenza.application.Features.RegistrationCmd.Handlers.CreateInstitutionCommandHandler>
    {
        public string? Error;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel level) => true;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel level, Microsoft.Extensions.Logging.EventId id, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter) { if (exception != null) Error = exception.ToString(); }
    }

    [TestCase(false, "success")]
    [TestCase(true, "success")]
    [TestCase(false, "save-failure")]
    [TestCase(true, "save-failure")]
    [TestCase(false, "plan-access-failure")]
    [TestCase(true, "plan-access-failure")]
    [TestCase(false, "duplicate-setup-data")]
    [TestCase(true, "duplicate-setup-data")]
    [TestCase(false, "duplicate-email")]
    [TestCase(true, "duplicate-email")]
    [TestCase(false, "cancelled")]
    [TestCase(true, "cancelled")]
    [TestCase(false, "email-failure")]
    [TestCase(true, "email-failure")]
    public async Task Institution_creation_awaits_dependencies_and_preserves_transaction_outcome(bool host, string scenario)
    {
        var calls = new List<string>();
        var departments = new Dictionary<string, int>();
        Campus? stagedInitialLocation = null;
        StaffMember? stagedAdministrator = null;
        var saveChangesCalls = 0;
        var requestCancellation = scenario == "cancelled"
            ? new CancellationToken(canceled: true)
            : CancellationToken.None;

        object? Invoke(MethodInfo method, object?[]? args)
        {
            calls.Add(method.Name);
            if (method.Name.StartsWith("get_"))
            {
                Assert.That(method.Name, Is.Not.EqualTo("get_InstitutionEmailConfigRepository"));
                var nested = DispatchProxy.Create(method.ReturnType, typeof(InterfaceProxy));
                ((InterfaceProxy)nested).Handler = Invoke;
                return nested;
            }
            Assert.That(method.Name, Is.Not.EqualTo("AutoCreateUserRoleAndAutomatedRolePermissionMappingAsync"),
                "Permissions must not be inserted again after BulkInsertAsync.");
            if (method.Name == "SaveChangesAsync")
            {
                saveChangesCalls++;
                if (scenario == "save-failure")
                    return Task.FromException<int>(new InvalidOperationException("Simulated database failure"));
                if (scenario == "cancelled")
                    return Task.FromCanceled<int>(requestCancellation);
                if (scenario == "duplicate-setup-data" && saveChangesCalls == 4)
                    return Task.FromException<int>(new RegistrationDbException("23505"));
            }
            if (method.Name == "GetAllSubscribedModuleAsync" && scenario == "plan-access-failure")
                throw new InvalidOperationException("Simulated plan-module failure");
            if (method.Name == "GetActiveDefaultEmailConfigAsync")
            {
                return Task.FromResult<DefaultEmailConfig?>(new DefaultEmailConfig
                {
                    Id = 1,
                    ConfigName = "Test",
                    SmtpHost = "smtp.example.test",
                    SmtpPort = 587,
                    SmtpUsername = "test",
                    SmtpPasswordEncrypted = "encrypted",
                    FromEmail = "no-reply@example.test",
                    FromName = "Skolenza",
                    IsActive = true,
                    IsDefault = true,
                    SecrateKey = "test-key"
                });
            }
            if (method.Name == "AddInstitutionAsync") ((Institution)args![0]!).Id = 501;
            if (method.Name == "AddAsync" && args?[0] is Campus campus)
                stagedInitialLocation = campus;
            if (method.Name == "AddStaffMemberAggregateAsync")
            {
                stagedAdministrator = (StaffMember)args![0]!;
                stagedAdministrator.Id = 50000001;
            }
            if (method.Name == "AutoCreateDepartmentSeedAsync")
                foreach (var d in (IEnumerable<Department>)args![0]!) departments[d.DepartmentName] = departments.Count + 1;
            if (method.ReturnType == typeof(Task)) return Task.Delay(1);
            var resultType = method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
                ? method.ReturnType.GetGenericArguments()[0] : method.ReturnType;
            object? value = method.Name switch
            {
                "CheckInstitutionByEmailAsync" => scenario == "duplicate-email",
                "GetStaffMemberIdByUserLogin" => null,
                "CheckHostUserPermissionAsync" => new HostUserPermissionCheckResponseDTO { ResultCode = 1 },
                "GetDepartmentNameIdMapAsync" => departments,
                "GetInstitutionAdminRoleAsync" => new Role { Id = 1 },
                "GetAllSubscribedModuleAsync" => new List<skolenza.domain.Entity.Module> { new() { Id = 8, IsLeafNode = true } },
                "GetModuleOperationMappings" => new List<ModuleOperationMapping> { new() { ModuleId = 8, OperationId = 4 } },
                "GetAllInstitutionModuleWithOperation" => new InstitutionEnabledOperationsResponseDTO { Modules = [new() { Id = 8, Operations = [new() { Id = 4 }] }] },
                "GetPositionsAsync" => new List<skolenza.application.DTOS.Staff.StaffPositionMasterResponseDTO>
                {
                    new()
                    {
                        Id = 101,
                        Code = ConstantValues.InstitutionAdministratorPositionCode,
                        Name = "Institution Administrator",
                        StaffCategoryId = AppConstants.StaffCategories.Administrative,
                        StaffJobFamilyMasterId = 201
                    }
                },
                "AddInstitutionSubscriptionAsync" => args![0],
                "SendTemplatedEmailUsingHostConfigAsync" => scenario != "email-failure",
                _ => resultType == typeof(bool) ? true : resultType == typeof(int) ? 1 : resultType == typeof(string) ? "test-value" : Activator.CreateInstance(resultType)
            };
            if (method.Name == "GetInstitutionSubscriptionPlanInfoAsync")
            {
                var list = (System.Collections.IList)value!;
                list.Add(Activator.CreateInstance(resultType.GetGenericArguments()[0]));
            }
            if (method.Name == "GetCreatedStaffMemberResponseAsync") resultType.GetProperty("Id")!.SetValue(value, "50000001");
            if (method.ReturnType == resultType) return value;
            return typeof(HostApiRegressionTests).GetMethod(nameof(DelayedResult), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(resultType).Invoke(null, [value]);
        }
        T Proxy<T>() where T : class => CreateProxy<T>(Invoke);
        var registrationLogger = new CreationProbeLogger();
        var handler = new skolenza.application.Features.RegistrationCmd.Handlers.CreateInstitutionCommandHandler(
            Proxy<ITokenService>(), new AutoMapper.MapperConfiguration(c => c.AddProfile<skolenza.application.Mappings.MappingProfile>()).CreateMapper(),
            Proxy<IUnitOfWork>(), registrationLogger,
            Proxy<IEmailService>(), Proxy<IStoreProcedureRepository>(), Proxy<skolenza.application.Interfaces.IHashed.IPasswordService>(),
            Proxy<IEncryptionService>(), Proxy<IIdEncoderService>(),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["FrontEndWebURL:BaseUrl"] = "https://example.test" }).Build(),
            CreateHostCommonRequestService());
        var dto = new skolenza.application.DTOs.Registration.InstitutionCreateRequestDTO
        {
            CompanyName = "Test Institution", InstitutionEmail = "institution@example.test", InstitutionCode = "QT", CompanyEmailDomain = "example.test",
            ContactPersonName = "Test Admin", CountryId = 1, SubscriptionPlanId = 2, InstitutionTypeId = 9
        };
        ApiResponse<skolenza.application.DTOs.Registration.InstitutionCreateResponseDTO> result;
        if (host)
        {
            var sender = CreateProxy<ISender>((_, args) => handler.Handle(
                (skolenza.application.Features.RegistrationCmd.Handlers.CreateInstitutionCommand)args![0]!, (CancellationToken)args[1]!));
            result = await new CreateNewInstitutionCommandHandler(sender).Handle(new CreateNewInstitutionCommand(new NewInstitutionCreationRequestDTO
            {
                ModuleId = 34, OperationId = 1, CompanyName = dto.CompanyName, InstitutionEmail = dto.InstitutionEmail,
                InstitutionCode = dto.InstitutionCode, CompanyEmailDomain = dto.CompanyEmailDomain, ContactPersonName = dto.ContactPersonName,
                CountryId = 1, SubscriptionPlanId = 2, InstitutionTypeId = 9
            }), requestCancellation);
        }
        else result = await handler.Handle(new(dto), requestCancellation);

        var expectedSuccess = scenario is "success" or "email-failure";
        Assert.That(result.IsSucceeded, Is.EqualTo(expectedSuccess), result.Message + " | " + string.Join(",", calls) + " | " + registrationLogger.Error);
        Assert.That(
            calls.Count(c => c == "CommitTransactionAsync"),
            Is.EqualTo(expectedSuccess ? 1 : 0));
        Assert.That(
            calls.Count(c => c == "RollbackTransactionAsync"),
            Is.EqualTo(scenario is "save-failure" or "plan-access-failure" or "duplicate-setup-data" or "cancelled" ? 1 : 0));

        if (expectedSuccess)
        {
            Assert.That(calls.Count(c => c == "BulkInsertAsync"), Is.EqualTo(1));
            Assert.That(calls, Does.Not.Contain("AutoCreatePolicyTypesAsync"));
            Assert.That(calls, Does.Not.Contain("AutoCreateDesignationAsync"));
            Assert.That(calls.Count(c => c == "AssignPrimaryPositionAsync"), Is.EqualTo(1));
            Assert.That(calls.IndexOf("SendTemplatedEmailUsingHostConfigAsync"), Is.GreaterThan(calls.IndexOf("CommitTransactionAsync")));
            Assert.That(result.Data.EmailSent, Is.EqualTo(scenario != "email-failure"));
            Assert.That(stagedInitialLocation, Is.Not.Null);
            Assert.That(stagedAdministrator, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(
                    stagedAdministrator!.StaffCategoryId,
                    Is.EqualTo(AppConstants.StaffCategories.Administrative));
                Assert.That(typeof(StaffMember).GetProperty("DesignationId"), Is.Null);
                Assert.That(stagedInitialLocation!.InstitutionId, Is.EqualTo(501));
                Assert.That(stagedInitialLocation.CountryId, Is.EqualTo(1));
                Assert.That(
                    stagedInitialLocation.LocationCode,
                    Is.EqualTo(host ? "HQ" : ConstantValues.InitialCampusCode));
                Assert.That(
                    stagedInitialLocation.TimeZoneId,
                    Is.EqualTo(host ? "Asia/Kolkata" : ConstantValues.InitialCampusTimeZoneId));
            });
        }

        if (scenario == "save-failure")
        {
            Assert.That(result.ErrorCode, Is.EqualTo(AppConstants.ErrorCodes.InstitutionRegistrationFailed));
            Assert.That(result.Message, Does.Contain("saving the institution account"));
        }

        if (scenario == "plan-access-failure")
        {
            Assert.That(result.ErrorCode, Is.EqualTo(AppConstants.ErrorCodes.InstitutionRegistrationFailed));
            Assert.That(result.Message, Does.Contain("loading the selected plan modules"));
        }

        if (scenario == "duplicate-email")
        {
            Assert.That(result.ErrorCode, Is.EqualTo(AppConstants.ErrorCodes.Conflict));
            Assert.That(result.Message, Is.EqualTo("An institution with this email already exists."));
            Assert.That(calls, Does.Not.Contain("BeginTransactionAsync"));
        }

        if (scenario == "duplicate-setup-data")
        {
            Assert.That(result.ErrorCode, Is.EqualTo(AppConstants.ErrorCodes.Conflict));
            Assert.That(result.Message, Does.Contain("conflicts with existing setup data"));
            Assert.That(result.Message, Does.Contain("creating institution security data"));
        }

        if (scenario == "cancelled")
        {
            Assert.That(result.ErrorCode, Is.EqualTo(AppConstants.ErrorCodes.InstitutionRegistrationFailed));
            Assert.That(result.Message, Does.Contain("cancelled before it completed"));
        }
    }

    [Test]
    public async Task Host_institution_update_synchronizes_the_canonical_location()
    {
        var institution = new Institution
        {
            Id = 71,
            InstitutionTypeId = 9,
            CompanyName = "Original Company",
            InstitutionCode = "ORIGINAL",
            CompanyEmailDomain = "example.test",
            InstitutionEmail = "admin@example.test",
            CountryId = 1,
            IsActive = true
        };
        var location = new Campus
        {
            Id = 19,
            InstitutionId = institution.Id,
            LocationCode = ConstantValues.InitialCampusCode,
            LocationName = institution.CompanyName,
            LocationType = (short)CampusType.HeadOffice,
            CountryId = 1,
            StateId = 10,
            DistrictId = 20,
            LocalityId = 30,
            TimeZoneId = ConstantValues.InitialCampusTimeZoneId,
            IsHeadOffice = true,
            IsActive = true
        };
        var institutionRepository = CreateProxy<IInstitutionRepository>((method, _) => method.Name switch
        {
            nameof(IInstitutionRepository.GetHostManagedInstitutionByIdAsync) => Task.FromResult<Institution?>(institution),
            nameof(IInstitutionRepository.StageHostManagedUpdateAsync) => Task.CompletedTask,
            _ => throw new NotSupportedException($"Unexpected Institution repository call: {method.Name}.")
        });
        var locationRepository = CreateProxy<ICampusRepository>((method, _) => method.Name switch
        {
            nameof(ICampusRepository.GetInitialForUpdateAsync) => Task.FromResult<Campus?>(location),
            _ => throw new NotSupportedException($"Unexpected Campus repository call: {method.Name}.")
        });
        var unitOfWork = CreateProxy<IUnitOfWork>((method, _) => method.Name switch
        {
            "get_InstitutionRepository" => institutionRepository,
            "get_CampusRepository" => locationRepository,
            nameof(IUnitOfWork.SaveChangesAsync) => Task.FromResult(2),
            _ => throw new NotSupportedException($"Unexpected UnitOfWork call: {method.Name}.")
        });
        var encoder = CreateProxy<IIdEncoderService>((method, _) => method.Name switch
        {
            nameof(IIdEncoderService.DecodeId_long) => institution.Id,
            nameof(IIdEncoderService.EncodeId_long) => "encoded-institution",
            _ => throw new NotSupportedException($"Unexpected encoder call: {method.Name}.")
        });
        var handler = new UpdateHostManagedInstitutionCommandHandler(
            unitOfWork,
            CreateHostCommonRequestService(),
            encoder);

        var result = await handler.Handle(
            new UpdateHostManagedInstitutionCommand(
                "encoded-institution",
                new UpdateHostManagedInstitutionRequestDTO
                {
                    CompanyName = "Renamed Company",
                    CountryId = 2
                },
                new PermissionRequestDTO()),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSucceeded, Is.True);
            Assert.That(location.CountryId, Is.EqualTo(2));
            Assert.That(location.StateId, Is.Null);
            Assert.That(location.DistrictId, Is.Null);
            Assert.That(location.LocalityId, Is.Null);
            Assert.That(location.LocationName, Is.EqualTo("Renamed Company"));
            Assert.That(location.UpdatedById, Is.EqualTo(1));
            Assert.That(location.UpdatedDateTime, Is.Not.Null);
        });
    }

    private sealed class RegistrationDbException(string sqlState)
        : DbException("Simulated institution-registration database failure")
    {
        public override string? SqlState => sqlState;
    }

    private static async Task<T> DelayedResult<T>(T value) { await Task.Delay(1); return value; }

    [Test]
    public void Host_institution_creation_contract_uses_host_email_config_only()
    {
        var properties = typeof(NewInstitutionCreationRequestDTO).GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.That(properties, Does.Contain(nameof(NewInstitutionCreationRequestDTO.Profile)));
        Assert.That(properties, Does.Contain(nameof(NewInstitutionCreationRequestDTO.InitialLocation)));
        Assert.That(properties, Does.Contain(nameof(NewInstitutionCreationRequestDTO.StaffMemberCodePattern)));
        Assert.That(properties, Does.Not.Contain("EmailConfiguration"));
        Assert.That(typeof(INewInstitutionOnboardingConfiguration).GetProperties()
            .Any(property => property.Name.Equals("EmailConfiguration", StringComparison.OrdinalIgnoreCase)), Is.False);
    }

    [Test]
    [Category("HostDatabaseRead")]
    public async Task Configured_host_device_database_reads_and_populated_mapping_succeed()
    {
        var settingsPath = Environment.GetEnvironmentVariable("SKOLENZA_HOST_DB_SETTINGS");
        if (string.IsNullOrWhiteSpace(settingsPath)) Assert.Ignore("Set SKOLENZA_HOST_DB_SETTINGS to opt into read-only configured database checks.");
        var configuration = new ConfigurationBuilder().AddJsonFile(settingsPath!).Build();
        await using var context = new SkolenzaDbContext(new DbContextOptionsBuilder<SkolenzaDbContext>()
            .UseNpgsql(configuration.GetConnectionString("DefaultConnection")).Options);
        var modules = await context.Set<skolenza.domain.Entity.Module>().Where(m => m.Id == 35 || m.Id == 36)
            .Select(m => new { m.Id, m.ModuleCode, m.ModuleScope }).ToListAsync();
        foreach (var module in modules) TestContext.WriteLine($"Module {module.Id}: {module.ModuleCode}, scope {module.ModuleScope}");
        var mapper = new AutoMapper.MapperConfiguration(c => c.AddProfile<skolenza.application.Mappings.MappingProfile>()).CreateMapper();
        var devices = await new InstitutionDeviceRepository(context).GetHostPagedAsync(new GetInstitutionDeviceListRequestDTO(), default);
        foreach (var device in devices.Data) mapper.Map<InstitutionDeviceResponseDTO>(device);
        TestContext.WriteLine($"Device rows mapped: {devices.Data.Count}");
        var configurations = await new InstitutionDeviceConfigurationRepository(context).GetHostPagedAsync(new GetInstitutionDeviceConfigurationListRequestDTO(), default);
        foreach (var item in configurations.Data) mapper.Map<InstitutionDeviceConfigurationResponseDTO>(item);
        TestContext.WriteLine($"Configuration rows mapped: {configurations.Data.Count}");
        var cardInstitutionId = (await context.Institutions.AsNoTracking().Select(t => (long?)t.Id).FirstOrDefaultAsync()) ?? 0;
        if (cardInstitutionId > 0)
        {
            var cards = await new InstitutionCardMasterRepository(context).GetPagedAsync(cardInstitutionId, new InstitutionCardMasterFilterRequestDTO(), default);
            TestContext.WriteLine($"Card rows read: {cards.Data.Count}");
        }
    }

    [TestCase(null)]
    [TestCase(true)]
    [TestCase(false)]
    public async Task Host_institution_list_translates_to_PostgreSql_with_verification_and_search_filters(bool? verified)
    {
        var commands = new HostQueryCommandInterceptor();
        await using var context = new SkolenzaDbContext(new DbContextOptionsBuilder<SkolenzaDbContext>()
            .UseNpgsql("Host=localhost;Database=translation_only;Username=unused;Password=unused")
            .AddInterceptors(new HostQueryConnectionInterceptor(), commands).Options);
        var repository = new InstitutionRepository(context, NullLogger<InstitutionRepository>.Instance);
        var page = await repository.GetHostManagedInstitutionsAsync(new GetAllInstitutionsRequestDTO
        {
            IsActive = true, IsVerified = verified, SearchKeyword = "office", PageNumber = 1, PageSize = 10
        });
        Assert.That(page.Data, Is.Empty);
        Assert.That(commands.Sql, Has.Count.EqualTo(2));
        Assert.That(commands.Sql[1], Does.Contain("LoginCredential").And.Contain("LIMIT"));
    }

    [TestCase("devices")]
    [TestCase("configurations")]
    [TestCase("catalogue")]
    [TestCase("institution-detail")]
    [TestCase("institution-update-load")]
    [TestCase("host-users")]
    [TestCase("host-roles")]
    public async Task Host_read_repositories_translate_to_PostgreSql(string resource)
    {
        var commands = new HostQueryCommandInterceptor();
        await using var context = new SkolenzaDbContext(new DbContextOptionsBuilder<SkolenzaDbContext>()
            .UseNpgsql("Host=localhost;Database=translation_only;Username=unused;Password=unused")
            .AddInterceptors(new HostQueryConnectionInterceptor(), commands).Options);
        switch (resource)
        {
            case "devices":
                await new InstitutionDeviceRepository(context).GetHostPagedAsync(new GetInstitutionDeviceListRequestDTO { Search = "office", IsActive = true }, default);
                break;
            case "configurations":
                await new InstitutionDeviceConfigurationRepository(context).GetHostPagedAsync(new GetInstitutionDeviceConfigurationListRequestDTO { Search = "office", IsEnrollmentEnabled = true }, default);
                break;
            case "catalogue":
                await new DeviceMasterRepository(context).GetPagedAsync(new GetDeviceMasterListRequestDTO { Search = "office", IsActive = true }, default);
                break;
            case "institution-detail":
                await new InstitutionRepository(context, NullLogger<InstitutionRepository>.Instance).GetHostManagedInstitutionDetailAsync(1);
                break;
            case "institution-update-load":
                await new InstitutionRepository(context, NullLogger<InstitutionRepository>.Instance).GetHostManagedInstitutionByIdAsync(1);
                break;
            case "host-users":
                await new HostUserRepository(context, null!, NullLogger<HostUserRepository>.Instance, null!, null!).GetPagedAsync(true, 1, 10, default);
                break;
            case "host-roles":
                await new HostRoleRepository(context, null!, NullLogger<HostRoleRepository>.Instance, null!, null!).GetAllAsync();
                break;
        }
        Assert.That(commands.Sql, Is.Not.Empty);
        Assert.That(commands.Sql.All(sql => sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    // Exercise real repository/provider translation, without opening a database or executing SQL.
    private sealed class HostQueryConnectionInterceptor : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private sealed class HostQueryCommandInterceptor : DbCommandInterceptor
    {
        public List<string> Sql { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Sql.Add(command.CommandText);
            var table = new DataTable();
            if (command.CommandText.Contains("count(*)", StringComparison.OrdinalIgnoreCase))
            {
                table.Columns.Add("count", typeof(int));
                table.Rows.Add(0);
            }
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(table.CreateDataReader()));
        }
    }

    private static readonly Type[] HostControllerTypes =
    [
        typeof(HostController),
        typeof(HostAccessController),
        typeof(HostRolePermissionController),
        typeof(DeviceMasterController),
        typeof(InstitutionDeviceController),
        typeof(InstitutionDeviceConfigurationController),
        typeof(InstitutionCardMasterController),
        typeof(DeviceCommandController),
        typeof(DeviceDdlOptionsController),
        typeof(DefaultEmailConfigController),
        typeof(InstitutionEmailConfigController),
        typeof(EmailTemplateController),
        typeof(InstitutionController)
    ];

    private static readonly HashSet<string> ExplicitlyAnonymousInstitutionActions =
    [
        nameof(InstitutionController.InstitutionCreation),
        nameof(InstitutionController.VerifyEmail)
    ];

    [Test]
    public void Every_host_administration_http_action_has_a_route_and_requires_an_authenticated_session()
    {
        var failures = new List<string>();

        foreach (var controllerType in HostControllerTypes)
        {
            var controllerIsAuthorized = controllerType.IsDefined(typeof(AuthorizeAttribute), inherit: true);
            var controllerRoutes = controllerType.GetCustomAttributes<RouteAttribute>(inherit: true).ToArray();
            if (controllerRoutes.Length == 0 || controllerRoutes.Any(route => string.IsNullOrWhiteSpace(route.Template)))
            {
                failures.Add($"{controllerType.Name} is missing its controller route.");
            }

            foreach (var action in controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                var verbs = action.GetCustomAttributes<HttpMethodAttribute>(inherit: true).ToArray();
                if (verbs.Length == 0)
                {
                    continue;
                }

                var isExplicitlyAnonymous =
                    controllerType == typeof(InstitutionController) &&
                    ExplicitlyAnonymousInstitutionActions.Contains(action.Name) &&
                    action.IsDefined(typeof(AllowAnonymousAttribute), inherit: true);
                var actionIsAuthorized = action.IsDefined(typeof(AuthorizeAttribute), inherit: true);

                if (!isExplicitlyAnonymous && !controllerIsAuthorized && !actionIsAuthorized)
                {
                    failures.Add($"{controllerType.Name}.{action.Name} must require [Authorize].");
                }

                if (verbs.Any(verb => string.IsNullOrWhiteSpace(verb.Template)))
                {
                    failures.Add($"{controllerType.Name}.{action.Name} is missing an HTTP route template.");
                }
            }
        }

        Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void Platform_administration_routes_coexist_with_legacy_host_routes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GetControllerRoutes(typeof(HostController)),
                Is.EquivalentTo(new[] { "api/Host", "api/Platform" }));
            Assert.That(GetControllerRoutes(typeof(HostAccessController)),
                Is.EquivalentTo(new[] { "api/HostAccess", "api/PlatformAccess" }));
            Assert.That(GetControllerRoutes(typeof(HostRolePermissionController)),
                Is.EquivalentTo(new[] { "api/HostRolePermission", "api/PlatformRolePermission" }));
        });
    }

    private static string[] GetControllerRoutes(Type controllerType)
    {
        return controllerType
            .GetCustomAttributes<RouteAttribute>(inherit: true)
            .Select(route => route.Template)
            .Where(template => !string.IsNullOrWhiteSpace(template))
            .Cast<string>()
            .ToArray();
    }

    [Test]
    public void Only_the_two_token_based_institution_onboarding_actions_remain_anonymous()
    {
        var anonymousActions = typeof(InstitutionController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttributes<HttpMethodAttribute>(inherit: true).Any())
            .Where(method => method.IsDefined(typeof(AllowAnonymousAttribute), inherit: true))
            .Select(method => method.Name)
            .ToArray();

        Assert.That(anonymousActions, Is.EquivalentTo(ExplicitlyAnonymousInstitutionActions));
    }

    [Test]
    public void Verified_legacy_onboarding_credential_is_not_eligible_for_another_resend_email()
    {
        var institution = CreateInstitution(isVerified: false, credentialIsOnboard: true);
        var handler = new ResendInstitutionVerificationCommandHandler(
            CreateUnitOfWork(institution),
            CreateHostCommonRequestService(),
            CreateProxy<ITokenService>((method, _) => throw new AssertionException($"Token generation must not run for {method.Name}.")),
            CreateProxy<IIdEncoderService>((method, args) => method.Name switch
            {
                nameof(IIdEncoderService.DecodeId_long) => 71L,
                _ => throw new AssertionException($"Unexpected identifier call: {method.Name}.")
            }),
            CreateProxy<IEmailService>((method, _) => throw new AssertionException($"Email delivery must not run for {method.Name}.")),
            new ConfigurationBuilder().Build());

        var exception = Assert.ThrowsAsync<ConflictException>(() => handler.Handle(
            new ResendInstitutionVerificationCommand("opaque-institution-id", new PermissionRequestDTO()),
            CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo(AppConstants.ErrorMessages.InstitutionAlreadyVerified));
    }

    [TestCase(true, true)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    public async Task Resend_delivery_result_is_truthful_and_preserves_pending_onboarding(
        bool tokenAvailable,
        bool deliveryAccepted)
    {
        var institution = CreateInstitution(isVerified: false, credentialIsOnboard: false);
        var emailCalls = 0;
        skolenza.application.DTOS.Token.GetTokenInfoDTO? issuedToken = null;
        var handler = new ResendInstitutionVerificationCommandHandler(
            CreateUnitOfWork(institution),
            CreateHostCommonRequestService(),
            CreateProxy<ITokenService>((method, args) =>
            {
                Assert.That(method.Name, Is.EqualTo(nameof(ITokenService.GenerateInstitutionToken)));
                issuedToken = (skolenza.application.DTOS.Token.GetTokenInfoDTO)args![0]!;
                return Task.FromResult(tokenAvailable ? "test-onboarding-token" : string.Empty);
            }),
            CreateProxy<IIdEncoderService>((method, _) => method.Name switch
            {
                nameof(IIdEncoderService.DecodeId_long) => 71L,
                nameof(IIdEncoderService.EncodeId_long) => "encoded-id",
                nameof(IIdEncoderService.EncodeId_int) => "encoded-purpose",
                _ => throw new AssertionException($"Unexpected identifier call: {method.Name}.")
            }),
            CreateProxy<IEmailService>((method, args) =>
            {
                emailCalls++;
                Assert.That(method.Name, Is.EqualTo(nameof(IEmailService.SendTemplatedEmailUsingHostConfigAsync)));
                Assert.That(args![0], Is.EqualTo(ConstantValues.WelcomeEmail));
                Assert.That(args[1], Is.EqualTo(institution.InstitutionEmail));
                Assert.That(args[2], Is.EqualTo(institution.Id));
                var placeholders = (Dictionary<string, string>)args[3]!;
                Assert.That(placeholders["InstitutionName"], Is.EqualTo(institution.CompanyName));
                Assert.That(placeholders["SupportEmail"], Is.EqualTo(institution.InstitutionEmail));
                Assert.That(placeholders["VerificationUrl"],
                    Is.EqualTo("https://ui.example.test/auth/set-password?token=test-onboarding-token"));
                Assert.That(placeholders["LinkExpiryMinutes"], Is.EqualTo("30"));
                return Task.FromResult(deliveryAccepted);
            }),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FrontEndWebURL:BaseUrl"] = "https://ui.example.test"
            }).Build());
        var request = new ResendInstitutionVerificationCommand(
            "opaque-institution-id", new PermissionRequestDTO { ModuleId = 33, OperationId = 2 });

        if (tokenAvailable && deliveryAccepted)
        {
            var response = await handler.Handle(request, CancellationToken.None);
            Assert.That(response.IsSucceeded, Is.True);
            Assert.That(response.Data, Is.True);
        }
        else
        {
            Assert.ThrowsAsync<ApiException>(() => handler.Handle(request, CancellationToken.None));
        }

        Assert.Multiple(() =>
        {
            Assert.That(emailCalls, Is.EqualTo(tokenAvailable ? 1 : 0));
            Assert.That(institution.IsVerified, Is.False);
            Assert.That(institution.StaffMember.Single().LoginCredential.Single().IsOnboard, Is.False);
            Assert.That(issuedToken, Is.Not.Null);
            Assert.That(issuedToken!.Email, Is.EqualTo(institution.InstitutionEmail));
            Assert.That(issuedToken.Expiry - issuedToken.IssuedAt,
                Is.EqualTo(TimeSpan.FromMinutes(30)).Within(TimeSpan.FromSeconds(1)));
        });
    }

    [TestCase(
        "5.7.1 Unauthorized IP address",
        AppConstants.ErrorCodes.SmtpIpNotAuthorized,
        AppConstants.ErrorMessages.SmtpIpNotAuthorized)]
    [TestCase(
        "Authentication credentials invalid",
        AppConstants.ErrorCodes.SmtpAuthenticationFailed,
        AppConstants.ErrorMessages.SmtpAuthenticationFailed)]
    [TestCase(
        "Sender address rejected",
        AppConstants.ErrorCodes.SmtpProviderRejected,
        AppConstants.ErrorMessages.SmtpProviderRejected)]
    public void Smtp_provider_rejections_are_mapped_to_safe_actionable_api_errors(
        string providerMessage,
        string expectedErrorCode,
        string expectedMessage)
    {
        var classifier = typeof(skolenza.infrastructure.MailService.EmailService).GetMethod(
            "CreateSmtpApiException",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(classifier, Is.Not.Null);

        var providerException = new SmtpCommandException(
            SmtpErrorCode.UnexpectedStatusCode,
            SmtpStatusCode.AuthenticationRequired,
            providerMessage);
        var exception = (ApiException)classifier!.Invoke(null, [providerException])!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.StatusCode, Is.EqualTo(StatusCodes.Status502BadGateway));
            Assert.That(exception.ErrorCode, Is.EqualTo(expectedErrorCode));
            Assert.That(exception.Message, Is.EqualTo(expectedMessage));
            Assert.That(exception.Message, Does.Not.Contain(providerMessage));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public async Task Resend_email_service_rejects_missing_template_or_configuration(int scenario)
    {
        var institutionReads = 0;
        var hostReads = 0;
        var service = new skolenza.infrastructure.MailService.EmailService(
            CreateProxy<IInstitutionEmailConfigRepository>((_, _) =>
            {
                institutionReads++;
                return Task.FromResult<InstitutionEmailConfig?>(null);
            }),
            CreateProxy<IDefaultEmailConfigRepository>((_, _) =>
            {
                hostReads++;
                return Task.FromResult<DefaultEmailConfig?>(null);
            }),
            CreateProxy<IEmailTemplateRepository>((_, _) => Task.FromResult<EmailTemplate?>(
                scenario == 0 ? null : new EmailTemplate
                {
                    IsActive = scenario == 2,
                    Subject = "Verification",
                    Body = "{{VerificationUrl}}"
                })),
            CreateProxy<IInstitutionEmailTemplateRepository>((_, _) => Task.FromResult<InstitutionEmailTemplate?>(null)),
            CreateProxy<IEmailQueueRepository>((method, _) => throw new AssertionException($"Unexpected queue call: {method.Name}.")),
            CreateProxy<IInstitutionKeyResolver>((_, _) => throw new InvalidOperationException("Unexpected key lookup.")),
            CreateProxy<IEncryptionService>((_, _) => throw new InvalidOperationException("Unexpected decryption.")),
            NullLogger<skolenza.infrastructure.MailService.EmailService>.Instance,
            new ConfigurationBuilder().Build());

        var exception = Assert.ThrowsAsync<ApiException>(() => service.SendTemplatedEmailAsync(
            ConstantValues.WelcomeEmail, "owner@example.test", 71,
            new Dictionary<string, string>()));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.StatusCode, Is.EqualTo(StatusCodes.Status502BadGateway));
            Assert.That(exception.ErrorCode, Is.EqualTo(AppConstants.ErrorCodes.SmtpConfigurationUnavailable));
            Assert.That(exception.Message, Is.EqualTo(AppConstants.ErrorMessages.SmtpConfigurationUnavailable));
            Assert.That(institutionReads, Is.EqualTo(1));
            Assert.That(hostReads, Is.EqualTo(1));
        });
    }

    [TestCase("Institution", true, "Institution subject", 1, 0)]
    [TestCase("Institution", false, "Host subject", 1, 1)]
    [TestCase("HostDefault", true, "Host subject", 0, 1)]
    public async Task Email_service_resolves_institution_template_only_for_institution_smtp_and_falls_back_by_code(
        string configurationSource,
        bool institutionTemplateExists,
        string expectedSubject,
        int expectedInstitutionReads,
        int expectedHostReads)
    {
        var institutionReads = 0;
        var hostReads = 0;
        var service = new skolenza.infrastructure.MailService.EmailService(
            CreateProxy<IInstitutionEmailConfigRepository>((method, _) => throw new AssertionException($"Unexpected call: {method.Name}.")),
            CreateProxy<IDefaultEmailConfigRepository>((method, _) => throw new AssertionException($"Unexpected call: {method.Name}.")),
            CreateProxy<IEmailTemplateRepository>((method, _) =>
            {
                Assert.That(method.Name, Is.EqualTo(nameof(IEmailTemplateRepository.GetTemplateByCodeAsync)));
                hostReads++;
                return Task.FromResult<EmailTemplate?>(new EmailTemplate
                {
                    IsActive = true,
                    Subject = "Host subject",
                    Body = "Host body"
                });
            }),
            CreateProxy<IInstitutionEmailTemplateRepository>((method, _) =>
            {
                Assert.That(method.Name, Is.EqualTo(nameof(IInstitutionEmailTemplateRepository.GetActiveByCodeAsync)));
                institutionReads++;
                return Task.FromResult<InstitutionEmailTemplate?>(institutionTemplateExists
                    ? new InstitutionEmailTemplate
                    {
                        InstitutionId = 71,
                        IsActive = true,
                        Subject = "Institution subject",
                        Body = "Institution body"
                    }
                    : null);
            }),
            CreateProxy<IEmailQueueRepository>((method, _) => throw new AssertionException($"Unexpected queue call: {method.Name}.")),
            CreateProxy<IInstitutionKeyResolver>((method, _) => throw new AssertionException($"Unexpected call: {method.Name}.")),
            CreateProxy<IEncryptionService>((method, _) => throw new AssertionException($"Unexpected call: {method.Name}.")),
            NullLogger<skolenza.infrastructure.MailService.EmailService>.Instance,
            new ConfigurationBuilder().Build());

        var resolver = typeof(skolenza.infrastructure.MailService.EmailService).GetMethod(
            "ResolveEmailTemplateAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(resolver, Is.Not.Null);

        var resolutionTask = (Task)resolver!.Invoke(
            service,
            [ConstantValues.WelcomeEmail, 71L, configurationSource])!;
        await resolutionTask;
        var resolution = resolutionTask.GetType().GetProperty("Result")!.GetValue(resolutionTask);

        Assert.Multiple(() =>
        {
            Assert.That(resolution, Is.Not.Null);
            Assert.That(resolution!.GetType().GetProperty("Subject")!.GetValue(resolution), Is.EqualTo(expectedSubject));
            Assert.That(institutionReads, Is.EqualTo(expectedInstitutionReads));
            Assert.That(hostReads, Is.EqualTo(expectedHostReads));
        });
    }

    [Test]
    public async Task Host_email_configuration_does_not_query_institution_smtp()
    {
        var institutionReads = 0;
        var service = new skolenza.infrastructure.MailService.EmailService(
            CreateProxy<IInstitutionEmailConfigRepository>((_, _) =>
            {
                institutionReads++;
                throw new AssertionException("Host delivery must not read Institution SMTP configuration.");
            }),
            CreateProxy<IDefaultEmailConfigRepository>((_, _) => Task.FromResult<DefaultEmailConfig?>(new DefaultEmailConfig
            {
                SmtpHost = "smtp-relay.brevo.com",
                SmtpPort = 587,
                SmtpUsername = "database-user@example.test",
                SmtpPasswordEncrypted = "database-secret",
                FromName = "Skolenza",
                FromEmail = "sender@example.test",
                IsActive = true,
                IsDefault = true
            })),
            CreateProxy<IEmailTemplateRepository>((method, _) => throw new AssertionException($"Unexpected call: {method.Name}.")),
            CreateProxy<IInstitutionEmailTemplateRepository>((method, _) => throw new AssertionException($"Unexpected call: {method.Name}.")),
            CreateProxy<IEmailQueueRepository>((method, _) => throw new AssertionException($"Unexpected call: {method.Name}.")),
            CreateProxy<IInstitutionKeyResolver>((method, _) => throw new AssertionException($"Unexpected call: {method.Name}.")),
            CreateProxy<IEncryptionService>((method, _) => throw new AssertionException($"Unexpected call: {method.Name}.")),
            NullLogger<skolenza.infrastructure.MailService.EmailService>.Instance,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmailConfig:SMTPUserName"] = "deployment-user@example.test",
                ["EmailConfig:Secret"] = "deployment-secret"
            }).Build());

        var resolver = typeof(skolenza.infrastructure.MailService.EmailService).GetMethod(
            "ResolveSmtpConfigurationAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(resolver, Is.Not.Null);

        var task = (Task)resolver!.Invoke(service, [71L, true])!;
        await task;
        var result = task.GetType().GetProperty("Result")!.GetValue(task);
        var resolvedType = result?.GetType();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(institutionReads, Is.Zero);
            Assert.That(
                resolvedType!.GetProperty("SmtpUsername")!.GetValue(result),
                Is.EqualTo("database-user@example.test"));
            Assert.That(
                resolvedType.GetProperty("SmtpSecret")!.GetValue(result),
                Is.EqualTo("database-secret"));
        });
    }

    [Test]
    [Category("InstitutionEmailTemplate")]
    public async Task Institution_template_sync_inserts_only_missing_active_defaults_and_preserves_inactive_existing_code()
    {
        var inserted = new List<InstitutionEmailTemplate>();
        var defaultRepository = CreateProxy<IEmailTemplateRepository>((method, _) => method.Name switch
        {
            nameof(IEmailTemplateRepository.GetActiveTemplatesAsync) => Task.FromResult(new List<EmailTemplate>
            {
                new() { Id = 1, TemplateCode = ConstantValues.WelcomeEmail, TemplateName = "Welcome", Subject = "Welcome", Body = "Body", IsActive = true },
                new() { Id = 2, TemplateCode = ConstantValues.ForgotPasswordEmail, TemplateName = "Forgot", Subject = "Reset", Body = "Body", IsActive = true }
            }),
            _ => throw new AssertionException($"Unexpected default-template repository call: {method.Name}.")
        });
        var institutionRepository = CreateProxy<IInstitutionEmailTemplateRepository>((method, args) => method.Name switch
        {
            nameof(IInstitutionEmailTemplateRepository.GetTemplateCodesAsync) =>
                Task.FromResult(new List<string> { ConstantValues.WelcomeEmail }),
            nameof(IInstitutionEmailTemplateRepository.AddRangeAsync) => CaptureTemplates(args, inserted),
            _ => throw new AssertionException($"Unexpected Institution-template repository call: {method.Name}.")
        });
        var unitOfWork = CreateProxy<IUnitOfWork>((method, _) => method.Name switch
        {
            "get_EmailTemplateRepository" => defaultRepository,
            "get_InstitutionEmailTemplateRepository" => institutionRepository,
            nameof(IUnitOfWork.SaveChangesAsync) => Task.FromResult(1),
            _ => throw new AssertionException($"Unexpected unit-of-work call: {method.Name}.")
        });
        var commonRequestService = CreateProxy<ICommonRequestService>((method, _) => method.Name switch
        {
            nameof(ICommonRequestService.ValidateInstitutionUserRequestAsync) => Task.FromResult(new CommonDecodedResult
            {
                Success = true,
                InstitutionId = 71,
                LoggedInStaffMemberId = 12
            }),
            _ => throw new AssertionException($"Unexpected common-request call: {method.Name}.")
        });
        var handler = new skolenza.application.Features.InstitutionEmailTemplateCmd.SyncInstitutionEmailTemplatesCommandHandler(
            unitOfWork,
            commonRequestService);

        var result = await handler.Handle(
            new skolenza.application.Features.InstitutionEmailTemplateCmd.SyncInstitutionEmailTemplatesCommand(
                new skolenza.application.DTOs.EmailTemplate.SyncInstitutionEmailTemplatesRequestDTO
                {
                    PermissionRequest = new PermissionRequestDTO { ModuleId = 119, OperationId = 1 }
                }),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSucceeded, Is.True);
            Assert.That(result.Data.ActiveDefaultTemplateCount, Is.EqualTo(2));
            Assert.That(result.Data.ExistingInstitutionTemplateCount, Is.EqualTo(1));
            Assert.That(result.Data.InsertedTemplateCount, Is.EqualTo(1));
            Assert.That(inserted.Single().TemplateCode, Is.EqualTo(ConstantValues.ForgotPasswordEmail));
            Assert.That(inserted.Single().InstitutionId, Is.EqualTo(71));
            Assert.That(inserted.Single().IsActive, Is.True);
        });
    }

    [Test]
    [Category("InstitutionEmailTemplate")]
    public async Task Institution_template_sync_is_idempotent_when_every_active_default_code_exists()
    {
        var defaultRepository = CreateProxy<IEmailTemplateRepository>((method, _) => method.Name switch
        {
            nameof(IEmailTemplateRepository.GetActiveTemplatesAsync) => Task.FromResult(new List<EmailTemplate>
            {
                new() { Id = 1, TemplateCode = ConstantValues.WelcomeEmail, TemplateName = "Welcome", Subject = "Welcome", Body = "Body", IsActive = true }
            }),
            _ => throw new AssertionException($"Unexpected default-template repository call: {method.Name}.")
        });
        var institutionRepository = CreateProxy<IInstitutionEmailTemplateRepository>((method, _) => method.Name switch
        {
            nameof(IInstitutionEmailTemplateRepository.GetTemplateCodesAsync) =>
                Task.FromResult(new List<string> { ConstantValues.WelcomeEmail }),
            nameof(IInstitutionEmailTemplateRepository.AddRangeAsync) => throw new AssertionException("No templates should be inserted."),
            _ => throw new AssertionException($"Unexpected Institution-template repository call: {method.Name}.")
        });
        var unitOfWork = CreateProxy<IUnitOfWork>((method, _) => method.Name switch
        {
            "get_EmailTemplateRepository" => defaultRepository,
            "get_InstitutionEmailTemplateRepository" => institutionRepository,
            nameof(IUnitOfWork.SaveChangesAsync) => throw new AssertionException("SaveChanges should not run when no templates are missing."),
            _ => throw new AssertionException($"Unexpected unit-of-work call: {method.Name}.")
        });
        var commonRequestService = CreateProxy<ICommonRequestService>((method, _) => method.Name switch
        {
            nameof(ICommonRequestService.ValidateInstitutionUserRequestAsync) => Task.FromResult(new CommonDecodedResult
            {
                Success = true,
                InstitutionId = 71,
                LoggedInStaffMemberId = 12
            }),
            _ => throw new AssertionException($"Unexpected common-request call: {method.Name}.")
        });
        var handler = new skolenza.application.Features.InstitutionEmailTemplateCmd.SyncInstitutionEmailTemplatesCommandHandler(
            unitOfWork,
            commonRequestService);

        var result = await handler.Handle(
            new skolenza.application.Features.InstitutionEmailTemplateCmd.SyncInstitutionEmailTemplatesCommand(
                new skolenza.application.DTOs.EmailTemplate.SyncInstitutionEmailTemplatesRequestDTO
                {
                    PermissionRequest = new PermissionRequestDTO { ModuleId = 119, OperationId = 1 }
                }),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Data.InsertedTemplateCount, Is.Zero);
            Assert.That(result.Data.InsertedTemplateCodes, Is.Empty);
        });
    }

    private static Task CaptureTemplates(object?[]? arguments, List<InstitutionEmailTemplate> destination)
    {
        destination.AddRange((IEnumerable<InstitutionEmailTemplate>)arguments![0]!);
        return Task.CompletedTask;
    }

    [Test]
    public void Onboarding_credential_lookup_uses_only_the_current_institution_email_and_active_staffMember()
    {
        var institution = CreateInstitution(isVerified: false, credentialIsOnboard: false);
        institution.StaffMember.Add(new StaffMember
        {
            Id = 99,
            InstitutionId = institution.Id + 1,
            IsSoftDeleted = false,
            LoginCredential =
            [
                new LoginCredential
                {
                    Id = 2,
                    InstitutionId = institution.Id + 1,
                    StaffMemberId = 99,
                    LoginId = institution.InstitutionEmail,
                    IsOnboard = true,
                    IsSoftDeleted = false
                }
            ]
        });

        var credential = InstitutionOnboardingVerification.FindCredential(institution);

        Assert.Multiple(() =>
        {
            Assert.That(credential, Is.Not.Null);
            Assert.That(credential!.InstitutionId, Is.EqualTo(institution.Id));
            Assert.That(InstitutionOnboardingVerification.IsVerified(institution, credential), Is.False);
        });
    }

    [Test]
    public async Task Host_can_read_configuration_with_its_host_module_grant_and_issue_an_initial_bootstrap_url()
    {
        var commonRequestService = CreateHostCommonRequestService();
        var behavior = new InstitutionDeviceConfigurationPermissionBehavior<
            GetAllInstitutionDeviceConfigurationsQuery,
            ApiResponse<List<InstitutionDeviceConfigurationResponseDTO>>>(
            CreateUnitOfWork(hostPermissionAllowed: true),
            CreateHostCommonRequestService(moduleCode: "INSTITUTION_DEVICE_CONFIG"),
            NullLogger<InstitutionDeviceConfigurationPermissionBehavior<
                GetAllInstitutionDeviceConfigurationsQuery,
                ApiResponse<List<InstitutionDeviceConfigurationResponseDTO>>>>.Instance);
        var listRequest = new GetAllInstitutionDeviceConfigurationsQuery(
            new GetInstitutionDeviceConfigurationListRequestDTO
            {
                ModuleId = 100,
                OperationId = 1
            });

        var listResponse = await behavior.Handle(
            listRequest,
            _ => Task.FromResult(ApiResponse<List<InstitutionDeviceConfigurationResponseDTO>>.Success([])),
            CancellationToken.None);
        Assert.That(listResponse.IsSucceeded, Is.True);

        var bootstrapBehavior = new InstitutionDeviceConfigurationPermissionBehavior<
            IssueInitialDeviceBootstrapCommand,
            ApiResponse<InitialDeviceBootstrapResponseDTO>>(
            CreateUnitOfWork(hostPermissionAllowed: true),
            commonRequestService,
            NullLogger<InstitutionDeviceConfigurationPermissionBehavior<
                IssueInitialDeviceBootstrapCommand,
                ApiResponse<InitialDeviceBootstrapResponseDTO>>>.Instance);
        var nextWasCalled = false;

        await bootstrapBehavior.Handle(
            new IssueInitialDeviceBootstrapCommand(
                new IssueInitialDeviceBootstrapRequestDTO
                {
                    ModuleId = 100,
                    OperationId = 1
                }),
            _ =>
            {
                nextWasCalled = true;
                return Task.FromResult(ApiResponse<InitialDeviceBootstrapResponseDTO>.Success(new InitialDeviceBootstrapResponseDTO()));
            },
            CancellationToken.None);

        Assert.That(nextWasCalled, Is.True);
    }

    [TestCase(false, "INSTITUTION_DEVICE_CONFIG")]
    [TestCase(true, "INSTITUTION_DEVICE_CONFIGURATION")]
    [TestCase(true, "HOST_INITIAL_DEVICE_CONFIGURATION")]
    public void Host_configuration_read_rejects_missing_grant_or_wrong_module(bool allowed, string moduleCode)
    {
        var behavior = new InstitutionDeviceConfigurationPermissionBehavior<GetAllInstitutionDeviceConfigurationsQuery, ApiResponse<List<InstitutionDeviceConfigurationResponseDTO>>>(
            CreateUnitOfWork(hostPermissionAllowed: allowed), CreateHostCommonRequestService(moduleCode: moduleCode),
            NullLogger<InstitutionDeviceConfigurationPermissionBehavior<GetAllInstitutionDeviceConfigurationsQuery, ApiResponse<List<InstitutionDeviceConfigurationResponseDTO>>>>.Instance);
        Assert.ThrowsAsync<ForbiddenAccessException>(() => behavior.Handle(
            new GetAllInstitutionDeviceConfigurationsQuery(new GetInstitutionDeviceConfigurationListRequestDTO { ModuleId = 36, OperationId = 4 }),
            _ => throw new AssertionException("Denied request reached handler."), default));
    }

    [Test]
    public async Task Institution_already_verified_conflict_is_serialised_as_http_409_in_the_standard_error_envelope()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new ErrorHandlerMiddleware(
            _ => throw new ConflictException(AppConstants.ErrorMessages.InstitutionAlreadyVerified),
            NullLogger<ErrorHandlerMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        Assert.Multiple(() =>
        {
            Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status409Conflict));
            Assert.That(context.Response.ContentType, Is.EqualTo("application/json"));
        });
    }

    [TestCase(AppConstants.SuperAdminHostRoleId)]
    [TestCase(2L)]
    public async Task My_menu_uses_the_current_host_role_for_all_host_users(long currentHostRoleId)
    {
        long? receivedRoleId = null;
        var moduleRepository = CreateProxy<IModuleRepository>((method, args) =>
        {
            if (method.Name != nameof(IModuleRepository.GetHostNavigationMenuAsync))
            {
                throw new NotSupportedException($"Unexpected module repository call: {method.Name}.");
            }

            receivedRoleId = Convert.ToInt64(args![0]);
            return Task.FromResult<IReadOnlyCollection<NavigationMenuItemResponseDTO>>(
            [
                new NavigationMenuItemResponseDTO
                {
                    Id = 44,
                    ModuleCode = "HOST_REGRESSION",
                    ModuleName = "Host Regression",
                    ModuleScope = (short)AppConstants.HostModuleScope,
                    IsLeafNode = true
                }
            ]);
        });
        var unitOfWork = CreateProxy<IUnitOfWork>((method, _) => method.Name switch
        {
            "get_ModuleRepository" => moduleRepository,
            _ => throw new NotSupportedException($"Unexpected unit-of-work call: {method.Name}.")
        });
        var handler = new GetMyNavigationMenuQueryHandler(
            CreateHostCommonRequestService(currentHostRoleId),
            unitOfWork,
            NullLogger<GetMyNavigationMenuQueryHandler>.Instance);

        var response = await handler.Handle(new GetMyNavigationMenuQuery(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(receivedRoleId, Is.EqualTo(currentHostRoleId));
            Assert.That(response.IsSucceeded, Is.True);
            Assert.That(response.Data!.Items.Single().ModuleCode, Is.EqualTo("HOST_REGRESSION"));
        });
    }

    [Test]
    public async Task Host_role_permission_editor_marks_only_persisted_host_grants_as_allowed()
    {
        var hostMapping = new ModuleOperationMapping
        {
            Id = 41,
            ModuleId = 11,
            OperationId = 7,
            IsActive = true,
            IsOperational = true,
            Module = new skolenza.domain.Entity.Module
            {
                Id = 11,
                ModuleScope = (short)AppConstants.HostModuleScope,
                ModuleName = "Host Regression",
                IsActive = true
            },
            Operation = new Operation
            {
                Id = 7,
                OperationName = "View",
                IsActive = true
            }
        };
        var institutionMapping = new ModuleOperationMapping
        {
            Id = 42,
            ModuleId = 12,
            OperationId = 7,
            IsActive = true,
            IsOperational = true,
            Module = new skolenza.domain.Entity.Module
            {
                Id = 12,
                ModuleScope = (short)AppConstants.InstitutionModuleScope,
                ModuleName = "Institution Regression",
                IsActive = true
            },
            Operation = new Operation
            {
                Id = 7,
                OperationName = "View",
                IsActive = true
            }
        };
        var hostRoleRepository = CreateProxy<IHostRoleRepository>((method, _) => method.Name switch
        {
            nameof(IHostRoleRepository.GetByIdAsync) => Task.FromResult<HostRole?>(new HostRole
            {
                Id = AppConstants.SuperAdminHostRoleId,
                Name = "Host-Super-Admin",
                IsActive = true,
                IsSoftDeleted = false
            }),
            _ => throw new NotSupportedException($"Unexpected Host role repository call: {method.Name}.")
        });
        var hostPermissionRepository = CreateProxy<IHostRolePermissionRepository>((method, _) => method.Name switch
        {
            nameof(IHostRolePermissionRepository.GetHostUserPermissionsAsync) =>
                Task.FromResult(new List<HostUserPermissionResponseDTO>
                {
                    new()
                    {
                        ModuleId = hostMapping.ModuleId,
                        OperationId = hostMapping.OperationId,
                        ModuleName = hostMapping.Module.ModuleName,
                        OperationName = hostMapping.Operation.OperationName
                    }
                }),
            _ => throw new NotSupportedException($"Unexpected Host permission repository call: {method.Name}.")
        });
        var moduleRepository = CreateProxy<IModuleRepository>((method, _) => method.Name switch
        {
            nameof(IModuleRepository.GetAllModuleOperationMappingsAsync) =>
                Task.FromResult(new List<ModuleOperationMapping> { hostMapping, institutionMapping }),
            _ => throw new NotSupportedException($"Unexpected module repository call: {method.Name}.")
        });
        var unitOfWork = CreateProxy<IUnitOfWork>((method, _) => method.Name switch
        {
            "get_HostRoleRepository" => hostRoleRepository,
            "get_HostRolePermissionRepository" => hostPermissionRepository,
            "get_ModuleRepository" => moduleRepository,
            _ => throw new NotSupportedException($"Unexpected unit-of-work call: {method.Name}.")
        });
        var commonRequestService = CreateProxy<ICommonRequestService>((method, _) => method.Name switch
        {
            nameof(ICommonRequestService.ValidateHostSuperAdminRequestAsync) => Task.FromResult(new HostUserRequestContext
            {
                HostUserId = 1,
                TokenHostRoleId = AppConstants.SuperAdminHostRoleId,
                CurrentHostRoleId = AppConstants.SuperAdminHostRoleId,
                UserType = AppConstants.HostUserType
            }),
            _ => throw new NotSupportedException($"Unexpected common-request call: {method.Name}.")
        });
        var handler = new GetHostRoleModulePermissionsQueryHandler(unitOfWork, commonRequestService);

        var response = await handler.Handle(
            new GetHostRoleModulePermissionsQuery(AppConstants.SuperAdminHostRoleId),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(response.IsSucceeded, Is.True);
            Assert.That(response.Data!.Modules, Has.Count.EqualTo(1));
            Assert.That(response.Data.Modules.Single().ModuleId, Is.EqualTo(hostMapping.ModuleId));
            Assert.That(response.Data.Modules.Single().Operations.Single().IsAllowed, Is.True);
        });
    }

    [Test]
    public void Host_super_admin_is_not_exempt_from_the_persisted_permission_check()
    {
        var storeProcedureRepository = CreateProxy<IStoreProcedureRepository>((method, _) => method.Name switch
        {
            nameof(IStoreProcedureRepository.CheckHostUserPermissionAsync) =>
                Task.FromResult(new HostUserPermissionCheckResponseDTO { ResultCode = 0 }),
            _ => throw new NotSupportedException($"Unexpected store procedure call: {method.Name}.")
        });

        var exception = Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            HostRuntimePermissionValidator.ValidateAsync(
                CreateHostCommonRequestService(AppConstants.SuperAdminHostRoleId),
                storeProcedureRepository,
                moduleId: 10,
                operationId: 20,
                CancellationToken.None));

        Assert.That(exception!.Message, Is.EqualTo(AppConstants.ErrorMessages.PermissionDenied));
    }

    [Test]
    public void Host_seed_contains_the_management_tree_and_canonical_host_auth_reset()
    {
        var seed = LoadProductionSeed();
        var hostSection = Slice(seed, "-- SECTION 8E", "-- SECTION 9");
        var cardUpgrade = File.ReadAllText(
            FindRepositoryFile("database-scripts/InstitutionCardMaster_StaffMemberDeviceCredential_Upgrade.sql"));
        var moduleCodes = new[]
        {
            "HOST_MANAGEMENT", "HOST_INSTITUTION_RFID_MANAGEMENT", "HOST_USERS", "HOST_ROLES", "HOST_ROLE_PERMISSIONS",
            "HOST_MODULES", "HOST_SUBMODULES", "HOST_OPERATIONS",
            "HOST_MODULE_OPERATIONS", "HOST_SUBSCRIPTIONS", "HOST_INSTITUTION_CARD_INVENTORY"
        };
        var moduleSeed = Slice(seed, "INSERT INTO module_seed", "UPDATE skolenza.\"Module\" module");

        Assert.Multiple(() =>
        {
            foreach (var moduleCode in moduleCodes)
            {
                Assert.That(seed, Does.Contain($"'{moduleCode}'"), $"Missing Host module code {moduleCode}.");
            }

            foreach (var childModuleCode in moduleCodes.Skip(2).Where(code => code != "HOST_INSTITUTION_CARD_INVENTORY"))
            {
                var tuple = Regex.Match(
                    moduleSeed,
                    $"\\(\\s*'{Regex.Escape(childModuleCode)}'.*?\\n\\),",
                    RegexOptions.Singleline).Value;
                Assert.That(tuple, Is.Not.Empty, $"Missing module_seed tuple for {childModuleCode}.");
                Assert.That(tuple, Does.Contain("'HOST_MANAGEMENT'"), $"{childModuleCode} must be under HOST_MANAGEMENT.");
                Assert.That(tuple, Does.Match(@"\n\s*2,\s*\n"), $"{childModuleCode} must be Host scope 2.");
            }

            Assert.That(moduleSeed, Does.Contain("'/app/host-users'"));
            Assert.That(moduleSeed, Does.Contain("'/app/host-roles'"));
            Assert.That(moduleSeed, Does.Contain("'/app/host-roles/permissions'"));
            Assert.That(moduleSeed, Does.Contain("'/app/modules'"));
            Assert.That(moduleSeed, Does.Contain("'/app/modules/sub-modules'"));
            Assert.That(moduleSeed, Does.Contain("'/app/modules/operations'"));
            Assert.That(moduleSeed, Does.Contain("'/app/modules/module-operations'"));
            Assert.That(moduleSeed, Does.Contain("'/app/subscriptions'"));
            var cardTuple = Regex.Match(
                moduleSeed,
                "\\(\\s*'HOST_INSTITUTION_CARD_INVENTORY'.*?\\n\\),",
                RegexOptions.Singleline).Value;
            Assert.That(cardTuple, Does.Contain("'HOST_INSTITUTION_RFID_MANAGEMENT'"));
            Assert.That(seed, Does.Contain("'HOST_INSTITUTION_RFID_MANAGEMENT'"));
            Assert.That(seed, Does.Contain("'bi bi-broadcast-pin'"));
            Assert.That(cardUpgrade, Does.Contain("'HOST_INSTITUTION_RFID_MANAGEMENT'"));
            Assert.That(cardUpgrade, Does.Contain("parent.\"Id\""));
            Assert.That(cardUpgrade, Does.Contain("m.\"ModuleCode\"='HOST_INSTITUTION_CARD_INVENTORY'"));
            Assert.That(cardUpgrade, Does.Not.Contain("m.\"ModuleCode\"='HOST_INSTITUTION_RFID_MANAGEMENT'"));
            Assert.That(seed, Does.Contain("'bi bi-shield-lock'"));
            Assert.That(hostSection, Does.Contain("DELETE FROM skolenza.\"RefreshToken\""));
            Assert.That(hostSection, Does.Contain("DELETE FROM skolenza.\"HostRoleModuleAndPermission\""));
            Assert.That(hostSection, Does.Contain("DELETE FROM skolenza.\"HostUser\""));
            Assert.That(hostSection, Does.Contain("DELETE FROM skolenza.\"HostRole\""));
            Assert.That(hostSection, Does.Contain("'Host-Super-Admin'"));
            Assert.That(Regex.Matches(hostSection, "'mca.deepesh@gmail.com'").Count, Is.EqualTo(2));
            Assert.That(hostSection, Does.Contain("'9111161399'"));
            Assert.That(hostSection, Does.Contain("ASP.NET Core Identity PasswordHasher V3"));
            Assert.That(hostSection, Does.Contain("initial password 12344321"));
            Assert.That(hostSection, Does.Contain("AQAAAAIAAYagAAAAEEDpT6tXHxx4OhhP394Aqp4vlsVunbyd3qQGOnszn4oghxYFlkERmuDjy0ATNqawgw=="));
            Assert.That(hostSection, Does.Contain("pg_get_serial_sequence('skolenza.\"HostRoleModuleAndPermission\"', 'Id')"));
            Assert.That(hostSection, Does.Contain("module.\"ModuleScope\" = 2"));
            Assert.That(hostSection, Does.Contain("mapping.\"IsOperational\" = TRUE"));
        });

        var refreshDelete = hostSection.IndexOf("DELETE FROM skolenza.\"RefreshToken\"", StringComparison.Ordinal);
        var permissionDelete = hostSection.IndexOf("DELETE FROM skolenza.\"HostRoleModuleAndPermission\"", StringComparison.Ordinal);
        var userDelete = hostSection.IndexOf("DELETE FROM skolenza.\"HostUser\"", StringComparison.Ordinal);
        var roleDelete = hostSection.IndexOf("DELETE FROM skolenza.\"HostRole\"", StringComparison.Ordinal);

        Assert.That(refreshDelete, Is.LessThan(permissionDelete));
        Assert.That(permissionDelete, Is.LessThan(userDelete));
        Assert.That(userDelete, Is.LessThan(roleDelete));
    }

    [Test]
    public void Host_seed_sql_has_balanced_plpgsql_blocks_and_no_runtime_bypass_marker()
    {
        var seed = LoadProductionSeed();
        var dollarQuoteCount = Regex.Matches(seed, "\\$\\$").Count;

        Assert.Multiple(() =>
        {
            Assert.That(seed.TrimStart(), Does.StartWith("--"));
            Assert.That(seed, Does.Contain("BEGIN;"));
            Assert.That(dollarQuoteCount % 2, Is.EqualTo(0), "PL/pgSQL dollar-quote delimiters must be paired.");
            Assert.That(seed, Does.Not.Contain("Host-Super-Admin bypass is used"));
            Assert.That(File.ReadAllText(FindRepositoryFile("skolenza.application/Common/Helpers/HostRuntimePermissionValidator.cs")),
                Does.Not.Contain("IsHostAdmin"));
            Assert.That(File.ReadAllText(FindRepositoryFile("skolenza.persistance/Repositories/ModuleRepository.cs")),
                Does.Not.Contain("includeAllScopedModules"));
        });
    }

    [Test]
    public void Host_seed_password_hash_accepts_the_documented_canonical_password()
    {
        var seed = LoadProductionSeed();
        var hash = Regex.Match(seed, @"AQAAAA[A-Za-z0-9+/]+=*").Value;
        var verification = new PasswordHasher<string>().VerifyHashedPassword(
            "mca.deepesh@gmail.com",
            hash,
            "12344321");

        Assert.Multiple(() =>
        {
            Assert.That(hash, Is.Not.Empty);
            Assert.That(verification, Is.EqualTo(PasswordVerificationResult.Success));
        });
    }

    [Test]
    public void Host_permission_sql_function_contract_matches_repository_and_persisted_grant_rules()
    {
        var functionSql = File.ReadAllText(
            FindRepositoryFile("database-scripts/AddCheckHostUserPermissionFunction.sql"));
        var repositorySql = File.ReadAllText(
            FindRepositoryFile("skolenza.persistance/Repositories/StoreProcedureRepository.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(functionSql, Does.Contain("p_hostuserid bigint"));
            Assert.That(functionSql, Does.Contain("p_tokenhostroleid bigint"));
            Assert.That(functionSql, Does.Contain("p_moduleid integer"));
            Assert.That(functionSql, Does.Contain("p_operationid integer"));
            Assert.That(functionSql, Does.Contain("'AUTH_CONTEXT_CHANGED'"));
            Assert.That(functionSql, Does.Contain("'PERMISSION_DENIED'"));
            Assert.That(functionSql, Does.Contain("v_currenthostroleid <> p_tokenhostroleid"));
            Assert.That(functionSql, Does.Contain("skolenza.\"HostRoleModuleAndPermission\""));
            Assert.That(functionSql, Does.Contain("permission.\"IsActive\" = TRUE"));
            Assert.That(functionSql, Does.Contain("permission.\"IsSoftDeleted\" = FALSE"));
            Assert.That(repositorySql, Does.Contain("skolenza.\"CheckHostUserPermission\"("));
            Assert.That(repositorySql, Does.Contain("@p_hostuserid"));
            Assert.That(repositorySql, Does.Contain("@p_tokenhostroleid"));
            Assert.That(repositorySql, Does.Contain("@p_moduleid"));
            Assert.That(repositorySql, Does.Contain("@p_operationid"));
        });
    }

    private static string LoadProductionSeed() =>
        File.ReadAllText(FindRepositoryFile(
            "database-scripts/production-seed/04-access-and-host/001-modules-operations-two-host-admins.sql"));

    private static string FindRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new AssertionException($"Could not locate repository file '{relativePath}'.");
    }

    private static string Slice(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        var end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
        if (start < 0 || end < 0 || end <= start)
        {
            throw new AssertionException($"Could not isolate SQL section '{startMarker}' to '{endMarker}'.");
        }

        return source[start..end];
    }

    private static Institution CreateInstitution(bool isVerified, bool credentialIsOnboard)
    {
        var institution = new Institution
        {
            Id = 71,
            CompanyName = "Regression Institution",
            CompanyEmailDomain = "example.test",
            InstitutionEmail = "owner@example.test",
            IsVerified = isVerified,
            IsActive = true,
            IsSoftDeleted = false
        };
        institution.StaffMember.Add(new StaffMember
        {
            Id = 12,
            InstitutionId = institution.Id,
            IsSoftDeleted = false,
            FirstName = "Owner",
            LoginCredential =
            [
                new LoginCredential
                {
                    Id = 1,
                    InstitutionId = institution.Id,
                    StaffMemberId = 12,
                    LoginId = institution.InstitutionEmail,
                    IsOnboard = credentialIsOnboard,
                    IsSoftDeleted = false
                }
            ]
        });
        return institution;
    }

    private static IUnitOfWork CreateUnitOfWork(
        Institution? institution = null,
        bool hostPermissionAllowed = false)
    {
        var institutionRepository = CreateProxy<IInstitutionRepository>((method, _) => method.Name switch
        {
            nameof(IInstitutionRepository.GetHostManagedInstitutionByIdAsync) => Task.FromResult(institution),
            _ => throw new NotSupportedException($"Unexpected Institution repository call: {method.Name}.")
        });
        return CreateProxy<IUnitOfWork>((method, _) => method.Name switch
        {
            "get_InstitutionRepository" => institutionRepository,
            "get_StoreProcedureRepository" => CreateProxy<IStoreProcedureRepository>((method, _) => method.Name switch
            {
                nameof(IStoreProcedureRepository.CheckHostUserPermissionAsync) =>
                    Task.FromResult(new HostUserPermissionCheckResponseDTO { ResultCode = hostPermissionAllowed ? 1 : 0 }),
                _ => throw new NotSupportedException($"Unexpected store procedure call: {method.Name}.")
            }),
            _ => throw new NotSupportedException($"Unexpected unit-of-work call: {method.Name}.")
        });
    }

    private static ICommonRequestService CreateHostCommonRequestService(
        long currentHostRoleId = AppConstants.SuperAdminHostRoleId,
        string moduleCode = "HOST_INITIAL_DEVICE_CONFIGURATION") =>
        CreateProxy<ICommonRequestService>((method, _) => method.Name switch
        {
            nameof(ICommonRequestService.ValidateAuthenticatedRequestAsync) => Task.FromResult(new AuthenticatedRequestContext
            {
                UserType = LoginUserType.Host,
                AuthenticatedUserId = 1
            }),
            nameof(ICommonRequestService.ValidateHostUserPermissionRequestAsync) => Task.FromResult(new HostUserRequestContext
            {
                HostUserId = 1,
                TokenHostRoleId = currentHostRoleId,
                CurrentHostRoleId = currentHostRoleId,
                UserType = AppConstants.HostUserType,
                InstitutionEncryptionKey = "host-regression-key"
            }),
            nameof(ICommonRequestService.GetModuleCodeAsync) => Task.FromResult<string?>(moduleCode),
            _ => throw new NotSupportedException($"Unexpected common-request call: {method.Name}.")
        });

    private class InterfaceProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler(targetMethod ?? throw new InvalidOperationException("A proxy method was not supplied."), args);
    }

    private static T CreateProxy<T>(Func<MethodInfo, object?[]?, object?> handler)
        where T : class
    {
        var proxy = DispatchProxy.Create<T, InterfaceProxy>();
        ((InterfaceProxy)(object)proxy).Handler = handler;
        return proxy;
    }
}

