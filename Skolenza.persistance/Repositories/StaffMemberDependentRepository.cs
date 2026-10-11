// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Provides persistence operations for StaffMember Dependent records.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers.PercentageHelper;
using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.DTOS.Staff.Contact;
using skolenza.application.DTOS.Staff.Dependent;
using skolenza.application.DTOS.Pagination;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IFileStorage;
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
    /// Provides persistence operations for StaffMemberDependent records.
    /// </summary>
    public class StaffMemberDependentRepository : IStaffMemberDependentRepository
    {
        private readonly SkolenzaDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<StaffMemberDependentRepository> _logger;

        private readonly IPasswordService _passwordService;
        private readonly IEncryptionService _encryptionService;
        private readonly IFileStorageService _fileStorageService;
        public StaffMemberDependentRepository(SkolenzaDbContext context, IMapper mapper, ILogger<StaffMemberDependentRepository> logger,
            IPasswordService passwordService, IEncryptionService encryptionService, IFileStorageService fileStorageService)
        {
            this._context = context;
            this._mapper = mapper;
            this._logger = logger;

            _passwordService = passwordService;
            _encryptionService = encryptionService;
            _fileStorageService = fileStorageService;

        }

        public async Task<PagedResponseDTO<GetDependentResponseDTO>> CreateAsync(StaffMemberDependent entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity), "Dependent info entity cannot be null.");

                if (entity.StaffMemberId <= 0)
                    throw new ArgumentException("Invalid StaffMemberId provided.");

                //  Insert
                await _context.StaffMemberDependents.AddAsync(entity);
                await _context.SaveChangesAsync();

                //  Base query
                var query = _context.StaffMemberDependents
                    .AsNoTracking()
                    .Where(x =>
                        x.StaffMemberId == entity.StaffMemberId &&
                        x.IsSoftDeleted != true &&
                        x.IsActive == true)
                    .OrderByDescending(x => x.Id);

                var totalRecords = await query.CountAsync();

                //  SAFE DEFAULTS
                int pageNumber = 1;
                int pageSize = 10;
                var records = await query.Take(pageSize).ToListAsync();

                //  Map
                var responseData = _mapper.Map<List<GetDependentResponseDTO>>(records);

                //  Completion calculation (FAST & CLEAN)
                foreach (var item in responseData)
                {
                    item.CompletionPercentage =
                        CompletionCalculatorHelper.DependentPropCalculate(item);

                }



                return new PagedResponseDTO<GetDependentResponseDTO>
                {
                    Data = responseData,
                    TotalCount = totalRecords,
                    PageNumber = 1,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                    CompletionPercentage = responseData.Any() ? Math.Round(responseData.Average(x => x.CompletionPercentage), 0) : 0,
                    HasUploadedAll = responseData.All(x => x.HasProofUploaded)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Error occurred while adding/fetching Dependent info for StaffMemberId: {StaffMemberId}",
                    entity.StaffMemberId);

                throw new Exception($"Failed to add or fetch Dependent info: {ex.Message}");
            }
        }
        public async Task<bool> UpdateAsync(StaffMemberDependent dependent)
        {
            try
            {
                if (dependent == null)
                    return false;

                // Attach & mark modified (safe for tracked/untracked both)
                _context.StaffMemberDependents.Update(dependent);

                int affectedRows = await _context.SaveChangesAsync();

                if (affectedRows > 0)
                {
                    _logger.LogInformation(
                        "✔ Dependent updated successfully | DependentId: {Id}",
                        dependent.Id);

                    return true;
                }

                _logger.LogWarning(
                    "⚠ No changes detected while updating dependent | DependentId: {Id}",
                    dependent.Id);

                return false;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Concurrency issue while updating dependent | DependentId: {Id}",
                    dependent.Id);

                throw new Exception(
                    "Dependent update failed due to concurrency conflict. Please retry.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Database error while updating dependent | DependentId: {Id}",
                    dependent.Id);

                throw new Exception(
                    "Database error occurred while updating dependent.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Unexpected error while updating dependent | DependentId: {Id}",
                    dependent.Id);

                throw;
            }
        }
        public async Task<GetDependentsDetailResponseDTO> GetDetailInfo(long staffMemberId, GetDependentRequestDTO dto)
        {
            try
            {
                // ===============================
                //  STEP 1: FETCH DATA
                // ===============================
                var records = await _context.StaffMemberDependents
                    .AsNoTracking()
                    .Where(x =>
                        x.StaffMemberId == staffMemberId &&
                        x.IsSoftDeleted != true &&
                        x.IsActive == true)
                    .OrderByDescending(x => x.Id)
                    .ToListAsync();

                if (records == null || !records.Any())
                {
                    return new GetDependentsDetailResponseDTO();
                }

                // ===============================
                //  STEP 2: MAP + CALCULATE
                // ===============================
                var result = records.Select(x =>
                {
                    //  FILE URL (S3)
                    string? fileUrl = !string.IsNullOrWhiteSpace(x.FilePath)
                        ? _fileStorageService.GetFileUrl(x.FilePath)
                        : null;

                    //  FLAGS
                    bool hasProof = !string.IsNullOrWhiteSpace(x.FilePath);




                    string relationType;

                    if (x.Relation == 1)
                        relationType = "Father";
                    else if (x.Relation == 2)
                        relationType = "Mother";
                    else if (x.Relation == 3)
                        relationType = "Spouse";
                    else if (x.Relation == 4)
                        relationType = "Son";
                    else if (x.Relation == 5)
                        relationType = "Daughter";
                    else if (x.Relation == 6)
                        relationType = "Father-in-law";
                    else if (x.Relation == 7)
                        relationType = "Mother-in-law";
                    else
                        relationType = "Other";
                    return new GetDependentResponseDTO
                    {
                        Id = x.Id,
                        StaffMemberId = x.StaffMemberId.ToString(),

                        DependentName = x.DependentName,

                        Relation = x.Relation,
                        RelationType = relationType, //  helper


                        DateOfBirth = x.DateOfBirth,
                        IsCoveredInPolicy = x.IsCoveredInPolicy,
                        IsMarried = x.IsMarried,

                        Remark = x.Remark,
                        Description = x.Description,

                        IsActive = x.IsActive,

                        HasProofUploaded = hasProof,
                        HasUploadedAll = hasProof, //  can extend later



                        FilePath = fileUrl,

                        InfoVerifiedById = x.InfoVerifiedById?.ToString(),
                        IsInfoVerified = x.IsInfoVerified,
                        IsEditAllowed = x.IsEditAllowed,
                        InfoVerifiedDateTime = x.InfoVerifiedDateTime
                    };
                }).ToList();

                // ===============================
                //  STEP 3: COUNTS
                // ===============================
                var response = new GetDependentsDetailResponseDTO
                {
                    TotalDependents = result.Count,

                    //  Children
                    TotalChilds = result.Count(x =>
                        x.Relation == (int)RelationDependant.CHILDREN),

                    //  Spouse
                    TotalSpouses = result.Count(x =>
                        x.Relation == (int)RelationDependant.SPOUSE),

                    // ‍‍ Parents
                    TotalParents = result.Count(x =>
                        x.Relation == (int)RelationDependant.PARENT),

                    //  In-Laws
                    TotalInLaws = result.Count(x =>
                        x.Relation == (int)RelationDependant.IN_LAWS),

                    Dependents = result
                };

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error while fetching dependent details");

                return new GetDependentsDetailResponseDTO();
            }
        }

        public async Task<PagedResponseDTO<GetDependentResponseDTO>> GetInfo(
            long staffMemberId,
            GetDependentRequestDTO dto)
        {
            try
            {

                //  Fetch ONLY latest 10 dependents for staffMember
                var records = await _context.StaffMemberDependents
                    .AsNoTracking()
                    .Where(x =>
                        x.StaffMemberId == staffMemberId &&
                        x.IsSoftDeleted != true &&
                        x.IsActive == true)
                    .OrderByDescending(x => x.Id)   //  Latest first
                    .Take(10)                       //  Only 10 records
                    .ToListAsync();

                //  Map to DTO
                var responseData = _mapper.Map<List<GetDependentResponseDTO>>(records);

                //  Completion calculation (STANDARD helper)
                foreach (var item in responseData)
                {
                    item.CompletionPercentage =
                        CompletionCalculatorHelper.DependentPropCalculate(item);
                }

                double averageCompletion = responseData.Any()
                    ? Math.Round(responseData.Average(x => x.CompletionPercentage), 0)
                    : 0;

                return new PagedResponseDTO<GetDependentResponseDTO>
                {
                    Data = responseData,
                    TotalCount = responseData.Count, // max 10
                    PageNumber = 1,
                    PageSize = 10,
                    TotalPages = 1,
                    CompletionPercentage = averageCompletion,
                    HasUploadedAll = responseData.All(x => x.HasProofUploaded)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Error fetching dependents for StaffMemberId: {StaffMemberId}",
                    staffMemberId);

                return new PagedResponseDTO<GetDependentResponseDTO>
                {
                    Data = new List<GetDependentResponseDTO>(),
                    TotalCount = 0,
                    PageNumber = 1,
                    PageSize = 10,
                    TotalPages = 0,
                    CompletionPercentage = 0,
                    HasUploadedAll = false
                };
            }
        }


        public async Task<StaffMemberDependent?> GetSingleRecordAsync(long id, bool isActive)
        {
            try
            {
                if (id <= 0)
                    return null;

                return await _context.StaffMemberDependents
                    .Where(x =>
                        x.Id == id &&
                        x.IsActive == isActive &&
                        x.IsSoftDeleted != true)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Error fetching Contact record | ContactId: {Id}",
                    id);

                throw new Exception(
                    $"Failed to fetch contact record with Id {id}.", ex);
            }
        }

        public async Task<bool> DeleteAsync(StaffMemberDependent entity)
        {
            try
            {
                _context.StaffMemberDependents.Update(entity);

                int affectedRows = await _context.SaveChangesAsync();

                if (affectedRows > 0)
                {
                    _logger.LogInformation(
                        "✔ Education record soft deleted | Id: {Id}",
                        entity.Id);

                    return true;
                }

                _logger.LogWarning(
                    "⚠ No rows affected while deleting Dependent record | Id: {Id}",
                    entity.Id);

                return false;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Concurrency issue while deleting StaffMemberDependent record | Id: {Id}",
                    entity.Id);

                throw new Exception("Record was modified by another process.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Database error while deleting education record | Id: {Id}",
                    entity.Id);

                throw new Exception("Database error occurred while deleting record.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Unexpected error while deleting education record | Id: {Id}",
                    entity.Id);

                throw;
            }
        }
        public async Task<List<GetDependentResponseDTO>> GetBulkInfo(List<long> dependentIds)
        {
            try
            {
                if (dependentIds == null || !dependentIds.Any())
                    return new List<GetDependentResponseDTO>();

                //  FETCH DEPENDENTS (BULK)
                var records = await _context.StaffMemberDependents
                    .AsNoTracking()
                    .Where(x =>
                        dependentIds.Contains(x.Id) &&     //  FIXED
                        x.IsSoftDeleted != true &&
                        x.IsActive == true)
                    .OrderByDescending(x => x.Id)
                    .ToListAsync();

                //  MAP TO DTO
                var responseData = _mapper.Map<List<GetDependentResponseDTO>>(records);

                return responseData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in GetBulkInfo");
                return new List<GetDependentResponseDTO>();
            }
        }

        public async Task<bool> UpdateAsyncRangeAsync(List<StaffMemberDependent> dependents)
        {
            try
            {
                if (dependents == null || !dependents.Any())
                    return false;

                //  TRACKING ENABLE (IMPORTANT)
                foreach (var entity in dependents)
                {
                    _context.StaffMemberDependents.Attach(entity);

                    //  MARK ONLY REQUIRED FIELDS (BEST PRACTICE)
                    _context.Entry(entity).Property(x => x.IsCoveredInPolicy).IsModified = true;
                    _context.Entry(entity).Property(x => x.UpdatedDateTime).IsModified = true;
                    _context.Entry(entity).Property(x => x.UpdatedById).IsModified = true;
                }

                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in UpdateAsyncRangeAsync (StaffMemberDependent)");
                throw;
            }
        }
    }



    #endregion
}












