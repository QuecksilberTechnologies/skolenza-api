// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Provides persistence operations for Ticket Classification records.
// ================================================================

using AutoMapper;
 
using skolenza.application.DTOS.TicketDTO.Classification;
using skolenza.application.Interfaces.IRepositories;

using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using skolenza.domain.Entity;
using skolenza.application.DTOS.Pagination;

namespace skolenza.persistance.Repositories
{

    #region Persistence Operations

    /// <summary>
    /// Provides persistence operations for TicketClassification records.
    /// </summary>
    public class TicketClassificationRepository : ITicketClassificationRepository
    {
        private readonly SkolenzaDbContext _context;
       
        private readonly IMapper _mapper;
        private readonly ILogger<TicketClassificationRepository> _logger;

        public TicketClassificationRepository(
            SkolenzaDbContext context,
            ILogger<TicketClassificationRepository> logger,
            IMapper mapper)
          
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
            
        }

        //  ADD
        public async Task<TicketClassification?> AddAsync(TicketClassification entity)
        {
            try
            {
                // ===============================
                // 1️⃣ VALIDATION
                // ===============================
                if (entity == null)
                    throw new ArgumentException("Request cannot be null.");

                if (string.IsNullOrWhiteSpace(entity.ClassificationName))
                    throw new ArgumentException("ClassificationName is required.");

                // ===============================
                // 2️⃣ DUPLICATE CHECK
                // ===============================
                var exists = await _context.TicketClassifications
                    .AnyAsync(x =>
                        x.InstitutionId == entity.InstitutionId &&
                        x.IsActive &&
                        (x.IsSoftDeleted != true) &&
                        x.ClassificationName.ToLower() == entity.ClassificationName.ToLower());

                if (exists)
                    throw new InvalidOperationException("Classification already exists.");

                // ===============================
                // 3️⃣ CREATE ENTITY (MANUAL)
                // ===============================
                // ===============================
                // 4️⃣ SAVE
                // ===============================
                await _context.TicketClassifications.AddAsync(entity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Ticket classification added. Id: {Id}", entity.Id);

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while adding ticket classification.");
                throw;
            }
        }

        //  GET BY ID
        public async Task<PagedResponseDTO<GetClassificationResponseDTO?>> GetByIdAsync(GetClassificationRequestDTO dto)
        {
            try
            {
                if (dto == null || dto.Id <= 0)
                {
                    _logger.LogWarning("⚠️ Invalid DTO or Id for GetByIdAsync.");
                    return null;
                }

               
                var entity = await _context.TicketClassifications
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == dto.Id && x.IsActive && (x.IsSoftDeleted !=true));

                if (entity == null)
                {
                    _logger.LogWarning("⚠️ No classification found for Id: {Id}", dto.Id);
                    return null;
                }

                var response = _mapper.Map<PagedResponseDTO<GetClassificationResponseDTO>>(entity);
               
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error while fetching classification by Id: {Id}", dto.Id);
                return new PagedResponseDTO<GetClassificationResponseDTO>();
            }
        }

        //  GET ALL
        /// <summary>
        /// Represents the BaseRequest application component.
        /// </summary>
        public class BaseRequest
        {
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
            public string? SortBy { get; set; }
            public string? SortOrder { get; set; } = "desc";
            public string? UserStaffMemberId { get; set; }



        }

        public async Task<List<GetClassificationResponseDTO>> GetDDLAsync(bool isActive, long institutionId)
        {
            try
            {
                var result = await _context.TicketClassifications
                    .AsNoTracking()
                    .Where(x =>
                        x.InstitutionId == institutionId &&
                        x.IsActive == isActive &&
                        (x.IsSoftDeleted != true))
                    .OrderBy(x => x.ClassificationName)
                    .Select(x => new GetClassificationResponseDTO
                    {
                        Id = x.Id,
                        ClassificationName = x.ClassificationName,
                        IsActive = x.IsActive,
                        Description = x.Description

                    })
                    .ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching classification DDL");
                throw;
            }
        }
        public async Task<PagedResponseDTO<GetClassificationResponseDTO>> GetAllAsync(long institutionId, GetAllClassificationRequestDTO dto)
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
                var query = _context.TicketClassifications
                    .AsNoTracking()
                    .Where(x =>
                        x.InstitutionId == institutionId &&
                        x.IsActive == true &&
                        (x.IsSoftDeleted != true));

