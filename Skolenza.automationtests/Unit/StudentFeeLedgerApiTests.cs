using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using skolenza.api.Controllers.Billing;
using skolenza.api.Controllers.Student;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers;
using skolenza.application.DTOS.Student;
using skolenza.application.Constants;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StudentFeeLedger")]
public sealed class StudentFeeLedgerApiTests
{
    [Test]
    public void Quote_rounds_currency_midpoints_away_from_zero()
    {
        Assert.Multiple(() =>
        {
            Assert.That(StudentFeeQuoteCalculator.RoundCurrency(1.005m), Is.EqualTo(1.01m));
            Assert.That(StudentFeeQuoteCalculator.RoundCurrency(-1.005m), Is.EqualTo(-1.01m));
        });
    }

    [Test]
    public void Quote_uses_configured_zero_and_three_decimal_currency_precision()
    {
        Assert.Multiple(() =>
        {
            Assert.That(StudentFeeQuoteCalculator.RoundCurrency(1.5m, 0), Is.EqualTo(2m));
            Assert.That(StudentFeeQuoteCalculator.RoundCurrency(1.2345m, 3), Is.EqualTo(1.235m));
        });

        var quote = StudentFeeQuoteCalculator.Calculate(
            12, 3, 4, "BHD", 1, 0m,
            [Item("TUITION", 1.2355m, StudentFeeComponentChargeNature.RecurringAnnual)],
            [Due(1, 2027, 4, 1)],
            3);

        Assert.Multiple(() =>
        {
            Assert.That(quote.CurrencyDecimalPlaces, Is.EqualTo(3));
            Assert.That(quote.GrossRecurringAmount, Is.EqualTo(1.236m));
            Assert.That(quote.Installments.Single().RecurringAmount, Is.EqualTo(1.236m));
        });
    }

    [Test]
    public void Quote_places_one_time_charges_in_first_installment_and_allocates_remainder_last()
    {
        var quote = StudentFeeQuoteCalculator.Calculate(
            12, 3, 4, "INR", 3, 0m,
            [Item("TUITION", 100m, StudentFeeComponentChargeNature.RecurringAnnual),
             Item("ADMISSION", 30m, StudentFeeComponentChargeNature.OneTime)],
            [Due(1, 2027, 4, 1), Due(2, 2027, 8, 1), Due(3, 2027, 12, 1)]);

        Assert.Multiple(() =>
        {
            Assert.That(quote.Installments.Select(item => item.RecurringAmount), Is.EqualTo([33.33m, 33.33m, 33.34m]));
            Assert.That(quote.Installments.Select(item => item.OneTimeAmount), Is.EqualTo([30m, 0m, 0m]));
            Assert.That(quote.Installments.Sum(item => item.RecurringAmount), Is.EqualTo(quote.GrossRecurringAmount));
            Assert.That(quote.NetAnnualAmount, Is.EqualTo(130m));
        });
    }

    [Test]
    public void Quote_discount_is_only_applied_to_recurring_amounts_and_line_totals_reconcile()
    {
        var quote = StudentFeeQuoteCalculator.Calculate(
            12, 3, 4, "INR", 1, 10m,
            [Item("TUITION-A", 0.05m, StudentFeeComponentChargeNature.RecurringAnnual),
             Item("TUITION-B", 0.05m, StudentFeeComponentChargeNature.RecurringAnnual),
             Item("ADMISSION", 20m, StudentFeeComponentChargeNature.OneTime)],
            [Due(1, 2027, 4, 1)]);

        Assert.Multiple(() =>
        {
            Assert.That(quote.DiscountAmount, Is.EqualTo(0.01m));
            Assert.That(quote.Lines.Sum(item => item.DiscountAmount), Is.EqualTo(quote.DiscountAmount));
            Assert.That(quote.Lines.Single(item => item.ComponentCode == "ADMISSION").DiscountAmount, Is.Zero);
            Assert.That(quote.Installments.Single().OneTimeAmount, Is.EqualTo(20m));
        });
    }

    [Test]
    public void Quote_rejects_missing_or_out_of_order_explicit_due_dates()
    {
        var items = new[] { Item("TUITION", 120m, StudentFeeComponentChargeNature.RecurringAnnual) };

        Assert.Throws<skolenza.application.Exceptions.ValidationErrorException>(() =>
            StudentFeeQuoteCalculator.Calculate(1, 2, 3, "INR", 2, 0m, items, [Due(1, 2027, 4, 1)]));
        Assert.Throws<skolenza.application.Exceptions.ValidationErrorException>(() =>
            StudentFeeQuoteCalculator.Calculate(1, 2, 3, "INR", 2, 0m, items,
                [Due(1, 2027, 8, 1), Due(2, 2027, 4, 1)]));
    }

