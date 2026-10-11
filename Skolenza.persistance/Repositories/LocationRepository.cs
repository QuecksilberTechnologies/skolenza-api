// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Persists location data and projects location option query results.
// ================================================================

using skolenza.application.DTOS.Location;
using skolenza.application.Constants;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace skolenza.persistance.Repositories
{
    /// <summary>
    /// Provides persistence operations for countries, states, districts, and their option projections.
    /// </summary>
    public class LocationRepository : ILocationRepository
    {
        #region Fields

        private readonly SkolenzaDbContext _context;
        private readonly ILogger<LocationRepository> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationRepository"/> class.
        /// </summary>
        public LocationRepository(
            SkolenzaDbContext context,
            ILogger<LocationRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Queries

        /// <summary>
        /// Gets all persisted countries.
        /// </summary>
        public Task<List<Country>> GetAllAsync() => _context.Countries.ToListAsync();

        /// <summary>
        /// Gets a country by identifier.
        /// </summary>
        public async Task<Country> GetByIdAsync(int id) => await _context.Countries.FindAsync(id);

        /// <summary>
        /// Projects active countries without constructing an API response.
        /// </summary>
        public async Task<List<GetCountryOptionResponseDTO>> GetCountryOptionAsync(GetCountryOptionRequestDTO dto)
        {
            try
            {
                _logger.LogInformation("Fetching country options for date: {Date}.", dto.TodaysDate!.Value.Date);

                return await _context.Countries
                    .AsNoTracking()
                    .Where(country => country.IsActive == true)
                    .Select(country => new GetCountryOptionResponseDTO
                    {
                        Id = country.Id,
                        CountryName = country.CountryName ?? string.Empty,
                        CountryCode = country.CountryCode ?? string.Empty,
                        STDCode = country.Stdcode,
                        IsActive = country.IsActive
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching country options.");
                throw;
            }
        }

        /// <summary>
        /// Projects active states for a country without constructing an API response.
        /// </summary>
        public async Task<List<GetStateOptionResponseDTO>> GetStateOptionAsync(GetStateOptionRequestDTO dto)
        {
            try
            {
                return await _context.States
                    .AsNoTracking()
                    .Where(state => state.CountryId == dto.CountryId && state.IsActive == true)
                    .Select(state => new GetStateOptionResponseDTO
                    {
                        Id = state.Id,
                        CountryId = state.CountryId,
                        CountryCode = state.Country != null
                            ? state.Country.CountryCode ?? string.Empty
                            : string.Empty,
                        STDCode = state.Country != null ? state.Country.Stdcode : string.Empty,
                        StateName = state.StateName ?? string.Empty,
                        StateCode = state.StateCode,
                        IsActive = state.IsActive
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching state options for country {CountryId}.", dto.CountryId);
                throw;
            }
        }

        /// <summary>
        /// Determines whether an active state exists for the supplied identifier.
        /// </summary>
        public Task<bool> IsActiveStateAsync(int stateId) => _context.States
            .AsNoTracking()
            .AnyAsync(state => state.Id == stateId && state.IsActive == true);

        public Task<bool> IsActiveDistrictAsync(int districtId) => _context.Districts
            .AsNoTracking()
            .AnyAsync(district => district.Id == districtId && district.IsActive);

        /// <summary>
        /// Projects active districts for a state without constructing an API response.
        /// </summary>
        public async Task<List<GetDistrictOptionResponseDTO>> GetDistrictOptionAsync(GetDistrictOptionRequestDTO dto)
        {
            try
            {
                return await _context.Districts
                    .AsNoTracking()
                    .Where(district => district.StateId == dto.StateId && district.IsActive == true)
                    .Select(district => new GetDistrictOptionResponseDTO
                    {
                        Id = district.Id,
                        StateId = district.StateId,
                        CountryCode = district.State != null && district.State.Country != null
                            ? district.State.Country.CountryCode ?? string.Empty
                            : string.Empty,
                        DistrictName = district.DistrictName ?? string.Empty,
                        DistrictCode = district.DistrictCode,
                        IsActive = district.IsActive
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching district options for state {StateId}.", dto.StateId);
                throw;
            }
        }

        public async Task<List<GetLocalityOptionResponseDTO>> GetLocalityOptionAsync(GetLocalityOptionRequestDTO dto)
        {
            return await _context.Localities
                .AsNoTracking()
                .Where(locality => locality.DistrictId == dto.DistrictId && locality.IsActive == true)
                .OrderBy(locality => locality.LocalityName)
                .Select(locality => new GetLocalityOptionResponseDTO
                {
                    Id = locality.Id,
                    DistrictId = locality.DistrictId,
                    StateId = locality.StateId,
                    LocalityTypeId = locality.LocalityTypeId,
                    LocalityName = locality.LocalityName,
                    LocalityCode = locality.LocalityCode,
                    PostalCode = locality.PostalCode,
                    LocalityTypeName = locality.LocalityType.TypeName,
                    IsActive = locality.IsActive
                })
                .ToListAsync();
        }

        public async Task<List<GetLocalityTypeOptionResponseDTO>> GetLocalityTypeOptionAsync(GetLocalityTypeOptionRequestDTO dto)
        {
            return await _context.LocalityTypes
                .AsNoTracking()
                .Where(localityType => localityType.IsActive)
                .Where(localityType => LocalityTypeConstants.Ids.Contains(localityType.Id))
                .OrderBy(localityType => localityType.Id)
                .Select(localityType => new GetLocalityTypeOptionResponseDTO
                {
                    Id = localityType.Id,
                    TypeName = localityType.TypeName
                })
                .ToListAsync();
        }

        #endregion

        #region Commands

        /// <summary>
        /// Adds a country and persists the change.
        /// </summary>
        public async Task AddAsync(Country country)
        {
            await _context.Countries.AddAsync(country);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Updates a country and persists the change.
        /// </summary>
        public async Task UpdateAsync(Country country)
        {
            _context.Countries.Update(country);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes a country when it exists.
        /// </summary>
        public async Task DeleteAsync(int id)
        {
            var country = await _context.Countries.FindAsync(id);
            if (country == null)
            {
                return;
            }

            _context.Countries.Remove(country);
            await _context.SaveChangesAsync();
        }

        #endregion
    }
}
