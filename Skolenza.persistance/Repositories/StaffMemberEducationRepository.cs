// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Provides persistence operations for StaffMember Education records.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers.PercentageHelper;
using skolenza.application.DTOS.Staff.CompletionPercentage;
using skolenza.application.DTOS.Staff.Dependent;
using skolenza.application.DTOS.Staff.Education;
using skolenza.application.DTOS.Pagination;
using skolenza.application.Extentions;
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
    /// Provides persistence operations for StaffMemberEducation records.
    /// </summary>
    public class StaffMemberEducationRepository : IStaffMemberEducationRepository
    {
        private readonly SkolenzaDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<StaffMemberEducationRepository> _logger;

        private readonly IPasswordService _passwordService;
        private readonly IEncryptionService _encryptionService;
        IFileStorageService _fileStorageService;
        public StaffMemberEducationRepository(SkolenzaDbContext context, IMapper mapper, ILogger<StaffMemberEducationRepository> logger,
            IPasswordService passwordService, IEncryptionService encryptionService, IFileStorageService fileStorageService)
        {
            // <SkolenzaDbContext> contextFactory,
            this._context = context;
            this._mapper = mapper;
            this._logger = logger;
            //
            _passwordService = passwordService;
            _encryptionService = encryptionService;
            _fileStorageService = fileStorageService;
        }

        public async Task<PagedResponseDTO<GetEducationResponseDTO>> CreateAsync(StaffMemberEducation entity)
        {
            try
            {
               // await using var _context = await _contextFactory.CreateDbContextAsync();

                //  1️⃣ Validation
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity), "Dependent info entity cannot be null.");

                if (entity.StaffMemberId <= 0)
                    throw new ArgumentException("Invalid StaffMemberId provided.");

                //  2️⃣ Record insert karo
                await _context.StaffMemberEducations.AddAsync(entity);
                await _context.SaveChangesAsync();

                //  3️⃣ Fetch updated list (latest record ke sath)
                var query = _context.StaffMemberEducations
                    .AsNoTracking()
                    .Where(x => x.StaffMemberId == entity.StaffMemberId && x.IsSoftDeleted != true)
                    .OrderByDescending(x => x.Id);

                var totalRecords = await query.CountAsync();

                //  4️⃣ Fetch paginated data
                var records = await query.Take(10).ToListAsync();

                //  5️⃣ Map to DTOs
                var responseData = _mapper.Map<List<GetEducationResponseDTO>>(records);

                //  6️⃣ Prepare PagedResponse
                return  new PagedResponseDTO<GetEducationResponseDTO>
                {
                    Data = responseData,
                    TotalCount = totalRecords,
                    PageNumber = 1,
                    PageSize = 10,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error occurred while adding/fetching Dependent info for StaffMemberId: {StaffMemberId}", entity.StaffMemberId);
                throw new Exception($"Failed to add or fetch Dependent info: {ex.Message}");
            }
        }

        public async Task<PagedResponseDTO<GetEducationResponseDTO>> GetInfo(long staffMemberId, GetEducationRequestDTO dto)
        {
            double averagePercentage = 0;
            bool hasUploadedAll = false;

            try
            {
                // -----------------
                // Pagination defaults
                // -----------------
                int pageNumber = dto.PageNumber <= 0 ? 1 : dto.PageNumber;
                int pageSize = dto.PageSize <= 0 ? 10 : dto.PageSize;

                string sortBy = dto.SortBy?.ToLower() ?? "id";
                bool isDescending = (dto.SortOrder?.ToLower() ?? "desc") == "desc";

                // -----------------
                // Base Query
                // -----------------
                var baseQuery = _context.StaffMemberEducations
                    .AsNoTracking()
                    .Where(edu =>
                        edu.StaffMemberId == staffMemberId &&
                        (dto.IsActive == null || edu.IsActive == dto.IsActive) &&
                        (edu.IsSoftDeleted != true)
                    );

                // -----------------
                // Filters
                // -----------------

                if (!string.IsNullOrEmpty(dto.InstituteName))
                    baseQuery = baseQuery.Where(x => x.InstituteName.Contains(dto.InstituteName));

                if (!string.IsNullOrEmpty(dto.Degree))
                    baseQuery = baseQuery.Where(x => x.Degree.Contains(dto.Degree));

                if (dto.EducationGap.HasValue)
                    baseQuery = baseQuery.Where(x => x.EducationGap == dto.EducationGap);

                if (dto.IsInfoVerified.HasValue)
                    baseQuery = baseQuery.Where(x => x.IsInfoVerified == dto.IsInfoVerified);

                if (dto.IsEditAllowed.HasValue)
                    baseQuery = baseQuery.Where(x => x.IsEditAllowed == dto.IsEditAllowed);

                // -----------------
                // Sorting
                // -----------------
                baseQuery = sortBy switch
                {
                    "degree" => isDescending ? baseQuery.OrderByDescending(x => x.Degree) : baseQuery.OrderBy(x => x.Degree),
                    "institutename" => isDescending ? baseQuery.OrderByDescending(x => x.InstituteName) : baseQuery.OrderBy(x => x.InstituteName),
                    "haseducationdocuploded" => isDescending ? baseQuery.OrderByDescending(x => x.HasEducationDocUploded) : baseQuery.OrderBy(x => x.HasEducationDocUploded),
                    "startdate" => isDescending ? baseQuery.OrderByDescending(x => x.StartDate) : baseQuery.OrderBy(x => x.StartDate),
                    "enddate" => isDescending ? baseQuery.OrderByDescending(x => x.EndDate) : baseQuery.OrderBy(x => x.EndDate),
                    _ => isDescending ? baseQuery.OrderByDescending(x => x.Id) : baseQuery.OrderBy(x => x.Id)
                };

                // -----------------
                // Count & Fetch
                // -----------------
                var totalRecords = await baseQuery.CountAsync();

                var eduList = await baseQuery
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // -----------------
                // Mapping + Per Record Completion Calculation
                // -----------------
                List<GetEducationResponseDTO> finalList = new();
                double totalPercentage = 0;

                foreach (var edu in eduList)
                {
                    var dtoItem = new GetEducationResponseDTO
                    {
                        Id = edu.Id,
                        StaffMemberId = edu.StaffMemberId.ToString(),
                        Degree = edu.Degree,
                        InstituteName = edu.InstituteName,
                        Remark = edu.Remark,
                        ScoreValue = edu.ScoreValue,
                        ScoreType = edu.ScoreType?.ToString(),
                        GradeDivision = edu.GradeDivision,
                        EducationGap = edu.EducationGap,
                        ReasonOfEducationGap = edu.ReasonOfEducationGap,
                        StartDate = edu.StartDate,
                        EndDate = edu.EndDate,
                        //  FIXED
                       // FilePath = !string.IsNullOrEmpty(edu.FilePath) ? _fileStorageService.GetFileUrl(edu.FilePath)  : null,
                         FilePath = edu.FilePath  ,
                        FileType = edu.FileType?? 0,
                        FileName = edu.FileName,
                        IsActive = edu.IsActive,
                        IsEditAllowed = edu.IsEditAllowed,
                        IsInfoVerified = edu.IsInfoVerified,
                        InfoVerifiedById = edu.InfoVerifiedById?.ToString(),
                        HasEducationDocUploded = edu.HasEducationDocUploded,


                    };

                    // -----------------
                    // Per-record completion calculation
                    // -----------------
                    dtoItem.CompletionPercentage = CompletionCalculatorHelper.EduPropCalculate(dtoItem);

                    totalPercentage += dtoItem.CompletionPercentage;

                    finalList.Add(dtoItem);
                }

                // -----------------
                // Average Percentage
                // -----------------
                averagePercentage = finalList.Count > 0 ? Math.Round(totalPercentage / finalList.Count, 0) : 0;

                // Section-level completion
                var educationSection = eduList.CalculateEducationCompletion();

                // All documents uploaded?
                hasUploadedAll = finalList.All(x => x.HasEducationDocUploded == true);

                // -----------------
                // Final Paged Response
                // -----------------
                return new PagedResponseDTO<GetEducationResponseDTO>
                {
                    Data = finalList,
                    TotalCount = totalRecords,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                    CompletionPercentage = educationSection.CompletionPercent,
                    HasUploadedAll = hasUploadedAll
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching education info for StaffMemberId: {StaffMemberId}", staffMemberId);
                throw new Exception($"Failed: {ex.Message}");
            }
        }


        public async Task<bool> UpdateStaffMemberFieldAsync(StaffMemberEducation entity)
        {
           // await using var _context = await _contextFactory.CreateDbContextAsync();

            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                _context.StaffMemberEducations.Update(entity);

                int affectedRows = await _context.SaveChangesAsync();

                return affectedRows > 0;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "⚠ Concurrency conflict while updating StaffMemberEducation Id={Id}", entity.Id);
                return false;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "⚠ Database update failure for StaffMemberEducation Id={Id}", entity.Id);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unexpected error while updating StaffMemberEducation Id={Id}", entity.Id);
                return false;
            }
        }


        public async Task<StaffMemberEducation?> GetSingleRecordAsync(long Id, bool IsActive)
        {
            try
            {
             //   using var context = await _contextFactory.CreateDbContextAsync();

                return await _context.StaffMemberEducations
                    .Where(x => x.Id == Id && x.IsActive == IsActive && x.IsSoftDeleted!=true)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching education record. Id: {Id}, Error: {ex.Message}");
                throw;
            }
        }




        public async Task<bool> DeleteAsync(StaffMemberEducation entity)
        {
            try
            {
                _context.StaffMemberEducations.Update(entity);

                int affectedRows = await _context.SaveChangesAsync();

                if (affectedRows > 0)
                {
                    _logger.LogInformation(
                        "✔ Education record soft deleted | Id: {Id}",
                        entity.Id);

                    return true;
                }

                _logger.LogWarning(
                    "⚠ No rows affected while deleting education record | Id: {Id}",
                    entity.Id);

                return false;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(
                    ex,
                    "❌ Concurrency issue while deleting education record | Id: {Id}",
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


    }




    #endregion
}