    [Test]
    public void Fee_ledger_apis_require_authentication_and_use_64_bit_id_routes()
    {
        var fee = typeof(StudentFeeLedgerController);
        var currency = typeof(HostCountryCurrencyMasterController);

        Assert.Multiple(() =>
        {
            Assert.That(fee.GetCustomAttributes(typeof(AuthorizeAttribute), true), Is.Not.Empty);
            Assert.That(currency.GetCustomAttributes(typeof(AuthorizeAttribute), true), Is.Not.Empty);
            Assert.That(fee.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().Single().Template,
                Is.EqualTo("api/student/fees"));
            Assert.That(currency.GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().Single().Template,
                Is.EqualTo("api/host/country-currency-masters"));
            Assert.That(fee.GetMethods().Select(item => item.Name),
                Does.Contain("GetStructures").And.Contain("CreateQuote").And.Contain("RecordOfflinePayment")
                    .And.Contain("GetAdmissionBalance").And.Contain("GetReceipts"));
            Assert.That(new StudentFeeStructureFilterDTO().PageNumber, Is.EqualTo(1));
            Assert.That(new StudentFeeStructureFilterDTO().PageSize, Is.EqualTo(10));
            Assert.That(new StudentFeeQuoteListRequestDTO().PageNumber, Is.EqualTo(1));
            Assert.That(new StudentFeeQuoteListRequestDTO().PageSize, Is.EqualTo(10));
            Assert.That(new StudentFeeReceiptListRequestDTO().PageNumber, Is.EqualTo(1));
            Assert.That(new StudentFeeReceiptListRequestDTO().PageSize, Is.EqualTo(10));
        });

        foreach (var route in fee.GetMethods().SelectMany(method => method.GetCustomAttributes(typeof(HttpMethodAttribute), true)
                     .Cast<HttpMethodAttribute>()).Where(attribute => attribute.Template?.Contains("{id}", StringComparison.Ordinal) == true))
        {
            Assert.That(route.Template, Does.Contain(":long"));
        }
    }

    [Test]
    public void Fee_ledger_entities_have_explicit_schema_mappings_and_precision()
    {
        using var context = new SkolenzaDbContext(
            new DbContextOptionsBuilder<SkolenzaDbContext>()
                .UseNpgsql("Host=localhost;Database=skolenza_model_validation;Username=postgres;Password=not-used")
                .Options);

        var mappings = new Dictionary<Type, string>
        {
            [typeof(CountryCurrencyMaster)] = "CountryCurrencyMaster",
            [typeof(StudentFeeStructure)] = "StudentFeeStructure",
            [typeof(StudentFeeStructureItem)] = "StudentFeeStructureItem",
            [typeof(StudentFeeStructureFrequency)] = "StudentFeeStructureFrequency",
            [typeof(StudentFeeStructureInstallmentDueDate)] = "StudentFeeStructureInstallmentDueDate",
            [typeof(StudentAdmissionFeeQuote)] = "StudentAdmissionFeeQuote",
            [typeof(StudentAdmissionFeeQuoteLine)] = "StudentAdmissionFeeQuoteLine",
            [typeof(StudentFeeInstallment)] = "StudentFeeInstallment",
            [typeof(StudentFeePaymentAttempt)] = "StudentFeePaymentAttempt",
            [typeof(StudentFeePaymentTransaction)] = "StudentFeePaymentTransaction",
            [typeof(StudentFeePaymentWebhookEvent)] = "StudentFeePaymentWebhookEvent",
            [typeof(StudentFeeReceipt)] = "StudentFeeReceipt"
        };

        foreach (var mapping in mappings)
        {
            var entity = context.Model.FindEntityType(mapping.Key);
            Assert.That(entity?.GetTableName(), Is.EqualTo(mapping.Value), mapping.Key.Name);
            Assert.That(entity?.GetSchema(), Is.EqualTo("skolenza"), mapping.Key.Name);
        }
    }

