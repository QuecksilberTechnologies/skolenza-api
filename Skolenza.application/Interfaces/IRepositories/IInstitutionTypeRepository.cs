
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Interfaces.IRepositories
{
    public interface IInstitutionTypeRepository
    {
        Task<List<InstitutionType>> GetAllActiveInstitutionTypesAsync();
        Task<InstitutionType?> GetInstitutionTypeByIdAsync(int id);
        Task<InstitutionType?> GetFirstActiveInstitutionTypeAsync();
        Task AddInstitutionTypeAsync(InstitutionType institutionType);
        Task UpdateInstitutionTypeAsync(InstitutionType institutionType);
        Task<bool> InstitutionTypeExistsAsync(int id);

    }
}
