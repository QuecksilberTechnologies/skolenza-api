using System.Reflection;
using skolenza.api.Controllers.EmailTemplate;
using skolenza.application.Constants;
using skolenza.application.DTOs.EmailTemplate;
using skolenza.application.Features.InstitutionEmailTemplateCmd;
using skolenza.domain.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("InstitutionEmailTemplate")]
public sealed class InstitutionEmailTemplateContractTests
{
    [Test]
    public void Entity_copies_email_template_fields_and_adds_institution_id()
    {
        var hostProperties = typeof(EmailTemplate).GetProperties().Select(x => x.Name)
            .Where(x => x != nameof(EmailTemplate.EmailQueue)).ToHashSet();
        var institutionProperties = typeof(InstitutionEmailTemplate).GetProperties().Select(x => x.Name).ToHashSet();

        Assert.Multiple(() =>
        {
            Assert.That(institutionProperties, Does.Contain(nameof(InstitutionEmailTemplate.InstitutionId)));
            Assert.That(hostProperties.All(institutionProperties.Contains), Is.True);
        });
    }

    [Test]
    public void Controller_exposes_authorized_matching_crud_routes()
    {
        var type = typeof(InstitutionEmailTemplateController);
        var routes = type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Select(x => x.GetCustomAttribute<HttpMethodAttribute>())
            .Where(x => x is not null)
            .Select(x => $"{x!.HttpMethods.Single()}:{x.Template}")
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(type.GetCustomAttribute<AuthorizeAttribute>(), Is.Not.Null);
            Assert.That(routes, Does.Contain("POST:create"));
            Assert.That(routes, Does.Contain("GET:get-all"));
            Assert.That(routes, Does.Contain("GET:get-by-id/{id:int}"));
            Assert.That(routes, Does.Contain("POST:update"));
            Assert.That(routes, Does.Contain("POST:update-status"));
            Assert.That(routes, Does.Contain("POST:sync"));
            Assert.That(routes, Does.Contain("DELETE:delete/{id:int}"));
        });
    }

    [Test]
    public void Permission_and_repository_are_institution_scoped()
    {
        var permission = Read("skolenza.application", "Features", "InstitutionEmailTemplateCmd", "InstitutionEmailTemplatePermissionBehavior.cs");
        var repository = Read("skolenza.persistance", "Repositories", "InstitutionEmailTemplateRepository.cs");

        Assert.Multiple(() =>
        {
            Assert.That(permission, Does.Contain("INSTITUTION_EMAIL_TEMPLATE"));
            Assert.That(permission, Does.Contain("CheckInstitutionStaffMemberPermissionAsync"));
            Assert.That(repository, Does.Contain("x.InstitutionId == institutionId"));
            Assert.That(repository, Does.Not.Contain("context.EmailTemplates"));
        });
    }

    [Test]
    public void Production_seed_contains_module_operations_plan_entitlements_and_data_copy()
    {
        var seed = Read("database-scripts", "complete seed data", "Skolenza_New_Production_Module_Operation_Seed.sql");

        Assert.Multiple(() =>
        {
            Assert.That(seed, Does.Contain("'INSTITUTION_EMAIL_TEMPLATE'"));
            Assert.That(seed, Does.Contain("parent.\"Id\" = parentMapping.\"ModuleId\""));
            Assert.That(seed, Does.Contain("ModuleOperationMapping"));
            Assert.That(seed, Does.Contain("PlanModuleMapping"));
            Assert.That(seed, Does.Contain("InstitutionEnabledModule"));
            Assert.That(seed, Does.Contain("InstitutionEnabledOperation"));
            Assert.That(seed, Does.Contain("CROSS JOIN skolenza.\"EmailTemplate\""));
        });
    }

    [TestCase("WELCOME_EMAIL", true)]
    [TestCase("forgot_password", true)]
    [TestCase("INSTITUTION_FREE_TEXT_CODE", false)]
    [TestCase("", false)]
    public void Institution_template_codes_are_limited_to_shared_predefined_constants(
        string templateCode,
        bool expected)
    {
        Assert.That(ConstantValues.IsSupportedEmailTemplateCode(templateCode), Is.EqualTo(expected));
    }

    [Test]
    public void Mail_delivery_resolves_institution_template_and_keeps_host_fallback()
    {
        var repository = Read("skolenza.persistance", "Repositories", "InstitutionEmailTemplateRepository.cs");
        var service = Read("skolenza.infrastructure", "MailService", "EmailService.cs");

        Assert.Multiple(() =>
        {
            Assert.That(repository, Does.Contain("GetActiveByCodeAsync"));
            Assert.That(repository, Does.Contain("template.InstitutionId == institutionId"));
            Assert.That(repository, Does.Contain("template.IsActive"));
            Assert.That(service, Does.Contain("_institutionTemplateRepository.GetActiveByCodeAsync"));
            Assert.That(service, Does.Contain("_templateRepository.GetTemplateByCodeAsync"));
            Assert.That(service, Does.Contain("falling back to the Host template"));
        });
    }

    [Test]
    public void Mail_delivery_does_not_turn_post_accept_disconnect_failure_into_delivery_failure()
    {
        var service = Read("skolenza.infrastructure", "MailService", "EmailService.cs");
        var sendIndex = service.IndexOf("await smtp.SendAsync(message);", StringComparison.Ordinal);
        var disconnectIndex = service.IndexOf("await smtp.DisconnectAsync(true);", StringComparison.Ordinal);
        var disconnectWarningIndex = service.IndexOf(
            "SMTP disconnect failed during cleanup",
            StringComparison.Ordinal);
        var disposeWarningIndex = service.IndexOf(
            "SMTP client disposal failed during cleanup",
            StringComparison.Ordinal);
        var acceptedLogIndex = service.IndexOf("SMTP accepted email", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(sendIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(disconnectIndex, Is.GreaterThan(sendIndex));
            Assert.That(disconnectWarningIndex, Is.GreaterThan(disconnectIndex));
            Assert.That(disposeWarningIndex, Is.GreaterThan(disconnectWarningIndex));
            Assert.That(acceptedLogIndex, Is.GreaterThan(disposeWarningIndex));

            var postSendSection = service.Substring(sendIndex, acceptedLogIndex - sendIndex);
            Assert.That(postSendSection, Does.Contain("try"));
            Assert.That(postSendSection, Does.Contain("catch (Exception ex)"));
            Assert.That(postSendSection, Does.Contain("LogWarning"));
            Assert.That(postSendSection, Does.Contain("smtp.Dispose()"));
            Assert.That(postSendSection, Does.Not.Contain("EmailDeliveryFailed"));
        });
    }

    [Test]
    public void Queue_worker_keeps_institution_code_retry_and_atomic_claim_contract()
    {
        var worker = Read("skolenza.infrastructure", "BackgroundJob", "EmailQueueWorker.cs");
        var repository = Read("skolenza.persistance", "Repositories", "EmailQueueRepository.cs");
        var migration = Read("database-scripts", "AlterEmailQueueForInstitutionTemplateResolution.sql");

        Assert.Multiple(() =>
        {
            Assert.That(worker, Does.Contain("SendTemplatedEmailAsync("));
            Assert.That(worker, Does.Contain("templateCode,"));
            Assert.That(worker, Does.Contain("item.ToEmail,"));
            Assert.That(worker, Does.Contain("item.InstitutionId,"));
            Assert.That(worker, Does.Contain("MaximumRetries = 5"));
            Assert.That(repository, Does.Contain("FOR UPDATE SKIP LOCKED"));
            Assert.That(repository, Does.Contain("ProcessingStartedDateTime"));
            Assert.That(migration, Does.Contain("\"InstitutionId\" bigint"));
            Assert.That(migration, Does.Contain("\"TemplateCode\" character varying(100)"));
            Assert.That(migration, Does.Contain("\"PlaceholdersJson\" text"));
        });
    }

    private static string Read(params string[] parts)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Skolenza.sln")))
        {
            directory = directory.Parent;
        }

        Assert.That(directory, Is.Not.Null);
        return File.ReadAllText(Path.Combine(new[] { directory!.FullName }.Concat(parts).ToArray()));
    }
}
