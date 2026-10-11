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
    public class StaffMemberExperienceDocumentRepository : IStaffMemberExperienceDocumentRepository
    {
        private readonly SkolenzaDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<StaffMemberExperienceDocumentRepository> _logger;
        private readonly IPasswordService _passwordService;
        private readonly IEncryptionService _encryptionService;

        public StaffMemberExperienceDocumentRepository(SkolenzaDbContext context, IMapper mapper, ILogger<StaffMemberExperienceDocumentRepository> logger,
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
        public async Task AddAsync(StaffMemberExperienceDocument entity)
        {
            await _context.StaffMemberExperienceDocuments.AddAsync(entity);
        }

        // ===============================
        // 🔹 ADD RANGE
        // ===============================
        public async Task AddRangeAsync(List<StaffMemberExperienceDocument> entities)
        {
            await _context.StaffMemberExperienceDocuments.AddRangeAsync(entities);
        }

        // ===============================
        // 🔹 GET BY DETAIL ID
        // ===============================
        public async Task<List<StaffMemberExperienceDocument>> GetByDetailIdAsync(long detailId)
        {
            return await _context.StaffMemberExperienceDocuments
                .Where(x => x.StaffMemberExperience.Id == detailId && !x.IsSoftDeleted)
                .ToListAsync();
        }
        public async Task<StaffMemberExperienceDocument?> GetSingleByDetailIdAsync(long detailId)
        {
            return await _context.StaffMemberExperienceDocuments
                .Include(x => x.StaffMemberExperience)
                .FirstOrDefaultAsync(x => x.StaffMemberExperience.Id == detailId && !x.IsSoftDeleted);
        }

        // ===============================
        // 🔹 SOFT DELETE
        // ===============================
        public Task<bool> SoftDeleteAsync(StaffMemberExperienceDocument entity)
        {
            entity.IsSoftDeleted = true;
            entity.IsActive = false;

            _context.StaffMemberExperienceDocuments.Update(entity);

            return Task.FromResult(true); // ✅ SaveChanges UoW karega
        }
    }
}
