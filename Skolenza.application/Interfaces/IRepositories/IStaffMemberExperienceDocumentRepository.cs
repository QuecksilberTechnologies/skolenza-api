using skolenza.domain.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.Interfaces.IRepositories
{
    public interface IStaffMemberExperienceDocumentRepository
    {
        Task AddAsync(StaffMemberExperienceDocument entity);
        Task AddRangeAsync(List<StaffMemberExperienceDocument> entities);

        Task<bool> SoftDeleteAsync(StaffMemberExperienceDocument entity);

         Task<List<StaffMemberExperienceDocument>> GetByDetailIdAsync(long detailId);
        Task<StaffMemberExperienceDocument> GetSingleByDetailIdAsync(long detailId);
    }
}
