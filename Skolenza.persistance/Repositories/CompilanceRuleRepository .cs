
using skolenza.application.DTOS.Compliances.ComplianceRule;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace skolenza.infrastructure.Repositories
{
    public class CompilanceRuleRepository : ICompilanceRuleRepository
    {
        private readonly SkolenzaDbContext _context;
        private readonly ILogger<CompilanceRuleRepository> _logger;

        public CompilanceRuleRepository(
            SkolenzaDbContext context,
            ILogger<CompilanceRuleRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        // 🔥 CORE ENGINE METHOD
        public async Task<ComplianceRule?> GetRuleAsync(
         int complianceTypeId,
         int countryId,
         int? stateId,
         long? institutionId,
         DateOnly effectiveFrom)
        {
            try
            {
                var query = _context.ComplianceRule
                    .Where(x =>
                        x.ComplianceTypeId == complianceTypeId &&
                        x.CountryId == countryId &&
                        (x.StateId == stateId || x.StateId == null) &&
                        (x.InstitutionId == institutionId || x.InstitutionId == null) &&
                        x.IsActive == true &&
                        x.IsSoftDeleted != true &&
                        x.EffectiveFrom <= effectiveFrom &&
                        (x.EffectiveTo == null || x.EffectiveTo >= effectiveFrom)
                    );

                var result = await query
                    // PostgreSQL DESC puts NULL first; rank specificity explicitly.
                    .OrderByDescending(x => x.InstitutionId.HasValue)
                    .ThenByDescending(x => x.StateId.HasValue)
                    .ThenByDescending(x => x.Priority ?? 0)
                    .ThenByDescending(x => x.EffectiveFrom)
                    .ThenByDescending(x => x.Id)
                    .FirstOrDefaultAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching compliance rule");
                throw;
            }
        }
        public async Task<ComplianceRule> GetByIdAsync(long id)
        {
            return await _context.ComplianceRule
                .FirstOrDefaultAsync(x => x.Id == id && x.IsSoftDeleted != true);
        }

        public async Task AddAsync(ComplianceRule entity)
        {
            await _context.ComplianceRule.AddAsync(entity);
        }

        public async Task UpdateAsync(ComplianceRule entity)
        {
            _context.ComplianceRule.Update(entity);
        }

        public async Task<bool> ExistsAsync(int complianceTypeId, int countryId, int? stateId, long? institutionId, DateOnly effectiveFrom)
        {
            try
            {
                return await _context.ComplianceRule.AnyAsync(x =>
                    x.ComplianceTypeId == complianceTypeId &&
                    x.CountryId == countryId &&
                    x.StateId == stateId &&
                    x.InstitutionId == institutionId &&
                    x.EffectiveFrom == effectiveFrom &&
                    x.IsSoftDeleted != true
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExistsAsync");
                throw;
            }
        }
    }
}
