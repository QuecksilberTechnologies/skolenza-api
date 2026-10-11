using System.Text.Json;
using skolenza.application.Common.Models;
using skolenza.application.Constants;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("BillingFoundation")]
public sealed class BillingFoundationContractTests
{
    private static string Root => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));

    [Test]
    public void Billing_operational_values_are_centralized_and_professional_defaults_are_bounded()
    {
        Assert.Multiple(() =>
        {
            Assert.That(BillingConstants.DefaultGatewayCode, Is.EqualTo("CASHFREE"));
            Assert.That(BillingConstants.PaymentRetryCount, Is.EqualTo(3));
            Assert.That(BillingConstants.GracePeriodDays, Is.EqualTo(1));
            Assert.That(new BillingOptions().PaymentRetryCount, Is.EqualTo(BillingConstants.PaymentRetryCount));
            Assert.That(new BillingOptions().GracePeriodDays, Is.EqualTo(BillingConstants.GracePeriodDays));
        });
    }

    [Test]
    public void Billing_schema_has_idempotency_webhook_tax_invoice_refund_and_audit_guards()
    {
        var sql = File.ReadAllText(Path.Combine(Root, "database-scripts", "AddSubscriptionBillingFoundation.sql"));

        Assert.Multiple(() =>
        {
            foreach (var table in new[]
            {
                "PaymentGateway", "HostBillingConfiguration", "SubscriptionPlanPrice", "BillingTaxRule", "InstitutionBillingProfile",
                "InstitutionBillingSubscription", "BillingOrder", "PaymentTransaction", "PaymentAttempt",
                "PaymentWebhookEvent", "BillingInvoice", "BillingInvoiceLine", "BillingRefund",
                "BillingSubscriptionChange", "BillingAuditLog"
            })
            {
                Assert.That(sql, Does.Contain($"CREATE TABLE IF NOT EXISTS skolenza.\"{table}\""), table);
            }

            Assert.That(sql, Does.Contain("UQ_BillingOrder_Idempotency"));
            Assert.That(sql, Does.Contain("UQ_PaymentWebhookEvent_GatewayEvent"));
            Assert.That(sql, Does.Contain("CK_BillingRefund_Amount"));
            Assert.That(sql, Does.Contain("UX_HostBillingConfiguration_Active"));
            Assert.That(sql, Does.Contain("AddedByHostUserId"));
            Assert.That(sql, Does.Contain("CASHFREE_CLIENT_SECRET"));
            Assert.That(sql, Does.Not.Contain("client_secret=").IgnoreCase);
        });
    }

    [Test]
    public void Appsettings_exposes_changeable_retry_and_grace_values()
    {
        var json = File.ReadAllText(Path.Combine(Root, "skolenza.api", "appsettings.json"));
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });
        var billing = document.RootElement.GetProperty(BillingOptions.SectionName);

        Assert.Multiple(() =>
        {
            Assert.That(billing.GetProperty("PaymentRetryCount").GetInt32(), Is.EqualTo(3));
            Assert.That(billing.GetProperty("GracePeriodDays").GetInt32(), Is.EqualTo(1));
        });
    }

    [Test]
    public void Host_billing_configuration_uses_authorization_and_existing_permission_pipeline()
    {
        var controller = File.ReadAllText(Path.Combine(Root, "skolenza.api", "Controllers", "Billing", "HostBillingConfigurationController.cs"));
        var handlers = File.ReadAllText(Path.Combine(Root, "skolenza.application", "Features", "BillingCmd", "HostBillingConfigurationHandlers.cs"));
        var repository = File.ReadAllText(Path.Combine(Root, "skolenza.persistance", "Repositories", "HostBillingConfigurationRepository.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(controller, Does.Contain("[Authorize]"));
            Assert.That(controller, Does.Contain("api/host/billing/configuration"));
            Assert.That(handlers, Does.Contain("HostRuntimePermissionValidator.ValidateAsync"));
            Assert.That(handlers, Does.Contain("BillingConstants.HostBillingConfigurationModuleCode"));
            Assert.That(repository, Does.Contain("BillingConstants.DefaultGatewayCode"));
            Assert.That(repository, Does.Not.Contain("ClientSecretEnvironmentVariable\"\"=@"));
        });
    }

    [Test]
    public void Host_billing_module_seed_is_idempotent_and_part_of_authoritative_seed()
    {
        var standalone = File.ReadAllText(Path.Combine(Root, "database-scripts", "SeedHostBillingConfigurationModule.sql"));
        var authoritative = File.ReadAllText(Path.Combine(Root, "database-scripts", "complete seed data", "Skolenza_New_Production_Module_Operation_Seed.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(standalone, Does.Contain("NOT EXISTS"));
            Assert.That(standalone, Does.Contain("HOST_MANAGEMENT"));
            Assert.That(standalone, Does.Contain("Host-Super-Admin"));
            Assert.That(authoritative, Does.Contain("HOST_BILLING_CONFIGURATION"));
            Assert.That(authoritative, Does.Contain("billing-configuration"));
            foreach (var moduleCode in new[]
            {
                "HOST_BILLING_PLAN_PRICES", "HOST_BILLING_TAX_RULES", "HOST_BILLING_TRANSACTIONS",
                "HOST_BILLING_REFUNDS", "HOST_BILLING_RECONCILIATION", "HOST_BILLING_AUDIT"
            })
            {
                Assert.That(standalone, Does.Contain(moduleCode), moduleCode);
                Assert.That(authoritative, Does.Contain(moduleCode), moduleCode);
            }
        });
    }

    [Test]
    public void Host_billing_administration_routes_are_authorized_and_permission_bound()
    {
        var controller = File.ReadAllText(Path.Combine(Root, "skolenza.api", "Controllers", "Billing", "HostBillingAdministrationController.cs"));
        var handlers = File.ReadAllText(Path.Combine(Root, "skolenza.application", "Features", "BillingCmd", "HostBillingAdministrationHandlers.cs"));
        var repository = File.ReadAllText(Path.Combine(Root, "skolenza.persistance", "Repositories", "HostBillingAdministrationRepository.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(controller, Does.Contain("[Authorize]"));
            foreach (var route in new[] { "plan-prices", "tax-rules", "transactions", "refunds", "reconciliation", "audit" })
            {
                Assert.That(controller, Does.Contain(route), route);
            }
            Assert.That(handlers, Does.Contain("HostBillingConfigurationAuthorization.ValidateAsync"));
            Assert.That(handlers, Does.Contain("\"Add\" : \"Update\""));
            Assert.That(repository, Does.Contain("billingOptions.Value.WebhookProcessingRetryCount"));
            Assert.That(repository, Does.Contain("IsolationLevel.Serializable"));
        });
    }
}
