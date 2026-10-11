// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Provides persistence operations for StaffMember Identity records.
// ================================================================



using AutoMapper;
using skolenza.application.DTOS.Staff.Experience;
using skolenza.application.DTOS.Staff.Sensitive;
using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.StoreProcedures;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IHashed;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Interfaces.ITokenService;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace skolenza.persistance.Repositories
{

    #region Persistence Operations


    /// <summary>
    /// Provides persistence operations for StaffMemberIdentity records.
    /// </summary>
    public class StaffMemberIdentityRepository : IStaffMemberIdentityRepository
    {
        private readonly SkolenzaDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<StaffMemberIdentityRepository> _logger;

        private readonly IPasswordService _passwordService;
        private readonly IEncryptionService _encryptionService;
        public StaffMemberIdentityRepository(SkolenzaDbContext context, IMapper mapper, ILogger<StaffMemberIdentityRepository> logger,
            IPasswordService passwordService, IEncryptionService encryptionService)
        {
            this._context = context;
            this._mapper = mapper;
            this._logger = logger;

            _passwordService = passwordService;
            _encryptionService = encryptionService;

        }


        //public async Task<GetIdentityResponseDTO> CreateAsync(StaffMemberPersonalDetail entity)
        //{
        //    try
        //    {
        //        // 1️⃣ Validation
        //        if (entity == null)
        //            throw new ArgumentNullException(nameof(entity), "Personal info entity cannot be null.");

        //        if (entity.StaffMemberId <= 0)
        //            throw new ArgumentException("Invalid StaffMemberId provided.");

        //        // 2️⃣ Insert Record
        //        await _context.StaffMemberPersonalDetails.AddAsync(entity);
        //        await _context.SaveChangesAsync();

        //        // 3️⃣ Fetch LATEST record → SINGLE OBJECT
        //        var response = await _context.StaffMemberPersonalDetails
        //            .AsNoTracking()
        //            .Where(x =>
        //                x.StaffMemberId == entity.StaffMemberId &&
        //                x.IsSoftDeleted != true &&
        //                x.IsActive == true)
        //            .OrderByDescending(x => x.Id)
        //            .Select(x => new GetIdentityResponseDTO
        //            {
        //                StaffMemberId = x.StaffMemberId.ToString(),

        //                AadhaarNumber = x.AadhaarNumber,
        //                PanNumber = x.PanNumber,
        //                PassportNumber = x.PassportNumber,
        //                DrivingLicenseNumber = x.DrivingLicenseNumber,
        //                VoterId = x.VoterId,

        //                BloodGroup = x.BloodGroup,
        //                MaritalStatus = x.MaritalStatus,
        //                Nationality = x.Nationality,

        //                HasEPFAccount = x.HasEPFAccount,
        //                UANNumber = x.UANNumber,

        //                EmergencyContactName = x.EmergencyContactName,
        //                EmergencyContactNumber = x.EmergencyContactNumber,
        //                EmergencyContactRelation = x.EmergencyContactRelation,

        //                //  Upload flags
        //                hasAadharIdUploaded = !string.IsNullOrEmpty(x.AadhaarDocPath),
        //                hasPanIdUploaded = !string.IsNullOrEmpty(x.PanDocPath),
        //                hasPassportIdUploaded = !string.IsNullOrEmpty(x.PassportDocPath),

        //                //  Paths
        //                aadharFilePath = x.AadhaarDocPath,
        //                panFilePath = x.PanDocPath,
        //                passportFilePath = x.PassportDocPath,

        //                IsInfoVerified = x.IsInfoVerified ?? false,
        //                IsEditAllowed = x.IsEditAllowed,

        //                CompletionPercentage =
        //                    Math.Round(
        //                        (new[]
        //                        {
        //                    string.IsNullOrEmpty(x.AadhaarNumber) ? 0 : 1,
        //                    string.IsNullOrEmpty(x.PanNumber) ? 0 : 1,
        //                    string.IsNullOrEmpty(x.DrivingLicenseNumber) ? 0 : 1,
        //                    string.IsNullOrEmpty(x.VoterId) ? 0 : 1,
        //                    string.IsNullOrEmpty(x.BloodGroup) ? 0 : 1,
        //                    string.IsNullOrEmpty(x.Nationality) ? 0 : 1,
        //                    string.IsNullOrEmpty(x.EmergencyContactName) ? 0 : 1,
        //                    string.IsNullOrEmpty(x.EmergencyContactRelation) ? 0 : 1,
        //                    !string.IsNullOrEmpty(x.AadhaarDocPath) ? 1 : 0,
        //                    !string.IsNullOrEmpty(x.PanDocPath) ? 1 : 0
        //                        }.Sum() / 10.0) * 100, 0)
        //            })
        //            .FirstOrDefaultAsync();   //  THIS IS THE KEY

        //        return response!;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex,
        //            " Error occurred while adding/fetching personal info for StaffMemberId: {StaffMemberId}",
        //            entity.StaffMemberId);

        //        throw;
        //    }
        //}

        public async Task<bool> UpdateIdentity(StaffMemberPersonalDetail staffMemberPersonal)
        {
            try
            {

                _context.StaffMemberPersonalDetails.Update(staffMemberPersonal);

                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error while updating staffMember identity info  (Id: {staffMemberPersonal?.Id})");
                return false;
            }
        }

        //public async Task<GetIdentityResponseDTO?> GetInfo(GetIdentityRequestDTO dto)
        //{
        //    try
        //    {
        //        //  Base Query
        //        var query = _context.StaffMemberPersonalDetails
        //            .AsNoTracking()
        //            .Where(x =>
        //                x.IsActive == dto.IsActive &&
        //                x.IsSoftDeleted != true);


        //        //  Keyword Search
        //        if (!string.IsNullOrWhiteSpace(dto.SortBy))
        //        {
        //            var keyword = dto.SortBy.Trim().ToLower();
        //            query = query.Where(x =>
        //                (x.BloodGroup ?? "").ToLower().Contains(keyword) ||
        //                (x.Nationality ?? "").ToLower().Contains(keyword) ||
        //                (x.EmergencyContactName ?? "").ToLower().Contains(keyword) ||
        //                (x.PanNumber ?? "").ToLower().Contains(keyword) ||
        //                (x.AadhaarNumber ?? "").ToLower().Contains(keyword) ||
        //                (x.PassportNumber ?? "").ToLower().Contains(keyword));
        //        }

        //        //  Sorting (latest record preferred)
        //        query = query.OrderByDescending(x => x.Id);

        //        //  SINGLE RECORD
        //        var identity = await query
        //            .Select(identity => new GetIdentityResponseDTO
        //            {
        //                StaffMemberId = identity.StaffMemberId.ToString(),
        //                BloodGroup = identity.BloodGroup,
        //                MaritalStatus = identity.MaritalStatus,
        //                Nationality = identity.Nationality,
        //                EmergencyContactName = identity.EmergencyContactName,
        //                EmergencyContactNumber = identity.EmergencyContactNumber,
        //                EmergencyContactRelation = identity.EmergencyContactRelation,

        //                PanNumber = identity.PanNumber,
        //                AadhaarNumber = identity.AadhaarNumber,
        //                PassportNumber = identity.PassportNumber,
        //                DrivingLicenseNumber = identity.DrivingLicenseNumber,

        //                HasEPFAccount = identity.HasEPFAccount,
        //                UANNumber = identity.UANNumber,

        //                hasAadharIdUploaded = identity.HasAadhaarIdUploaded,
        //                hasPanIdUploaded = identity.HasPanIdUploaded,
        //                hasPassportIdUploaded = identity.HasPassportIdUploaded,

        //                aadharFilePath = identity.AadhaarDocPath,
        //                panFilePath = identity.PanDocPath,
        //                passportFilePath = identity.PassportDocPath,

        //                CompletionPercentage =
        //                    Math.Round(
        //                        (new[]
        //                        {
        //                    string.IsNullOrEmpty(identity.AadhaarNumber) ? 0 : 1,
        //                    string.IsNullOrEmpty(identity.PanNumber) ? 0 : 1,
        //                    string.IsNullOrEmpty(identity.DrivingLicenseNumber) ? 0 : 1,
        //                    string.IsNullOrEmpty(identity.VoterId) ? 0 : 1,
        //                    string.IsNullOrEmpty(identity.BloodGroup) ? 0 : 1,
        //                    string.IsNullOrEmpty(identity.Nationality) ? 0 : 1,
        //                    string.IsNullOrEmpty(identity.EmergencyContactName) ? 0 : 1,
        //                    string.IsNullOrEmpty(identity.EmergencyContactRelation) ? 0 : 1,
        //                    identity.HasAadhaarIdUploaded ? 1 : 0,
        //                    identity.HasPanIdUploaded ? 1 : 0
        //                        }.Sum() / 10.0) * 100, 0)
        //            })
        //            .FirstOrDefaultAsync();

        //        return identity; //  single object or null
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex,
        //            " Error while fetching identity info | StaffMemberId={StaffMemberId}",

        //        throw;
        //    }
        //}

        public async Task<StaffMemberIdentity?> GetSingleRecordAsync(long id, bool isActive)
        {
            try
            {
                IQueryable<StaffMemberIdentity> query =
                    _context.StaffMemberIdentities
                        .AsNoTracking()
                        .Where(x =>
                            x.Id == id &&
                            (x.IsSoftDeleted != true));

                // Apply IsActive ONLY when true is explicitly required
                if (isActive)
                {
                    query = query.Where(x => x.IsActive == true);
                }

                // SINGLE RECORD
                return await query
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Error while fetching StaffMemberPersonalDetail | Id={Id}, IsActive={IsActive}",
                    id, isActive);

                throw;
            }
        }

        public Task<StaffMemberIdentity?> GetByStaffMemberAndDocumentAsync(
            long staffMemberId,
            int documentId,
            CancellationToken cancellationToken)
        {
            return _context.StaffMemberIdentities.FirstOrDefaultAsync(
                identity =>
                    identity.StaffMemberId == staffMemberId &&
                    identity.IdentityCategoryDocumentId == documentId &&
                    !identity.IsSoftDeleted &&
                    identity.IsActive,
                cancellationToken);
        }

        public async Task<bool> UpdateAsync(
            StaffMemberIdentity entity,
            CancellationToken cancellationToken)
        {
            _context.StaffMemberIdentities.Update(entity);
            return await _context.SaveChangesAsync(cancellationToken) > 0;
        }

        public async Task<bool> SoftDeleteAsync(
            StaffMemberIdentity entity,
            CancellationToken cancellationToken)
        {
            _context.StaffMemberIdentities.Update(entity);
            return await _context.SaveChangesAsync(cancellationToken) > 0;
        }


        public async Task<bool> IsStaffMemberIdentityExistsAsync(long id, bool? isActive)
        {
            try
            {
                IQueryable<StaffMemberPersonalDetail> query =
                    _context.StaffMemberPersonalDetails
                        .AsNoTracking()
                        .Where(x =>
                            x.StaffMemberId == id &&
                            (x.IsSoftDeleted !=true));

                // Apply IsActive ONLY if client sends it
                if (isActive.HasValue)
                {
                    query = query.Where(x => x.IsActive == isActive.Value);
                }

                // SINGLE SQL HIT
                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Error in IsStaffMemberPersonalDetailExistsAsync | Id={Id}, IsActive={IsActive}",
                    id, isActive);

                throw;
            }
        }

        public Task<GetStaffMemberIdentityResponseDTO> GetInfo(GetIdentityRequestDTO dto)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> CreateAsync(IEnumerable<StaffMemberIdentity> entities)
        {
            if (entities == null || !entities.Any())
                return false;

            await _context.StaffMemberIdentities.AddRangeAsync(entities);
            return true; // SaveChanges UnitOfWork karega
        }

        /// <inheritdoc />
        public async Task<bool> IsDocumentAllowedAsync(
            long staffMemberId,
            long institutionId,
            int documentId,
            CancellationToken cancellationToken)
        {
            return await (
                from staffMember in _context.StaffMembers
                join rule in _context.CountryIdentityRules on staffMember.CountryId equals rule.CountryId
                where staffMember.Id == staffMemberId &&
                      staffMember.InstitutionId == institutionId &&
                      !staffMember.IsSoftDeleted &&
                      rule.IdentityCategoryDocumentId == documentId &&
                      rule.IsActive &&
                      rule.IdentityCategoryDocument.IsActive &&
                      rule.IdentityCategoryDocument.IdentityCategory.IsActive
                select rule.Id
            ).AnyAsync(cancellationToken);
        }

    }




    #endregion
}











