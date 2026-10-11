using skolenza.application.Interfaces.IRepositories;

using skolenza.persistance.Data.Context;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using skolenza.domain.Entity;

namespace skolenza.persistance.Repositories
{
    public class CandidateCategorySkillRepository : ICandidateCategorySkillRepository
    {
        private SkolenzaDbContext _context;
        private ILogger _logger;


        public CandidateCategorySkillRepository(SkolenzaDbContext context, ILogger<CandidateCategorySkillRepository> logger)
        {
            this._context = context;
            this._logger = logger;
        }
        public async Task<int> AddSkillsAsync(List<CandidateCategorySkill> candidateCategorySkills)
        {
            try
            {
                // Multiple records add karne ke liye AddRangeAsync ka use karein
                await _context.CandidateCategorySkills.AddRangeAsync(candidateCategorySkills);

                // SaveChangesAsync inserted records ka count return karega
                int recordsInserted = await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Successfully added {Count} skills for CandidateId {CandidateId}.",
                    recordsInserted,
                    candidateCategorySkills.FirstOrDefault()?.CandidateId ?? 0
                );

                return recordsInserted;  // Inserted records ka count return karenge
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while adding skills for CandidateId {CandidateId}.",
                    candidateCategorySkills.FirstOrDefault()?.CandidateId ?? 0
                );

                throw;
            }
        }

       

        public Task DeleteSkillAsync(int skillId)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetSkillByIdAsync(int skillId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> GetSkillsByCandidateIdAsync(int candidateId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> GetSkillsByCategoryIdAsync(int categoryId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SkillExistsAsync(int candidateId, int categoryId, string skillName)
        {
            throw new NotImplementedException();
        }

        public Task UpdateSkillAsync(int skillId, string skillName)
        {
            throw new NotImplementedException();
        }
    }
}
