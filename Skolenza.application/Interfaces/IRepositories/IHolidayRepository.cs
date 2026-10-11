
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Interfaces.IRepositories
{
    public interface IHolidayRepository
    {
        Task<List<Holiday>> GetInstitutionHolidaysAsync(long institutionId, long? campusId, int? year, CancellationToken cancellationToken);
        Task<Holiday?> GetInstitutionHolidayAsync(long institutionId, long id, CancellationToken cancellationToken);
        Task<Holiday?> GetInstitutionHolidayForWriteAsync(long institutionId, long id, CancellationToken cancellationToken);
        Task<bool> CampusExistsAsync(long institutionId, long campusId, CancellationToken cancellationToken);
        Task<Holiday?> FindConflictingHolidayAsync(long institutionId, long campusId, DateOnly date, long? excludeId, CancellationToken cancellationToken);
        Task<List<Holiday>> GetInstitutionHolidaysForDuplicateCheckAsync(long institutionId, CancellationToken cancellationToken);
        Task<Holiday> SaveHolidayAsync(Holiday holiday, CancellationToken cancellationToken);
        Task<int> ImportHolidaysAsync(IReadOnlyList<Holiday> holidays, CancellationToken cancellationToken);
        Task<List<Holiday>> GetAllHolidaysAsync();
        Task<IEnumerable<Holiday>> GetHolidaysByInstitutionAsync(long institutionId, int year);
        Task<IEnumerable<Holiday>> GetHolidaysByCountryAsync(int countryId, int year);
        Task<IEnumerable<Holiday>> GetHolidaysByStateAsync(int countryId, int stateId, int year);
        Task<Holiday?> GetHolidayByIdAsync(long id);
        Task AddHolidayAsync(Holiday holiday);
        Task UpdateHolidayAsync(Holiday holiday);
        Task DeleteHolidayAsync(long id, long SoftDeletedById);
         
    }
}
