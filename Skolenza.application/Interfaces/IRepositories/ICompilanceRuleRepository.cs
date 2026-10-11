using skolenza.application.DTOS.Compliances.ComplianceRule;
using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IRepositories
{
    public interface ICompilanceRuleRepository
    {

        Task<ComplianceRule?> GetRuleAsync(
                  int complianceTypeId,
                  int countryId,
                  int? stateId,
                  long? institutionId, DateOnly effectiveFrom);
        Task<bool>  ExistsAsync(int complianceTypeId, int countryId, int? stateId, long? institutionId, DateOnly effectiveFrom);
        Task<ComplianceRule> GetByIdAsync(long id);

        Task AddAsync(ComplianceRule entity);

        Task UpdateAsync(ComplianceRule entity);
    }


}