                // ===============================
                // 3️⃣ TOTAL COUNT
                // ===============================
                var totalCount = await query.CountAsync();

                // ===============================
                // 4️⃣ SORTING
                // ===============================
                query = dto.SortBy?.ToLower() switch
                {
                    "classificationname" => dto.SortOrder == "asc"
                        ? query.OrderBy(x => x.ClassificationName)
                        : query.OrderByDescending(x => x.ClassificationName),

                    "addeddatetime" => dto.SortOrder == "asc"
                        ? query.OrderBy(x => x.AddedDateTime)
                        : query.OrderByDescending(x => x.AddedDateTime),

                    _ => query.OrderByDescending(x => x.Id)
                };

                // ===============================
                // 5️⃣ PAGINATION
                // ===============================
                var data = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(x => new GetClassificationResponseDTO
                    {
                        Id = x.Id,
                        ClassificationName = x.ClassificationName,
                        Description = x.Description,
                        IsActive = x.IsActive,
                      
                    })
                    .ToListAsync();

                // ===============================
                // 6️⃣ TOTAL PAGES
                // ===============================
                int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                // ===============================
                // 7️⃣ RESPONSE
                // ===============================
                return new PagedResponseDTO<GetClassificationResponseDTO>
                {
                    Data = data,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = totalPages,
                    HasUploadedAll = pageNumber >= totalPages
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching classifications.");
                throw;
            }
        }

        //  UPDATE
        public async Task<TicketClassification?> GetByIdForInstitutionAsync(int id, long institutionId)
        {
            return await _context.TicketClassifications.FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.InstitutionId == institutionId &&
                x.IsSoftDeleted != true);
        }

        public async Task<TicketClassification?> UpdateAsync(TicketClassification entity)
        {
            _context.TicketClassifications.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        //  DELETE (Soft Delete)
        public async Task<bool> DeleteAsync(DeleteClassificationRequestDTO dto , long staffMemberId)
        {
            try
            {
                if (dto == null || dto.Id <= 0)
                {
                    _logger.LogWarning("⚠️ Invalid DTO or Id for DeleteClassificationAsync.");
                    return false;
                }
 
                var entity = await _context.TicketClassifications.FirstOrDefaultAsync(x => x.Id == dto.Id && x.IsSoftDeleted !=true);
                if (entity == null)
                {
                    _logger.LogWarning("⚠️ Classification not found for deletion. Id: {Id}", dto.Id);
                    return false;
                }

                entity.IsActive = false;
                entity.IsSoftDeleted = true;
                entity.SoftDeletedById = staffMemberId;
                entity.SoftDeletedTime = DateTime.UtcNow;

                _context.TicketClassifications.Update(entity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("🗑️ Classification soft-deleted successfully. Id: {Id}", dto.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error while deleting classification. Id: {Id}", dto.Id);
                return false;
            }
        }

        public async Task<GetClassificationResponseDTO?> GetByIdAsync(long id, long institutionId)
        {
            try
            {
                var result = await _context.TicketClassifications
                    .AsNoTracking()
                    .Where(x =>
                        x.Id == id &&
                        x.InstitutionId == institutionId &&
                        x.IsActive == true &&
                        (x.IsSoftDeleted != true))
                    .Select(x => new GetClassificationResponseDTO
                    {
                        Id = x.Id,
                        ClassificationName = x.ClassificationName,
                        Description = x.Description,
                        IsActive = x.IsActive,
                        
                    })
                    .FirstOrDefaultAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching classification by Id: {Id}", id);
                throw;
            }
        }
    }

    #endregion
}