    [Test]
    public void Fee_ledger_schema_script_creates_all_tables_and_dynamic_host_and_fee_permissions()
    {
        var root = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        var script = File.ReadAllText(Path.Combine(root, "database-scripts", "CreateStudentFeeLedgerAndCountryCurrencyMaster.sql"));
        var feeScript = File.ReadAllText(Path.Combine(root, "database-scripts", "CreateStudentFeeMastersAndModule.sql"));

        Assert.Multiple(() =>
        {
            Assert.That(script, Does.Contain(BillingConstants.HostCountryCurrencyMasterModuleCode));
            Assert.That(script, Does.Contain("'view', 'add', 'update', 'delete'"));
            Assert.That(script, Does.Contain("JOIN skolenza.\"Operation\""));
            Assert.That(script, Does.Contain("CREATE UNIQUE INDEX IF NOT EXISTS \"UX_StudentAdmissionFeeQuote_Admission\""));
            Assert.That(script, Does.Contain("role grants").IgnoreCase);
            Assert.That(feeScript, Does.Contain("'view', 'add', 'update', 'delete'"));
        });
    }

    [Test]
    public void Host_currency_deactivation_uses_the_existing_canonical_Delete_permission()
    {
        var source = File.ReadAllText(Path.Combine(
            Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..")),
            "Skolenza.application", "Features", "BillingCmd", "HostCountryCurrencyMasterHandlers.cs"));

        Assert.That(source, Does.Contain("HostCountryCurrencyMasterModuleCode, \"Delete\""));
    }

    [Test]
    public void Fee_balance_and_receipt_queries_use_the_fee_view_permission()
    {
        var source = File.ReadAllText(Path.Combine(
            Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..")),
            "Skolenza.application", "Features", "StudentCmd", "StudentPermissionBehavior.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("GetStudentAdmissionFeeBalanceQuery => (StudentConstants.FeesModuleCode, \"View\")"));
            Assert.That(source, Does.Contain("GetStudentFeeReceiptsQuery => (StudentConstants.FeesModuleCode, \"View\")"));
            Assert.That(typeof(StudentFeeLedgerController).GetMethod("GetAdmissionBalance")?.GetCustomAttributes(typeof(HttpGetAttribute), true), Is.Not.Empty);
            Assert.That(typeof(StudentFeeLedgerController).GetMethod("GetReceipts")?.GetCustomAttributes(typeof(HttpGetAttribute), true), Is.Not.Empty);
        });
    }

    [Test]
    public void Offline_payment_rechecks_idempotency_under_a_database_transaction_lock()
    {
        var source = File.ReadAllText(Path.Combine(
            Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..")),
            "Skolenza.persistance", "Repositories", "StudentFeeLedgerRepository.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("pg_advisory_xact_lock"));
            Assert.That(source, Does.Contain("MatchesOfflinePayment(existing, installmentId, request, decimalPlaces)"));
            Assert.That(source, Does.Contain("BeginTransactionAsync(System.Data.IsolationLevel.Serializable"));
        });
    }

    [Test]
    public void Currency_precision_is_carried_into_fee_structure_and_quote_snapshots()
    {
        using var context = new SkolenzaDbContext(
            new DbContextOptionsBuilder<SkolenzaDbContext>()
                .UseNpgsql("Host=localhost;Database=skolenza_model_validation;Username=postgres;Password=not-used")
                .Options);

        Assert.Multiple(() =>
        {
            Assert.That(context.Model.FindEntityType(typeof(CountryCurrencyMaster))?.FindProperty(nameof(CountryCurrencyMaster.DecimalPlaces)), Is.Not.Null);
            Assert.That(context.Model.FindEntityType(typeof(StudentFeeStructure))?.FindProperty(nameof(StudentFeeStructure.CurrencyDecimalPlaces)), Is.Not.Null);
            Assert.That(context.Model.FindEntityType(typeof(StudentAdmissionFeeQuote))?.FindProperty(nameof(StudentAdmissionFeeQuote.CurrencyDecimalPlaces)), Is.Not.Null);
        });
    }

    private static StudentFeeStructureItemResponseDTO Item(
        string code,
        decimal amount,
        StudentFeeComponentChargeNature nature) => new()
    {
        ComponentCode = code,
        ComponentName = code,
        ChargeNature = (short)nature,
        Amount = amount
    };

    private static StudentFeeStructureDueDateResponseDTO Due(int installment, int year, int month, int day) => new()
    {
        InstallmentNumber = (short)installment,
        DueDate = new DateOnly(year, month, day)
    };
}
