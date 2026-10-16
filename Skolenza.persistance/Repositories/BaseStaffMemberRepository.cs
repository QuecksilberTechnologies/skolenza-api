// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Provides persistence operations for Base StaffMember records.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers.PercentageHelper;
using skolenza.application.Constants;
using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.application.DTOS.Staff.CompletionPercentage;
using skolenza.application.DTOS.Staff.Contact;
using skolenza.application.DTOS.Staff.Education;
using skolenza.application.DTOS.Pagination;
using skolenza.application.Extentions;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IHashed;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace skolenza.persistance.Repositories
{

    /// <summary>
    /// Provides persistence operations for BaseStaffMember records.
    /// </summary>
    public class BaseStaffMemberRepository : IBaseStaffMemberRepository
    {
        private readonly SkolenzaDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<BaseStaffMemberRepository> _logger;

        private readonly IPasswordService _passwordService;
        private readonly IEncryptionService _encryptionService;

        #region Operational Section Defaults

        public async Task<bool> IsOperationalSectionEditAllowedAsync(long institutionId, string moduleCode, CancellationToken ct)
        {
            return await _context.Set<InstitutionStaffMemberSectionDefault>()
                .AnyAsync(item => item.InstitutionId == institutionId && item.ModuleCode == moduleCode && item.IsEditAllowed, ct);
        }

        public async Task SetOperationalSectionDefaultAsync(
            long institutionId, string moduleCode, bool isEditAllowed, long actorId, CancellationToken ct)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO skolenza."InstitutionStaffSectionDefault"
                    ("InstitutionId", "ModuleCode", "IsEditAllowed", "UpdatedById", "UpdatedDateTime")
                VALUES ({institutionId}, {moduleCode}, {isEditAllowed}, {actorId}, {DateTime.UtcNow})
                ON CONFLICT ("InstitutionId", "ModuleCode") DO UPDATE
                SET "IsEditAllowed" = EXCLUDED."IsEditAllowed",
                    "UpdatedById" = EXCLUDED."UpdatedById",
                    "UpdatedDateTime" = EXCLUDED."UpdatedDateTime"
                """, ct);
        }

        #endregion

        public BaseStaffMemberRepository(SkolenzaDbContext context, IMapper mapper, ILogger<BaseStaffMemberRepository> logger,
            IPasswordService passwordService, IEncryptionService encryptionService)
        {
            this._context = context;
            this._mapper = mapper;
            this._logger = logger;

            _passwordService = passwordService;
            _encryptionService = encryptionService;
#if DEBUG
            var entityType = _context.Model.FindEntityType(typeof(StaffMember));

            if (entityType != null)
            {
                foreach (var p in entityType.GetProperties())
                {
                    Console.WriteLine($"{p.Name} | Shadow: {p.IsShadowProperty()}");
                }
            }
#endif

        }








        /// <summary>
        /// Common fun to for all tabs to update IsEditAllowed status
        /// </summary>
        /// <param name="tabInfoType"></param>
        /// <param name="staffMemberId"></param>
        /// <param name="userStaffMemberId"></param>
        /// <param name="isVerified"></param>
        /// <param name="ct"></param>
        /// <returns></returns>

        public async Task<bool> UpdateEditableStatusByEntityAsync(
         int tabInfoType,
    long staffMemberId,
    long userStaffMemberId,
    bool isVerified,
    CancellationToken ct)
        {
            if (staffMemberId <= 0)
                return false;

            if (!Enum.IsDefined(typeof(TabInfoType), tabInfoType))
                return false;

            DateTime now = DateTime.UtcNow;
            int affected = 0;

            switch ((TabInfoType)tabInfoType)
            {
                case TabInfoType.StaffMember:
                    affected = await _context.StaffMembers
                        .Where(x => x.Id == staffMemberId && x.IsSoftDeleted != true)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.IsEditAllowed, p => isVerified && !p.IsInfoVerified)
                            .SetProperty(p => p.UpdatedById, userStaffMemberId)
                            .SetProperty(p => p.UpdatedDateTime, now), ct);
                    break;

                case TabInfoType.Bank:
                    affected = await _context.StaffMemberBankDetails
                        .Where(x => x.StaffMemberId == staffMemberId && x.IsSoftDeleted != true)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.IsEditAllowed, p => isVerified && !p.IsInfoVerified)
                            .SetProperty(p => p.UpdatedById, userStaffMemberId)
                            .SetProperty(p => p.UpdatedDateTime, now), ct);
                    break;

                case TabInfoType.Contact:
                    affected = await _context.StaffMemberContacts
                        .Where(x => x.StaffMemberId == staffMemberId && x.IsSoftDeleted != true)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.IsEditAllowed, p => isVerified && p.IsInfoVerified != true)
                            .SetProperty(p => p.UpdatedById, userStaffMemberId)
                            .SetProperty(p => p.UpdatedDateTime, now), ct);
                    break;

                case TabInfoType.Experience:
                    affected = await _context.StaffMemberExperienceDetails
                        .Where(x => x.StaffMemberId == staffMemberId && x.IsSoftDeleted != true)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.IsEditAllowed, p => isVerified && p.IsInfoVerified != true)
                            .SetProperty(p => p.UpdatedById, userStaffMemberId)
                            .SetProperty(p => p.UpdatedDateTime, now), ct);
                    break;

                case TabInfoType.Identity:
                    affected = await _context.StaffMemberIdentities
                        .Where(x => x.StaffMemberId == staffMemberId && x.IsSoftDeleted != true)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.IsEditAllowed, p => isVerified && !p.IsInfoVerified)
                            .SetProperty(p => p.UpdatedById, userStaffMemberId)
                            .SetProperty(p => p.UpdatedDateTime, now), ct);
                    break;

                case TabInfoType.Education:
                    affected = await _context.StaffMemberEducations
                        .Where(x => x.StaffMemberId == staffMemberId && x.IsSoftDeleted != true)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.IsEditAllowed, p => isVerified && p.IsInfoVerified != true)
                            .SetProperty(p => p.UpdatedById, userStaffMemberId)
                            .SetProperty(p => p.UpdatedDateTime, now), ct);
                    break;

                case TabInfoType.Dependent:
                    affected = await _context.StaffMemberDependents
                        .Where(x => x.StaffMemberId == staffMemberId && x.IsSoftDeleted != true)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.IsEditAllowed, p => isVerified && p.IsInfoVerified != true)
                            .SetProperty(p => p.UpdatedById, userStaffMemberId)
                            .SetProperty(p => p.UpdatedDateTime, now), ct);
                    break;
            }

            return affected > 0;
        }

        public async Task<bool> UpdateVerificationStatusByTabAsync(
    int tabInfoType,
    long staffMemberId,
    long userStaffMemberId,
    bool isVerified,
    CancellationToken ct)
        {
            if (staffMemberId <= 0)
                return false;

            if (!Enum.IsDefined(typeof(TabInfoType), tabInfoType))
                return false;

            DateTime now = DateTime.UtcNow;
            int affected = 0;

            switch ((TabInfoType)tabInfoType)
            {
                case TabInfoType.StaffMember:
                    affected = await _context.StaffMembers
                        .Where(x => x.Id == staffMemberId && x.IsSoftDeleted != true)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.IsInfoVerified, isVerified)
                            .SetProperty(p => p.IsEditAllowed, p => isVerified ? false : p.IsEditAllowed)
                            .SetProperty(p => p.InfoVerifiedById, userStaffMemberId)
                            .SetProperty(p => p.InfoVerifiedDateTime, now), ct);
                    break;

                case TabInfoType.Bank:
                    affected = await _context.StaffMemberBankDetails
                        .Where(x => x.StaffMemberId == staffMemberId && x.IsSoftDeleted != true)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.IsInfoVerified, isVerified)
                            .SetProperty(p => p.IsEditAllowed, p => isVerified ? false : p.IsEditAllowed)
                            .SetProperty(p => p.InfoVerifiedById, userStaffMemberId)
                            .SetProperty(p => p.InfoVerifiedDateTime, now), ct);
                    break;

                case TabInfoType.Contact:
                    affected = await _context.StaffMemberContacts
                        .Where(x => x.StaffMemberId == staffMemberId && x.IsSoftDeleted != true)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.IsInfoVerified, isVerified)
                            .SetProperty(p => p.IsEditAllowed, p => isVerified ? false : p.IsEditAllowed)
                            .SetProperty(p => p.InfoVerifiedById, userStaffMemberId)
                            .SetProperty(p => p.InfoVerifiedDateTime, now), ct);
                    break;

                case TabInfoType.Experience:
                    affected = await _context.StaffMemberExperienceDetails
                        .Where(x => x.StaffMemberId == staffMemberId && x.IsSoftDeleted != true)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.IsInfoVerified, isVerified)
                            .SetProperty(p => p.IsEditAllowed, p => isVerified ? false : p.IsEditAllowed)
                            .SetProperty(p => p.InfoVerifiedById, userStaffMemberId)
                            .SetProperty(p => p.InfoVerifiedDateTime, now), ct);
                    break;

                case TabInfoType.Identity:
                    affected = await _context.StaffMemberIdentities
                        .Where(x => x.StaffMemberId == staffMemberId && x.IsSoftDeleted != true)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.IsInfoVerified, isVerified)
                            .SetProperty(p => p.IsEditAllowed, p => isVerified ? false : p.IsEditAllowed)
                            .SetProperty(p => p.InfoVerifiedById, userStaffMemberId)
                            .SetProperty(p => p.InfoVerifiedDateTime, now), ct);
                    break;

                case TabInfoType.Education:
                    affected = await _context.StaffMemberEducations
                        .Where(x => x.StaffMemberId == staffMemberId && x.IsSoftDeleted != true)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.IsInfoVerified, isVerified)
                            .SetProperty(p => p.IsEditAllowed, p => isVerified ? false : p.IsEditAllowed)
                            .SetProperty(p => p.InfoVerifiedById, userStaffMemberId)
                            .SetProperty(p => p.InfoVerifiedDateTime, now), ct);
                    break;

                case TabInfoType.Dependent:
                    affected = await _context.StaffMemberDependents
                        .Where(x => x.StaffMemberId == staffMemberId && x.IsSoftDeleted != true)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.IsInfoVerified, isVerified)
                            .SetProperty(p => p.IsEditAllowed, p => isVerified ? false : p.IsEditAllowed)
                            .SetProperty(p => p.InfoVerifiedById, userStaffMemberId)
                            .SetProperty(p => p.InfoVerifiedDateTime, now), ct);
                    break;
            }

            return affected > 0;
        }


        //public async Task<bool> UpdateVerificationStatus(
        //        long staffMemberId,
        //         long userId,
        //         bool status)
        //{
        //    var staffMember = await _context.StaffMembers
        //        .FirstOrDefaultAsync(x =>
        //            x.Id == staffMemberId &&
        //            x.IsSoftDeleted != true);

        //    if (staffMember == null)
        //        return false;

        //    staffMember.IsInfoVerified = status;
        //    staffMember.InfoVerifiedById = userId;
        //    staffMember.InfoVerifiedDateTime = DateTime.UtcNow;

        //    var rowsAffected = await _context.SaveChangesAsync();

        //    return rowsAffected > 0;
        //}

    //    public async Task<bool> UpdateEditStatus(
    //long staffMemberId,
    //long userId,
    //bool status)
    //    {
    //        var staffMember = await _context.StaffMembers
    //            .FirstOrDefaultAsync(x =>
    //                x.Id == staffMemberId &&
    //                x.IsSoftDeleted != true);

    //        if (staffMember == null)
    //            return false;

    //        staffMember.IsEditAllowed = status;
    //        staffMember.UpdatedById = userId;
    //        staffMember.UpdatedDateTime = DateTime.UtcNow;

    //        var rowsAffected = await _context.SaveChangesAsync();

    //        return rowsAffected > 0;
    //    }

        public async Task<bool> UpdateSectionVerifyStatusAsync(
           int tabInfoType,
           long staffMemberId,
           long institutionId,
           bool isVerified,
           bool isEditAllowed,
           long userId,
           CancellationToken ct)
        {
            if (staffMemberId <= 0)
                return false;

            if (!Enum.IsDefined(typeof(TabInfoType), tabInfoType))
                return false;

            bool belongsToInstitution = await _context.StaffMembers
                .AsNoTracking()
                .AnyAsync(x =>
                    x.Id == staffMemberId &&
                    x.InstitutionId == institutionId &&
                    x.IsSoftDeleted != true,
                    ct);

            if (!belongsToInstitution)
                return false;

            DateTime now = DateTime.UtcNow;
            int affected = 0;
            bool effectiveIsEditAllowed = !isVerified && isEditAllowed;

            switch ((TabInfoType)tabInfoType)
            {
                // ================= OVERVIEW =================
                case TabInfoType.StaffMember:
                    affected = await _context.StaffMembers
                        .Where(x =>
                            x.Id == staffMemberId &&
                            x.InstitutionId == institutionId &&
                            x.IsSoftDeleted != true)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.IsInfoVerified, isVerified)
                            .SetProperty(p => p.IsEditAllowed, effectiveIsEditAllowed)
                            .SetProperty(p => p.InfoVerifiedById, userId)
                            .SetProperty(p => p.InfoVerifiedDateTime, now),
                            ct);
                    break;

                // ================= EDUCATION =================
                case TabInfoType.Education:
                    affected = await _context.StaffMemberEducations
                        .Where(x =>
                            x.StaffMemberId == staffMemberId &&
                            x.IsSoftDeleted != true)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.IsInfoVerified, isVerified)
                            .SetProperty(p => p.IsEditAllowed, effectiveIsEditAllowed)
                            .SetProperty(p => p.InfoVerifiedById, userId)
                            .SetProperty(p => p.InfoVerifiedDateTime, now),
                            ct);
                    break;

                // ================= BANK =================
                case TabInfoType.Bank:
                    affected = await _context.StaffMemberBankDetails
                        .Where(x =>
                            x.StaffMemberId == staffMemberId &&
                            x.IsSoftDeleted != true)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.IsInfoVerified, isVerified)
                            .SetProperty(p => p.IsEditAllowed, effectiveIsEditAllowed)
                            .SetProperty(p => p.InfoVerifiedById, userId)
                            .SetProperty(p => p.InfoVerifiedDateTime, now),
                            ct);
                    break;

                // ================= EXPERIENCE =================
                case TabInfoType.Experience:
                    //  DETAILS TABLE
                    affected = await _context.StaffMemberExperienceDetails
                        .Where(x =>
                            x.StaffMemberId == staffMemberId &&
                            x.IsSoftDeleted != true)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.IsInfoVerified, isVerified)
                            .SetProperty(p => p.IsEditAllowed, effectiveIsEditAllowed)
                            .SetProperty(p => p.InfoVerifiedById, userId)
                            .SetProperty(p => p.InfoVerifiedDateTime, now),
                            ct);
                    break;

                // ================= CONTACT =================
                case TabInfoType.Contact:
                    affected = await _context.StaffMemberContacts
                        .Where(x =>
                            x.StaffMemberId == staffMemberId &&
                            x.IsSoftDeleted != true)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.IsInfoVerified, isVerified)
                            .SetProperty(p => p.IsEditAllowed, effectiveIsEditAllowed)
                            .SetProperty(p => p.InfoVerifiedById, userId)
                            .SetProperty(p => p.InfoVerifiedDateTime, now),
                            ct);
                    break;

                // ================= IDENTITY =================
                case TabInfoType.Identity:
                    affected = await _context.StaffMemberIdentities
                        .Where(x =>
                            x.StaffMemberId == staffMemberId &&
                            x.IsSoftDeleted != true)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.IsInfoVerified, isVerified)
                            .SetProperty(p => p.IsEditAllowed, effectiveIsEditAllowed)
                            .SetProperty(p => p.InfoVerifiedById, userId)
                            .SetProperty(p => p.InfoVerifiedDateTime, now),
                            ct);
                    break;

                // ================= DEPENDENT =================
                case TabInfoType.Dependent:
                    affected = await _context.StaffMemberDependents
                        .Where(x =>
                            x.StaffMemberId == staffMemberId &&
                            x.IsSoftDeleted != true)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.IsInfoVerified, isVerified)
                            .SetProperty(p => p.IsEditAllowed, effectiveIsEditAllowed)
                            .SetProperty(p => p.InfoVerifiedById, userId)
                            .SetProperty(p => p.InfoVerifiedDateTime, now),
                            ct);
                    break;
            }

            return affected > 0;
        }


        //public async Task<bool> UpdateVerifyEditStatusAsyncisVerified, bool? isEditAllowed, bool? isActive, long userId)
        //{(string sectionType, long staffMemberId, bool?
        //    if (staffMemberId <= 0 || string.IsNullOrWhiteSpace(sectionType))
        //        return false;

        //    sectionType = sectionType.Trim().ToLowerInvariant();

        //    DateTime now = DateTime.UtcNow;

        //    //  COMMON LOCAL FUNCTION — ALL TABLES FOLLOW SAME PATTERN
        //    async Task<bool> UpdateMainTable<TEntity>(DbSet<TEntity> dbSet)
        //        where TEntity : class
        //    {
        //        var rows = await dbSet
        //            .Where(x => EF.Property<long>(x, "StaffMemberId") == staffMemberId &&
        //                        (bool?)EF.Property<bool?>(x, "IsSoftDeleted") != true)
        //            .ToListAsync();

        //        if (rows.Count == 0)
        //            return false;

        //        foreach (var row in rows)
        //        {
        //            // NULL → FALSE conversion here
        //            typeof(TEntity).GetProperty("IsInfoVerified")?
        //                .SetValue(row, isVerified ?? false);

        //            typeof(TEntity).GetProperty("IsEditAllowed")?
        //                .SetValue(row, isEditAllowed ?? false);

        //            typeof(TEntity).GetProperty("IsActive")?
        //                .SetValue(row, isActive ?? false);

        //            typeof(TEntity).GetProperty("InfoVerifiedById")?
        //                .SetValue(row, userId);

        //            typeof(TEntity).GetProperty("InfoVerifiedDateTime")?
        //                .SetValue(row, now);

        //            //  typeof(TEntity).GetProperty("UpdatedById")?
        //            //   .SetValue(row, userId);

        //            //    typeof(TEntity).GetProperty("UpdatedDateTime")?
        //            // .SetValue(row, now);
        //        }



        //        return true;
        //    }

        //    bool mainUpdated = false;

        //    switch (sectionType)
        //    {
        //        case "education":
        //            mainUpdated = await UpdateMainTable(_context.StaffMemberEducations);
        //            break;

        //        case "experience":
        //            mainUpdated = await UpdateMainTable(_context.StaffMemberExperiences);

        //            if (mainUpdated)
        //            {
        //                //  ExperienceDetails update — SAME PATTERN — ONE extra hit only
        //                var details = await _context.StaffMemberExperienceDetails
        //                    .Where(x => x.StaffMemberId == staffMemberId && x.IsSoftDeleted != true)
        //                    .ToListAsync();

        //                if (details.Count > 0)
        //                {
        //                    foreach (var d in details)
        //                    {
        //                        if (isVerified.HasValue) d.IsInfoVerified = isVerified.Value;
        //                        if (isEditAllowed.HasValue) d.IsEditAllowed = isEditAllowed.Value;
        //                        if (isActive.HasValue) d.IsActive = isActive.Value;
        //                        d.UpdatedById = d.InfoVerifiedById = userId;
        //                        d.UpdatedDateTime = d.InfoVerifiedDateTime = now;

        //                    }
        //                }
        //            }
        //            break;

        //        case "bank":
        //            mainUpdated = await UpdateMainTable(_context.StaffMemberBankDetails);
        //            break;

        //        default:
        //            return false;
        //    }

        //    if (mainUpdated)
        //        await _context.SaveChangesAsync();

        //    return mainUpdated;
        //}



        public async Task<bool> ActivateAllStaffMemberAsync(StaffMember staffMember, bool isActive)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (staffMember == null)
                    throw new Exception("StaffMember not found.");

                //  STAFF_MEMBER (MASTER)
                staffMember.IsActive = isActive;
                staffMember.IsEditAllowed = false;
                staffMember.IsInfoVerified = false;
                staffMember.UpdatedById = staffMember.Id;
                staffMember.UpdatedDateTime = DateTime.UtcNow;

                //  BANK DETAILS
                var bankDetails = await _context.StaffMemberBankDetails
                    .Where(x => x.StaffMemberId == staffMember.Id && (x.IsSoftDeleted == null || x.IsSoftDeleted == false))
                    .ToListAsync();

                foreach (var bank in bankDetails)
                {
                    bank.IsActive = isActive;
                    bank.IsEditAllowed = false;
                    bank.IsInfoVerified = false;
                    bank.UpdatedById = staffMember.Id;
                    bank.UpdatedDateTime = DateTime.UtcNow;
                }

                //  CONTACT DETAILS
                var contacts = await _context.StaffMemberContacts
                    .Where(x => x.StaffMemberId == staffMember.Id && (x.IsSoftDeleted == null || x.IsSoftDeleted == false))
                    .ToListAsync();

                foreach (var contact in contacts)
                {
                    contact.IsActive = isActive;
                    contact.IsEditAllowed = false;
                    contact.IsInfoVerified = false;
                    contact.UpdatedById = staffMember.Id;
                    contact.UpdatedDateTime = DateTime.UtcNow;
                }

                //  DEPENDENT DETAILS
                var dependents = await _context.StaffMemberDependents
                    .Where(x => x.StaffMemberId == staffMember.Id && (x.IsSoftDeleted == null || x.IsSoftDeleted == false))
                    .ToListAsync();

                foreach (var dep in dependents)
                {
                    dep.IsActive = isActive;
                    dep.IsEditAllowed = false;
                    dep.IsInfoVerified = false;
                    dep.UpdatedById = staffMember.Id;
                    dep.UpdatedDateTime = DateTime.UtcNow;
                }

                //  IMAGE DETAILS
                var images = await _context.StaffMemberImages
                    .Where(x => x.StaffMemberId == staffMember.Id && (x.IsSoftDeleted == null || x.IsSoftDeleted == false))
                    .ToListAsync();

                foreach (var img in images)
                {
                    img.IsActive = isActive;
                    img.UpdateById = staffMember.Id;
                    img.UpdatedDateTime = DateTime.UtcNow;
                }

                //  EDUCATION DETAILS
                var educations = await _context.StaffMemberEducations
                    .Where(x => x.StaffMemberId == staffMember.Id && (x.IsSoftDeleted == null || x.IsSoftDeleted == false))
                    .ToListAsync();

                foreach (var edu in educations)
                {
                    edu.IsActive = isActive;
                    edu.IsEditAllowed = false;
                    edu.IsInfoVerified = false;
                    edu.UpdatedById = staffMember.Id;
                    edu.UpdatedDateTime = DateTime.UtcNow;
                }

                //  PERSONAL / IDENTITY DETAILS
                var identities = await _context.StaffMemberPersonalDetails
                    .Where(x => x.StaffMemberId == staffMember.Id && (x.IsSoftDeleted == null || x.IsSoftDeleted == false))
                    .ToListAsync();

                foreach (var iden in identities)
                {
                    iden.IsActive = isActive;
                    iden.IsEditAllowed = false;
                    iden.IsInfoVerified = false;
                    iden.UpdatedById = staffMember.Id;
                    iden.UpdatedDateTime = DateTime.UtcNow;
                }

                //  SAVE ONCE
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    $"StaffMember {staffMember.Id} {(isActive ? "activated" : "deactivated")} successfully."
                );

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Failed to update IsActive status for staffMember {staffMember?.Id}");
                throw;
            }
        }

        public async Task<bool> DeleteAllAsync(StaffMember staffMember)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {


                if (staffMember == null)
                    throw new Exception("StaffMember not found.");

                staffMember.IsSoftDeleted = true;
                staffMember.SoftDeletedById = staffMember.Id;
                staffMember.DeletedDateTime = DateTime.UtcNow;
                staffMember.IsActive = false;
                staffMember.IsEditAllowed = false;
                staffMember.IsInfoVerified = false;

                //  BANK DETAILS
                var bankDetails = await _context.StaffMemberBankDetails
                    .Where(x => x.StaffMemberId == staffMember.Id
                        && (x.IsSoftDeleted == null || x.IsSoftDeleted == false))
                    .ToListAsync();

                foreach (var bank in bankDetails)
                {
                    bank.IsSoftDeleted = staffMember.IsSoftDeleted;
                    bank.SoftDeletedById = staffMember.SoftDeletedById;
                    bank.DeletedDateTime = staffMember.DeletedDateTime;
                    bank.IsActive = staffMember.IsActive;
                    bank.IsEditAllowed = staffMember.IsEditAllowed ;
                    bank.IsInfoVerified = staffMember.IsInfoVerified ;

                }

                //  CONTACT DETAILS
                var contacts = await _context.StaffMemberContacts
                    .Where(x => x.StaffMemberId == staffMember.Id
                        && (x.IsSoftDeleted == null || x.IsSoftDeleted == false))
                    .ToListAsync();

                foreach (var contact in contacts)
                {
                    contact.IsSoftDeleted = staffMember.IsSoftDeleted;
                    contact.SoftDeletedById = staffMember.SoftDeletedById;
                    contact.DeletedDateTime = staffMember.DeletedDateTime;
                    contact.IsActive = staffMember.IsActive;
                    contact.IsEditAllowed = staffMember.IsEditAllowed ;
                    contact.IsInfoVerified = staffMember.IsInfoVerified ;
                }

                //  Dependant DETAILS
                var dependants = await _context.StaffMemberDependents
                    .Where(x => x.StaffMemberId == staffMember.Id
                        && (x.IsSoftDeleted == null || x.IsSoftDeleted == false))
                    .ToListAsync();

                foreach (var dep in dependants)
                {
                    dep.IsSoftDeleted = staffMember.IsSoftDeleted;
                    dep.SoftDeletedById = staffMember.SoftDeletedById;
                    dep.DeletedDateTime = staffMember.DeletedDateTime;
                    dep.IsActive = staffMember.IsActive;
                    dep.IsEditAllowed = staffMember.IsEditAllowed ;
                    dep.IsInfoVerified = staffMember.IsInfoVerified ;
                }
                //  Images DETAILS
                var images = await _context.StaffMemberImages
                    .Where(x => x.StaffMemberId == staffMember.Id
                        && (x.IsSoftDeleted == null || x.IsSoftDeleted == false))
                    .ToListAsync();

                foreach (var img in images)
                {
                    img.IsSoftDeleted = staffMember.IsSoftDeleted;
                    img.SoftDeletedById = staffMember.SoftDeletedById;
                    img.DeletedDateTime = staffMember.DeletedDateTime;
                    img.IsActive = staffMember.IsActive;

                }

                //  Education DETAILS
                var educations = await _context.StaffMemberEducations
                    .Where(x => x.StaffMemberId == staffMember.Id
                        && (x.IsSoftDeleted == null || x.IsSoftDeleted == false))
                    .ToListAsync();

                foreach (var edu in educations)
                {
                    edu.IsSoftDeleted = staffMember.IsSoftDeleted;
                    edu.SoftDeletedById = staffMember.SoftDeletedById;
                    edu.DeletedDateTime = staffMember.DeletedDateTime;
                    edu.IsActive = staffMember.IsActive;
                    edu.IsEditAllowed = staffMember.IsEditAllowed ;
                    edu.IsInfoVerified = staffMember.IsInfoVerified ;
                }
                //  Identity DETAILS
                var identites = await _context.StaffMemberPersonalDetails
                    .Where(x => x.StaffMemberId == staffMember.Id
                        && (x.IsSoftDeleted == null || x.IsSoftDeleted == false))
                    .ToListAsync();

                foreach (var iden in identites)
                {
                    iden.IsSoftDeleted = staffMember.IsSoftDeleted;
                    iden.SoftDeletedById = staffMember.SoftDeletedById;
                    iden.DeletedDateTime = staffMember.DeletedDateTime;
                    iden.IsActive = staffMember.IsActive;
                    iden.IsEditAllowed = staffMember.IsEditAllowed ;
                    iden.IsInfoVerified = staffMember.IsInfoVerified ;
                }

                //  SAVE ONCE
                await _context.SaveChangesAsync();
                //  await transaction.CommitAsync();

                _logger.LogInformation($"StaffMember {staffMember.SoftDeletedById} soft deleted successfully.");

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Failed to soft delete staffMember {staffMember.SoftDeletedById}");
                throw;
            }
        }

        public async Task<string?> ProfileImage(long staffMemberId)
        {
            try
            {
                // Fetch latest primary active image
                var image = await _context.StaffMemberImages
                    .AsNoTracking()
                    .Where(x => x.StaffMemberId == staffMemberId
                            && x.IsActive == true
                            && x.IsPrimary == true
                            && x.IsSoftDeleted != true)
                    .OrderByDescending(x => x.AddedDateTime) // latest preference
                    .Select(x => x.FilePath) // Only fetch needed column (performance)
                    .FirstOrDefaultAsync();

                // If no image found → return default placeholder
                return string.IsNullOrWhiteSpace(image)
                    ? null
                    : image;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error fetching profile image for StaffMemberId: {staffMemberId}");

                // Fail-safe fallback → never throw
                return "/assets/images/default-profile.png";
            }
        }
        public async Task<GetStaffMemberImageReponseDTO> AddImageAsync(StaffMemberImage entity)
        {
            try
            {
                // ---------------------------------------------
                // 1️⃣ If this image is primary, remove current primary
                // ---------------------------------------------
                if (entity.IsPrimary)
                {
                    var existingPrimary = await _context.StaffMemberImages
                        .Where(x =>
                            x.StaffMemberId == entity.StaffMemberId &&
                            x.IsPrimary == true &&
                            x.IsSoftDeleted != true)
                        .ToListAsync();

                    foreach (var img in existingPrimary)
                    {
                        img.IsPrimary = false;
                        img.UpdatedDateTime = DateTime.UtcNow;
                        img.UpdateById = entity.AddedById;
                    }
                }

                // ---------------------------------------------
                // 2️⃣ Insert new image
                // ---------------------------------------------
                await _context.StaffMemberImages.AddAsync(entity);
                await _context.SaveChangesAsync();

                // ---------------------------------------------
                // 3️⃣ Fetch final primary image (ONLY ONE)
                // ---------------------------------------------
                var primaryImage = await _context.StaffMemberImages
                    .AsNoTracking()
                    .Where(x =>
                        x.StaffMemberId == entity.StaffMemberId &&
                        x.IsSoftDeleted != true &&
                        x.IsPrimary == true)
                    .OrderByDescending(x => x.AddedDateTime)
                    .FirstOrDefaultAsync();

                // ---------------------------------------------
                // 4️⃣ Build completion %
                // ---------------------------------------------
                double completionPercentage = primaryImage != null ? 100 : 0;

                // ---------------------------------------------
                // 5️⃣ Prepare response
                // ---------------------------------------------
                if (primaryImage == null)
                    primaryImage = entity; // fallback if no primary (rare case)

                return new GetStaffMemberImageReponseDTO
                {
                    Id = primaryImage.Id,
                    StaffMemberId = primaryImage.StaffMemberId.ToString(),
                    FileName = primaryImage.FileName,
                    FilePath = primaryImage.FilePath,
                    IsActive = primaryImage.IsActive,
                    IsPrimary = primaryImage.IsPrimary,
                    HasImageUploaded = primaryImage.HasImageUploaded ,
                    CompletionPercentage = completionPercentage
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Error occurred in AddImageAsync | StaffMemberId={StaffMemberId}",
                    entity?.StaffMemberId);

                throw new Exception($"Failed to add / fetch image info: {ex.Message}");
            }
        }

        public async Task<GetStaffMemberImageReponseDTO> GetImage(long institutionId, long staffMemberId, GetStaffMemberImageRequestDTO dto)
        {
            try
            {
                _logger.LogInformation("Fetching staffMember primary image | EmpId={Emp} | Institution={Institution}",
                    staffMemberId, institutionId);

                // ---------------------------------------------
                // 1️⃣ Base Query
                // ---------------------------------------------
                var query = _context.StaffMemberImages
                    .AsNoTracking()
                    .Where(x =>
                        x.InstitutionId == institutionId &&
                        x.StaffMemberId == staffMemberId &&
                        x.IsSoftDeleted != true &&
                        x.IsPrimary == true);   // always fetch only primary

                // ---------------------------------------------
                // 2️⃣ Optional Active Filter
                // ---------------------------------------------
                if (dto.IsActive)
                    query = query.Where(x => x.IsActive == dto.IsActive);

                // ---------------------------------------------
                // 3️⃣ Fetch Single Primary Image
                // ---------------------------------------------
                var img = await query
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();

                if (img == null)
                {
                    // Safe fallback (no crash)
                    return new GetStaffMemberImageReponseDTO
                    {
                        StaffMemberId = staffMemberId.ToString(),
                        IsActive = false,
                        IsPrimary = false,
                        HasImageUploaded = false,
                        CompletionPercentage = 0,
                        FilePath = null,
                        FileName = null
                    };
                }

                // ---------------------------------------------
                // 4️⃣ Completion Percentage Logic
                // ---------------------------------------------
                double completion = img.HasImageUploaded ? 100 : 0;

                // ---------------------------------------------
                // 5️⃣ Final Single Object Return
                // ---------------------------------------------
                return new GetStaffMemberImageReponseDTO
                {
                    Id = img.Id,
                    StaffMemberId = img.StaffMemberId.ToString(),
                    FileName = img.FileName,
                    FilePath = img.FilePath,
                    IsActive = img.IsActive,
                    IsPrimary = img.IsPrimary,
                    HasImageUploaded = img.HasImageUploaded,
                    CompletionPercentage = completion
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Error fetching primary image | StaffMemberId={Emp}",
                    staffMemberId);

                // SAFE fallback
                return new GetStaffMemberImageReponseDTO
                {
                    StaffMemberId = staffMemberId.ToString(),
                    CompletionPercentage = 0
                };
            }
        }


        public async Task<PagedResponseDTO<GetBaseStaffMemberResponseDTO>> GetInfo(
            long institutionId,
            long staffMemberId,
            GetBaseStaffMemberRequestDTO dto)
        {
            try
            {
                //  Step 1: Pagination defaults
                int pageNumber = dto.PageNumber <= 0 ? 1 : dto.PageNumber;
                int pageSize = dto.PageSize <= 0 ? 10 : dto.PageSize;

                //  Step 2: Base staffMember query (NO tracking = faster)
                var query = _context.StaffMembers
               .AsNoTracking()
               .Include(x => x.UserRole)
                   .ThenInclude(ur => ur.Role)
               .Where(x =>
                    x.InstitutionId == institutionId &&
                            (x.IsSoftDeleted != true && x.IsActive==true));

                //  Step 3: Dynamic filters (SAFE & SAME RESULT)
                if (staffMemberId > 0)
                    query = query.Where(x => x.Id == staffMemberId);

                if (dto.IsActive)
                    query = query.Where(x => x.IsActive == dto.IsActive);

                if (!string.IsNullOrWhiteSpace(dto.EmployementCode))
                    query = query.Where(x => x.EmployementCode.Contains(dto.EmployementCode));

                if (!string.IsNullOrWhiteSpace(dto.FirstName))
                    query = query.Where(x => x.FirstName.Contains(dto.FirstName));

                if (!string.IsNullOrWhiteSpace(dto.LastName))
                    query = query.Where(x => x.LastName.Contains(dto.LastName));

                if (dto.DateOfOnBoarding.HasValue)
                    query = query.Where(x => x.DateOfOnBoarding == dto.DateOfOnBoarding);

                if (dto.DateOfBirth.HasValue)
                    query = query.Where(x => x.DateOfBirth == dto.DateOfBirth);

                if (dto.TypeId > 0)
                    query = query.Where(x => x.StaffEmploymentTypeId == dto.TypeId);

                if (dto.HasPermanent.HasValue)
                    query = query.Where(x => x.HasPermanent == dto.HasPermanent);

                if (dto.IsEditAllowed.HasValue)
                    query = query.Where(x => x.IsEditAllowed == dto.IsEditAllowed);

                if (dto.IsInfoVerified.HasValue)
                    query = query.Where(x => x.IsInfoVerified == dto.IsInfoVerified);

                //  Step 4: Sorting
                bool isAsc = string.Equals(dto.SortOrder, "asc", StringComparison.OrdinalIgnoreCase);
                query = dto.SortBy?.ToLower() switch
                {
                    "firstname" => isAsc ? query.OrderBy(x => x.FirstName) : query.OrderByDescending(x => x.FirstName),
                    "lastname" => isAsc ? query.OrderBy(x => x.LastName) : query.OrderByDescending(x => x.LastName),
                    "employementcode" => isAsc ? query.OrderBy(x => x.EmployementCode) : query.OrderByDescending(x => x.EmployementCode),
                    _ => isAsc ? query.OrderBy(x => x.Id) : query.OrderByDescending(x => x.Id)
                };

                //  Step 5: Total count (pagination metadata)
                int totalCount = await query.CountAsync();

                //  Step 6: Fetch paged staffMembers ONLY (lightweight)
                var staffMembers = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new
                    {
                        StaffMember = x,
                        Country = x.Country
                    })
                    .ToListAsync();

                // ----------------------------------------------------
                //  PERFORMANCE MAGIC STARTS HERE
                // ----------------------------------------------------

                bool hasPrimaryImage = await _context.StaffMemberImages
                        .AsNoTracking()
                         .AnyAsync(x =>
                         x.StaffMemberId == staffMemberId &&
                             x.IsPrimary &&
                              x.HasImageUploaded &&
                             x.IsSoftDeleted != true);


                //  Step 8: ONE-TIME lookup tables (Dictionary = O(1))
                var pageStaffIds = staffMembers.Select(row => row.StaffMember.Id).ToList();
                var positionLookup = await StaffProfessionalStructureRepository.CurrentPositionIdentities(
                        _context, DateOnly.FromDateTime(DateTime.UtcNow))
                    .Where(position => pageStaffIds.Contains(position.StaffMemberId))
                    .ToDictionaryAsync(position => position.StaffMemberId);

                var departmentLookup = await _context.Departments
                    .ToDictionaryAsync(x => x.Id, x => x.DepartmentName);

                var typeLookup = await _context.StaffEmploymentTypes
                    .ToDictionaryAsync(x => x.Id, x => x.TypeName);

                var staffCategoryLookup = await _context.StaffCategories
                    .AsNoTracking()
                    .ToDictionaryAsync(x => x.Id, x => x.Name);

                var genderLookup = await _context.Genders
                    .ToDictionaryAsync(x => x.Id, x => x.GenderName);

                //  Step 9: Final DTO mapping + IN-MEMORY completion
                var records = staffMembers.Select(x =>
                {
                 //   bool hasPrimary = hasPrimaryImageLookup.Contains(x.StaffMember.Id);

                    double completion =
                        CompletionCalculatorHelper.StaffMemberPropCalculate(
                            x.StaffMember,
                            hasPrimaryImage);

                    return new GetBaseStaffMemberResponseDTO
                    {
                        Id = x.StaffMember.Id.ToString(),
                        EmployementCode = x.StaffMember.EmployementCode,
                        FirstName = x.StaffMember.FirstName,
                        MiddleName = x.StaffMember.MiddleName,
                        LastName = x.StaffMember.LastName,
                        RoleId = x.StaffMember.UserRole.FirstOrDefault()?.RoleId,
                        RoleName = x.StaffMember.UserRole.FirstOrDefault()?.Role?.RoleName,
                        RoleType = x.StaffMember.UserRole.FirstOrDefault()?.Role?.RoleType,
                        GenderId = x.StaffMember.GenderId ?? 0,
                        GenderName = genderLookup.GetValueOrDefault(x.StaffMember.GenderId ?? 0),

                        StaffPositionMasterId = positionLookup.GetValueOrDefault(x.StaffMember.Id)?.StaffPositionMasterId ?? 0,
                        PositionName = positionLookup.GetValueOrDefault(x.StaffMember.Id)?.PositionName,

                        DepartmentId = x.StaffMember.DepartmentId ?? 0,
                        DepartmentName = departmentLookup.GetValueOrDefault(x.StaffMember.DepartmentId ?? 0),

                        StaffEmploymentTypeId = x.StaffMember.StaffEmploymentTypeId ?? 0,
                        StaffCategoryId = x.StaffMember.StaffCategoryId,
                        InstitutionStaffCategoryMasterId = x.StaffMember.InstitutionStaffCategoryMasterId,
                        StaffCategoryName = x.StaffMember.StaffCategoryId.HasValue
                            ? staffCategoryLookup.GetValueOrDefault(x.StaffMember.StaffCategoryId.Value)
                            : null,
                        Type = typeLookup.GetValueOrDefault(x.StaffMember.StaffEmploymentTypeId ?? 0),
                        DateOfBirth = x.StaffMember.DateOfBirth,
                        DateOfOnBoarding = x.StaffMember.DateOfOnBoarding,
                        CountryId = x.Country.Id,
                        CountryCode = x.Country.CountryCode,
                        Nationality = x.Country.CountryName,

                        OfficialEmail = x.StaffMember.OfficialEmail,
                        MobileNumber = x.StaffMember.MobileNumber,

                        EmergencyContactPerson = x.StaffMember.EmergencyContactPerson,
                        EmergencyContactNumber = x.StaffMember.EmergencyContactNumber,
                        BloodGroup = x.StaffMember.BloodGroup,
                        Relation = x.StaffMember.Relation,

                        HasPermanent = x.StaffMember.HasPermanent,
                        IsActive = x.StaffMember.IsActive,
                        IsEditAllowed = x.StaffMember.IsEditAllowed,
                        IsInfoVerified = x.StaffMember.IsInfoVerified,
                        IsMarried = x.StaffMember.IsMarried,


                        // ⭐ SINGLE SOURCE OF TRUTH
                        CompletionPercentage = completion
                    };
                }).ToList();

                //  Step 10: Overall completion (AVG, fast, accurate)
                double overallCompletion =
                    records.Any()
                        ? Math.Round(records.Average(x => x.CompletionPercentage), 2)
                        : 0;

                //  Step 11: Final response
                return new PagedResponseDTO<GetBaseStaffMemberResponseDTO>
                {
                    Data = records,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    CompletionPercentage = overallCompletion
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error occurred while fetching base staffMember info.");
                throw;
            }
        }

        public async Task<PagedResponseDTO<GetAllStaffMemberInfoResponseDTO>> GetAllInfo(
            long institutionId,
            long? staffMemberId,
            GetAllStaffMemberInfoRequestDTO dto,
            long requestingStaffMemberId,
            int requestingRoleTypeId)
        {
            try
            {

                int pageNumber = dto.PageNumber <= 0 ? 1 : dto.PageNumber;
                int pageSize = dto.PageSize <= 0 ? 10 : dto.PageSize;

                // ----------------------------------------------------
                // 1️⃣ BASE QUERY (LEFT JOIN + AS NO TRACKING)
                // ----------------------------------------------------
                var baseQuery =
                    from emp in _context.StaffMembers.AsNoTracking()

                    join gender in _context.Genders
                        on emp.GenderId equals (long?)gender.Id into genderJoin
                    from g in genderJoin.DefaultIfEmpty()

                    join position in StaffProfessionalStructureRepository.CurrentPositionIdentities(_context, DateOnly.FromDateTime(DateTime.UtcNow))
                        on emp.Id equals position.StaffMemberId into positionJoin
                    from d in positionJoin.DefaultIfEmpty()

                    join empType in _context.StaffEmploymentTypes
                        on emp.StaffEmploymentTypeId equals (long?)empType.Id into typeJoin
                    from et in typeJoin.DefaultIfEmpty()

                    join department in _context.Departments
                        on emp.DepartmentId equals (long?)department.Id into deptJoin
                    from dep in deptJoin.DefaultIfEmpty()

                     join country in _context.Countries
                        on emp.CountryId equals (int)country.Id into countryJoin
                    from c in countryJoin.DefaultIfEmpty()

                    join contact in _context.StaffMemberContacts
                            .Where(ec => ec.IsPrimary == true)
                           on emp.Id equals contact.StaffMemberId into contactJoin
                    from cont in contactJoin.DefaultIfEmpty()

                    join district in _context.Districts
                    on cont.DistrictId equals district.Id into districtJoin
                    from dist in districtJoin.DefaultIfEmpty()

                    where emp.InstitutionId == institutionId
                          && emp.IsSoftDeleted != true


                    select new
                    {
                        emp,
                        cont,
                        dist,
                        GenderName = g != null ? g.GenderName : "",
                        PositionId = d != null ? (int?)d.StaffPositionMasterId : null,
                        PositionName = d != null ? d.PositionName : "",
                        StaffEmploymentTypeName = et != null ? et.TypeName : "",
                        DepartmentName = dep != null ? dep.DepartmentName : "",
                        CountryName = c != null ? c.CountryName : "",
                        CountryCode = c != null ? c.CountryCode : ""   //  ADD THIS
                    };



                // ----------------------------------------------------
                // 2️⃣ FILTERS
                // ----------------------------------------------------
                if (staffMemberId.HasValue && staffMemberId.Value > 0)
                    baseQuery = baseQuery.Where(x => x.emp.Id == staffMemberId.Value);

                // Managers are restricted to their own record and current direct reports. An
                // StaffMember can browse the institution directory, but the handler later projects every
                // colleague row to directory-only fields. Unknown role types fail closed to self.
                if (requestingRoleTypeId == ConstantValues.RoleTypeManager)
                {
                    var today = DateTime.UtcNow.Date;
                    baseQuery = baseQuery.Where(x =>
                        x.emp.Id == requestingStaffMemberId ||
                        _context.StaffMemberManagerMappings.Any(mapping =>
                            mapping.InstitutionId == institutionId &&
                            mapping.ManagerId == requestingStaffMemberId &&
                            mapping.StaffMemberId == x.emp.Id &&
                            mapping.IsActive &&
                            mapping.IsSoftDeleted != true &&
                            mapping.EffectiveFrom <= today &&
                            (!mapping.EffectiveTo.HasValue || mapping.EffectiveTo.Value >= today)));
                }
                else if (requestingRoleTypeId != ConstantValues.RoleTypeAdmin &&
                         requestingRoleTypeId != ConstantValues.RoleTypeStaffMember)
                {
                    baseQuery = baseQuery.Where(x => x.emp.Id == requestingStaffMemberId);
                }

                if (!string.IsNullOrWhiteSpace(dto.EmailId))
                    baseQuery = baseQuery.Where(x => x.emp.OfficialEmail == dto.EmailId.Trim());

                if (dto.DepartmentId > 0)
                    baseQuery = baseQuery.Where(x => x.emp.DepartmentId == dto.DepartmentId);

                if (dto.StaffPositionMasterId > 0)
                {
                    var today = DateOnly.FromDateTime(DateTime.UtcNow);
                    baseQuery = baseQuery.Where(x => _context.StaffPositionAssignments.Any(assignment =>
                        assignment.InstitutionId == institutionId && assignment.StaffMemberId == x.emp.Id &&
                        assignment.StaffPositionMasterId == dto.StaffPositionMasterId &&
                        assignment.IsPrimary && assignment.IsActive && assignment.EffectiveFrom <= today &&
                        (!assignment.EffectiveTo.HasValue || assignment.EffectiveTo >= today)));
                }

                if (dto.StaffEmploymentTypeId.HasValue && dto.StaffEmploymentTypeId.Value > 0)
                    baseQuery = baseQuery.Where(x => x.emp.StaffEmploymentTypeId == dto.StaffEmploymentTypeId.Value);

                if (dto.GenderId > 0)
                    baseQuery = baseQuery.Where(x => x.emp.GenderId == dto.GenderId);

                if (!string.IsNullOrWhiteSpace(dto.FirstName))
                    baseQuery = baseQuery.Where(x => x.emp.FirstName.Contains(dto.FirstName));

                if (!string.IsNullOrWhiteSpace(dto.LastName))
                    baseQuery = baseQuery.Where(x => x.emp.LastName.Contains(dto.LastName));

                if (!string.IsNullOrWhiteSpace(dto.EmployementCode))
                    baseQuery = baseQuery.Where(x => x.emp.EmployementCode.Contains(dto.EmployementCode));

                int totalCount = await baseQuery.CountAsync();

                // ----------------------------------------------------
                // 3️⃣ PAGING AND DISPLAY ORDER
                // Keep the authenticated StaffMember first, then group all remaining rows by
                // Department so the API returns a stable, UI-ready directory without in-memory sorting.
                // ----------------------------------------------------
                var pagedStaffMembers = await baseQuery
                    .OrderByDescending(x => x.emp.Id == requestingStaffMemberId)
                    .ThenBy(x => x.emp.DepartmentId ?? int.MaxValue)
                    .ThenBy(x => x.DepartmentName)
                    .ThenBy(x => x.emp.FirstName)
                    .ThenBy(x => x.emp.LastName)
                    .ThenBy(x => x.emp.Id)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // ----------------------------------------------------
                // 4️⃣ IMAGES
                // ----------------------------------------------------
                var ids = pagedStaffMembers.Select(x => x.emp.Id).ToList();

                var images = await _context.StaffMemberImages
                    .Where(i => ids.Contains(i.StaffMemberId) && i.IsSoftDeleted != true)
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenByDescending(i => i.HasImageUploaded)
                    .ThenByDescending(i => i.Id)
                    .ToListAsync();

                var imgLookup = images
                    .GroupBy(i => i.StaffMemberId)
                    .ToDictionary(g => g.Key, g => g.FirstOrDefault());

                var hasPrimaryLookup = images
                    .Where(i => i.IsPrimary == true)
                    .Select(i => i.StaffMemberId)
                    .ToHashSet();

                // ----------------------------------------------------
                // 5️⃣ ACTIVE INSTITUTION ROLE ASSIGNMENTS
                // Load the roles for the current page in one query. Duplicate active
                // assignments are collapsed by RoleId in the response.
                // ----------------------------------------------------
                var roleAssignments = await _context.UserRoles
                    .AsNoTracking()
                    .Where(userRole =>
                        userRole.StaffMemberId.HasValue &&
                        userRole.RoleId.HasValue &&
                        ids.Contains(userRole.StaffMemberId.Value) &&
                        userRole.IsActive &&
                        userRole.IsSoftDeleted != true &&
                        userRole.Role != null &&
                        userRole.Role.InstitutionId == institutionId &&
                        userRole.Role.IsActive &&
                        userRole.Role.IsSoftDeleted != true)
                    .Select(userRole => new
                    {
                        StaffMemberId = userRole.StaffMemberId!.Value,
                        RoleId = userRole.RoleId!.Value,
                        RoleName = userRole.Role!.RoleName ?? string.Empty,
                        IsSystemDefault = userRole.Role.IsSystemDefault == true
                    })
                    .ToListAsync();

                var roleLookup = roleAssignments
                    .GroupBy(role => role.StaffMemberId)
                    .ToDictionary(
                        group => group.Key,
                        group => group
                            .GroupBy(role => role.RoleId)
                            .Select(roleGroup => roleGroup.First())
                            .OrderBy(role => role.RoleId)
                            .Select(role => new AssignedStaffMemberRoleDTO
                            {
                                Id = role.RoleId,
                                Code = BuildRoleCode(role.RoleName),
                                Name = role.RoleName,
                                Type = role.IsSystemDefault ? "SYSTEM" : "CUSTOM"
                            })
                            .ToList());


                // ----------------------------------------------------
                // 6️⃣ BUILD RESPONSE
                // ----------------------------------------------------
                var result = pagedStaffMembers.Select(x =>
                {
                    imgLookup.TryGetValue(x.emp.Id, out var img);
                    bool hasPrimary = hasPrimaryLookup.Contains(x.emp.Id);
                    roleLookup.TryGetValue(x.emp.Id, out var assignedRoles);

                    double completionPercentage = CompletionCalculatorHelper.StaffMemberPropCalculate(x.emp, hasPrimary);

                    SummaryStaffMemberInfo summaryStaffMemberInfo = new SummaryStaffMemberInfo
                    {

                        EmergencyContactPerson = x.emp.EmergencyContactPerson,
                        EmergencyContactNumber = x.emp.EmergencyContactNumber,
                        BloodGroup = x.emp?.BloodGroup,
                        MobileNumber = x.emp?.MobileNumber,
                        Relation = x.emp?.Relation,
                        CountryCode = x.CountryCode,
                        IsActive = x.emp.IsActive,
                        IsMarried = x.emp?.IsMarried,
                        StaffMemberCode= x.emp.EmployementCode,

                        OnlineStatus = null, //  Redis / SignalR se aayega (DB se nahi)
                        LastLoginDateTime = DateTime.UtcNow,

                        CurrentSalaryStatusId = 1,               //  salary join nahi hai
                        CurrentSalaryStatusRemark = null,

                        RoleId = 1,                              //  role join nahi hai
                        RoleType = null,

                        StaffPositionMasterId = x.PositionId ?? 0,
                        PositionName = x.PositionName,

                        DepartmentId = x.emp.DepartmentId ?? 0,
                        Department = x.DepartmentName,

                        ProfileImage = !string.IsNullOrWhiteSpace(img?.FilePath) ? img.FilePath : null,
                        City = x.dist?.DistrictName,   //  District table se
                                                       //  FIXED
                        Address = x.cont?.Address,

                        DateOfJoining = x.emp.DateOfOnBoarding,

                        StaffEmploymentTypeId = x.emp.StaffEmploymentTypeId ?? 0,
                        StaffEmploymentTypeName = x.StaffEmploymentTypeName
                    };


                    return new GetAllStaffMemberInfoResponseDTO
                    {
                        StaffMemberId = x.emp.Id.ToString(),
                        EmployementCode = x.emp.EmployementCode,
                        FirstName = x.emp.FirstName,
                        LastName = x.emp.LastName,
                        DateOfOnBoarding = x.emp.DateOfOnBoarding?.ToString(),
                        DateOfBirth = x.emp.DateOfBirth?.ToString(),
                        CountryId = x.emp.CountryId,
                        Nationality = x.CountryName,   //  FIXED (NO navigation access)
                        CountryCode = x.CountryCode,
                        EmergencyContactPerson = x.emp.EmergencyContactPerson,
                        GenderId = x.emp.GenderId ?? 0,
                        StaffEmploymentTypeId = x.emp.StaffEmploymentTypeId ?? 0,
                        StaffCategoryId = x.emp.StaffCategoryId,
                        StaffPositionMasterId = x.PositionId,
                        DepartmentId = x.emp.DepartmentId ?? 0,
                        GenderName = x.GenderName,
                        MobileNumber  = x.emp?.MobileNumber,
                        StaffEmploymentTypeName = x.StaffEmploymentTypeName,
                        PositionName = x.PositionName,
                        DepartmentName = x.DepartmentName,
                        OfficialEmail = x.emp.OfficialEmail,
                        StaffMemberImagePath = img?.FilePath,
                        HasImagePicUploaded = hasPrimary,
                        IsActive = x.emp.IsActive,
                        AssignedRoles = assignedRoles ?? new List<AssignedStaffMemberRoleDTO>(),
                      //  SummaryStaffMemberInfo = summaryStaffMemberInfo  , //  THIS WAS MISSING
                        CompletionPercentage = completionPercentage
                    };
                }).ToList();

                foreach (var staffMemberResult in result)
                {
                    if (!long.TryParse(staffMemberResult.StaffMemberId, out long resultStaffMemberId))
                        continue;

                    var completionSections = await GetStaffMemberCompletionAsync(resultStaffMemberId);
                    staffMemberResult.CompletionPercentage =
                        StaffMemberProfileCompletionCalculator.CalculateOverallCompletion(completionSections);
                }

                return new PagedResponseDTO<GetAllStaffMemberInfoResponseDTO>
                {
                    Data = result,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),


                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FAILED in GetAllInfo");

                return new PagedResponseDTO<GetAllStaffMemberInfoResponseDTO>
                {
                    Data = new List<GetAllStaffMemberInfoResponseDTO>(),
                    TotalCount = 0,
                    PageNumber = dto.PageNumber,
                    PageSize = dto.PageSize,
                    TotalPages = 0
                };
            }
        }

        private static string BuildRoleCode(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return string.Empty;
            }

            var characters = roleName
                .Trim()
                .Select(character => char.IsLetterOrDigit(character)
                    ? char.ToUpperInvariant(character)
                    : '_')
                .ToArray();

            return string.Join(
                '_',
                new string(characters)
                    .Split('_', StringSplitOptions.RemoveEmptyEntries));
        }

        public async Task<SummaryStaffMemberInfo?> BuildStaffMemberSummaryAsync(
       long staffMemberId,
       bool isActive)
        {
            if (staffMemberId <= 0)
                throw new ArgumentException("Invalid StaffMemberId.");

            // =====================================================
            //  STAFF_MEMBER + MASTER DATA (STRICT FILTERS)
            // =====================================================
            var staffMemberData =
                await (
                    from emp in _context.StaffMembers.AsNoTracking()

                    //  UserRole
                    join userRole in _context.UserRoles
                        .Where(x => x.IsActive == true && x.IsSoftDeleted != true)
                        on emp.Id equals userRole.StaffMemberId into userRoleJoin
                    from ur in userRoleJoin.DefaultIfEmpty()

                        //  Role
                    join role in _context.Roles
                        .Where(x => x.IsActive == true && x.IsSoftDeleted != true)
                        on ur.RoleId equals role.Id into roleJoin
                    from r in roleJoin.DefaultIfEmpty()

                        //  Gender
                    join gender in _context.Genders
                        on emp.GenderId equals (long?)gender.Id into genderJoin
                    from g in genderJoin.DefaultIfEmpty()

                        //  Position
                    join position in StaffProfessionalStructureRepository.CurrentPositionIdentities(_context, DateOnly.FromDateTime(DateTime.UtcNow))
                        on emp.Id equals position.StaffMemberId into positionJoin
                    from d in positionJoin.DefaultIfEmpty()

                        //  StaffMember Type
                    join empType in _context.StaffEmploymentTypes
                        .Where(x => x.IsActive == true && x.IsSoftDeleted != true)
                        on emp.StaffEmploymentTypeId equals (long?)empType.Id into typeJoin
                    from et in typeJoin.DefaultIfEmpty()

                        //  Department
                    join department in _context.Departments
                        .Where(x => x.IsActive == true && x.IsSoftDeleted != true)
                        on emp.DepartmentId equals (long?)department.Id into deptJoin
                    from dep in deptJoin.DefaultIfEmpty()

                        //  Country
                    join country in _context.Countries
                        .Where(x => x.IsActive == true)
                        on emp.CountryId equals country.Id into countryJoin
                    from c in countryJoin.DefaultIfEmpty()

                        //  Primary Contact
                    join contact in _context.StaffMemberContacts
                        .Where(ec =>
                            ec.IsPrimary == true &&
                            ec.IsActive == true &&
                            ec.IsSoftDeleted != true)
                        on emp.Id equals contact.StaffMemberId into contactJoin
                    from cont in contactJoin.DefaultIfEmpty()

                        //  District
                    join district in _context.Districts
                        .Where(x => x.IsActive == true)
                        on cont.DistrictId equals district.Id into districtJoin
                    from dist in districtJoin.DefaultIfEmpty()

                        //  FINAL FILTER
                    where emp.Id == staffMemberId
                          && emp.IsActive == isActive
                          && emp.IsSoftDeleted != true

                    select new
                    {
                        emp,
                        cont,
                        dist,

                        RoleId = ur != null ? ur.RoleId : (int?)null,
                        RoleName = r != null ? r.RoleName : null,
                        RoleType = r != null ? r.RoleType : (int?)null,

                        PositionId = d != null ? (int?)d.StaffPositionMasterId : null,
                        PositionName = d != null ? d.PositionName : string.Empty,
                        StaffEmploymentTypeName = et != null ? et.TypeName : string.Empty,
                        DepartmentName = dep != null ? dep.DepartmentName : string.Empty,
                        CountryCode = c != null ? c.CountryCode : string.Empty
                    }
                ).FirstOrDefaultAsync();

            if (staffMemberData == null)
                return null;

            // =====================================================
            //  PROFILE IMAGE
            // =====================================================
            var img = await _context.StaffMemberImages
                .Where(i =>
                    i.StaffMemberId == staffMemberId &&
                    i.IsSoftDeleted != true)
                .OrderByDescending(i => i.IsPrimary)
                .ThenByDescending(i => i.HasImageUploaded)
                .ThenByDescending(i => i.Id)
                .FirstOrDefaultAsync();

            bool hasPrimaryImage = img?.IsPrimary == true;

            // =====================================================
            //  COMPLETION %
            // =====================================================
            double completion =
                CompletionCalculatorHelper.StaffMemberPropCalculate(
                    staffMemberData.emp,
                    hasPrimaryImage);

            // =====================================================
            //  FINAL DTO
            // =====================================================
            return new SummaryStaffMemberInfo
            {
                EmergencyContactPerson = staffMemberData.emp.EmergencyContactPerson,
                EmergencyContactNumber = staffMemberData.emp.EmergencyContactNumber,
                BloodGroup = staffMemberData.emp.BloodGroup,
                MobileNumber = staffMemberData.emp.MobileNumber,
                Relation = staffMemberData.emp.Relation,

                RoleId = staffMemberData.RoleId ?? 0,
                RoleName = staffMemberData.RoleName,
                RoleType = staffMemberData.RoleType,

                CountryCode = staffMemberData.CountryCode,
                IsActive = staffMemberData.emp.IsActive,
                IsMarried = staffMemberData.emp.IsMarried,
                StaffMemberCode = staffMemberData.emp.EmployementCode,

                RelationName = staffMemberData.emp.Relation != null ?
                    (staffMemberData.emp.Relation == 1 ? "Father" :
                     staffMemberData.emp.Relation == 2 ? "Mother" :
                     staffMemberData.emp.Relation == 3 ? "Spouse" :
                     staffMemberData.emp.Relation == 4 ? "Sibling" : "Other") : null,

                OnlineStatus = null,
                LastLoginDateTime = DateTime.UtcNow,

                CurrentSalaryStatusId = 1,
                CurrentSalaryStatusRemark = null,

                StaffMemberId = staffMemberData.emp.Id.ToString(),

                StaffPositionMasterId = staffMemberData.PositionId ?? 0,
                PositionName = staffMemberData.PositionName,

                DepartmentId = staffMemberData.emp.DepartmentId ?? 0,
                Department = staffMemberData.DepartmentName,

                ProfileImage = img?.FilePath,
                City = staffMemberData.dist?.DistrictName,
                Address = staffMemberData.cont?.Address,

                DateOfJoining = staffMemberData.emp.DateOfOnBoarding,

                StaffEmploymentTypeId = staffMemberData.emp.StaffEmploymentTypeId ?? 0,
                StaffEmploymentTypeName = staffMemberData.StaffEmploymentTypeName
            };
        }
        public async Task<StaffMemberProfileSummaryInfo?> StaffMemberProfileSummaryAsync(
   long staffMemberId,
   bool isActive)
        {
            if (staffMemberId <= 0)
                throw new ArgumentException("Invalid StaffMemberId.");

            var data =
                await (
                    from emp in _context.StaffMembers.AsNoTracking()

                        //  Position (Active + Not Deleted)
                    join position in StaffProfessionalStructureRepository.CurrentPositionIdentities(_context, DateOnly.FromDateTime(DateTime.UtcNow))
                        on emp.Id equals position.StaffMemberId into positionJoin
                    from d in positionJoin.DefaultIfEmpty()

                        //  Department (Active + Not Deleted)
                    join department in _context.Departments
                        .Where(x => x.IsActive == true && x.IsSoftDeleted != true)
                        on emp.DepartmentId equals (long?)department.Id into deptJoin
                    from dep in deptJoin.DefaultIfEmpty()

                        //  Primary Contact (Active + Not Deleted)
                    join contact in _context.StaffMemberContacts
                        .Where(c =>
                            c.IsPrimary == true &&
                            c.IsActive == true &&
                            c.IsSoftDeleted != true)
                        on emp.Id equals contact.StaffMemberId into contactJoin
                    from cont in contactJoin.DefaultIfEmpty()

                        //  FINAL FILTER (MOST IMPORTANT)
                    where emp.Id == staffMemberId
                          && emp.IsActive == isActive
                          && emp.IsSoftDeleted != true

                    select new StaffMemberProfileSummaryInfo
                    {
                        StaffMemberId = emp.Id.ToString(),

                        FullName =
                            (emp.FirstName ?? string.Empty) + " " +
                            (emp.MiddleName ?? string.Empty) + " " +
                            (emp.LastName ?? string.Empty),

                        StaffPositionMasterId = d != null ? (int?)d.StaffPositionMasterId : null,
                        PositionName = d != null ? d.PositionName : string.Empty,

                        Department = emp.DepartmentId,
                        DepartmentType = dep != null ? dep.DepartmentName : string.Empty,

                        StaffMemberCode = emp.EmployementCode,


                        MobileNumber = emp.MobileNumber,

                        OffilcialEmail = emp.OfficialEmail
                    }
                ).FirstOrDefaultAsync();

            return data;
        }

        public async Task<List<CompletionSectionDTO>> GetStaffMemberCompletionAsync(long staffMemberId)
        {
            try
            {
                // If bad staffMemberId, return empty list safely
                if (staffMemberId <= 0)
                    return new List<CompletionSectionDTO>();
                #region Contact Details

                var contactList = await _context.StaffMemberContacts
                    .AsNoTracking()
                    .Where(x =>
                        x.StaffMemberId == staffMemberId &&
                        x.IsSoftDeleted == false &&
                        x.IsActive == true
                    )
                    .Select(x => new GetContactResponseDTO
                    {
                        Id = x.Id,
                        ContactType = x.ContactType,
                        Relation = x.Relation,
                        ContactName = x.ContactName,
                        ContactNumber = x.ContactNumber,
                        AlternateNumber = x.AlternateNumber,
                     //   Email = x.Email,
                        Address = x.Address,
                        CountryId = x.CountryId,
                        StateId = x.StateId,
                        DistrictId = x.DistrictId,
                        IsPrimary = x.IsPrimary,
                        IsEditAllowed = x.IsEditAllowed,
                        IsInfoVerified = x.IsInfoVerified,
                      //  Description = x.Description
                    })
                    .ToListAsync();

                #endregion

                #region Education Details
                var eduList = await _context.StaffMemberEducations
                    .AsNoTracking()
                    .Where(x => x.StaffMemberId == staffMemberId && x.IsSoftDeleted != true)
                    .Select(x => new GetEducationResponseDTO
                    {
                        Degree = x.Degree,
                        InstituteName = x.InstituteName,
                        ScoreType = x.ScoreType.ToString(),
                        HasEducationDocUploded = x.HasEducationDocUploded,

                        StartDate = x.StartDate,
                        EndDate = x.EndDate,
                        IsEditAllowed =x.IsEditAllowed,
                        IsInfoVerified =x.IsInfoVerified,


                    })
                    .ToListAsync();
                #endregion
                #region Bank Details
                var bankList = await _context.StaffMemberBankDetails
                    .AsNoTracking()
                    .Where(x => x.StaffMemberId == staffMemberId && x.IsSoftDeleted != true)
                    .Select(x => new GetBankResponseDTO
                    {
                        AccountNumber = x.AccountNumber,
                        BankName = x.BankName,
                        IFSCCode = x.Ifsccode,
                        BranchName = x.BranchName,
                        AccountType = x.AccountType,
                        HasChequeDocUploaded = x.HasChequeDocUploaded,
                        IsPrimaryAccount = x.IsPrimaryAccount,
                        IsEditAllowed = x.IsEditAllowed,
                        IsInfoVerified = x.IsInfoVerified,
                        FileName = x.FileName,
                        FilePath = x.FilePath,



                    })
                    .ToListAsync();
                #endregion
                #region Remaining Profile And Assignment Sections

                var staffMember = await _context.StaffMembers
                    .AsNoTracking()
                    .Where(x => x.Id == staffMemberId && x.IsSoftDeleted != true)
                    .Select(x => new
                    {
                        x.FirstName,
                        x.LastName,
                        x.DateOfBirth,
                        x.DateOfOnBoarding,
                        x.DepartmentId,
                        x.OfficialEmail,
                        x.CountryId,
                        x.IsInfoVerified,
                        x.IsEditAllowed
                    })
                    .SingleOrDefaultAsync();

                var hasPrimaryImage = await _context.StaffMemberImages
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.StaffMemberId == staffMemberId &&
                        x.IsPrimary &&
                        x.IsActive &&
                        !x.IsSoftDeleted &&
                        x.HasImageUploaded);

                var experienceRows = await _context.StaffMemberExperienceDetails
                    .AsNoTracking()
                    .Where(x => x.StaffMemberId == staffMemberId && x.IsSoftDeleted != true)
                    .Select(x => new
                    {
                        x.CompanyName,
                        x.PreviousPositionTitle,
                        x.StartDate,
                        x.EndDate,
                        x.IsInfoVerified,
                        x.IsEditAllowed
                    })
                    .ToListAsync();

                int staffMemberCountryId = staffMember?.CountryId ?? 0;
                var identityRows = await (
                    from rule in _context.CountryIdentityRules.AsNoTracking()
                    join identity in _context.StaffMemberIdentities
                            .AsNoTracking()
                            .Where(x =>
                                x.StaffMemberId == staffMemberId &&
                                x.IsActive &&
                                !x.IsSoftDeleted)
                        on rule.IdentityCategoryDocumentId equals identity.IdentityCategoryDocumentId
                        into savedIdentities
                    from identity in savedIdentities.DefaultIfEmpty()
                    where rule.CountryId == staffMemberCountryId &&
                          rule.IsActive &&
                          rule.IdentityCategoryDocument.IsActive &&
                          rule.IdentityCategoryDocument.IdentityCategory.IsActive
                    select new
                    {
                        rule.IsMandatory,
                        HasSavedRecord = identity != null,
                        IdentityValue = identity == null ? null : identity.IdentityValue,
                        HasIdentityUploaded = identity != null && identity.HasIdentityUploaded,
                        IsInfoVerified = identity == null ? null : (bool?)identity.IsInfoVerified,
                        IsEditAllowed = identity == null ? null : (bool?)identity.IsEditAllowed
                    })
                    .ToListAsync();

                var dependentRows = await _context.StaffMemberDependents
                    .AsNoTracking()
                    .Where(x => x.StaffMemberId == staffMemberId && x.IsSoftDeleted != true)
                    .Select(x => new
                    {
                        x.DependentName,
                        x.Relation,
                        x.DateOfBirth,
                        x.HasProofUploaded,
                        x.IsInfoVerified,
                        x.IsEditAllowed
                    })
                    .ToListAsync();

                bool hasWorkLocation = await _context.StaffMemberLocationAssignments
                    .AsNoTracking()
                    .AnyAsync(x => x.StaffMemberId == staffMemberId && x.IsSoftDeleted != true);
                bool hasDevice = await _context.StaffMemberDeviceEnrollments
                    .AsNoTracking()
                    .AnyAsync(x => x.StaffMemberId == staffMemberId && x.IsSoftDeleted != true);
                bool hasWorkArrangement = await _context.StaffMemberWorkArrangements
                    .AsNoTracking()
                    .AnyAsync(x => x.StaffMemberId == staffMemberId && x.IsSoftDeleted != true);
                bool hasWorkPattern = await _context.StaffMemberWorkPatterns
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.StaffMemberWorkArrangement.StaffMemberId == staffMemberId &&
                        x.IsSoftDeleted != true);
                bool hasOverride = await _context.StaffMemberWorkModeOverrideRequests
                    .AsNoTracking()
                    .AnyAsync(x => x.StaffMemberId == staffMemberId && x.IsSoftDeleted != true);

                #endregion

                // Existing Education, Bank and Contact formulas are intentionally preserved.
                var educationSection = eduList.CalculateEducationCompletionDTO();
                var bankSection = bankList.CalculateBankCompletionDTO();
                var contactSection = contactList.CalculateContactCompletionDTO();

                var overviewPercentages = staffMember == null
                    ? Array.Empty<double>()
                    : new[]
                    {
                        StaffMemberProfileCompletionCalculator.CalculateOverviewRow(
                            staffMember.FirstName,
                            staffMember.LastName,
                            staffMember.DateOfBirth,
                            staffMember.DateOfOnBoarding,
                            staffMember.DepartmentId,
                            staffMember.OfficialEmail,
                            hasPrimaryImage,
                            await _context.StaffPositionAssignments.AsNoTracking().AnyAsync(assignment =>
                                assignment.StaffMemberId == staffMemberId && assignment.IsPrimary && assignment.IsActive &&
                                assignment.EffectiveFrom <= DateOnly.FromDateTime(DateTime.UtcNow) &&
                                (!assignment.EffectiveTo.HasValue || assignment.EffectiveTo >= DateOnly.FromDateTime(DateTime.UtcNow))))
                    };

                var overviewSection = StaffMemberProfileCompletionCalculator.CreateSection(
                    "Overview",
                    overviewPercentages,
                    staffMember == null ? null : new bool?[] { staffMember.IsInfoVerified },
                    staffMember == null ? null : new bool?[] { staffMember.IsEditAllowed });
                var experienceSection = StaffMemberProfileCompletionCalculator.CreateSection(
                    "Experience",
                    experienceRows.Select(x => StaffMemberProfileCompletionCalculator.CalculateRowPercentage(
                        !string.IsNullOrWhiteSpace(x.CompanyName),
                        !string.IsNullOrWhiteSpace(x.PreviousPositionTitle),
                        x.StartDate.HasValue,
                        x.EndDate.HasValue)).ToArray(),
                    experienceRows.Select(x => x.IsInfoVerified).ToArray(),
                    experienceRows.Select(x => x.IsEditAllowed).ToArray());
                var applicableIdentityRows = identityRows
                    .Where(x => x.IsMandatory || x.HasSavedRecord)
                    .ToArray();
                var identitySection = StaffMemberProfileCompletionCalculator.CreateIdentitySection(
                    applicableIdentityRows.Select(x => StaffMemberProfileCompletionCalculator.CalculateIdentityRow(
                        x.IdentityValue,
                        x.HasIdentityUploaded,
                        x.IsMandatory,
                        x.HasSavedRecord)).ToArray(),
                    applicableIdentityRows.Select(x => x.IsInfoVerified).ToArray(),
                    applicableIdentityRows.Select(x => x.IsEditAllowed).ToArray());
                var dependentSection = StaffMemberProfileCompletionCalculator.CreateSection(
                    "Dependent",
                    dependentRows.Select(x => StaffMemberProfileCompletionCalculator.CalculateRowPercentage(
                        !string.IsNullOrWhiteSpace(x.DependentName),
                        x.Relation > 0,
                        x.DateOfBirth.HasValue,
                        x.HasProofUploaded == true)).ToArray(),
                    dependentRows.Select(x => x.IsInfoVerified).ToArray(),
                    dependentRows.Select(x => x.IsEditAllowed).ToArray());

                return StaffMemberProfileCompletionCalculator.ApplyVerificationContract(
                    new List<CompletionSectionDTO>
                    {
                        overviewSection,
                        bankSection,
                        contactSection,
                        experienceSection,
                        identitySection,
                        educationSection,
                        dependentSection,
                        StaffMemberProfileCompletionCalculator.CreateAssignmentSection("Work Locations", hasWorkLocation),
                        StaffMemberProfileCompletionCalculator.CreateAssignmentSection("Devices", hasDevice),
                        StaffMemberProfileCompletionCalculator.CreateAssignmentSection("Work Arrangement", hasWorkArrangement),
                        StaffMemberProfileCompletionCalculator.CreateAssignmentSection("Work Pattern", hasWorkPattern),
                        StaffMemberProfileCompletionCalculator.CreateAssignmentSection("Overrides", hasOverride)
                    });
            }
            catch (Exception ex)
            {
                //  Log error safely
                _logger.LogError(ex,
                    "Error in GetStaffMemberCompletionAsync for StaffMemberId: {StaffMemberId}",
                    staffMemberId);

                // Central middleware reports database failures; an empty successful profile
                // would incorrectly represent a failed lookup as zero completion.
                throw;
            }
        }

        public async Task<GetMinimalStaffMemberResponseDTO> GetSingleRecordAsync(long id, bool isActive)
        {
            try
            {

                if (id <= 0)
                    throw new ArgumentException("Invalid Id provided for fetching staffMember record.");



                var record = await (
                    from emp in _context.StaffMembers.AsNoTracking()

                    join et in _context.StaffEmploymentTypes.AsNoTracking()
                        on emp.StaffEmploymentTypeId equals et.Id

                    join gt in _context.Genders.AsNoTracking()
                        on emp.GenderId equals gt.Id into genderJoin
                    from gender in genderJoin.DefaultIfEmpty()

                    join dept in _context.Departments.AsNoTracking()
                        on emp.DepartmentId equals dept.Id into deptJoin
                    from department in deptJoin.DefaultIfEmpty()

                    join positionIdentity in StaffProfessionalStructureRepository.CurrentPositionIdentities(_context, DateOnly.FromDateTime(DateTime.UtcNow))
                        on emp.Id equals positionIdentity.StaffMemberId into positionJoin
                    from position in positionJoin.DefaultIfEmpty()

                    join country in _context.Countries.AsNoTracking()
                        on emp.CountryId equals country.Id into countryJoin
                    from nation in countryJoin.DefaultIfEmpty()

                    where emp.Id == id
                       && emp.IsActive == isActive
                       && emp.IsSoftDeleted != true
                       && et.IsActive == true
                       && et.IsSoftDeleted != true
                       && (department == null || (department.IsActive && department.IsSoftDeleted != true))

                    select new GetMinimalStaffMemberResponseDTO
                    {
                        Id = emp.Id,
                        InstitutionId = emp.InstitutionId ?? 0,
                        FirstName = emp.FirstName,
                        MiddleName = emp.MiddleName,
                        LastName = emp.LastName,
                        EmployementCode = emp.EmployementCode,
                        StaffEmploymentTypeId = emp.StaffEmploymentTypeId ?? 0,
                        StaffEmploymentTypeName = et.TypeName,
                        GenderId = emp.GenderId ?? 0,
                        GenderName = gender != null ? gender.GenderName : null,
                        DepartmentId = emp.DepartmentId ?? 0,
                        DepartmentName = department != null ? department.DepartmentName : null,
                        StaffPositionMasterId = position != null ? position.StaffPositionMasterId : 0,
                        PositionName = position != null ? position.PositionName : null,
                        CountryId = nation != null ? nation.Id : 0,
                        Nationality = nation != null ? nation.CountryName : null,
                        CountryCode = nation != null ? nation.CountryCode : null,
                        DateOfOnBoarding = emp.DateOfOnBoarding,
                        DateOfBirth = emp.DateOfBirth,
                        EmergencyContactNumber = emp.EmergencyContactNumber,
                        EmergencyContactPerson = emp.EmergencyContactPerson,
                        MobileNumber = emp.MobileNumber,
                        OfficialEmail = emp.OfficialEmail,
                        IsActive = emp.IsActive,
                        HasPermanent = emp.HasPermanent,


                    }
                ).FirstOrDefaultAsync();

                if (record == null)
                    throw new InvalidOperationException($"No staffMember record found for Id: {id}");

                return record;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error while fetching staffMember record for Id: {Id}", id);
                throw;
            }
        }


        public async Task<bool> UpdateStaffMemberAsync(StaffMember entity, long institutionId)
        {
            _context.StaffMembers.Update(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        #region StaffMember-Base-info

        public async Task<StaffMemberImage?> IsImageExist(long id, bool isActive)
        {
            try
            {

                return await _context.StaffMemberImages
                    .AsNoTracking()
                    .FirstOrDefaultAsync(img =>
                        img.Id == id &&
                        img.IsActive == isActive &&
                        img.IsSoftDeleted != true &&
                        img.IsPrimary == true
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error while checking image existence for Id: {id}");
                return null;
            }
        }

        public async Task<StaffMember?> GetByIdAsync(long id, long institutionId, bool track = true)
        {
            IQueryable<StaffMember> query = _context.StaffMembers.Where(x => x.InstitutionId == institutionId && x.IsSoftDeleted!=true);

            if (!track)
                query = query.AsNoTracking();

              return await query.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> UpdateProfileImage(StaffMemberImage staffMemberImageInfo)
        {
            try
            {

                _context.StaffMemberImages.Update(staffMemberImageInfo);

                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error while updating staffMember image (Id: {staffMemberImageInfo?.Id})");
                return false;
            }
        }



      public async Task<GetBaseStaffMemberResponseDTO> CreateStaffMemberAsync( StaffMember staffMember, LoginCredential loginCredential, UserRole userRole)
{
    // The allocator/import holds the institution code lock until this account transaction commits.
    await StaffMemberCapacityGuard.EnsureSpaceAsync(_context, staffMember.InstitutionId ?? 0, CancellationToken.None);
    var email = loginCredential.LoginId.Trim().ToUpperInvariant();
    await _context.Database.ExecuteSqlInterpolatedAsync(
        $"SELECT pg_advisory_xact_lock(hashtextextended({$"{BulkImportConstants.StaffMemberLoginLockPrefix}{email}"}, 0))");
    if (await _context.LoginCredentials.AnyAsync(item => item.LoginId.ToUpper() == email))
    {
        throw new skolenza.application.Exceptions.ValidationErrorException("OfficialEmail is unavailable.");
    }
    // 1️⃣ Save entities
    await _context.StaffMembers.AddAsync(staffMember);

    var staffMemberImage = new StaffMemberImage
    {
        StaffMember = staffMember,
        InstitutionId = staffMember.InstitutionId,
        IsPrimary = true,
        HasImageUploaded = false,
        IsActive = true,
        AddedById = staffMember.AddedById,
        AddedDateTime = DateTime.UtcNow,
        FileType = 1
    };

     await _context.StaffMemberImages.AddAsync(staffMemberImage);
    await _context.LoginCredentials.AddAsync(loginCredential);
    await _context.UserRoles.AddAsync(userRole);

    await _context.SaveChangesAsync();

    // 2️⃣ Fetch with joins (Single source of truth)
    var result =
        await (from emp in _context.StaffMembers.AsNoTracking()
               where emp.Id == staffMember.Id

               join d in StaffProfessionalStructureRepository.CurrentPositionIdentities(_context, DateOnly.FromDateTime(DateTime.UtcNow))
                   on emp.Id equals d.StaffMemberId into desigJoin
               from desig in desigJoin.DefaultIfEmpty()

               join g in _context.Genders
                   on emp.GenderId equals g.Id into genderJoin
               from gender in genderJoin.DefaultIfEmpty()

               join dept in _context.Departments
                   on emp.DepartmentId equals dept.Id into deptJoin
               from department in deptJoin.DefaultIfEmpty()

               join ur in _context.UserRoles
                   on emp.Id equals ur.StaffMemberId

                join nc in _context.Countries
                 on emp.CountryId equals nc.Id into countryJoin
                from nationCountry in countryJoin.DefaultIfEmpty()


               join r in _context.Roles
                   on ur.RoleId equals r.Id

               join t in _context.StaffEmploymentTypes
                   on emp.StaffEmploymentTypeId equals t.Id into empTypeJoin

               from empType in empTypeJoin.DefaultIfEmpty()


               select new GetBaseStaffMemberResponseDTO
               {
                   Id = emp.Id.ToString(),
                   EmployementCode = emp.EmployementCode,
                   FirstName = emp.FirstName,
                   MiddleName = emp.MiddleName,
                   LastName = emp.LastName,
                   OfficialEmail = emp.OfficialEmail,

                   GenderId = emp.GenderId ?? 0,
                   GenderName = gender != null ? gender.GenderName : null,

                   StaffPositionMasterId = desig != null ? desig.StaffPositionMasterId : 0,
                   PositionName = desig != null ? desig.PositionName : null,

                   DepartmentId = emp.DepartmentId ?? 0,
                   DepartmentName = department != null ? department.DepartmentName : null,

                   RoleId    = r.Id,

                   RoleType = r.RoleType,
                   RoleName = r.RoleName,
                    CountryId= emp.CountryId,
                    MobileNumber= emp.MobileNumber,
                   CountryCode = nationCountry != null ? nationCountry.CountryCode : null,
                   Nationality = nationCountry != null ? nationCountry.CountryName : null,
                   EmergencyContactPerson = emp.EmergencyContactPerson,
                   EmergencyContactNumber = emp.EmergencyContactNumber,
                   StaffEmploymentTypeId = emp.StaffEmploymentTypeId ?? 0,
                   StaffCategoryId = emp.StaffCategoryId,
                   InstitutionStaffCategoryMasterId = emp.InstitutionStaffCategoryMasterId,
                   Type = empType != null ? empType.TypeName : null,
                   DateOfBirth = emp.DateOfBirth,
                   DateOfOnBoarding = emp.DateOfOnBoarding,
                   DateOfExit = emp.DateOfExit,
                   BloodGroup = emp.BloodGroup,
                   HasPermanent = emp.HasPermanent,
                   IsActive = emp.IsActive,
                   IsEditAllowed = emp.IsEditAllowed,
                   IsInfoVerified = emp.IsInfoVerified,
               }).FirstOrDefaultAsync();
            if (result != null)
            {
                bool hasPrimaryImage = await _context.StaffMemberImages
                      .AsNoTracking()
                      .AnyAsync(x =>
                          x.StaffMemberId == staffMember.Id &&      //  FIX
                          x.IsPrimary == true &&
                          x.HasImageUploaded == true &&
                          x.IsSoftDeleted != true);

                // =====================================================
                // 4️⃣ COMPLETION % (SINGLE SOURCE OF TRUTH)
                // =====================================================
                 result.CompletionPercentage =    CompletionCalculatorHelper.StaffMemberPropCalculate(staffMember, hasPrimaryImage);


            }


            return result!;
}





        public Task<StaffMember?> IsStaffMemberExist(string StaffMemberCode, long institutionId, bool track = true)
        {
            throw new NotImplementedException();
        }


        public async Task AddStaffMemberAggregateAsync(
                StaffMember staffMember,
                LoginCredential loginCredential,
                UserRole userRole,
                CancellationToken cancellationToken = default)
        {
            try
            {
                if (staffMember == null) throw new ArgumentNullException(nameof(staffMember));
                if (loginCredential == null) throw new ArgumentNullException(nameof(loginCredential));
                if (userRole == null) throw new ArgumentNullException(nameof(userRole));

                await _context.StaffMembers.AddAsync(staffMember, cancellationToken);

                var staffMemberImage = new StaffMemberImage
                {
                    StaffMember = staffMember,
                    InstitutionId = staffMember.InstitutionId,
                    IsPrimary = true,
                    HasImageUploaded = false,
                    IsActive = true,
                    AddedById = staffMember.AddedById,
                    AddedDateTime = DateTime.UtcNow,
                    FileType = 1
                };

                await _context.StaffMemberImages.AddAsync(staffMemberImage, cancellationToken);
                await _context.LoginCredentials.AddAsync(loginCredential, cancellationToken);
                await _context.UserRoles.AddAsync(userRole, cancellationToken);

                _logger.LogInformation(
                    "StaffMember aggregate added to DbContext successfully for InstitutionId: {InstitutionId}, StaffMemberCode: {StaffMemberCode}",
                    staffMember.InstitutionId,
                    staffMember.EmployementCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error while adding staffMember aggregate to DbContext for InstitutionId: {InstitutionId}, StaffMemberCode: {StaffMemberCode}",
                    staffMember?.InstitutionId,
                    staffMember?.EmployementCode);
                throw;
            }
        }

        public async Task<GetBaseStaffMemberResponseDTO?> GetCreatedStaffMemberResponseAsync(
            long staffMemberId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await
                    (from emp in _context.StaffMembers.AsNoTracking()
                     where emp.Id == staffMemberId

                     join d in StaffProfessionalStructureRepository.CurrentPositionIdentities(_context, DateOnly.FromDateTime(DateTime.UtcNow))
                         on emp.Id equals d.StaffMemberId into desigJoin
                     from desig in desigJoin.DefaultIfEmpty()

                     join g in _context.Genders
                         on emp.GenderId equals g.Id into genderJoin
                     from gender in genderJoin.DefaultIfEmpty()

                     join dept in _context.Departments
                         on emp.DepartmentId equals dept.Id into deptJoin
                     from department in deptJoin.DefaultIfEmpty()

                     join ur in _context.UserRoles
                         on emp.Id equals ur.StaffMemberId

                     join nc in _context.Countries
                         on emp.CountryId equals nc.Id into countryJoin
                     from nationCountry in countryJoin.DefaultIfEmpty()

                     join r in _context.Roles
                         on ur.RoleId equals r.Id

                     join t in _context.StaffEmploymentTypes
                         on emp.StaffEmploymentTypeId equals t.Id into empTypeJoin
                     from empType in empTypeJoin.DefaultIfEmpty()

                     select new GetBaseStaffMemberResponseDTO
                     {
                         Id = emp.Id.ToString(),
                         EmployementCode = emp.EmployementCode,
                         FirstName = emp.FirstName,
                         MiddleName = emp.MiddleName,
                         LastName = emp.LastName,
                         OfficialEmail = emp.OfficialEmail,
                         GenderId = emp.GenderId ?? 0,
                         GenderName = gender != null ? gender.GenderName : null,
                         StaffPositionMasterId = desig != null ? desig.StaffPositionMasterId : 0,
                         PositionName = desig != null ? desig.PositionName : null,
                         DepartmentId = emp.DepartmentId ?? 0,
                         DepartmentName = department != null ? department.DepartmentName : null,
                         RoleId = r.Id,
                         RoleType = r.RoleType,
                         RoleName = r.RoleName,
                         CountryId = emp.CountryId,
                         MobileNumber = emp.MobileNumber,
                         CountryCode = nationCountry != null ? nationCountry.CountryCode : null,
                         Nationality = nationCountry != null ? nationCountry.CountryName : null,
                         EmergencyContactPerson = emp.EmergencyContactPerson,
                         EmergencyContactNumber = emp.EmergencyContactNumber,
                         StaffEmploymentTypeId = emp.StaffEmploymentTypeId ?? 0,
                         StaffCategoryId = emp.StaffCategoryId,
                         InstitutionStaffCategoryMasterId = emp.InstitutionStaffCategoryMasterId,
                         Type = empType != null ? empType.TypeName : null,
                         DateOfBirth = emp.DateOfBirth,
                         DateOfOnBoarding = emp.DateOfOnBoarding,
                         DateOfExit = emp.DateOfExit,
                         BloodGroup = emp.BloodGroup,
                         HasPermanent = emp.HasPermanent,
                         IsActive = emp.IsActive,
                         IsEditAllowed = emp.IsEditAllowed,
                         IsInfoVerified = emp.IsInfoVerified,
                     }).FirstOrDefaultAsync(cancellationToken);

                if (result == null)
                {
                    _logger.LogWarning("Created staffMember response not found for StaffMemberId: {StaffMemberId}", staffMemberId);
                    return null;
                }

                bool hasPrimaryImage = await _context.StaffMemberImages
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.StaffMemberId == staffMemberId &&
                        x.IsPrimary == true &&
                        x.HasImageUploaded == true &&
                        x.IsSoftDeleted != true,
                        cancellationToken);

                var staffMemberEntity = await _context.StaffMembers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == staffMemberId, cancellationToken);

                if (staffMemberEntity != null)
                {
                    result.CompletionPercentage =
                        CompletionCalculatorHelper.StaffMemberPropCalculate(staffMemberEntity, hasPrimaryImage);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching created staffMember response for StaffMemberId: {StaffMemberId}", staffMemberId);
                throw;
            }
        }

    }


}




        #endregion
















