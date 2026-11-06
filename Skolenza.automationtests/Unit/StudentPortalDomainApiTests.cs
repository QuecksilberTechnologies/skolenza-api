using System.Reflection;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using skolenza.api.Controllers.Student;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOS.Student;
using skolenza.application.Features.StudentCmd;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using skolenza.infrastructure.Dns;
using skolenza.api.Middlewares;
using skolenza.application.Interfaces.IRepositories;
using Microsoft.Extensions.Options;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StudentPortalDomain")]
public sealed class StudentPortalDomainApiTests
{
    [TestCase("https://school.example.test", "/api/Student/portal-auth/login", true, true)]
    [TestCase("https://school.example.test", "/api/Student/portal/students/1/documents", true, true)]
    [TestCase("https://unknown.example.test", "/api/Student/portal-auth/login", false, true)]
    [TestCase("http://school.example.test", "/api/Student/portal-auth/login", false, false)]
    [TestCase("https://school.example.test:444", "/api/Student/portal-auth/login", false, false)]
    [TestCase("https://school.example.test.", "/api/Student/portal-auth/login", false, false)]
    [TestCase("https://school.example.test.evil.test", "/api/Student/portal-auth/login", false, true)]
    [TestCase("null", "/api/Student/portal-auth/login", false, false)]
    [TestCase("https://school.example.test", "/api/Student/portal-domains", false, false)]
    [TestCase("https://school.example.test", "/api/Staff", false, false)]
    [TestCase("https://skolenza.vercel.app", "/api/Student/portal-auth/login", true, false)]
    public async Task Verified_school_origins_are_allowed_only_for_portal_routes(
        string origin, string path, bool expectedAllowed, bool expectedLookup)
    {
        var lookups = 0;
        var repository = DispatchProxy.Create<IStudentPortalAuthRepository, BulkImportPermissionTests.TestProxy>();
        ((BulkImportPermissionTests.TestProxy)(object)repository).InvokeMethod = (method, arguments) =>
        {
            Assert.That(method.Name, Is.EqualTo(nameof(IStudentPortalAuthRepository.ResolveVerifiedInstitutionAsync)));
            lookups++;
            return Task.FromResult<long?>(string.Equals(arguments![0] as string, "school.example.test", StringComparison.Ordinal) ? 12L : null);
        };
        using var services = new ServiceCollection().AddSingleton(repository).BuildServiceProvider();
        var options = new CorsOptions();
        options.AddPolicy(VerifiedPortalCorsPolicyProvider.FrontendPolicyName,
            policy => policy.WithOrigins("https://skolenza.vercel.app").AllowAnyHeader().AllowAnyMethod().AllowCredentials());
        var provider = new VerifiedPortalCorsPolicyProvider(Options.Create(options),
            NullLogger<VerifiedPortalCorsPolicyProvider>.Instance);
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Path = path;
        context.Request.Headers.Origin = origin;

        var policy = await provider.GetPolicyAsync(context, VerifiedPortalCorsPolicyProvider.FrontendPolicyName);

        Assert.That(policy, Is.Not.Null);
        Assert.That(policy!.Origins.Contains(origin, StringComparer.OrdinalIgnoreCase), Is.EqualTo(expectedAllowed));
        Assert.That(lookups > 0, Is.EqualTo(expectedLookup));
        if (expectedAllowed)
        {
            Assert.That(policy.SupportsCredentials, Is.True);
        }
    }

    [Test]
    public async Task Portal_cors_fails_closed_if_domain_lookup_fails()
    {
        var repository = DispatchProxy.Create<IStudentPortalAuthRepository, BulkImportPermissionTests.TestProxy>();
        ((BulkImportPermissionTests.TestProxy)(object)repository).InvokeMethod = (_, _) =>
            throw new InvalidOperationException("Synthetic lookup failure.");
        using var services = new ServiceCollection().AddSingleton(repository).BuildServiceProvider();
        var options = new CorsOptions();
        options.AddPolicy(VerifiedPortalCorsPolicyProvider.FrontendPolicyName,
            policy => policy.WithOrigins("https://skolenza.vercel.app"));
        var provider = new VerifiedPortalCorsPolicyProvider(Options.Create(options),
            NullLogger<VerifiedPortalCorsPolicyProvider>.Instance);
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Path = "/api/Student/portal-auth/login";
        context.Request.Headers.Origin = "https://school.example.test";

        var policy = await provider.GetPolicyAsync(context, VerifiedPortalCorsPolicyProvider.FrontendPolicyName);

        Assert.That(policy!.Origins, Does.Not.Contain("https://school.example.test"));
    }

