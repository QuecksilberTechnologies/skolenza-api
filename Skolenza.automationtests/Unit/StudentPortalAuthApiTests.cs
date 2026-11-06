using System.Reflection;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using MediatR;
using NUnit.Framework;
using skolenza.application.Common.Helpers;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.api.Controllers.Student;
using skolenza.application.Common.Enums;
using skolenza.application.Constants;
using skolenza.application.DTOS.Student;
using skolenza.application.Features.StudentPortalAuthCmd;
using skolenza.application.Interfaces.IHashed;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Interfaces.ITokenService;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using skolenza.infrastructure.CommonRequest;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StudentPortalAuth")]
public sealed class StudentPortalAuthApiTests
{
    private const string TestSigningKey = "synthetic-principal-check-signing-key-32bytes";

    [Test]
    public async Task Portal_login_currently_resolves_tenant_from_api_host_not_browser_origin()
    {
        StudentPortalLoginCommand? captured = null;
        var mediator = Proxy<IMediator>((method, arguments) =>
        {
            Assert.That(method.Name, Is.EqualTo("Send"));
            captured = arguments![0] as StudentPortalLoginCommand;
            return Task.FromResult(ApiResponse<StudentPortalSessionResponseDTO>.Success(new StudentPortalSessionResponseDTO()));
        });
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("api.example.test");
        context.Request.Headers.Origin = "https://school.example.test";
        var controller = new StudentPortalAuthController(mediator)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };

        await controller.Login(new StudentPortalLoginRequestDTO
        {
            Email = "guardian@example.test",
            Password = "synthetic-test-password"
        }, CancellationToken.None);

