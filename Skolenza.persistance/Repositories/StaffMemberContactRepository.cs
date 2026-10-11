// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Provides persistence operations for StaffMember Contact records.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers.PercentageHelper;
using skolenza.application.DTOS.Staff.Contact;
using skolenza.application.DTOS.Pagination;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IHashed;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;

using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace skolenza.persistance.Repositories
{

    #region Persistence Operations

    /// <summary>
    /// Provides persistence operations for StaffMemberContact records.
    /// </summary>
    public class StaffMemberContactRepository : IStaffMemberContactRepository
    {
        private readonly SkolenzaDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<StaffMemberContactRepository> _logger;

        private readonly IPasswordService _passwordService;
        private readonly IEncryptionService _encryptionService;
        public StaffMemberContactRepository(SkolenzaDbContext context, IMapper mapper, ILogger<StaffMemberContactRepository> logger,
            IPasswordService passwordService, IEncryptionService encryptionService)
        {
            this._context = context;
            this._mapper = mapper;
            this._logger = logger;

            _passwordService = passwordService;
            _encryptionService = encryptionService;

        }

        public async Task<PagedResponseDTO<GetContactResponseDTO>> CreateAsync(StaffMemberContact entity)
        {
            try
            {
                //  1️⃣ Validation
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                if (entity.StaffMemberId <= 0)
                    throw new ArgumentException("Invalid StaffMemberId");

                //  2️⃣ SINGLE PRIMARY RULE (VERY IMPORTANT)
                if (entity.IsPrimary == true)
                {
                    var existingPrimaryContacts = await _context.StaffMemberContacts
                        .Where(x =>
                            x.StaffMemberId == entity.StaffMemberId &&
                            x.IsPrimary == true &&
                            x.IsSoftDeleted != true)
                        .ToListAsync();

                    if (existingPrimaryContacts.Any())
                    {
                        foreach (var contact in existingPrimaryContacts)
                        {
                            contact.IsPrimary = false;
                        }

                        _context.StaffMemberContacts.UpdateRange(existingPrimaryContacts);
                        await _context.SaveChangesAsync();
                    }
                }

                //  3️⃣ Insert new contact
                await _context.StaffMemberContacts.AddAsync(entity);
                await _context.SaveChangesAsync();

                //  4️⃣ Fetch list with LEFT JOINs
                //  4️⃣ Fetch list with LEFT JOINs
                var query =
                    from c in _context.StaffMemberContacts.AsNoTracking()

                    join country in _context.Countries
                        on (c.CountryId > 0 ? c.CountryId : null)
                        equals (int?)country.Id into countryJoin
                    from country in countryJoin.DefaultIfEmpty()

                    join state in _context.States
                        on (c.StateId > 0 ? c.StateId : null)
                        equals (int?)state.Id into stateJoin
                    from state in stateJoin.DefaultIfEmpty()

                    join district in _context.Districts
                        on (c.DistrictId > 0 ? c.DistrictId : null)
                        equals (int?)district.Id into districtJoin
                    from district in districtJoin.DefaultIfEmpty()

                    join locality in _context.Localities
                        on (c.LocalityId > 0 ? c.LocalityId : null)
                        equals (int?)locality.Id into localityJoin
                    from locality in localityJoin.DefaultIfEmpty()

                    where c.StaffMemberId == entity.StaffMemberId
                          && c.IsSoftDeleted != true
                    //  IsActive filter hata diya

                    orderby c.Id descending

                    select new GetContactResponseDTO
                    {
                        Id = c.Id,
                        StaffMemberId = c.StaffMemberId.ToString(),

                        ContactName = c.ContactName,
                        ContactNumber = c.ContactNumber,
                        AlternateNumber = c.AlternateNumber,
                        Email = c.Email,
                        IsPrimary = c.IsPrimary,
                        ContactType = c.ContactType,
                        Relation = c.Relation,

                        CountryId = c.CountryId,
                        CountryName = country != null ? country.CountryName : string.Empty,

                        StateId = c.StateId,
                        StateName = state != null ? state.StateName : string.Empty,

                        DistrictId = c.DistrictId,
                        DistrictName = district != null ? district.DistrictName : string.Empty,

                        LocalityId = c.LocalityId,
                        LocalityName = locality != null ? locality.LocalityName : string.Empty,

                        HouseNo = c.HouseNo,
                        LandMark = c.LandMark,
                        Street = c.Street,
                        Address = c.Address,

                        Remark = c.Remark,
                        Description = c.Description,

                        //  Completion %
                        CompletionPercentage = CompletionCalculatorHelper.ContactPropCalculate(
                                 new GetContactResponseDTO
                                         {
                                     Id = c.Id,
             ContactType = c.ContactType,
             Relation = c.Relation,
             ContactName = c.ContactName,
             ContactNumber = c.ContactNumber,
             AlternateNumber = c.AlternateNumber,
             Email = c.Email,
             IsPrimary = c.IsPrimary,
             HouseNo = c.HouseNo,
             CountryId = c.CountryId,
             StateId = c.StateId,
             DistrictId = c.DistrictId,
             Address = c.Address
         })
                    };


                //  Pagination (same filter)
                var totalRecords = await _context.StaffMemberContacts
                    .AsNoTracking()
                    .Where(x =>
                        x.StaffMemberId == entity.StaffMemberId &&
                        x.IsSoftDeleted != true)
                    .CountAsync();

                var records = await query.Take(10).ToListAsync();


                return new PagedResponseDTO<GetContactResponseDTO>
                {
                    Data = records,
                    TotalCount = totalRecords,
                    PageNumber = 1,
                    PageSize = 10
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Error while adding/fetching Contact info for StaffMemberId: {StaffMemberId}",
                    entity.StaffMemberId);

                throw;
            }
        }
        public async Task<PagedResponseDTO<GetContactResponseDTO>> GetInfo(long staffMemberId, GetContactRequestDTO dto)
        {
            try
            {
                //  SAFETY
                if (dto == null || staffMemberId <= 0)
                    throw new Exception("Invalid StaffMemberId.");


                //  QUERY (NULL SAFE LEFT JOINS)
                var records = await
                (
                    from c in _context.StaffMemberContacts.AsNoTracking()

                        //  COUNTRY LEFT JOIN (NULL SAFE)
                    join country in _context.Countries
                        on c.CountryId equals (int?)country.Id into countryJoin
                    from country in countryJoin.DefaultIfEmpty()

                        //  STATE LEFT JOIN
                    join state in _context.States
                        on c.StateId equals (int?)state.Id into stateJoin
                    from state in stateJoin.DefaultIfEmpty()

                        //  DISTRICT LEFT JOIN (0 / NULL SAFE)
                    join district in _context.Districts
                        on c.DistrictId equals (int?)district.Id into districtJoin
                    from district in districtJoin.DefaultIfEmpty()

                    join locality in _context.Localities
                        on c.LocalityId equals (int?)locality.Id into localityJoin
                    from locality in localityJoin.DefaultIfEmpty()

                    where
                        c.StaffMemberId == staffMemberId &&
                        (c.IsSoftDeleted == false || c.IsSoftDeleted == null)

                    orderby c.Id descending   //  LATEST FIRST

                    select new GetContactResponseDTO
                    {
                        Id = c.Id,                //  STRING
                        StaffMemberId = c.StaffMemberId.ToString(),

                        ContactName = c.ContactName ?? string.Empty,
                        ContactNumber = c.ContactNumber ?? string.Empty,
                        AlternateNumber = c.AlternateNumber,
                        Email = c.Email,
                        Relation = c.Relation,
                        ContactType = c.ContactType,

                        Address = c.Address,
                        HouseNo = c.HouseNo,
                        Street = c.Street,
                        LandMark = c.LandMark,

                        CountryId = c.CountryId,
                        CountryName = country != null ? country.CountryName : string.Empty,

                        StateId = c.StateId,
                        StateName = state != null ? state.StateName : string.Empty,

                        DistrictId = c.DistrictId,
                        DistrictName = district != null ? district.DistrictName : string.Empty,

                        LocalityId = c.LocalityId,
                        LocalityName = locality != null ? locality.LocalityName : string.Empty,

                        IsPrimary = c.IsPrimary,
                        IsActive = c.IsActive,
                        IsEditAllowed = c.IsEditAllowed,
                        IsInfoVerified = c.IsInfoVerified,

                        Remark = c.Remark,
                        Description = c.Description
                    }
                )
                .Take(10)
                .ToListAsync();


                // =========================================================
                // 7️⃣ Detect PRIMARY existence (LIST LEVEL)
                // =========================================================
                bool hasAtLeastOnePrimary = records.Any(x => x.IsPrimary);

                // =========================================================
                // 8️⃣ Per-record Completion (CORRECT LOGIC)
                // =========================================================
                foreach (var item in records)
                {
                    item.CompletionPercentage = hasAtLeastOnePrimary
                        ? CompletionCalculatorHelper.ContactPropCalculate(item)
                        : CompletionCalculatorHelper.ContactPropCalculate_NoPrimary(item);
                }


                // =========================================================
                //  UI Average Completion (ALWAYS show average)
                // =========================================================
                double uiAverageCompletion = records.Any()
                    ? Math.Round(records.Average(x => x.CompletionPercentage), 0)
                    : 0;


                return new PagedResponseDTO<GetContactResponseDTO>
                {
                    Data = records,
                    TotalCount = records.Count,
                    PageNumber = 1,
                    PageSize = 10,
                    TotalPages = 1,
                    CompletionPercentage = uiAverageCompletion,
                    HasUploadedAll = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching contacts for StaffMemberId: {StaffMemberId}",
                    staffMemberId);

                throw;
            }
        }





        public Task<PagedResponseDTO<GetContactResponseDTO>> AutoCreatedAsync(StaffMemberContact entity)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> DeleteAsync(StaffMemberContact staffMemberContact)
        {
            try
            {


                int affectedRows = await _context.SaveChangesAsync();

                if (affectedRows > 0)
                {
                    _logger.LogInformation("✔ Contact record updated successfully");

                    return true;
                }

                //  No Row Updated means something unexpected (maybe no actual new values)
                _logger.LogWarning("⚠ No changes detected | ContactId: {Id}", staffMemberContact.Id);
                return false;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "❌ Concurrency conflict while updating Contact record | Id: {Id}", staffMemberContact.Id);
                throw new Exception("Record update failed due to concurrency conflict. Please retry.");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "❌ Database error during update | Id: {Id}", staffMemberContact.Id);
                throw new Exception("Database error while updating Contact record.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error occurred while updating Contact record | Id: {Id}", staffMemberContact.Id);
                throw new Exception($"Unexpected update failure: {ex.Message}");
            }
        }

        public async Task<bool> UpdateContactAsync(StaffMemberContact staffMemberContact)
        {
            try
            {
                //  SINGLE PRIMARY RULE (Repository level safety)
                if (staffMemberContact.IsPrimary == true)
                {
                    var existingPrimaries = await _context.StaffMemberContacts
                        .Where(x =>
                            x.StaffMemberId == staffMemberContact.StaffMemberId &&
                            x.IsPrimary == true &&
                            x.Id != staffMemberContact.Id &&
                            x.IsSoftDeleted != true)
                        .ToListAsync();

                    if (existingPrimaries.Any())
                    {
                        foreach (var item in existingPrimaries)
                        {
                            item.IsPrimary = false;
                            item.UpdatedById = staffMemberContact.UpdatedById;
                            item.UpdatedDateTime = DateTime.UtcNow;
                        }
                    }
                }

                //  Update current contact
                _context.StaffMemberContacts.Update(staffMemberContact);

                int affectedRows = await _context.SaveChangesAsync();

                if (affectedRows > 0)
                {
                    _logger.LogInformation(
                        "✔ Contact updated successfully | ContactId: {Id} | StaffMemberId: {StaffMemberId}",
                        staffMemberContact.Id,
                        staffMemberContact.StaffMemberId);

                    return true;
                }

                _logger.LogWarning(
                    "⚠ No changes detected | ContactId: {Id}",
                    staffMemberContact.Id);

                return false;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Concurrency conflict while updating Contact | Id: {Id}",
                    staffMemberContact.Id);

                throw new Exception(
                    "Record update failed due to concurrency conflict. Please retry.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Database error while updating Contact | Id: {Id}",
                    staffMemberContact.Id);

                throw new Exception(
                    "Database error while updating Contact record.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Unexpected error while updating Contact | Id: {Id}",
                    staffMemberContact.Id);

                throw;
            }
        }

        public async Task<StaffMemberContact?> GetPrimaryLocationAsync( long staffMemberId,  bool isActive, bool track = true )
        {
            {
                IQueryable<StaffMemberContact> query = _context.StaffMemberContacts.Where(x => x.IsSoftDeleted != true && x.StaffMemberId == staffMemberId);

                if (!track)
                    query = query.AsNoTracking();

                return await query.FirstOrDefaultAsync(x => x.IsActive == isActive && x.IsPrimary== true);
            }

        }

        public async Task<StaffMemberContact?> GetSingleRecordAsync(long id, bool track = true)
        {
            IQueryable<StaffMemberContact> query =
                _context.StaffMemberContacts
                    .Where(x =>
                        x.Id == id &&
                        (x.IsSoftDeleted == false || x.IsSoftDeleted == null));

            if (!track)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync();
        }



    }

    #endregion
}
