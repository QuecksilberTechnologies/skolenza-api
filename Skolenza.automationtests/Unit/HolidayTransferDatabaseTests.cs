using System.Reflection;
using System.Text;
using skolenza.application.Common.Models.Security;
using skolenza.application.DTOs.Holiday;
using skolenza.application.Exceptions;
using skolenza.application.Features.HolidayCmd;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.persistance.Data.Context;
using skolenza.persistance.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("HolidayTransferDatabase")]
public sealed class HolidayTransferDatabaseTests
{
    [Test]
    public async Task Csv_import_export_duplicate_and_invalid_row_are_transactionally_safe()
    {
        var connectionString = Environment.GetEnvironmentVariable("SKOLENZA_HOLIDAY_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Ignore("Set SKOLENZA_HOLIDAY_TEST_CONNECTION for the rollback-only import/export test.");
        }

        var options = new DbContextOptionsBuilder<SkolenzaDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var context = new SkolenzaDbContext(options);
        await using var transaction = await context.Database.BeginTransactionAsync();
        var location = await context.Campuss.AsNoTracking()
            .Where(item => item.IsActive && !item.IsSoftDeleted)
            .OrderBy(item => item.Id)
            .FirstAsync();
        var repository = new HolidayRepository(
            context,
            NullLogger<HolidayRepository>.Instance);
        var actor = new CommonDecodedResult
        {
            Success = true,
            InstitutionId = location.InstitutionId,
            LoggedInStaffMemberId = 4,
            RoleId = 2
        };
        var common = Proxy<ICommonRequestService>((_, _) => Task.FromResult(actor));
        var unit = Proxy<IUnitOfWork>((method, _) => method.Name == "get_HolidayRepository"
            ? repository
            : throw new AssertionException($"Unexpected unit request: {method.Name}"));
        var importer = new ImportHolidaysCommandHandler(unit, common);
        var exporter = new ExportHolidaysQueryHandler(unit, common);
        var csv = "CampusId,HolidayName,HolidayDate,IsOptional,Description,Icon\r\n"
            + $"{location.Id},Calendar Import Verification,2099-12-29,false,Annual office holiday,bi bi-calendar-event\r\n";

        try
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
            var file = new FormFile(stream, 0, stream.Length, "File", "holidays.csv");
            var created = await importer.Handle(new ImportHolidaysCommand(new ImportHolidayRequestDTO
            {
                File = file
            }), CancellationToken.None);
            Assert.Multiple(() =>
            {
                Assert.That(created.Data.TotalRows, Is.EqualTo(1));
                Assert.That(created.Data.CreatedCount, Is.EqualTo(1));
                Assert.That(created.Data.SkippedExistingCount, Is.Zero);
            });

            var exported = await exporter.Handle(new ExportHolidaysQuery(new BasicRequestDTO
            {
                CampusId = location.Id,
                HolidayYear = 2099
            }), CancellationToken.None);
            var exportedText = Encoding.UTF8.GetString(exported);
            Assert.That(exportedText, Does.Contain("Calendar Import Verification"));
            Assert.That(exportedText, Does.Contain("2099-12-29"));
            Assert.That(exportedText, Does.Contain("bi bi-calendar-event"));

            var repeatError = Assert.ThrowsAsync<ValidationErrorException>(async () =>
                await importer.Handle(new ImportHolidaysCommand(new ImportHolidayRequestDTO
                {
                    PastedText = csv.Replace("Calendar Import Verification", "Different Holiday Same Date")
                }), CancellationToken.None));
            Assert.That(repeatError!.Errors.Any(error => error.Contains("already exists")), Is.True);

            var stored = await context.Holidays.SingleAsync(item =>
                item.InstitutionId == location.InstitutionId && item.HolidayName == "Calendar Import Verification");
            Assert.That(stored.Icon, Is.EqualTo("bi bi-calendar-event"));
            stored.IsActive = false;
            await context.SaveChangesAsync();
            Assert.ThrowsAsync<ValidationErrorException>(async () =>
                await importer.Handle(new ImportHolidaysCommand(new ImportHolidayRequestDTO
                {
                    PastedText = csv
                }), CancellationToken.None));

            var invalid = csv.Replace("2099-12-29", "not-a-date");
            Assert.ThrowsAsync<ValidationErrorException>(async () =>
                await importer.Handle(new ImportHolidaysCommand(new ImportHolidayRequestDTO
                {
                    PastedText = invalid
                }), CancellationToken.None));
            Assert.That(await context.Holidays.CountAsync(item =>
                item.InstitutionId == location.InstitutionId && item.HolidayName == "Calendar Import Verification"),
                Is.EqualTo(1));

            stored.IsSoftDeleted = true;
            await context.SaveChangesAsync();
            var replacement = await importer.Handle(new ImportHolidaysCommand(new ImportHolidayRequestDTO
            {
                PastedText = csv
            }), CancellationToken.None);
            Assert.That(replacement.Data.CreatedCount, Is.EqualTo(1));
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> invoke) where T : class
    {
        var proxy = DispatchProxy.Create<T, BulkImportPermissionTests.TestProxy>();
        ((BulkImportPermissionTests.TestProxy)(object)proxy).InvokeMethod = invoke;
        return proxy;
    }
}
