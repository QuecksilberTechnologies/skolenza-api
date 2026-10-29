using System.Reflection;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
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
using Microsoft.Extensions.Options;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StudentPortalDomain")]
public sealed class StudentPortalDomainApiTests
{
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
