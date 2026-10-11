using skolenza.application.DTOS.Institution;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Common.Helpers;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace skolenza.persistance.Repositories
{
    public partial class InstitutionStaffCodePatternRepository : IInstitutionStaffCodePatternRepository
    {
        private readonly SkolenzaDbContext _context;
        private readonly ILogger<InstitutionStaffCodePatternRepository> _logger;
       

        public InstitutionStaffCodePatternRepository(
            SkolenzaDbContext context,
            ILogger<InstitutionStaffCodePatternRepository> logger
             )
        {
            _context = context;
            _logger = logger;
            
        }

        public async Task<GetStaffMemberCodePatternResponseDTO?> GetInstitutionStaffMemberCodePatternAsync(long institutionId, bool isActive)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching staffMember code pattern for InstitutionId: {InstitutionId}", institutionId);

                var institutionParam = new NpgsqlParameter("p_institutionid", institutionId);

                var result = await _context
     .Set<GetStaffMemberCodePatternResponseDTO>()
     .FromSqlRaw(
         @"SELECT * FROM skolenza.""GetStaffMemberCodePatternByInstitution""(@p_institutionid)",
         institutionParam)
     .AsNoTracking()
     .FirstOrDefaultAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching staffMember code pattern for InstitutionId: {InstitutionId}",
                    institutionId);

                return null;
            }
        }

        /// <summary>
        /// Retrieves the active pattern as a tracked entity so its issued sequence can be preserved during configuration updates.
        /// </summary>
        public Task<StaffMemberCodePattern?> GetActivePatternForUpdateAsync(long institutionId, CancellationToken cancellationToken = default)
        {
            return _context.StaffMemberCodePatterns
                .Where(pattern => pattern.InstitutionId == institutionId && pattern.IsActive)
                .OrderBy(pattern => pattern.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // ============================================================
        // 2️⃣ CREATE PATTERN
        // ============================================================
        public async Task<bool> CreatePatternAsync(StaffMemberCodePattern entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                await _context.StaffMemberCodePatterns.AddAsync(entity);

                _logger.LogInformation(
                    "StaffMember code pattern added to DbContext for InstitutionId: {InstitutionId}",
                    entity.InstitutionId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error while adding staffMember code pattern to DbContext for InstitutionId: {InstitutionId}",
                    entity?.InstitutionId);
                throw;
            }
        }

        // ============================================================
        // 3️⃣ UPDATE PATTERN
        // ============================================================
        public async Task<bool> UpdatePatternAsync(StaffMemberCodePattern entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                _context.StaffMemberCodePatterns.Update(entity);

                _logger.LogInformation(
                    "StaffMember code pattern marked for update for PatternId: {PatternId}",
                    entity.Id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error while updating staffMember code pattern for PatternId: {PatternId}",
                    entity?.Id);
                throw;
            }
        }

        // ============================================================
        // 4️⃣ DEACTIVATE ALL OTHER PATTERNS (only 1 active allowed)
        // ============================================================
        public async Task<bool> DeactivateExistingPatternsAsync(long institutionId)
        {
            try
            {
                var activePatterns = await _context.StaffMemberCodePatterns
                    .Where(x => x.InstitutionId == institutionId && x.IsActive)
                    .ToListAsync();

                if (!activePatterns.Any())
                    return true;

                foreach (var p in activePatterns)
                {
                    p.IsActive = false;
                    p.UpdatedDateTime = DateTime.UtcNow;
                }

                _logger.LogInformation(
                    "Existing active staffMember code patterns marked inactive for InstitutionId: {InstitutionId}",
                    institutionId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error deactivating existing patterns for InstitutionId: {InstitutionId}",
                    institutionId);
                throw;
            }
        }

        // ============================================================
        // 5️⃣ ATOMIC SEQUENCE INCREMENT (Transaction Safe)
        // ============================================================
        public async Task<int> IncrementAndGetNextRunningNumberAsync(long institutionId)
        {
            try
            {
                var pattern = await _context.StaffMemberCodePatterns
                    .FirstOrDefaultAsync(x => x.InstitutionId == institutionId && x.IsActive);

                if (pattern == null)
                    throw new Exception("Active staffMember code pattern not found.");

                pattern.LastUsedNumber += 1;
                pattern.UpdatedDateTime = DateTime.UtcNow;

                _logger.LogInformation(
                    "StaffMember code running number incremented in DbContext for InstitutionId: {InstitutionId}, NextNumber: {NextNumber}",
                    institutionId,
                    pattern.LastUsedNumber);

                return pattern.LastUsedNumber;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error incrementing running number for InstitutionId: {InstitutionId}",
                    institutionId);
                throw;
            }
        }

        // ============================================================
        // 6️⃣ GENERATE FINAL STAFF_MEMBER CODE + UPDATE LastUsedNumber
        // ============================================================
        public async Task<string> GenerateStaffMemberCodeAsync(long institutionId, int? departmentId = null, DateTime? joiningDate = null)
        {
            try
            {
                if (_context.Database.CurrentTransaction is null)
                {
                    throw new InvalidOperationException("StaffMember code allocation requires the staffMember creation transaction.");
                }

                await LockInstitutionCodeAllocationAsync(institutionId, CancellationToken.None);
                var pattern = await _context.StaffMemberCodePatterns
                    .FromSqlInterpolated($"SELECT * FROM skolenza.\"StaffMemberCodePattern\" WHERE \"InstitutionId\" = {institutionId} AND \"IsActive\" = TRUE FOR UPDATE")
                    .SingleOrDefaultAsync();

                if (pattern == null)
                    throw new Exception("StaffMember code pattern not configured for institution.");

                // A tracked pattern may have been read before another allocator committed.
                await _context.Entry(pattern).ReloadAsync();
                int nextSeq = checked(pattern.LastUsedNumber + 1);
                var allocatedAt = DateTime.UtcNow;
                await _context.StaffMemberCodePatterns.Where(item => item.Id == pattern.Id)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(item => item.LastUsedNumber, nextSeq)
                        .SetProperty(item => item.UpdatedDateTime, allocatedAt));
                await _context.Entry(pattern).ReloadAsync();

                string staffMemberCode = StaffMemberCodePatternFormatter.Format(
                    pattern,
                    joiningDate ?? DateTime.UtcNow,
                    departmentId,
                    nextSeq);

                _logger.LogInformation(
                    "StaffMember code generated in DbContext for InstitutionId: {InstitutionId}, StaffMemberCode: {StaffMemberCode}",
                    institutionId,
                    staffMemberCode);

                return staffMemberCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error generating staffMember code for InstitutionId: {InstitutionId}",
                    institutionId);
                throw;
            }
        }
    }

}
