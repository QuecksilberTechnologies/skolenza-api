// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Provides persistence operations for Reporting Type records.
// ================================================================

using AutoMapper;
using skolenza.application.DTOs.Manager.ReportingType;
using skolenza.application.DTOS.Pagination;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;

using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace skolenza.persistance.Repositories
{
    /// <summary>
    /// Provides persistence operations for ReportingType records.
    /// </summary>
    public class ReportingTypeRepository : IReportingTypeRepository
    {
        #region Fields
        private readonly SkolenzaDbContext _context;
        private readonly ILogger<ReportingTypeRepository> _logger;
        private readonly IMapper _mapper;

        #endregion



        #region Constructor
        public ReportingTypeRepository(SkolenzaDbContext context, ILogger<ReportingTypeRepository> logger, IMapper mapper)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        }

        #endregion

        #region AddAsync
        public async Task<ReportingType?> AddAsync(ReportingType entity)
        {
            try
            {
                // ===============================
                // 1️⃣ Duplicate check (FIXED)
                // ===============================
                bool isExist = await _context.ReportingTypes
                    .AnyAsync(x =>
                        x.TypeName.ToLower() == entity.TypeName.ToLower()
                        && x.IsSoftDeleted != true
                        && x.IsActive == true);

                if (isExist)
                    throw new Exception($"Reporting type '{entity.TypeName}' already exists.");

                await _context.ReportingTypes.AddAsync(entity);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "ReportingType '{TypeName}' added successfully by UserId {UserId}",
                    entity.TypeName,
                    entity.AddedById);

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding ReportingType.");
                throw;
            }
        }

        #endregion

        #region AllAsync
        public async Task<PagedResponseDTO<GetReportingTypeResponseDTO>> AllAsync(long institutionId, GetReportingTypeRequestDTO dto)
        {
            try
            {
                // ===============================
                // 1️⃣ DEFAULT PAGINATION
                // ===============================
                int pageNumber = dto.PageNumber <= 0 ? 1 : dto.PageNumber;
                int pageSize = dto.PageSize <= 0 ? 10 : dto.PageSize;

                // ===============================
                // 2️⃣ BASE QUERY
                // ===============================
                var query = _context.ReportingTypes
                    .AsNoTracking()
                    .Where(x => x.IsSoftDeleted != true
                                && x.InstitutionId == institutionId);

                // ===============================
                // 3️⃣ FILTERS
                // ===============================
                if (!string.IsNullOrWhiteSpace(dto.TypeName))
                    query = query.Where(x => x.TypeName.Contains(dto.TypeName));

                if (dto.IsActive.HasValue)
                    query = query.Where(x => x.IsActive == dto.IsActive.Value);

                // ===============================
                // 4️⃣ TOTAL COUNT
                // ===============================
                var totalCount = await query.CountAsync();

                // ===============================
                // 5️⃣ SORTING
                // ===============================
                query = dto.SortBy?.ToLower() switch
                {
                    "typename" => dto.SortOrder == "asc"
                        ? query.OrderBy(x => x.TypeName)
                        : query.OrderByDescending(x => x.TypeName),

                    _ => query.OrderByDescending(x => x.AddedDateTime)
                };

                // ===============================
                // 6️⃣ PAGINATION + PROJECTION
                // ===============================
                var data = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new GetReportingTypeResponseDTO
                    {
                        Id = x.Id, //  tumhara rule (encoded id)
                        TypeName = x.TypeName,
                        Description = x.Description,
                        IsActive = x.IsActive,

                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        SortBy = dto.SortBy,
                        SortOrder = dto.SortOrder
                    })
                    .ToListAsync();

                // ===============================
                // 7️⃣ RESPONSE
                // ===============================
                return new PagedResponseDTO<GetReportingTypeResponseDTO>
                {
                    Data = data,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    HasUploadedAll = data.Count >= totalCount,
                   // CompletionPercentage = totalCount == 0 ? 0 : (int)((double)data.Count / totalCount * 100)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching ReportingType list.");
                throw;
            }
        }
        #endregion

        #region GetByIdAsync
        public async Task<GetReportingTypeResponseDTO?> GetByIdAsync(long id)
        {
            try
            {

                var entity = await _context.ReportingTypes.FirstOrDefaultAsync(x => x.Id == id && x.IsSoftDeleted != true && x.IsActive == true);
                if (entity == null)
                    return null;

                return _mapper.Map<GetReportingTypeResponseDTO>(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching ReportingType by ID {Id}", id);
                throw;
            }
        }
        #endregion

        #region UpdateAsync
        public async Task<bool> UpdateAsync(UpdateReportingTypeRequestDTO dto)
        {
            try
            {

                var existing = await _context.ReportingTypes.FirstOrDefaultAsync(x => x.Id == dto.Id && x.IsSoftDeleted!=true && x.IsActive ==true);
                if (existing == null)
                    throw new Exception("Reporting type not found.");

                // Duplicate check (excluding same record)
                bool isDuplicate = await _context.ReportingTypes
                    .AnyAsync(x => x.TypeName.ToLower() == dto.TypeName.ToLower() && x.Id != dto.Id);

                if (isDuplicate)
                    throw new Exception($"Reporting type '{dto.TypeName}' already exists.");

                existing.TypeName = dto.TypeName;
                existing.Description = dto.Description;
                existing.IsActive = dto.IsActive;
                existing.UpdatedById = dto.StaffMemberId;
                existing.UpdatedDateTime = DateTime.UtcNow;

                _context.ReportingTypes.Update(existing);
                await _context.SaveChangesAsync();

                _logger.LogInformation("ReportingType '{TypeName}' updated successfully by UserId {UserId}", dto.TypeName, dto.StaffMemberId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating ReportingType Id {Id}", dto.Id);
                throw;
            }
        }
        #endregion

        #region DeleteAsync
        public async Task<bool> DeleteAsync(long id, long staffMemberId)
        {
            try
            {

                var existing = await _context.ReportingTypes.FirstOrDefaultAsync(x => x.Id == id && x.IsSoftDeleted != true && x.IsActive == true);
                if (existing == null)
                    throw new Exception("Reporting type not found.");

                existing.IsActive = false;
                existing.IsSoftDeleted = true;
                existing.SoftDeletedById = staffMemberId;
                existing.SoftDeletedDateTime = DateTime.UtcNow;


                _context.ReportingTypes.Update(existing);
                await _context.SaveChangesAsync();

                _logger.LogInformation("ReportingType Id {Id} deactivated by UserId {UserId}", id, staffMemberId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting ReportingType Id {Id}", id);
                throw;
            }
        }
        #endregion
    }
}
