using skolenza.application.Interfaces.IRepositories;

using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using skolenza.domain.Entity;
using skolenza.application.Exceptions;
using Npgsql;



namespace skolenza.persistance.Repositories
{
    public class HolidayRepository : IHolidayRepository
    {
        private readonly SkolenzaDbContext _context;
        private readonly ILogger<HolidayRepository> _logger;

        public HolidayRepository(SkolenzaDbContext context, ILogger<HolidayRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Task<List<Holiday>> GetInstitutionHolidaysAsync(
            long institutionId,
            long? campusId,
            int? year,
            CancellationToken cancellationToken)
        {
            var query = _context.Holidays.AsNoTracking()
                .Where(holiday => holiday.InstitutionId == institutionId
                    && holiday.IsActive == true
                    && holiday.IsSoftDeleted != true);

            if (campusId.HasValue)
            {
                query = query.Where(holiday => holiday.CampusId == campusId.Value);
            }

            if (year.HasValue)
            {
                query = query.Where(holiday => holiday.HolidayDate.Year == year.Value);
            }

            return query.OrderBy(holiday => holiday.HolidayDate)
                .ThenBy(holiday => holiday.Id)
                .ToListAsync(cancellationToken);
        }

        public Task<Holiday?> GetInstitutionHolidayAsync(
            long institutionId,
            long id,
            CancellationToken cancellationToken)
        {
            return _context.Holidays.FirstOrDefaultAsync(
                holiday => holiday.Id == id
                    && holiday.InstitutionId == institutionId
                    && holiday.IsActive == true
                    && holiday.IsSoftDeleted != true,
                cancellationToken);
        }

        public Task<Holiday?> GetInstitutionHolidayForWriteAsync(
            long institutionId,
            long id,
            CancellationToken cancellationToken)
        {
            return _context.Holidays.FirstOrDefaultAsync(
                holiday => holiday.Id == id
                    && holiday.InstitutionId == institutionId
                    && holiday.IsSoftDeleted != true,
                cancellationToken);
        }

        public Task<bool> CampusExistsAsync(
            long institutionId,
            long campusId,
            CancellationToken cancellationToken)
        {
            return _context.Campuss.AnyAsync(
                location => location.Id == campusId
                    && location.InstitutionId == institutionId
                    && location.IsActive
                    && !location.IsSoftDeleted,
                cancellationToken);
        }

        public Task<Holiday?> FindConflictingHolidayAsync(
            long institutionId,
            long campusId,
            DateOnly date,
            long? excludeId,
            CancellationToken cancellationToken)
        {
            return _context.Holidays.AsNoTracking().FirstOrDefaultAsync(
                holiday => holiday.InstitutionId == institutionId
                    && holiday.CampusId == campusId
                    && holiday.HolidayDate == date
                    && holiday.IsSoftDeleted != true
                    && (!excludeId.HasValue || holiday.Id != excludeId.Value),
                cancellationToken);
        }

        public Task<List<Holiday>> GetInstitutionHolidaysForDuplicateCheckAsync(
            long institutionId,
            CancellationToken cancellationToken)
        {
            return _context.Holidays.AsNoTracking()
                .Where(holiday => holiday.InstitutionId == institutionId && holiday.IsSoftDeleted != true)
                .ToListAsync(cancellationToken);
        }

        public async Task<Holiday> SaveHolidayAsync(
            Holiday holiday,
            CancellationToken cancellationToken)
        {
            if (holiday.Id == 0)
            {
                await _context.Holidays.AddAsync(holiday, cancellationToken);
            }

            await SaveChangesWithHolidayConflictAsync(cancellationToken);
            return holiday;
        }

        public async Task<int> ImportHolidaysAsync(
            IReadOnlyList<Holiday> holidays,
            CancellationToken cancellationToken)
        {
            if (holidays.Count == 0)
            {
                return 0;
            }

            await _context.Holidays.AddRangeAsync(holidays, cancellationToken);
            return await SaveChangesWithHolidayConflictAsync(cancellationToken);
        }

        private async Task<int> SaveChangesWithHolidayConflictAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation,
                    ConstraintName: "UX_Holiday_Institution_Location_Date_NotDeleted"
                })
            {
                throw new ValidationErrorException(
                    "A holiday entry already exists for this location and date. Edit that entry or soft-delete it before creating another.");
            }
        }

        public async Task<List<Holiday>> GetAllHolidaysAsync()
        {
            try
            {
                return await _context.Holidays
                    .Where(x => x.IsActive==true && !x.IsSoftDeleted == true)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetAllHolidaysAsync {ex.Message} -  {ex.InnerException} - {ex.StackTrace}");
                return new List<Holiday>();
            }
        }

        public async Task<IEnumerable<Holiday>> GetHolidaysByInstitutionAsync(long institutionId, int year)
        {
            try
            {
                return await _context.Holidays
                    .Where(x => x.InstitutionId == institutionId && x.HolidayDate.Year == year && x.IsActive == true && x.IsSoftDeleted != true)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetHolidaysByInstitutionAsync for institutionId: {institutionId}, year: {year}");
                return Enumerable.Empty<Holiday>();
            }
        }

        public async Task<IEnumerable<Holiday>> GetHolidaysByCountryAsync(int countryId, int year)
        {
            try
            {
                return await _context.Holidays
                    .Where(x => x.Campus.CountryId == countryId && x.HolidayDate.Year == year && x.IsActive == true && x.IsSoftDeleted != true)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetHolidaysByCountryAsync for countryId: {CountryId}, year: {Year}", countryId, year);
                return Enumerable.Empty<Holiday>();
            }
        }

        public async Task<IEnumerable<Holiday>> GetHolidaysByStateAsync(int countryId, int stateId, int year)
        {
            try
            {
                return await _context.Holidays
                    .Where(x => x.Campus.CountryId == countryId && x.Campus.StateId == stateId && x.HolidayDate.Year == year && x.IsActive == true && x.IsSoftDeleted != true)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetHolidaysByStateAsync for countryId: {CountryId}, stateId: {StateId}, year: {Year}", countryId, stateId, year);
                return Enumerable.Empty<Holiday>();
            }
        }

        public async Task<Holiday?> GetHolidayByIdAsync(long id)
        {
            try
            {
                return await _context.Holidays
                    .FirstOrDefaultAsync(x => x.Id == id && x.IsActive == true && !x.IsSoftDeleted == true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetHolidayByIdAsync for id: {id}");
                return null;
            }
        }

        public async Task AddHolidayAsync(Holiday holiday)
        {
            try
            {
                await _context.Holidays.AddAsync(holiday);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddHolidayAsync");
                throw;
            }
        }

        public async Task UpdateHolidayAsync(Holiday holiday)
        {
            try
            {
                _context.Holidays.Update(holiday);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in UpdateHolidayAsync for id: {holiday.Id}");
                throw;
            }
        }

        public async Task DeleteHolidayAsync(long id, long SoftDeletedById)
        {
            try
            {
                var holiday = await _context.Holidays.FirstOrDefaultAsync(x => x.Id == id && x.IsActive == true && !x.IsSoftDeleted == true);
                if (holiday != null)
                {
                    holiday.IsSoftDeleted = true;
                    holiday.SoftDeletedById = SoftDeletedById;
                    holiday.DeletedDateTime = DateTime.UtcNow;
                    _context.Holidays.Update(holiday);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in DeleteHolidayAsync for id: {id}");
                throw;
            }
        }
    }

}
