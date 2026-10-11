using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Interfaces.IRepositories
{
    
    using System.Collections.Generic;
    using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

    public interface ICandidateCategorySkillRepository
    {
        // Naya skill add karne ke liye
        Task<int> AddSkillsAsync(List<CandidateCategorySkill> candidateCategorySkill);

        // Kisi skill ko update karne ke liye
        Task UpdateSkillAsync(int skillId, string skillName);

        // Kisi specific skill ko retrieve karne ke liye
        Task<string> GetSkillByIdAsync(int skillId);

        // Kisi candidate ke saare skills retrieve karne ke liye
        Task<IEnumerable<string>> GetSkillsByCandidateIdAsync(int candidateId);

        // Kisi category ke andar ke saare skills retrieve karne ke liye
        Task<IEnumerable<string>> GetSkillsByCategoryIdAsync(int categoryId);

        // Skill ko delete karne ke liye
        Task DeleteSkillAsync(int skillId);

        // Check karne ke liye ki koi skill already exist karti hai ya nahi
        Task<bool> SkillExistsAsync(int candidateId, int categoryId, string skillName);
    }


}