    [Test]
    public async Task Portal_cors_preflight_allows_verified_school_without_widening_staff_routes()
    {
        var repository = DispatchProxy.Create<IStudentPortalAuthRepository, BulkImportPermissionTests.TestProxy>();
        ((BulkImportPermissionTests.TestProxy)(object)repository).InvokeMethod = (method, arguments) =>
        {
            Assert.That(method.Name, Is.EqualTo(nameof(IStudentPortalAuthRepository.ResolveVerifiedInstitutionAsync)));
            return Task.FromResult<long?>(arguments![0] as string == "school.example.test" ? 12L : null);
        };
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(repository);
        builder.Services.AddCors(options => options.AddPolicy(VerifiedPortalCorsPolicyProvider.FrontendPolicyName,
            policy => policy.WithOrigins("https://skolenza.vercel.app").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
        builder.Services.AddSingleton<ICorsPolicyProvider, VerifiedPortalCorsPolicyProvider>();
        await using var app = builder.Build();
        app.UseRouting();
        app.UseCors(VerifiedPortalCorsPolicyProvider.FrontendPolicyName);
        app.MapPost("/api/Student/portal-auth/login", () => Results.Ok());
        app.MapPost("/api/Staff", () => Results.Ok());
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };

        async Task<string?> PreflightAsync(string origin, string path)
        {
            using var request = new HttpRequestMessage(HttpMethod.Options, path);
            request.Headers.TryAddWithoutValidation("Origin", origin);
            request.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "POST");
            using var response = await client.SendAsync(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
            return response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values)
                ? values.Single()
                : null;
        }

        Assert.That(await PreflightAsync("https://school.example.test", "/api/Student/portal-auth/login"),
            Is.EqualTo("https://school.example.test"));
        Assert.That(await PreflightAsync("https://unknown.example.test", "/api/Student/portal-auth/login"), Is.Null);
        Assert.That(await PreflightAsync("https://school.example.test", "/api/Staff"), Is.Null);
        Assert.That(await PreflightAsync("https://skolenza.vercel.app", "/api/Staff"),
            Is.EqualTo("https://skolenza.vercel.app"));
    }
    [Test]
    public void Controller_is_authenticated_and_exposes_domain_lifecycle_routes()
    {
        var controller = typeof(InstitutionPortalDomainMasterController);
        var routes = controller.GetMethods().SelectMany(method => method
            .GetCustomAttributes(typeof(HttpMethodAttribute), true)
            .Cast<HttpMethodAttribute>()
            .Select(attribute => attribute.Template)).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(controller.GetCustomAttributes(typeof(AuthorizeAttribute), true), Is.Not.Empty);
            Assert.That(routes, Does.Contain("{id:long}/challenge"));
            Assert.That(routes, Does.Contain("{id:long}/verify"));
            Assert.That(routes, Does.Contain("{id:long}"));
        });
    }

    [Test]
    public void Domain_normalization_accepts_hostname_and_rejects_url_ip_and_wildcard()
    {
        Assert.That(InstitutionPortalDomainChallenge.NormalizeDomain("  Portal.Example.COM. "),
            Is.EqualTo("portal.example.com"));

        Assert.Multiple(() =>
        {
            Assert.Throws<skolenza.application.Exceptions.ValidationErrorException>(() =>
                InstitutionPortalDomainChallenge.NormalizeDomain("https://portal.example.com"));
            Assert.Throws<skolenza.application.Exceptions.ValidationErrorException>(() =>
                InstitutionPortalDomainChallenge.NormalizeDomain("192.0.2.10"));
            Assert.Throws<skolenza.application.Exceptions.ValidationErrorException>(() =>
                InstitutionPortalDomainChallenge.NormalizeDomain("*.example.com"));
        });
    }

    [Test]
    public void Challenge_is_random_hashed_and_verification_accepts_split_dns_txt_segments()
    {
        var first = InstitutionPortalDomainChallenge.CreateChallengeValue();
        var second = InstitutionPortalDomainChallenge.CreateChallengeValue();
        var hash = InstitutionPortalDomainChallenge.HashChallenge(first);
        var splitIndex = first.Length / 2;
        var dnsAnswer = $"\"{first[..splitIndex]}\" \"{first[splitIndex..]}\"";

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.EqualTo(second));
            Assert.That(hash, Is.Not.EqualTo(first));
            Assert.That(InstitutionPortalDomainChallenge.Matches(hash, [dnsAnswer]), Is.True);
            Assert.That(InstitutionPortalDomainChallenge.Matches(hash,
                [$"\"{second}\""]), Is.False);
            Assert.That(InstitutionPortalDomainChallenge.CreateRecordName("portal.example.com"),
                Is.EqualTo($"{StudentConstants.PortalDomainTxtRecordPrefix}.portal.example.com"));
        });
    }

    [Test]
    public void Domain_master_uses_institution_and_hostname_unique_indexes()
    {
        using var context = new SkolenzaDbContext(
            new DbContextOptionsBuilder<SkolenzaDbContext>()
                .UseNpgsql("Host=localhost;Database=skolenza_model_validation;Username=postgres;Password=not-used")
                .Options);

        var entity = context.Model.FindEntityType(typeof(InstitutionPortalDomainMaster));
        var indexes = entity?.GetIndexes().ToArray() ?? [];

        Assert.Multiple(() =>
        {
            Assert.That(entity?.GetTableName(), Is.EqualTo("InstitutionPortalDomainMaster"));
            Assert.That(entity?.GetSchema(), Is.EqualTo("skolenza"));
            Assert.That(indexes.Any(index => index.IsUnique &&
                index.Properties.Select(property => property.Name).SequenceEqual([nameof(InstitutionPortalDomainMaster.InstitutionId)])), Is.True);
            Assert.That(indexes.Any(index => index.IsUnique &&
                index.Properties.Select(property => property.Name).SequenceEqual([nameof(InstitutionPortalDomainMaster.DomainName)])), Is.True);
        });
    }

    [Test]
    public void Domain_api_uses_dynamic_portal_domain_module_permissions()
    {
        var resolver = typeof(StudentPermissionBehavior<object, object>)
            .GetMethod("ResolvePermission", BindingFlags.NonPublic | BindingFlags.Static);
        var requests = new object[]
        {
            new GetInstitutionPortalDomainsQuery(new InstitutionPortalDomainFilterDTO()),
            new CreateInstitutionPortalDomainCommand(new InstitutionPortalDomainCreateDTO()),
            new RenewInstitutionPortalDomainChallengeCommand(1, new InstitutionPortalDomainVerifyDTO()),
            new VerifyInstitutionPortalDomainCommand(1, new InstitutionPortalDomainVerifyDTO()),
            new DeleteInstitutionPortalDomainCommand(1, new InstitutionPortalDomainDeleteDTO())
        };
        var expectedOperations = new[] { "View", "Add", "Update", "Update", "Delete" };

        for (var index = 0; index < requests.Length; index++)
        {
            var permission = resolver?.Invoke(null, [requests[index]]);
            Assert.Multiple(() =>
            {
                Assert.That(permission?.GetType().GetField("Item1")?.GetValue(permission),
                    Is.EqualTo(StudentConstants.PortalDomainsModuleCode));
                Assert.That(permission?.GetType().GetField("Item2")?.GetValue(permission),
                    Is.EqualTo(expectedOperations[index]));
            });
        }

        Assert.That(Enum.GetValues<StudentPortalDomainVerificationStatus>(), Has.Length.EqualTo(2));
    }

    [Test]
    public async Task Dns_resolver_reads_txt_answers_and_surfaces_provider_unavailability()
    {
        HttpRequestMessage? capturedRequest = null;
        using var httpClient = new HttpClient(new RecordingHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"Answer\":[{\"type\":16,\"data\":\"\\\"first\\\" \\\"second\\\"\"},{\"type\":1,\"data\":\"192.0.2.1\"}]}",
                    Encoding.UTF8,
                    "application/dns-json")
            };
        }));
        var resolver = new DnsOverHttpsTxtResolver(
            httpClient,
            Options.Create(new PortalDomainVerificationOptions
            {
                DnsOverHttpsEndpoint = "https://dns.example.test/resolve"
            }));

        var answers = await resolver.ResolveAsync("_skolenza-verification.example.test", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(answers, Is.EqualTo(["\"first\" \"second\""]));
            Assert.That(capturedRequest?.RequestUri?.Query, Does.Contain("type=TXT"));
            Assert.That(capturedRequest?.RequestUri?.Query, Does.Contain("name=_skolenza-verification.example.test"));
        });

        using var unavailableClient = new HttpClient(new RecordingHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        var unavailableResolver = new DnsOverHttpsTxtResolver(
            unavailableClient,
            Options.Create(new PortalDomainVerificationOptions
            {
                DnsOverHttpsEndpoint = "https://dns.example.test/resolve"
            }));

        var exception = Assert.ThrowsAsync<skolenza.application.Exceptions.ApiException>(async () =>
            await unavailableResolver.ResolveAsync("_skolenza-verification.example.test", CancellationToken.None));

        Assert.That(exception?.StatusCode, Is.EqualTo(503));
    }

    private sealed class RecordingHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => Task.FromResult(responseFactory(request));
    }
}
