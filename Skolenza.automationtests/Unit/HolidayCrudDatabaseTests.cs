using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using skolenza.persistance.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("HolidayCrudDatabase")]
public sealed class HolidayCrudDatabaseTests
{
    [Test]
    public async Task Repository_create_read_update_and_soft_delete_stays_institution_scoped()
    {
        var connectionString = Environment.GetEnvironmentVariable("SKOLENZA_HOLIDAY_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Assert.Ignore("Set SKOLENZA_HOLIDAY_TEST_CONNECTION to run the rollback-only PostgreSQL test.");
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

        var holiday = new Holiday
        {
            InstitutionId = location.InstitutionId,
            CampusId = location.Id,
            HolidayName = "Calendar repository rollback verification",
            HolidayDate = new DateOnly(2099, 12, 30),
            IsOptional = false,
            IsActive = true,
            IsSoftDeleted = false,
            Icon = "bi bi-calendar-event",
            AddedDateTime = DateTime.UtcNow
        };

        try
        {
            Assert.That(await repository.CampusExistsAsync(
                location.InstitutionId, location.Id, CancellationToken.None), Is.True);
            Assert.That(await repository.CampusExistsAsync(
                location.InstitutionId + 100000, location.Id, CancellationToken.None), Is.False);

            await repository.SaveHolidayAsync(holiday, CancellationToken.None);
            Assert.That(holiday.Id, Is.GreaterThan(0));
            Assert.That((await repository.FindConflictingHolidayAsync(
                location.InstitutionId, location.Id, holiday.HolidayDate, null, CancellationToken.None))?.Id,
                Is.EqualTo(holiday.Id));
            Assert.That(await repository.GetInstitutionHolidayAsync(
                location.InstitutionId + 100000, holiday.Id, CancellationToken.None), Is.Null);
            Assert.That((await repository.GetInstitutionHolidaysAsync(
                location.InstitutionId, location.Id, 2099, CancellationToken.None))
                .Any(item => item.Id == holiday.Id), Is.True);

            holiday.Description = "Updated in rollback transaction";
            await repository.SaveHolidayAsync(holiday, CancellationToken.None);
            Assert.That((await repository.GetInstitutionHolidayAsync(
                location.InstitutionId, holiday.Id, CancellationToken.None))?.Description,
                Is.EqualTo("Updated in rollback transaction"));
            Assert.That((await repository.GetInstitutionHolidayAsync(
                location.InstitutionId, holiday.Id, CancellationToken.None))?.Icon,
                Is.EqualTo("bi bi-calendar-event"));

            holiday.IsActive = false;
            await repository.SaveHolidayAsync(holiday, CancellationToken.None);
            Assert.That((await repository.FindConflictingHolidayAsync(
                location.InstitutionId, location.Id, holiday.HolidayDate, null, CancellationToken.None))?.Id,
                Is.EqualTo(holiday.Id), "Inactive rows must still block the same date.");
            Assert.That((await repository.GetInstitutionHolidayForWriteAsync(
                location.InstitutionId, holiday.Id, CancellationToken.None))?.Id, Is.EqualTo(holiday.Id));

            holiday.IsSoftDeleted = true;
            holiday.DeletedDateTime = DateTime.UtcNow;
            await repository.SaveHolidayAsync(holiday, CancellationToken.None);
            Assert.That(await repository.GetInstitutionHolidayAsync(
                location.InstitutionId, holiday.Id, CancellationToken.None), Is.Null);
            Assert.That(await repository.FindConflictingHolidayAsync(
                location.InstitutionId, location.Id, holiday.HolidayDate, null, CancellationToken.None), Is.Null,
                "Soft-deleted rows must allow replacement.");
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }
}