        Assert.That(captured?.Hostname, Is.EqualTo("api.example.test"));
    }

    [TestCase("login", false)]
    [TestCase("login", true)]
    [TestCase("refresh", false)]
    [TestCase("refresh", true)]
    [TestCase("accept", false)]
    [TestCase("accept", true)]
    [TestCase("claim", false)]
    [TestCase("claim", true)]
    [TestCase("request-reset", false)]
    [TestCase("request-reset", true)]
    [TestCase("complete-reset", false)]
    [TestCase("complete-reset", true)]
    public async Task Portal_auth_routes_forward_optional_hostname_or_preserve_host_fallback(string action, bool explicitHostname)
    {
        string? hostname = null;
        var mediator = Proxy<IMediator>((method, arguments) =>
        {
            Assert.That(method.Name, Is.EqualTo("Send"));
            return arguments![0] switch
            {
                StudentPortalLoginCommand command => Login(command),
                StudentPortalRefreshCommand command => Refresh(command),
                AcceptStudentPortalInvitationCommand command => Accept(command),
                RequestStudentPortalPasswordResetCommand command => RequestReset(command),
                CompleteStudentPortalPasswordResetCommand command => CompleteReset(command),
                _ => throw new AssertionException("Unexpected portal command.")
            };
        });
        Task<ApiResponse<StudentPortalSessionResponseDTO>> Login(StudentPortalLoginCommand command)
        {
            hostname = command.Hostname;
            return Task.FromResult(ApiResponse<StudentPortalSessionResponseDTO>.Success(new StudentPortalSessionResponseDTO()));
        }
        Task<ApiResponse<StudentPortalSessionResponseDTO>> Refresh(StudentPortalRefreshCommand command)
        {
            hostname = command.Hostname;
            return Task.FromResult(ApiResponse<StudentPortalSessionResponseDTO>.Success(new StudentPortalSessionResponseDTO()));
        }
        Task<ApiResponse<StudentPortalInvitationAcceptedResponseDTO>> Accept(AcceptStudentPortalInvitationCommand command)
        {
            hostname = command.Hostname;
            return Task.FromResult(ApiResponse<StudentPortalInvitationAcceptedResponseDTO>.Success(new StudentPortalInvitationAcceptedResponseDTO()));
        }
        Task<ApiResponse<StudentPortalPasswordResetResponseDTO>> RequestReset(RequestStudentPortalPasswordResetCommand command)
        {
            hostname = command.Hostname;
            return Task.FromResult(ApiResponse<StudentPortalPasswordResetResponseDTO>.Success(new StudentPortalPasswordResetResponseDTO()));
        }
        Task<ApiResponse<StudentPortalPasswordResetResponseDTO>> CompleteReset(CompleteStudentPortalPasswordResetCommand command)
        {
            hostname = command.Hostname;
            return Task.FromResult(ApiResponse<StudentPortalPasswordResetResponseDTO>.Success(new StudentPortalPasswordResetResponseDTO()));
        }

        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("api.example.test");
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("InstitutionId", "12"),
            new Claim(StudentConstants.PortalAccountIdClaim, "7")
        }, "synthetic"));
        var controller = new StudentPortalAuthController(mediator)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
        var requestedHost = explicitHostname ? "school.example.test" : null;
        switch (action)
        {
            case "login":
                await controller.Login(new StudentPortalLoginRequestDTO { PortalHostname = requestedHost }, CancellationToken.None);
                break;
            case "refresh":
                await controller.Refresh(new StudentPortalRefreshRequestDTO { PortalHostname = requestedHost }, CancellationToken.None);
                break;
            case "accept":
                await controller.AcceptInvitation(new StudentPortalInvitationAcceptanceRequestDTO { PortalHostname = requestedHost }, CancellationToken.None);
                break;
            case "claim":
                await controller.ClaimInvitation(new StudentPortalInvitationAcceptanceRequestDTO { PortalHostname = requestedHost }, CancellationToken.None);
                break;
            case "request-reset":
                await controller.RequestPasswordReset(new StudentPortalPasswordResetRequestDTO { PortalHostname = requestedHost }, CancellationToken.None);
                break;
            case "complete-reset":
                await controller.CompletePasswordReset(new StudentPortalPasswordResetCompletionRequestDTO { PortalHostname = requestedHost }, CancellationToken.None);
                break;
            default:
                throw new AssertionException("Unexpected test action.");
        }

        Assert.That(hostname, Is.EqualTo(requestedHost ?? "api.example.test"));
    }

    [TestCase("school.example.test", "correct-test-password", true)]
    [TestCase("school.example.test", "wrong-test-password", false)]
    [TestCase("unverified.example.test", "correct-test-password", false)]
    [TestCase("other-school.example.test", "correct-test-password", false)]
    public async Task Portal_hostname_selects_verified_tenant_without_granting_login(
        string hostname, string password, bool expectedSuccess)
    {
        var account = new StudentPortalAccount
        {
            Id = 7,
            InstitutionId = 12,
            NormalizedEmail = "GUARDIAN@EXAMPLE.TEST",
            PasswordHash = "AQAAAA-synthetic-hash",
            IsActive = true,
            IsEmailVerified = true
        };
        var refreshWrites = 0;
        var repository = Proxy<IStudentPortalAuthRepository>((method, arguments) => method.Name switch
        {
            nameof(IStudentPortalAuthRepository.ResolveVerifiedInstitutionAsync) => Task.FromResult<long?>(
                arguments![0] as string == "school.example.test" ? 12L :
                arguments[0] as string == "other-school.example.test" ? 13L : null),
            nameof(IStudentPortalAuthRepository.GetAccountForLoginAsync) => Task.FromResult<StudentPortalAccount?>(
                (long)arguments![0]! == 12 ? account : null),
            nameof(IStudentPortalAuthRepository.HasActivePersonaAsync) => Task.FromResult(true),
            nameof(IStudentPortalAuthRepository.AddRefreshTokenAsync) => RecordRefresh(),
            nameof(IStudentPortalAuthRepository.ResetLoginFailuresAsync) => Task.CompletedTask,
            nameof(IStudentPortalAuthRepository.RegisterFailedLoginAsync) => Task.CompletedTask,
            _ => throw new AssertionException($"Unexpected repository call: {method.Name}")
        });
        Task RecordRefresh()
        {
            refreshWrites++;
            return Task.CompletedTask;
        }
        var passwords = Proxy<IPasswordService>((method, arguments) => method.Name == nameof(IPasswordService.VerifyPassword)
            ? arguments![1] as string == "correct-test-password"
            : throw new AssertionException($"Unexpected password call: {method.Name}"));
        var tokens = Proxy<ITokenService>((method, _) => method.Name switch
        {
            nameof(ITokenService.GenerateStudentPortalToken) => Task.FromResult("synthetic-access-token"),
            nameof(ITokenService.GetExpiryFromToken) => (DateTime?)DateTime.UtcNow.AddMinutes(5),
            _ => throw new AssertionException($"Unexpected token call: {method.Name}")
        });
        var handler = new StudentPortalLoginCommandHandler(repository, passwords, tokens);
        var command = new StudentPortalLoginCommand(hostname, new StudentPortalLoginRequestDTO
        {
            PortalHostname = hostname,
            Email = "guardian@example.test",
            Password = password
        });

        if (expectedSuccess)
        {
            var response = await handler.Handle(command, CancellationToken.None);
            Assert.That(response.IsSucceeded, Is.True);
            Assert.That(response.Data.InstitutionId, Is.EqualTo(12));
        }
        else
        {
            Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await handler.Handle(command, CancellationToken.None));
        }

        Assert.That(refreshWrites, Is.EqualTo(expectedSuccess ? 1 : 0));
    }

    [Test]
    public void Portal_refresh_rejects_a_verified_hostname_from_another_institution()
    {
        var token = new StudentPortalRefreshToken
        {
            InstitutionId = 12,
            StudentPortalAccountId = 7,
            ExpiresDateTime = DateTime.UtcNow.AddMinutes(5)
        };
        var repository = Proxy<IStudentPortalAuthRepository>((method, _) => method.Name switch
        {
            nameof(IStudentPortalAuthRepository.ResolveVerifiedInstitutionAsync) => Task.FromResult<long?>(13),
            nameof(IStudentPortalAuthRepository.GetRefreshTokenAsync) => Task.FromResult<StudentPortalRefreshToken?>(token),
            _ => throw new AssertionException($"Unexpected repository call: {method.Name}")
        });
        var tokenService = Proxy<ITokenService>((method, _) =>
            throw new AssertionException($"Token must not be issued: {method.Name}"));
        var handler = new StudentPortalRefreshCommandHandler(repository, tokenService);
        var command = new StudentPortalRefreshCommand("other-school.example.test",
            new StudentPortalRefreshRequestDTO { RefreshToken = "synthetic-refresh-token" });

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await handler.Handle(command, CancellationToken.None));
    }

    [TestCase(AppConstants.InstitutionUserType)]
    [TestCase(AppConstants.HostUserType)]
    [TestCase(StudentConstants.PortalUserType)]
    [TestCase(null)]
    public void Signed_principal_type_survives_shared_claim_mapping(string? userType)
    {
        var configuration = TestTokenConfiguration();
        var token = CreateSyntheticToken(userType);
        var mapped = RequestCommonHelper.ValidateAndExtractClaims(token, configuration);

        Assert.That(mapped, Is.Not.Null);
        Assert.That(mapped!.UserType, Is.EqualTo(userType));
    }

    [Test]
    public void Principal_type_is_not_trusted_from_expired_or_wrongly_signed_tokens()
    {
        var configuration = TestTokenConfiguration();
        var wrongSignature = CreateSyntheticToken(StudentConstants.PortalUserType,
            "synthetic-wrong-signing-key-at-least-32bytes");
        var expired = CreateSyntheticToken(StudentConstants.PortalUserType, TestSigningKey, -1);

        Assert.That(RequestCommonHelper.ValidateAndExtractClaims(wrongSignature, configuration), Is.Null);
        Assert.That(RequestCommonHelper.ValidateAndExtractClaims(expired, configuration), Is.Null);
    }

    [TestCase(StudentConstants.PortalUserType, false)]
    [TestCase(AppConstants.HostUserType, false)]
    [TestCase(AppConstants.InstitutionUserType, true)]
    [TestCase(null, true)]
    public async Task Institution_request_rejects_other_principal_types_before_staff_lookup(
        string? userType, bool permittedPrincipal)
    {
        var lookupCount = 0;
        var storedProcedures = Proxy<IStoreProcedureRepository>((method, _) =>
        {
            Assert.That(method.Name, Is.EqualTo(nameof(IStoreProcedureRepository.ValidateActiveUserLoginOnlyAsync)));
            lookupCount++;
            return Task.FromResult(4L);
        });
        var unit = Proxy<IUnitOfWork>((method, _) => method.Name == "get_StoreProcedureRepository"
            ? storedProcedures : throw new AssertionException($"Unexpected unit-of-work request: {method.Name}"));
        var encoder = Proxy<IIdEncoderService>((method, _) => method.Name == nameof(IIdEncoderService.DecodeId_long)
            ? 8L : throw new AssertionException($"Unexpected encoder request: {method.Name}"));
        var http = new DefaultHttpContext();
        http.Request.Headers.Authorization = $"Bearer {CreateSyntheticToken(userType)}";
        var service = new CommonRequestService(new HttpContextAccessor { HttpContext = http },
            TestTokenConfiguration(), unit, encoder, NullLogger<CommonRequestService>.Instance);

        var result = await service.ValidateInstitutionUserRequestAsync();

        Assert.That(result.Success, Is.EqualTo(permittedPrincipal));
        Assert.That(lookupCount, Is.EqualTo(permittedPrincipal ? 1 : 0));
    }

    private static IConfiguration TestTokenConfiguration() => new ConfigurationBuilder().AddInMemoryCollection(
        new Dictionary<string, string?> { ["JWTSettings:Secret"] = TestSigningKey }).Build();

    private static string CreateSyntheticToken(string? userType, string signingKey = TestSigningKey, int minutes = 5)
    {
        var claims = new List<Claim>
        {
            new("UserId", "synthetic-user"),
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
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256)));
    }

    [TestCase("never-locked", true)]
    [TestCase("expired", true)]
    [TestCase("active", false)]
    [TestCase("inactive", false)]
    public void Failed_login_updates_counter_only_for_active_unlocked_accounts(string state, bool shouldRecordFailure)
    {
        var account = new StudentPortalAccount
        {
            Id = 7,
            InstitutionId = 12,
            NormalizedEmail = "TEST@EXAMPLE.TEST",
            PasswordHash = "AQAAAA-synthetic-hash",
            IsActive = state != "inactive",
            IsEmailVerified = true,
            LockoutEndDateTime = state switch
            {
                "expired" => DateTime.UtcNow.AddMinutes(-1),
                "active" => DateTime.UtcNow.AddMinutes(1),
                _ => null
            }
        };
        var recordedFailures = 0;
        var repository = Proxy<IStudentPortalAuthRepository>((method, arguments) => method.Name switch
        {
            nameof(IStudentPortalAuthRepository.ResolveVerifiedInstitutionAsync) => Task.FromResult<long?>(12),
            nameof(IStudentPortalAuthRepository.GetAccountForLoginAsync) => Task.FromResult<StudentPortalAccount?>(account),
            nameof(IStudentPortalAuthRepository.RegisterFailedLoginAsync) => RecordFailure(arguments),
            _ => throw new AssertionException($"Unexpected repository call: {method.Name}")
        });
        Task RecordFailure(object?[]? arguments)
        {
            Assert.That(arguments![2], Is.EqualTo(StudentConstants.PortalLoginFailureLimit));
            recordedFailures++;
            return Task.CompletedTask;
        }
        var passwords = Proxy<IPasswordService>((method, _) => method.Name == nameof(IPasswordService.VerifyPassword)
            ? false : throw new AssertionException($"Unexpected password call: {method.Name}"));
        var tokens = Proxy<ITokenService>((method, _) => throw new AssertionException($"Unexpected token call: {method.Name}"));
        var handler = new StudentPortalLoginCommandHandler(repository, passwords, tokens);
        var command = new StudentPortalLoginCommand("portal.example.test", new StudentPortalLoginRequestDTO
        {
            Email = "test@example.test",
            Password = "wrong-test-password"
        });

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await handler.Handle(command, CancellationToken.None));
        Assert.That(recordedFailures, Is.EqualTo(shouldRecordFailure ? 1 : 0));
    }

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> invoke) where T : class
    {
        var proxy = DispatchProxy.Create<T, BulkImportPermissionTests.TestProxy>();
        ((BulkImportPermissionTests.TestProxy)(object)proxy).InvokeMethod = invoke;
        return proxy;
    }
    [Test]
    public void Portal_login_and_refresh_are_anonymous_but_logout_requires_portal_policy()
    {
        var controller = typeof(StudentPortalAuthController);
        var route = controller.GetCustomAttribute<Microsoft.AspNetCore.Mvc.RouteAttribute>();
        var login = controller.GetMethod(nameof(StudentPortalAuthController.Login));
        var refresh = controller.GetMethod(nameof(StudentPortalAuthController.Refresh));
        var logout = controller.GetMethod(nameof(StudentPortalAuthController.Logout));
        var profile = controller.GetMethod(nameof(StudentPortalAuthController.GetProfile));
        var acceptInvitation = controller.GetMethod(nameof(StudentPortalAuthController.AcceptInvitation));
        var claimInvitation = controller.GetMethod(nameof(StudentPortalAuthController.ClaimInvitation));
        var requestPasswordReset = controller.GetMethod(nameof(StudentPortalAuthController.RequestPasswordReset));
        var completePasswordReset = controller.GetMethod(nameof(StudentPortalAuthController.CompletePasswordReset));

        Assert.Multiple(() =>
        {
            Assert.That(route?.Template, Is.EqualTo("api/Student/portal-auth"));
            Assert.That(login?.GetCustomAttribute<AllowAnonymousAttribute>(), Is.Not.Null);
            Assert.That(refresh?.GetCustomAttribute<AllowAnonymousAttribute>(), Is.Not.Null);
            Assert.That(logout?.GetCustomAttribute<AuthorizeAttribute>()?.Policy,
                Is.EqualTo(StudentConstants.PortalAuthenticationPolicy));
            Assert.That(profile?.GetCustomAttribute<AuthorizeAttribute>()?.Policy,
                Is.EqualTo(StudentConstants.PortalAuthenticationPolicy));
            Assert.That(acceptInvitation?.GetCustomAttribute<AllowAnonymousAttribute>(), Is.Not.Null);
            Assert.That(claimInvitation?.GetCustomAttribute<AuthorizeAttribute>()?.Policy,
                Is.EqualTo(StudentConstants.PortalAuthenticationPolicy));
            Assert.That(requestPasswordReset?.GetCustomAttribute<AllowAnonymousAttribute>(), Is.Not.Null);
            Assert.That(completePasswordReset?.GetCustomAttribute<AllowAnonymousAttribute>(), Is.Not.Null);
            Assert.That(controller.GetMethods().SelectMany(method => method
                    .GetCustomAttributes(typeof(HttpMethodAttribute), true)
                    .Cast<HttpMethodAttribute>()
                    .Select(attribute => attribute.Template)),
                Is.EquivalentTo(["login", "refresh", "logout", "me", "invitations/accept", "invitations/claim",
                    "password-reset/request", "password-reset/complete"]));
        });
    }

    [Test]
    public void Login_rejects_plaintext_credential_hashes_before_password_service_fallback()
    {
        var check = typeof(StudentPortalLoginCommandHandler)
            .GetMethod("IsIdentityPasswordHash", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.Multiple(() =>
        {
            Assert.That(check?.Invoke(null, ["plain-text-password"]), Is.EqualTo(false));
            Assert.That(check?.Invoke(null, ["AQAAAAIAAYagAAAAEidentity-hash"]), Is.EqualTo(true));
            Assert.That(check?.Invoke(null, [null]), Is.EqualTo(false));
        });
    }

    [Test]
    public void Portal_accounts_and_tokens_have_separate_owner_and_hash_indexes()
    {
        using var context = new SkolenzaDbContext(
            new DbContextOptionsBuilder<SkolenzaDbContext>()
                .UseNpgsql("Host=localhost;Database=skolenza_portal_model_validation;Username=postgres;Password=not-used")
                .Options);

        var account = context.Model.FindEntityType(typeof(StudentPortalAccount));
        var guardianLink = context.Model.FindEntityType(typeof(StudentPortalAccountGuardian));
        var studentLink = context.Model.FindEntityType(typeof(StudentPortalAccountStudent));
        var refresh = context.Model.FindEntityType(typeof(StudentPortalRefreshToken));
        var oneTime = context.Model.FindEntityType(typeof(StudentPortalOneTimeToken));

        Assert.Multiple(() =>
        {
            Assert.That(account?.GetTableName(), Is.EqualTo("StudentPortalAccount"));
            Assert.That(account?.GetIndexes().Any(index => index.IsUnique &&
                index.Properties.Select(property => property.Name).SequenceEqual(
                    [nameof(StudentPortalAccount.InstitutionId), nameof(StudentPortalAccount.NormalizedEmail)])), Is.True);
            Assert.That(guardianLink?.GetTableName(), Is.EqualTo("StudentPortalAccountGuardian"));
            Assert.That(studentLink?.GetTableName(), Is.EqualTo("StudentPortalAccountStudent"));
            Assert.That(refresh?.GetTableName(), Is.EqualTo("StudentPortalRefreshToken"));
            Assert.That(refresh?.GetIndexes().Any(index => index.IsUnique &&
                index.Properties.Select(property => property.Name).SequenceEqual([nameof(StudentPortalRefreshToken.TokenHash)])), Is.True);
            Assert.That(oneTime?.GetTableName(), Is.EqualTo("StudentPortalOneTimeToken"));
            Assert.That(oneTime?.GetIndexes().Any(index => index.IsUnique &&
                index.Properties.Select(property => property.Name).SequenceEqual([nameof(StudentPortalOneTimeToken.TokenHash)])), Is.True);
        });
    }

    [Test]
    public void Portal_lockout_and_token_lifetimes_are_centralized_and_enums_are_listable()
    {
        Assert.Multiple(() =>
        {
            Assert.That(StudentConstants.PortalLoginFailureLimit, Is.EqualTo(5));
            Assert.That(StudentConstants.PortalLoginLockoutMinutes, Is.EqualTo(15));
            Assert.That(StudentConstants.PortalRefreshTokenExpiryDays, Is.EqualTo(14));
            Assert.That(Enum.GetValues<StudentPortalPersonaType>(), Has.Length.EqualTo(2));
            Assert.That(Enum.GetValues<StudentPortalOneTimeTokenPurpose>(), Has.Length.EqualTo(2));
            Assert.That(ConstantValues.IsSupportedEmailTemplateCode(StudentConstants.PortalInvitationTemplateCode), Is.True);
            Assert.That(ConstantValues.IsSupportedEmailTemplateCode(StudentConstants.PortalPasswordResetTemplateCode), Is.True);
        });
    }

    [Test]
    public void Portal_one_time_tokens_are_random_opaque_and_stored_as_sha256_hashes()
    {
        var first = StudentPortalTokenHelper.CreateOpaqueToken();
        var second = StudentPortalTokenHelper.CreateOpaqueToken();
        var hash = StudentPortalTokenHelper.HashToken(first);

        Assert.Multiple(() =>
        {
            Assert.That(first, Has.Length.EqualTo(43));
            Assert.That(first, Does.Match("^[A-Za-z0-9_-]+$"));
            Assert.That(second, Is.Not.EqualTo(first));
            Assert.That(hash, Has.Length.EqualTo(64));
            Assert.That(hash, Does.Match("^[a-f0-9]{64}$"));
            Assert.That(hash, Is.Not.EqualTo(first));
            Assert.That(StudentPortalTokenHelper.HashToken(first), Is.EqualTo(hash));
        });
    }
}
