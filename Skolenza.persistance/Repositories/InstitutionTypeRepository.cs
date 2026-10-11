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
    public class InstitutionTypeRepository: IInstitutionTypeRepository
    {
        private readonly SkolenzaDbContext _context;
        private ILogger<InstitutionTypeRepository> _logger;
        public InstitutionTypeRepository(SkolenzaDbContext context, ILogger<InstitutionTypeRepository> logger)
        {
            this._context = context;
            this._logger = logger;
        }
        public async Task<List<InstitutionType>> GetAllActiveInstitutionTypesAsync()
        {
            try
            {
                _logger.LogInformation("Fetching all active Institution types.");
                return await _context.InstitutionTypes
                                     .Where(i => i.IsActive)
                                     .OrderBy(i => i.InstitutionTypeName)
                                     .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching Institution types.");
                return new List<InstitutionType>();
            }
        }

        public async Task<InstitutionType?> GetInstitutionTypeByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Fetching Institution type by ID: {Id}", id);
                return await _context.InstitutionTypes.FirstOrDefaultAsync(i => i.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching Institution type by ID: {Id}", id);
                return null;
            }
        }


        public async Task<InstitutionType?> GetFirstActiveInstitutionTypeAsync()
        {
            try
            {
              
                return await _context.InstitutionTypes.FirstOrDefaultAsync(i => i.IsActive == true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching Institution type.");
                return null;
            }
        }

        public async Task AddInstitutionTypeAsync(InstitutionType institutionType)
        {
            try
            {
                _logger.LogInformation("Adding new Institution type: {Name}", institutionType.InstitutionTypeName);
                await _context.InstitutionTypes.AddAsync(institutionType);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while adding Institution type: {Name}", institutionType.InstitutionTypeName);
            }
        }

        public async Task UpdateInstitutionTypeAsync(InstitutionType institutionType)
        {
            try
            {
                _logger.LogInformation("Updating Institution type ID: {Id}", institutionType.Id);
                _context.InstitutionTypes.Update(institutionType);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating Institution type ID: {Id}", institutionType.Id);
            }
        }

        public async Task<bool> InstitutionTypeExistsAsync(int id)
        {
            try
            {
                _logger.LogInformation("Checking if Institution type exists: {Id}", id);
                return await _context.InstitutionTypes.AnyAsync(i => i.Id == id && i.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existence for Institution type ID: {Id}", id);
                return false;
            }
        }

    }
}
