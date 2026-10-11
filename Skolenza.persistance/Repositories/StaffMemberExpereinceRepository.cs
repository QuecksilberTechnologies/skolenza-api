// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Provides persistence operations for StaffMember Expereince records.
// ================================================================


using AutoMapper;
using skolenza.application.Common.Helpers.PercentageHelper;
using skolenza.application.DTOS.Staff.Experience;
using skolenza.application.DTOS.Pagination;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Interfaces.IHashed;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;

using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace skolenza.persistance.Repositories
{

    #region Persistence Operations

    /// <summary>
    /// Provides persistence operations for StaffMemberExpereince records.
    /// </summary>
    public class StaffMemberExpereinceRepository : IStaffMemberExperienceRepository
    {
        private readonly SkolenzaDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<StaffMemberExpereinceRepository> _logger;

        private readonly IPasswordService _passwordService;
        private readonly IEncryptionService _encryptionService;
        private readonly IFileStorageService _fileStorageService;
        public StaffMemberExpereinceRepository(SkolenzaDbContext context, IMapper mapper, ILogger<StaffMemberExpereinceRepository> logger,
            IPasswordService passwordService, IEncryptionService encryptionService,IFileStorageService fileStorageService)
        {
            this._context = context;
            this._mapper = mapper;
            this._logger = logger;

            _passwordService = passwordService;
            _encryptionService = encryptionService;
            _fileStorageService = fileStorageService;

        }
        // ===============================
        //  CREATE
        // ===============================
        public async Task<GetStaffMemberExperienceResponseDTO> AddAsync(StaffMemberExperience entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            //  ONLY ADD (NO SAVE)
            await _context.StaffMemberExperiences.AddAsync(entity);

            //  REMOVE THIS LINE
            // await _context.SaveChangesAsync();

            //  Just return empty or minimal DTO (optional)
            return new GetStaffMemberExperienceResponseDTO();
        }
        //public async Task<GetStaffMemberExperienceResponseDTO> AddAsync(StaffMemberExperience entity)
        //{
        //    if (entity == null)
        //        throw new ArgumentNullException(nameof(entity));

        //    //  Add
        //    await _context.StaffMemberExperiences.AddAsync(entity);

        //    //  Save (IMPORTANT )
        //    await _context.SaveChangesAsync();

        //    //  Map to DTO
        //    var result = new GetStaffMemberExperienceResponseDTO
        //    {
        //        Id = entity.Id,
        //        StaffMemberId = entity.StaffMemberId.ToString(),

        //        CompanyName = entity.CompanyName,
        //        Designation = entity.Designation,
        //        StaffMemberIdOfCompany = entity.StaffMemberIdOfCompany,
        //        Ctc = entity.Ctc,

        //        StartDate = entity.StartDate,
        //        EndDate = entity.EndDate,
        //        Experience = entity.Experience,

        //        IsWFH = entity.IsWFH,

        //        WorkingCountryId = entity.WorkingCountryId,
        //        WorkingStateId = entity.WorkingStateId,
        //        WorkingDistrictId = entity.WorkingDistrictId,

        //        IsForeignExperience = entity.IsForeignExperience,

        //        ReasonForLeaving = entity.ReasonForLeaving,
        //        Remark = entity.Remark,

        //        ColleagueName = entity.ColleagueName,
        //        ColleagueDesignation = entity.ColleagueDesignation,
        //        ColleagueContactNumber = entity.ColleagueContactNumber,

        //        ReportingManagerName = entity.ReportingManagerName,
        //        ReportingManagerNumber = entity.ReportingManagerNumber,

        //        VerificationEmail = entity.VerificationEmail,

        //        IsAnyGap = entity.IsAnyGap,
        //        ReasonOfGap = entity.ReasonOfGap,
        //        GapYearFrom = entity.GapYearFrom,
        //        GapYearTo = entity.GapYearTo,

        //        IsEditAllowed = entity.IsEditAllowed,
        //        IsInfoVerified = entity.IsInfoVerified,

        //        Documents = new List<GetStaffMemberExperienceDocumentDTO>()
        //    };

        //    //  Completion calculate
        //    result.CompletionPercentage =
        //        CompletionCalculatorHelper.ExperiencePropCalculate(result);

        //    return result;
        //}

        // ===============================
        //  UPDATE
        // ===============================
        public async Task<bool> UpdateAsync(StaffMemberExperience entity)
        {
            _context.StaffMemberExperiences.Update(entity);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> SoftDeleteAsync(StaffMemberExperience entity)
        {
            //  Parent Soft Delete
            entity.IsSoftDeleted = true;
            entity.IsActive = false;

            //  Child Documents Soft Delete
            if (entity.StaffMemberExperienceDocument != null && entity.StaffMemberExperienceDocument.Any())
            {
                foreach (var doc in entity.StaffMemberExperienceDocument)
                {
                    doc.IsSoftDeleted = true;
                    doc.IsActive = false;
                    doc.DeletedDateTime = DateTime.UtcNow;
                    doc.SoftDeletedById = entity.SoftDeletedById; // Assuming the same user is performing the delete action
                }
            }

            _context.StaffMemberExperiences.Update(entity);

            return await _context.SaveChangesAsync() > 0;
        }
        public async Task<bool> SoftDeleteDocAsync(StaffMemberExperienceDocument entity)
        {

            _context.StaffMemberExperienceDocuments.Update(entity);

            return await _context.SaveChangesAsync() > 0;
        }

        // ===============================
        //  GET BY ID
        // ===============================
        public async Task<StaffMemberExperience?> GetByIdAsync(long id, long staffMemberId)
        {
            return await _context.StaffMemberExperiences
                .Include(x => x.StaffMemberExperienceDocument) //  direct include
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.StaffMemberId == staffMemberId &&
                    !x.IsSoftDeleted);
        }
        // ===============================
        //  GET LIST (PAGINATED)
        // ===============================

        public async Task<PagedResponseDTO<GetStaffMemberExperienceResponseDTO>> GetByStaffMemberIdWithDocumentsAsync(long staffMemberId, GetExperienceRequestDTO dto)
        {
            try
            {
                _logger.LogInformation("Starting experience query for StaffMemberId: {StaffMemberId}", staffMemberId);

                // -----------------
                // Pagination defaults
                // -----------------
                int pageNumber = dto.PageNumber <= 0 ? 1 : dto.PageNumber;
                int pageSize = dto.PageSize <= 0 ? 10 : dto.PageSize;

                string sortBy = dto.SortBy?.ToLower() ?? "id";
                bool isDescending = (dto.SortOrder?.ToLower() ?? "desc") == "desc";

                _logger.LogInformation("📄 Pagination | Page: {Page}, Size: {Size}, SortBy: {Sort}, Desc: {Desc}",
                    pageNumber, pageSize, sortBy, isDescending);

                // -----------------
                // Base Query
                // -----------------
                var baseQuery = _context.StaffMemberExperiences
                    .AsNoTracking()
                    .Include(x => x.StaffMemberExperienceDocument)
                    .Where(x =>
                        x.StaffMemberId == staffMemberId &&
                        x.IsActive &&
                        !x.IsSoftDeleted
                    );

                _logger.LogInformation("BaseQuery applied | StaffMemberId: {StaffMemberId}", staffMemberId);

                // -----------------
                // Sorting
                // -----------------
                baseQuery = sortBy switch
                {
                    "companyname" => isDescending ? baseQuery.OrderByDescending(x => x.CompanyName) : baseQuery.OrderBy(x => x.CompanyName),
                    "designation" => isDescending ? baseQuery.OrderByDescending(x => x.Designation) : baseQuery.OrderBy(x => x.Designation),
                    "startdate" => isDescending ? baseQuery.OrderByDescending(x => x.StartDate) : baseQuery.OrderBy(x => x.StartDate),
                    "enddate" => isDescending ? baseQuery.OrderByDescending(x => x.EndDate) : baseQuery.OrderBy(x => x.EndDate),
                    _ => isDescending ? baseQuery.OrderByDescending(x => x.Id) : baseQuery.OrderBy(x => x.Id)
                };

                _logger.LogInformation("📊 Sorting applied");

                // -----------------
                // Count
                // -----------------
                var totalRecords = await baseQuery.CountAsync();
                _logger.LogInformation("📦 Total Records: {Count}", totalRecords);

                // -----------------
                // Fetch
                // -----------------
                var expList = await baseQuery
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation("📥 Records Fetched: {Count}", expList.Count);

                // -----------------
                // Mapping + Completion
                // -----------------
                List<GetStaffMemberExperienceResponseDTO> finalList = new();
                double totalPercentage = 0;

                foreach (var exp in expList)
                {
                    _logger.LogInformation("➡️ Mapping Experience Id: {Id}", exp.Id);

                    var dtoItem = new GetStaffMemberExperienceResponseDTO
                    {
                        Id = exp.Id,

                        CompanyName = exp.CompanyName,
                        Designation = exp.Designation,
                        StaffMemberIdOfCompany = exp.StaffMemberIdOfCompany,
                        Ctc = exp.Ctc,

                        StartDate = exp.StartDate,
                        EndDate = exp.EndDate,
                        Experience = exp.Experience,

                        IsWFH = exp.IsWfh,

                        WorkingCountryId = exp.WorkingCountryId,
                        WorkingStateId = exp.WorkingStateId,
                        WorkingDistrictId = exp.WorkingDistrictId,

                        IsForeignExperience = exp.IsForeignExperience,

                        ReasonForLeaving = exp.ReasonForLeaving,
                        Remark = exp.Remark,

                        ColleagueName = exp.ColleagueName,
                        ColleagueDesignation = exp.ColleagueDesignation,
                        ColleagueContactNumber = exp.ColleagueContactNumber,

                        ReportingManagerName = exp.ReportingManagerName,
                        ReportingManagerNumber = exp.ReportingManagerNumber,

                        VerificationEmail = exp.VerificationEmail,

                        IsAnyGap = exp.IsAnyGap,
                        ReasonOfGap = exp.ReasonOfGap,
                        GapYearFrom = exp.GapYearFrom,
                        GapYearTo = exp.GapYearTo,

                        IsEditAllowed = exp.IsEditAllowed,
                        IsInfoVerified = exp.IsInfoVerified,

                        Documents = exp.StaffMemberExperienceDocument.Select(d => new GetStaffMemberExperienceDocumentDTO
                        {
                            Id = d.Id,
                            DocumentType = d.DocumentType,
                            HasExperienceDocUploaded = d.HasExperienceDocUploaded,
                            FileName = d.FileName,
                            FilePath = !string.IsNullOrEmpty(d.FilePath)
                                ? _fileStorageService.GetFileUrl(d.FilePath)
                                : null,
                            Remark = d.Remark
                        }).ToList()
                    };

                    dtoItem.CompletionPercentage = CompletionCalculatorHelper.ExperiencePropCalculate(dtoItem);

                    totalPercentage += dtoItem.CompletionPercentage;

                    finalList.Add(dtoItem);
                }

                _logger.LogInformation("🧮 Mapping Completed | Items: {Count}", finalList.Count);

                // -----------------
                // Section Completion
                // -----------------
                double averagePercentage = finalList.Count > 0
                    ? Math.Round(totalPercentage / finalList.Count, 0)
                    : 0;

                _logger.LogInformation("📊 Completion %: {Percent}", averagePercentage);

                // -----------------
                // Has Uploaded All Docs
                // -----------------
                bool hasUploadedAll = finalList.All(x =>
                    x.Documents != null && x.Documents.Any());

                _logger.LogInformation("📁 HasUploadedAll: {Flag}", hasUploadedAll);

                // -----------------
                // Final Response
                // -----------------
                _logger.LogInformation("✅ END GetExperience SUCCESS");

                return new PagedResponseDTO<GetStaffMemberExperienceResponseDTO>
                {
                    Data = finalList,
                    TotalCount = totalRecords,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                    CompletionPercentage = averagePercentage,
                    HasUploadedAll = hasUploadedAll
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error retrieving experience for StaffMemberId: {StaffMemberId}",
                    staffMemberId);

                throw;
            }
        }


    }


    #endregion
}










