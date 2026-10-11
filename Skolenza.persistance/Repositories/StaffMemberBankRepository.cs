// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Provides persistence operations for StaffMember Bank records.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers.PercentageHelper;
using skolenza.application.DTOS.Staff.Bank;
using skolenza.application.DTOS.Staff.CompletionPercentage;
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
    /// Provides persistence operations for StaffMemberBank records.
    /// </summary>
    public class StaffMemberBankRepository : IStaffMemberBankRepository
    {
        private readonly SkolenzaDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<StaffMemberBankRepository> _logger;

        private readonly IPasswordService _passwordService;
        private readonly IEncryptionService _encryptionService;
        IFileStorageService _fileStorageService;
        public StaffMemberBankRepository(SkolenzaDbContext context, IMapper mapper, ILogger<StaffMemberBankRepository> logger,
            IPasswordService passwordService, IEncryptionService encryptionService, IFileStorageService fileStorageService)
        {
            this._context = context;
            this._mapper = mapper;
            this._logger = logger;
            _fileStorageService = fileStorageService;
            _passwordService = passwordService;
            _encryptionService = encryptionService;

        }
        public async Task<PagedResponseDTO<GetBankResponseDTO>> CreateAsync(StaffMemberBankDetail entity)
        {
            try
            {
                // -----------------------------
                // 1️⃣ Insert Record
                // -----------------------------
                if (entity.IsPrimaryAccount)
                    await ResetPrimaryAccountAsync(entity.StaffMemberId, entity.AddedById);
                await _context.StaffMemberBankDetails.AddAsync(entity);
                await _context.SaveChangesAsync();

                // -----------------------------
                // 2️⃣ Pagination defaults
                // -----------------------------
                int pageNumber = 1;
                int pageSize = 10;

                // -----------------------------
                // 3️⃣ Base Query (latest first)
                // -----------------------------
                var baseQuery = _context.StaffMemberBankDetails
                    .AsNoTracking()
                    .Where(x =>
                        x.StaffMemberId == entity.StaffMemberId &&
                        (x.IsSoftDeleted !=true)
                    )
                    .OrderByDescending(x => x.Id);

                int totalRecords = await baseQuery.CountAsync();

                // -----------------------------
                // 4️⃣ Pagination + Projection
                // -----------------------------
                var records = await baseQuery
                   .Skip((pageNumber - 1) * pageSize)
                      .Take(pageSize)
                       .Select(bank => new GetBankResponseDTO
      {
          Id = bank.Id,
          StaffMemberId = bank.StaffMemberId.ToString(),
          BankName = bank.BankName,
          AccountNumber = bank.AccountNumber,
          AccountType = bank.AccountType,
          IFSCCode = bank.Ifsccode,
          BranchName = bank.BranchName,
          IsPrimaryAccount = bank.IsPrimaryAccount,
          IsInfoVerified = bank.IsInfoVerified,
          IsEditAllowed = bank.IsEditAllowed,
          IsActive = bank.IsActive,
          HasChequeDocUploaded = bank.HasChequeDocUploaded,
          FilePath = bank.FilePath,
          FileName = bank.FileName
            })
      .ToListAsync();


                // =========================================================
                // 7️⃣ Detect PRIMARY existence (LIST LEVEL)
                // =========================================================
                bool hasAtLeastOnePrimary = records.Any(x => x.IsPrimaryAccount);

                // =========================================================
                // 8️⃣ Per-record Completion (CORRECT LOGIC)
                // =========================================================
                foreach (var item in records)
                {
                    item.CompletionPercentage = hasAtLeastOnePrimary
                        ? CompletionCalculatorHelper.BankPropCalculate(item)
                        : CompletionCalculatorHelper.BankPropCalculate_NoPrimary(item);
                }

                // =========================================================
                // 9️⃣ Section-level Mandatory Rules
                // =========================================================
                bool hasUploadedAllDocs = hasAtLeastOnePrimary &&
                    records
                        .Where(x => x.IsPrimaryAccount)
                        .All(x => x.HasChequeDocUploaded);

                // =========================================================
                //  UI Average Completion (ALWAYS show average)
                // =========================================================
                double uiAverageCompletion = records.Any()
                    ? Math.Round(records.Average(x => x.CompletionPercentage), 0)
                    : 0;

                // =========================================================
                //  Final Completion (Business Rule Applied)
                // =========================================================
                double finalCompletionPercentage = uiAverageCompletion;

                //  HARD BUSINESS RULE
                // No primary ⇒ section invalid
                if (!hasAtLeastOnePrimary)
                {
                    finalCompletionPercentage = 0;
                }


                // -----------------------------
                // 7️⃣ Final Response
                // -----------------------------
                return new PagedResponseDTO<GetBankResponseDTO>
                {
                    Data = records,
                    TotalCount = totalRecords,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),

                    //  IMPORTANT (same as GetInfo)
                    CompletionPercentage = finalCompletionPercentage,
                    HasUploadedAll = hasUploadedAllDocs
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Error occurred while creating bank record for StaffMemberId: {StaffMemberId}",
                    entity.StaffMemberId
                );
                throw;
            }
        }
        public async Task<bool> UpdateVerificationStatus(
     long staffMemberId,
     long userId,
     bool status)
        {
            int affectedRows = await _context.StaffMemberBankDetails
                .Where(x =>
                    x.StaffMemberId == staffMemberId &&
                    x.IsSoftDeleted != true)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.IsInfoVerified, status)
                    .SetProperty(x => x.InfoVerifiedById, userId)
                    .SetProperty(x => x.InfoVerifiedDateTime, DateTime.UtcNow)
                );

            return affectedRows > 0;
        }
        /*

         */

        public async Task<bool> UpdateEditStatus(
    long staffMemberId,
    long userId,
    bool status)
        {
            var affectedRows = await _context.StaffMemberBankDetails
                   .Where(x =>
                    x.StaffMemberId == staffMemberId &&
                    x.IsSoftDeleted != true)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.IsEditAllowed, status)
                    .SetProperty(x => x.UpdatedById, userId)
                    .SetProperty(x => x.InfoVerifiedDateTime, DateTime.UtcNow)
                );

            return affectedRows > 0;
        }



        public async Task<bool> DeleteAsync(StaffMemberBankDetail staffMemberBankDetail)
        {
            try
            {
                if (staffMemberBankDetail == null)
                {
                    _logger.LogWarning("⚠️ Delete failed: StaffMemberBankDetail entity is null");
                    return false;
                }

                // Entity already tracked hai (GetSingleRecordAsync(track=true) se aaya)
                _context.StaffMemberBankDetails.Update(staffMemberBankDetail);

                int affectedRows = await _context.SaveChangesAsync();

                if (affectedRows > 0)
                {
                    _logger.LogInformation(
                        "✅ Bank record soft-deleted successfully | BankId: {Id}",
                        staffMemberBankDetail.Id);

                    return true;
                }

                _logger.LogWarning(
                    "⚠️ No rows affected while deleting bank record | BankId: {Id}",
                    staffMemberBankDetail.Id);

                return false;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Concurrency issue while deleting bank record | BankId: {Id}",
                    staffMemberBankDetail.Id);

                throw new Exception(
                    "Record could not be deleted due to concurrent update. Please retry.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Database error while deleting bank record | BankId: {Id}",
                    staffMemberBankDetail.Id);

                throw new Exception(
                    "Database error occurred while deleting bank record.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Unexpected error while deleting bank record | BankId: {Id}",
                    staffMemberBankDetail.Id);

                throw;
            }
        }

        public async Task<bool> ResetPrimaryAccountAsync(long staffMemberId,  long byUserId)
        {
            try
            {
                var primaries = await _context.StaffMemberBankDetails
                    .Where(x =>
                        x.StaffMemberId == staffMemberId &&
                        x.IsPrimaryAccount == true &&
                        x.IsSoftDeleted != true)
                    .ToListAsync();

                if (!primaries.Any())
                    return true; // nothing to reset

                foreach (var item in primaries)
                {
                    item.IsPrimaryAccount = false;
                    item.UpdatedDateTime = DateTime.UtcNow;
                      item.UpdatedById = byUserId; // if available
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // optional logger
                _logger?.LogError(ex,
                    "❌ Failed to reset primary bank accounts for StaffMemberId: {StaffMemberId}",
                    staffMemberId);

                return false;
            }
        }

        public async Task<PagedResponseDTO<GetBankResponseDTO>> GetInfoAsync(long staffMemberId, GetBankReqestDTO dto)
        {
            try
            {

                // =========================================================
                // 1️⃣ Pagination & Sorting
                // =========================================================
                int pageNumber = dto.PageNumber <= 0 ? 1 : dto.PageNumber;
                int pageSize = dto.PageSize <= 0 ? 10 : dto.PageSize;

                string sortBy = dto.SortBy?.ToLower() ?? "id";
                bool isDescending = (dto.SortOrder?.ToLower() ?? "desc") == "desc";

                // =========================================================
                // 2️⃣ Base Query
                // =========================================================
                IQueryable<StaffMemberBankDetail> query = _context.StaffMemberBankDetails
                    .AsNoTracking()
                    .Where(x =>
                        x.StaffMemberId == staffMemberId &&
                        (x.IsSoftDeleted == false || x.IsSoftDeleted == null)
                    );

                // =========================================================
                // 3️⃣ Filters
                // =========================================================
                if (dto.Id.HasValue && dto.Id.Value > 0)
                    query = query.Where(x => x.Id == dto.Id.Value);

                if (dto.IsActive)
                    query = query.Where(x => x.IsActive == dto.IsActive);

                if (dto.HasChequeDocUploaded)
                    query = query.Where(x => x.HasChequeDocUploaded == dto.HasChequeDocUploaded);

                if (dto.IsPrimaryAccount.HasValue)
                    query = query.Where(x => x.IsPrimaryAccount == dto.IsPrimaryAccount.Value);

                if (dto.IsInfoVerified.HasValue)
                    query = query.Where(x => x.IsInfoVerified == dto.IsInfoVerified.Value);

                if (dto.IsEditAllowed.HasValue)
                    query = query.Where(x => x.IsEditAllowed == dto.IsEditAllowed.Value);

                if (!string.IsNullOrWhiteSpace(dto.AccountType))
                    query = query.Where(x => x.AccountType.Contains(dto.AccountType));

                // =========================================================
                // 4️⃣ Sorting
                // =========================================================
                query = sortBy switch
                {
                    "bankname" => isDescending ? query.OrderByDescending(x => x.BankName) : query.OrderBy(x => x.BankName),
                    "accountnumber" => isDescending ? query.OrderByDescending(x => x.AccountNumber) : query.OrderBy(x => x.AccountNumber),
                    "branchname" => isDescending ? query.OrderByDescending(x => x.BranchName) : query.OrderBy(x => x.BranchName),
                    "accounttype" => isDescending ? query.OrderByDescending(x => x.AccountType) : query.OrderBy(x => x.AccountType),
                    "haschequedocuploaded" => isDescending ? query.OrderByDescending(x => x.HasChequeDocUploaded) : query.OrderBy(x => x.HasChequeDocUploaded),
                    _ => isDescending ? query.OrderByDescending(x => x.Id) : query.OrderBy(x => x.Id)
                };

                // =========================================================
                // 5️⃣ Count
                // =========================================================
                int totalCount = await query.CountAsync();

                // =========================================================
                // 6️⃣ Pagination + Projection
                // =========================================================
                var records = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new GetBankResponseDTO
                    {
                        Id = x.Id,
                        StaffMemberId = x.StaffMemberId.ToString(),
                        BankName = x.BankName,
                        AccountNumber = x.AccountNumber,
                        IFSCCode = x.Ifsccode,
                        BranchName = x.BranchName,
                        AccountType = x.AccountType,
                        IsPrimaryAccount = x.IsPrimaryAccount,
                        IsActive = x.IsActive,
                        IsInfoVerified = x.IsInfoVerified,
                        IsEditAllowed = x.IsEditAllowed,
                        HasChequeDocUploaded = x.HasChequeDocUploaded,
                        //  Correct FilePath handling
                        FilePath =  x.FilePath,
                        FileName = x.FileName,
                        UPIId = x.Upiid   }).ToListAsync();

                // =========================================================
                // 7️⃣ Detect PRIMARY existence (LIST LEVEL)
                // =========================================================
                bool hasAtLeastOnePrimary = records.Any(x => x.IsPrimaryAccount);

                // =========================================================
                // 8️⃣ Per-record Completion (CORRECT LOGIC)
                // =========================================================
                //foreach (var item in records)
                //{
                //    item.CompletionPercentage = hasAtLeastOnePrimary? CompletionCalculatorHelper.BankPropCalculate(item): CompletionCalculatorHelper.BankPropCalculate_NoPrimary(item);
                //}
                foreach (var item in records)
                {
                    if (hasAtLeastOnePrimary)
                    {
                        item.CompletionPercentage = CompletionCalculatorHelper.BankPropCalculate(item);
                    }
                    else
                    {
                        item.CompletionPercentage = CompletionCalculatorHelper.BankPropCalculate_NoPrimary(item);
                    }
                }

                // =========================================================
                // 9️⃣ Section-level Mandatory Rules
                // =========================================================
                bool hasUploadedAllDocs = hasAtLeastOnePrimary &&
                    records
                        .Where(x => x.IsPrimaryAccount)
                        .All(x => x.HasChequeDocUploaded);

                // =========================================================
                //  UI Average Completion (ALWAYS show average)
                // =========================================================
                double uiAverageCompletion = records.Any()
                    ? Math.Round(records.Average(x => x.CompletionPercentage), 0)
                    : 0;

                // =========================================================
                //  Final Completion (Business Rule Applied)
                // =========================================================
                double finalCompletionPercentage = uiAverageCompletion;

                //  HARD BUSINESS RULE
                // No primary ⇒ section invalid
                if (!hasAtLeastOnePrimary)
                {
                    finalCompletionPercentage = 0;
                }



                // =========================================================
                // 1️⃣1️⃣ Final Response
                // =========================================================
                return new PagedResponseDTO<GetBankResponseDTO>
                {
                    Data = records,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),

                    //  UI ko business-rule validated progress dikhao.
                    CompletionPercentage = finalCompletionPercentage,

                    HasUploadedAll = hasUploadedAllDocs
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Error fetching bank info for StaffMemberId: {StaffMemberId}",
                    staffMemberId
                );

                throw new Exception($"Failed to fetch bank information: {ex.Message}");
            }
        }



        public async Task<bool> UpdateAsync(StaffMemberBankDetail entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            _context.StaffMemberBankDetails.Update(entity);
            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<StaffMemberBankDetail?> GetSingleRecordAsync(int id, bool isActive)
        {
            try
            {
                var entity = await _context.StaffMemberBankDetails
                    .Where(b =>
                        b.Id == id &&
                        b.IsActive == isActive &&
                        b.IsSoftDeleted != true)
                    .FirstOrDefaultAsync();

                if (entity == null)
                {
                    _logger.LogWarning(
                        "⚠️ No active bank info found | Id: {Id}, IsActive: {IsActive}",
                        id, isActive);

                    return null;
                }

                _logger.LogInformation(
                    "✅ Bank info record fetched successfully | Id: {Id}",
                    id);

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Error while fetching single bank record | Id: {Id}",
                    id);

                throw;
            }
        }













    }

    #endregion
}
