using skolenza.application.Interfaces.IRepositories;

using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using skolenza.domain.Entity;

namespace skolenza.persistance.Repositories
{
    public class TravelRepository : ITravelRepository
    {
        private readonly SkolenzaDbContext _context;
        private readonly ILogger<TravelRepository> _logger;

        public TravelRepository(SkolenzaDbContext context, ILogger<TravelRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<TravelMode>> CreateTravelTypeAsync(TravelMode travelMode)
        {
            try
            {
                if (travelMode == null) throw new ArgumentNullException(nameof(travelMode));

                await _context.TravelModes.AddAsync(travelMode);
                await _context.SaveChangesAsync();

                _logger.LogInformation("TravelMode type added successfully: {TravelModeName}", travelMode.TravelModeName);
                return await GetAllTravelModeTypeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating TravelMode type.");
                throw;
            }

        }

        public Task<bool> DeleteClientTypeAsync(int Id)
        {
            throw new NotImplementedException();
        }

        public async Task<List<TravelMode>> GetAllTravelModeTypeAsync()
        {
                try
                {
                    _logger.LogInformation("Fetching all TravelMode types from the database...");
                    var travelModes = await _context.TravelModes.ToListAsync();

                    if (!travelModes.Any())
                    {
                        _logger.LogWarning("No TravelMode types found in the database.");
                    }

                    return travelModes;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while fetching TravelMode types.");
                    return new List<TravelMode>();
                }
            }

        public Task<TravelMode> GetClientTypeByIdAsync(int Id)
        {
            throw new NotImplementedException();
        }

        public async Task<List<TravelMode>> UpdateClientTypeAsync(TravelMode TravelModeType)
        {
            try
            {
                var existingTravelMode = await _context.TravelModes.FirstOrDefaultAsync(l => l.Id == TravelModeType.Id);
                if (existingTravelMode == null)
                {
                    _logger.LogWarning("TravelMode type with ID {TravelModeId} not found.", TravelModeType.Id);
                    return await GetAllTravelModeTypeAsync();
                }

                existingTravelMode.TravelModeName = TravelModeType.TravelModeName;
                existingTravelMode.Description = TravelModeType.Description;
                existingTravelMode.IsActive = TravelModeType.IsActive;

                await _context.SaveChangesAsync();
                _logger.LogInformation("TravelMode type with ID {TravelModeId} updated successfully.", TravelModeType.Id);
                return await GetAllTravelModeTypeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating TravelMode type with ID {TravelModeId}.", TravelModeType.Id);
                throw;
            }
        }
    }
}
