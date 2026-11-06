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
    public void Discount_applies_only_to_selected_recurring_heads()
    {
        var tuition = Item("TUITION", 1500m, StudentFeeComponentChargeNature.RecurringAnnual);
        var sports = Item("SPORTS", 500m, StudentFeeComponentChargeNature.RecurringAnnual);
        sports.IsDiscountEligible = false;
        var exam = Item("EXAM", 500m, StudentFeeComponentChargeNature.OneTime);
        var quote = StudentFeeQuoteCalculator.Calculate(12, 3, 4, "INR", 1, 10m,
            [tuition, sports, exam], [Due(1, 2028, 4, 1)], 2);
        Assert.That(quote.GrossRecurringAmount, Is.EqualTo(2000m));
        Assert.That(quote.DiscountAmount, Is.EqualTo(150m));
        Assert.That(quote.NetAnnualAmount, Is.EqualTo(2350m));
        Assert.That(quote.Lines.Single(item => item.ComponentCode == "TUITION").DiscountAmount, Is.EqualTo(150m));
        Assert.That(quote.Lines.Single(item => item.ComponentCode == "SPORTS").DiscountAmount, Is.Zero);
        Assert.That(quote.Lines.Single(item => item.ComponentCode == "EXAM").IsDiscountEligible, Is.False);
    }

    [Test]
    public void Discount_rounding_reconciles_across_multiple_small_eligible_heads()
    {
        var items = Enumerable.Range(1, 7)
            .Select(number => Item($"HEAD-{number}", 0.01m, StudentFeeComponentChargeNature.RecurringAnnual))
            .ToArray();
        var quote = StudentFeeQuoteCalculator.Calculate(12, 3, 4, "INR", 1, 50m,
            items, [Due(1, 2028, 4, 1)], 2);
        Assert.That(quote.DiscountAmount, Is.EqualTo(0.04m));
        Assert.That(quote.Lines.Sum(item => item.DiscountAmount), Is.EqualTo(0.04m));
        Assert.That(quote.Lines.All(item => item.DiscountAmount >= 0m &&
            item.DiscountAmount <= item.AnnualAmount && item.NetAmount >= 0m), Is.True);
        Assert.That(quote.NetAnnualAmount, Is.EqualTo(0.03m));
    }
    [Test]
    public void Head_schedule_reconciles_each_installment_and_each_head_at_currency_precision()
    {
        var heads = new[]
        {
            new StudentFeeHeadScheduleCalculator.Head(1, 1500.01m),
            new StudentFeeHeadScheduleCalculator.Head(2, 499.99m)
        };
        var installments = new[]
        {
            new StudentFeeHeadScheduleCalculator.Installment(1, 666.67m),
            new StudentFeeHeadScheduleCalculator.Installment(2, 666.67m),
            new StudentFeeHeadScheduleCalculator.Installment(3, 666.66m)
        };
        var result = StudentFeeHeadScheduleCalculator.Distribute(heads, installments, 2);
        foreach (var installment in installments)
        {
            Assert.That(result.Where(item => item.InstallmentNumber == installment.Number).Sum(item => item.Amount),
                Is.EqualTo(installment.Amount));
        }
        foreach (var head in heads)
        {
            Assert.That(result.Where(item => item.HeadId == head.Id).Sum(item => item.Amount), Is.EqualTo(head.Amount));
        }
        Assert.That(result.All(item => item.Amount >= 0 && item.Amount == decimal.Round(item.Amount, 2)), Is.True);
    }

    [Test]
    public void Head_schedule_rejects_mismatched_or_duplicate_amounts()
    {
        var head = new StudentFeeHeadScheduleCalculator.Head(1, 10m);
        var installment = new StudentFeeHeadScheduleCalculator.Installment(1, 9m);
        Assert.Throws<skolenza.application.Exceptions.ValidationErrorException>(() =>
            StudentFeeHeadScheduleCalculator.Distribute([head], [installment], 2));
        Assert.Throws<skolenza.application.Exceptions.ValidationErrorException>(() =>
            StudentFeeHeadScheduleCalculator.Distribute([head, head],
                [new StudentFeeHeadScheduleCalculator.Installment(1, 20m)], 2));
    }

    [TestCase(0)]
    [TestCase(2)]
    [TestCase(3)]
    public void Head_schedule_preserves_minor_units_and_is_deterministic(int decimalPlaces)
    {
        var scale = decimalPlaces switch { 0 => 1m, 2 => 100m, _ => 1000m };
        var heads = new[]
        {
            new StudentFeeHeadScheduleCalculator.Head(4, 500m / scale),
            new StudentFeeHeadScheduleCalculator.Head(9, 501m / scale)
        };
        var installments = new[]
        {
            new StudentFeeHeadScheduleCalculator.Installment(1, 334m / scale),
            new StudentFeeHeadScheduleCalculator.Installment(2, 334m / scale),
            new StudentFeeHeadScheduleCalculator.Installment(3, 333m / scale),
            new StudentFeeHeadScheduleCalculator.Installment(4, 0m)
        };
        var first = StudentFeeHeadScheduleCalculator.Distribute(heads, installments, decimalPlaces);
        var second = StudentFeeHeadScheduleCalculator.Distribute(heads.Reverse().ToArray(), installments, decimalPlaces);
        Assert.That(second, Is.EqualTo(first));
        foreach (var installment in installments)
        {
            Assert.That(first.Where(item => item.InstallmentNumber == installment.Number).Sum(item => item.Amount),
                Is.EqualTo(installment.Amount));
        }
        foreach (var head in heads)
        {
            Assert.That(first.Where(item => item.HeadId == head.Id).Sum(item => item.Amount), Is.EqualTo(head.Amount));
        }
    }

    [Test]
    public void Small_recurring_total_never_creates_negative_installment_when_rounded()
    {
        var dates = Enumerable.Range(1, 12)
            .Select(number => Due((short)number, 2028, number, 1))
            .ToArray();
        var quote = StudentFeeQuoteCalculator.Calculate(
            12, 3, 4, "INR", 12, 0m,
            [Item("TUITION", 0.06m, StudentFeeComponentChargeNature.RecurringAnnual)],
            dates, 2);

        Assert.That(quote.Installments.All(item => item.RecurringAmount >= 0m), Is.True);
        Assert.That(quote.Installments.Sum(item => item.RecurringAmount), Is.EqualTo(0.06m));
        Assert.That(quote.Installments.Count(item => item.RecurringAmount == 0.01m), Is.EqualTo(6));
    }

    [Test]
    public void Oldest_first_allocation_splits_partial_payments_by_due_date_and_head_priority()
    {
        var day = new DateOnly(2028, 4, 10);
        var heads = new[]
        {
            new StudentFeeAllocationCalculator.OutstandingHead(1, day, 1, 1, 1500m),
            new StudentFeeAllocationCalculator.OutstandingHead(1, day, 2, 2, 500m),
            new StudentFeeAllocationCalculator.OutstandingHead(1, day, 3, 3, 500m),
            new StudentFeeAllocationCalculator.OutstandingHead(1, day, 4, 4, 500m)
        };
        var first = StudentFeeAllocationCalculator.AllocateOldestFirst(2200m, 2, heads);
        Assert.That(first.Select(item => item.Amount), Is.EqualTo(new[] { 1500m, 500m, 200m }));
        var later = new[]
        {
            new StudentFeeAllocationCalculator.OutstandingHead(1, day, 3, 3, 300m),
            new StudentFeeAllocationCalculator.OutstandingHead(1, day, 4, 4, 500m),
            new StudentFeeAllocationCalculator.OutstandingHead(2, day.AddMonths(1), 1, 1, 3000m)
        };
        var second = StudentFeeAllocationCalculator.AllocateOldestFirst(3500m, 2, later);
        Assert.That(second.Select(item => item.Amount), Is.EqualTo(new[] { 300m, 500m, 2700m }));
        Assert.That(second.Sum(item => item.Amount), Is.EqualTo(3500m));
    }

    [Test]
    public void Allocation_rejects_duplicate_heads_overpayment_and_imprecise_currency()
    {
        var day = new DateOnly(2028, 4, 10);
        var head = new StudentFeeAllocationCalculator.OutstandingHead(1, day, 1, 1, 3m);
        Assert.Throws<skolenza.application.Exceptions.ValidationErrorException>(() =>
            StudentFeeAllocationCalculator.AllocateOldestFirst(4m, 2, [head]));
        Assert.Throws<skolenza.application.Exceptions.ValidationErrorException>(() =>
            StudentFeeAllocationCalculator.AllocateOldestFirst(1.001m, 2, [head]));
        Assert.Throws<skolenza.application.Exceptions.ValidationErrorException>(() =>
            StudentFeeAllocationCalculator.AllocateOldestFirst(1m, 2, [head, head]));
        Assert.Throws<skolenza.application.Exceptions.ValidationErrorException>(() =>
            StudentFeeAllocationCalculator.AllocateOldestFirst(1m, 2,
                [head, new StudentFeeAllocationCalculator.OutstandingHead(1, day.AddDays(1), 2, 2, 3m)]));
    }

    [Test]
    public void Selected_installment_first_supports_current_month_and_partial_old_arrears()
    {
        var day = new DateOnly(2028, 4, 10);
        var heads = new[]
        {
            new StudentFeeAllocationCalculator.OutstandingHead(1, day, 3, 3, 300m),
            new StudentFeeAllocationCalculator.OutstandingHead(1, day, 4, 4, 500m),
            new StudentFeeAllocationCalculator.OutstandingHead(2, day.AddMonths(1), 1, 1, 3000m)
        };
        var result = StudentFeeAllocationCalculator.AllocateSelectedInstallmentFirst(3500m, 2, heads, 2);
        Assert.That(result.Select(item => (item.InstallmentId, item.Amount)),
            Is.EqualTo(new[] { (2L, 3000m), (1L, 300m), (1L, 200m) }));
        Assert.Throws<skolenza.application.Exceptions.ValidationErrorException>(() =>
            StudentFeeAllocationCalculator.AllocateSelectedInstallmentFirst(3500m, 2, heads, 3));
        Assert.Throws<skolenza.application.Exceptions.ValidationErrorException>(() =>
            StudentFeeAllocationCalculator.AllocateSelectedInstallmentFirst(1m, 2,
                [new StudentFeeAllocationCalculator.OutstandingHead(1, day, 1, 1, 0m),
                 new StudentFeeAllocationCalculator.OutstandingHead(2, day.AddMonths(1), 1, 1, 3m)], 1));
    }

    [Test]
    public void Partial_reversal_uses_unreversed_allocations_in_reverse_charge_order()
    {
        var allocations = new[]
        {
            new StudentFeeAllocationCalculator.PostedAllocation(1, 11, 1, false, 1500m, 0m),
            new StudentFeeAllocationCalculator.PostedAllocation(2, 12, 2, false, 500m, 100m),
            new StudentFeeAllocationCalculator.PostedAllocation(3, null, 0, true, 50m, 0m)
        };
        var reversal = StudentFeeAllocationCalculator.AllocateReversal(300m, 2, allocations);
        Assert.That(reversal.Select(item => (item.AllocationId, item.Amount)),
            Is.EqualTo(new[] { (3L, 50m), (2L, 250m) }));
        Assert.That(reversal.Sum(item => item.Amount), Is.EqualTo(300m));
        Assert.Throws<skolenza.application.Exceptions.ValidationErrorException>(() =>
            StudentFeeAllocationCalculator.AllocateReversal(1950.01m, 2, allocations));
        Assert.Throws<skolenza.application.Exceptions.ValidationErrorException>(() =>
            StudentFeeAllocationCalculator.AllocateReversal(1.001m, 2, allocations));
        Assert.Throws<skolenza.application.Exceptions.ValidationErrorException>(() =>
            StudentFeeAllocationCalculator.AllocateReversal(1m, 2,
                [new StudentFeeAllocationCalculator.PostedAllocation(3, null, 0, true, 50m, 51m)]));
    }

    [Test]
    public void Allocation_covers_late_fee_after_heads_within_each_installment()
    {
        var due = new DateOnly(2028, 4, 10);
        var heads = new[]
        {
            new StudentFeeAllocationCalculator.OutstandingHead(1, due, 11, 2, 300m),
            new StudentFeeAllocationCalculator.OutstandingHead(1, due, 0, -1, 50m, IsLateFee: true),
            new StudentFeeAllocationCalculator.OutstandingHead(2, due.AddMonths(1), 12, 1, 500m)
        };
        var oldest = StudentFeeAllocationCalculator.AllocateOldestFirst(400m, 2, heads);
        Assert.That(oldest.Select(item => (item.InstallmentId, item.Amount, item.IsLateFee)),
            Is.EqualTo(new[] { (1L, 300m, false), (1L, 50m, true), (2L, 50m, false) }));

        var selected = StudentFeeAllocationCalculator.AllocateSelectedInstallmentFirst(550m, 2, heads, 2);
        Assert.That(selected.Select(item => (item.InstallmentId, item.Amount, item.IsLateFee)),
            Is.EqualTo(new[] { (2L, 500m, false), (1L, 50m, false) }));

        Assert.Throws<skolenza.application.Exceptions.ValidationErrorException>(() =>
            StudentFeeAllocationCalculator.AllocateOldestFirst(1m, 2,
                [new StudentFeeAllocationCalculator.OutstandingHead(1, due, 12, 1, 5m, IsLateFee: true)]));
        Assert.Throws<skolenza.application.Exceptions.ValidationErrorException>(() =>
            StudentFeeAllocationCalculator.AllocateOldestFirst(1m, 2,
                [new StudentFeeAllocationCalculator.OutstandingHead(1, due, 0, 1, 5m)]));
    }

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
            [typeof(StudentFeePaymentBatch)] = "StudentFeePaymentBatch",
            [typeof(StudentFeePaymentBatchItem)] = "StudentFeePaymentBatchItem",
            [typeof(StudentFeePaymentWebhookEvent)] = "StudentFeePaymentWebhookEvent",
            [typeof(StudentFeeReceipt)] = "StudentFeeReceipt"
        };

        foreach (var mapping in mappings)
        {
            var entity = context.Model.FindEntityType(mapping.Key);
            Assert.That(entity?.GetTableName(), Is.EqualTo(mapping.Value), mapping.Key.Name);
            Assert.That(entity?.GetSchema(), Is.EqualTo("skolenza"), mapping.Key.Name);
        }

        var batch = context.Model.FindEntityType(typeof(StudentFeePaymentBatch))!;
        var item = context.Model.FindEntityType(typeof(StudentFeePaymentBatchItem))!;
        Assert.That(batch.GetForeignKeys().Select(key => key.GetConstraintName()),
            Does.Contain("FK_StudentFeePaymentBatch_Quote"));
        Assert.That(batch.GetForeignKeys().Select(key => key.GetConstraintName()),
            Does.Contain("FK_StudentFeePaymentBatch_SelectedInstallment"));
        Assert.That(item.GetForeignKeys().Select(key => key.GetConstraintName()),
            Does.Contain("FK_StudentFeePaymentBatchItem_Batch"));
        Assert.That(item.GetForeignKeys().Select(key => key.GetConstraintName()),
            Does.Contain("FK_StudentFeePaymentBatchItem_Installment"));
        Assert.That(item.GetForeignKeys().Select(key => key.GetConstraintName()),
            Does.Contain("FK_StudentFeePaymentBatchItem_Payment"));
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
