// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Persists gender data and projects gender option query results.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Gender;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace skolenza.persistance.Repositories
{
    /// <summary>
    /// Provides persistence operations for gender data.
    /// </summary>
    public class GenderRepository : IGenderRepository
    {
        #region Fields

        private readonly SkolenzaDbContext _context;
        private readonly ILogger<GenderRepository> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="GenderRepository"/> class.
        /// </summary>
        public GenderRepository(
            SkolenzaDbContext context,
            ILogger<GenderRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region Queries

        /// <summary>
        /// Gets all persisted gender entities.
        /// </summary>
        public async Task<IEnumerable<Gender>> GetAllAsync()
        {
            try
            {
                var genders = await _context.Genders.ToListAsync();
                _logger.LogInformation("Retrieved {Count} genders.", genders.Count);

                return genders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching genders.");
                throw;
            }
        }

        /// <summary>
        /// Projects available genders for an option control without constructing an API response.
        /// </summary>
        /// <param name="dto">The option query criteria.</param>
        /// <returns>The available gender option projections.</returns>
        public async Task<List<GetGenderOptionResponseDTO>> GetOptionAsync(GetOptionRequestDTO dto)
        {
            try
            {
                if (dto.TodaysDate.HasValue)
                {
                    _logger.LogInformation(
                        "Fetching gender options for date: {Date}",
                        dto.TodaysDate.Value.Date);
                }

                return await _context.Genders
                    .AsNoTracking()
                    .Select(gender => new GetGenderOptionResponseDTO
                    {
                        Id = gender.Id,
                        GenderName = gender.GenderName
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching gender options.");
                throw;
            }
        }

        public Task<Gender> GetByIdAsync(long id) => throw new NotImplementedException();

        #endregion

        #region Commands

        public Task AddAsync(Gender gender) => throw new NotImplementedException();

        public Task UpdateAsync(Gender gender) => throw new NotImplementedException();

        public Task DeleteAsync(long id) => throw new NotImplementedException();

        #endregion
    }
}
