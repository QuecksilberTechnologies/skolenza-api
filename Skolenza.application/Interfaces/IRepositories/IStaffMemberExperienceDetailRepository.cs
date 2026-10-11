using skolenza.domain.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.Interfaces.IRepositories
{
    public interface IStaffMemberExperienceDetailRepository
    {
        // 🔹 CREATE
        Task AddAsync(StaffMemberExperience entity);
        Task AddRangeAsync(List<StaffMemberExperience> entities);

        // 🔹 UPDATE
        Task<bool> UpdateAsync(StaffMemberExperience entity);

        // 🔹 DELETE (Soft Delete)
        Task<bool> SoftDeleteAsync(StaffMemberExperience entity);

        // 🔹 GET (By Parent)
        Task<List<StaffMemberExperience>> GetByExperienceIdAsync(long experienceId);

        // 🔹 GET SINGLE (optional but useful)
        Task<StaffMemberExperience?> GetByIdAsync(long id);
    }
}
