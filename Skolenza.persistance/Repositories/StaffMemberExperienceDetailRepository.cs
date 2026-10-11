using AutoMapper;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IHashed;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace skolenza.persistance.Repositories
{
    public class StaffMemberExperienceDetailRepository : IStaffMemberExperienceDetailRepository
    {
        private readonly SkolenzaDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<StaffMemberExperienceDetailRepository> _logger;
        private readonly IPasswordService _passwordService;
        private readonly IEncryptionService _encryptionService;

        public StaffMemberExperienceDetailRepository(SkolenzaDbContext context, IMapper mapper, ILogger<StaffMemberExperienceDetailRepository> logger,
            IPasswordService passwordService, IEncryptionService encryptionService)
        {
            this._context = context;
            this._mapper = mapper;
            this._logger = logger;

            _passwordService = passwordService;
            _encryptionService = encryptionService;

        }

        // ===============================
        // 🔹 ADD SINGLE
        // ===============================
        public async Task AddAsync(StaffMemberExperience entity)
        {
            await _context.StaffMemberExperienceDetails.AddAsync(entity);
        }

        // ===============================
        // 🔹 ADD RANGE
        // ===============================
        public async Task AddRangeAsync(List<StaffMemberExperience> entities)
        {
            await _context.StaffMemberExperienceDetails.AddRangeAsync(entities);
        }

        // ===============================
        // 🔹 GET BY EXPERIENCE ID
        // ===============================
        public async Task<List<StaffMemberExperience>> GetByExperienceIdAsync(long experienceId)
        {
            return await _context.StaffMemberExperienceDetails
                .Where(x => x.StaffMemberId == experienceId && !x.IsSoftDeleted)
                .ToListAsync();
        }

        // ===============================
        // 🔹 UPDATE
        // ===============================
        public Task<bool> UpdateAsync(StaffMemberExperience entity)
        {
            _context.StaffMemberExperienceDetails.Update(entity);
            return Task.FromResult(true); // SaveChanges UoW karega
        }

        // ===============================
        // 🔹 SOFT DELETE
        // ===============================
        public Task<bool> SoftDeleteAsync(StaffMemberExperience entity)
        {
            entity.IsSoftDeleted = true;
            entity.IsActive = false;

            _context.StaffMemberExperienceDetails.Update(entity);

            return Task.FromResult(true);
        }

        public Task<StaffMemberExperience?> GetByIdAsync(long id)
        {
            throw new NotImplementedException();
        }
    }
}