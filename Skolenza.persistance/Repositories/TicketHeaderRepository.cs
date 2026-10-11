// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Provides persistence operations for Ticket Header records.
// ================================================================

using AutoMapper;
using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.TicketDTO.Header;
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

    #region Persistence Operations

    /// <summary>
    /// Provides persistence operations for TicketHeader records.
    /// </summary>
    public class TicketHeaderRepository : ITicketHeaderRepository
    {
        private readonly SkolenzaDbContext _context;

        private readonly IMapper _mapper;
        private readonly ILogger<TicketHeaderRepository> _logger;

        public TicketHeaderRepository(
            SkolenzaDbContext context,
            ILogger<TicketHeaderRepository> logger,
            IMapper mapper
          )
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;

        }

        //  ADD
        public async Task<TicketHeader?> AddAsync(TicketHeader entity)
        {
            try
            {
                await _context.TicketHeaders.AddAsync(entity);
                await _context.SaveChangesAsync();
                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while adding TicketHeader");
                throw;
            }
        }
        //  GET BY ID
        /// <summary>
        /// Get header by Id
        /// </summary>
        public async Task<List<GetHeaderResponseDTO>> GetAllHeaderAsync(GetHeaderRequestDTO dto)
        {
            var result = new List<GetHeaderResponseDTO>();

            try
            {
                if (dto == null || dto.Id <= 0)
                {
                    _logger.LogWarning("⚠️ Invalid DTO or Id for GetByIdAsync.");

                    return result;
                }


                var entity = await _context.TicketHeaders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == dto.Id && x.IsActive && (x.IsSoftDeleted !=true));

                if (entity == null)
                {
                    _logger.LogWarning("⚠️ Ticket header not found for Id: {Id}", dto.Id);
                    return new List<GetHeaderResponseDTO>();

                }

                var response = _mapper.Map<GetHeaderResponseDTO>(entity);



                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error while fetching ticket header by Id: {Id}", dto.Id);
                return new List<GetHeaderResponseDTO>();

            }
        }

        public async Task<GetHeaderResponseDTO> GetByIdAsync(long headerId)
        {
            try
            {
                if (headerId <= 0)
                {
                    _logger.LogWarning("⚠️ Invalid Id for GetByIdAsync.");
                    return new GetHeaderResponseDTO();
                }
                var entity = await _context.TicketHeaders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == headerId && x.IsActive && (x.IsSoftDeleted != true));
                if (entity == null)
                {
                    _logger.LogWarning("⚠️ Ticket header not found for Id: {Id}", headerId);
                    return new GetHeaderResponseDTO();
                }
                var response = _mapper.Map<GetHeaderResponseDTO>(entity);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error while fetching ticket header by Id: {Id}", headerId);
                return new GetHeaderResponseDTO();
            }
        }

        public async Task<List<GetHeaderResponseDTO>> GetByClassificationIdAsync(long institutionId, GetTicketHeaderByClassifyIdRequestDTO dto)
        {
            return await _context.TicketHeaders .AsNoTracking()
                .AsNoTracking()
                .Where(x =>
                    x.TicketClassificationId == dto.TicketClassifyId &&
                    x.InstitutionId == institutionId &&
                    x.IsActive &&
                    !x.IsSoftDeleted)
                .Select(x => new GetHeaderResponseDTO
                {
                    Id = x.Id, //  encode later
                    HeaderName = x.HeaderName,

                    TicketClassificationId = x.TicketClassificationId,
                    TicketClassificationName = x.TicketClassification.ClassificationName,

                    Description = x.Description,
                    IsActive = x.IsActive
                })
                .ToListAsync();
        }
        // TODO: Add other repository functions (AddAsync, UpdateAsync, DeleteAsync, GetAllAsync) in similar style

        //  GET ALL
        public async Task<List<GetHeaderResponseDTO>> GetAllAsync(GetHeaderRequestDTO dto)
        {
            var result = new List<GetHeaderResponseDTO>();

            try
            {

                //  Step 1: Base Query
                var query = _context.TicketHeaders
                    .Where(x => (x.IsSoftDeleted != true) && x.InstitutionId ==dto.InstitutionId)
                    .AsQueryable();

                //  Step 3: Optional Filters
                if (dto.Id > 0)
                    query = query.Where(x => x.Id == dto.Id);

                if (!string.IsNullOrEmpty(dto.HeaderName))
                    query = query.Where(x => x.HeaderName.ToLower().Contains(dto.HeaderName.ToLower()));

                if (!string.IsNullOrEmpty(dto.Description))
                    query = query.Where(x => x.Description.ToLower().Contains(dto.Description.ToLower()));

                if (dto.IsActive.HasValue)
                    query = query.Where(x => x.IsActive == dto.IsActive.Value);

                if (dto.TicketClassificationId.HasValue && dto.TicketClassificationId.Value > 0)
                    query = query.Where(x => x.TicketClassificationId == dto.TicketClassificationId.Value);

                if (dto.IsAssetRelated.HasValue)
                    query = query.Where(x => x.IsAssetRelated == dto.IsAssetRelated.Value);

                // (Optional) Role-based filtering example (if needed)
                if (dto.RoleId > 0)
                {
                    _logger.LogInformation("🔑 Applying RoleId filter: {RoleId}", dto.RoleId);
                    // yahan agar koi Role-based header visibility logic ho, to wo lagao
                }

                //  Step 4: Fetch
                var headers = await query.AsNoTracking().ToListAsync();

                //  Step 5: Map to DTO
                result = _mapper.Map<List<GetHeaderResponseDTO>>(headers);

                //  Step 6: Add success message


                _logger.LogInformation("✅ {Count} headers fetched successfully for InstitutionId={InstitutionId}", result.Count, dto.InstitutionId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error while fetching filtered ticket headers.");

                return new List<GetHeaderResponseDTO>();
            };

        }

        //  UPDATE
        public async Task<TicketHeader?> GetByIdForInstitutionAsync(long id, long institutionId)
        {
            return await _context.TicketHeaders.FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.InstitutionId == institutionId &&
                x.IsSoftDeleted != true);
        }

        public async Task<TicketHeader?> UpdateAsync(TicketHeader entity)
        {
            _context.TicketHeaders.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }
        //  DELETE (Soft Delete)
        public async Task<bool> DeleteAsync(DeleteHeaderRequestDTO dto, long StaffMemberId)
        {
            try
            {
                if (dto == null || dto.Id <= 0)
                {
                    _logger.LogWarning("⚠️ Invalid DTO or Id for DeleteHeaderAsync.");
                    return false;
                }

                var entity = await _context.TicketHeaders.FirstOrDefaultAsync(x => x.Id == dto.Id && (x.IsSoftDeleted != true && x.IsActive == true));
                if (entity == null)
                {
                    _logger.LogWarning("⚠️ Ticket header not found for deletion. Id: {Id}", dto.Id);
                    return false;
                }

                entity.SoftDeletedById = StaffMemberId;
                entity.SoftDeletedTime = DateTime.UtcNow;
                entity.IsActive = false;
                entity.IsSoftDeleted = true;

                _context.TicketHeaders.Update(entity);
                await _context.SaveChangesAsync();

                _logger.LogInformation("🗑️ Ticket header soft-deleted successfully. Id: {Id}", dto.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error while deleting ticket header. Id: {Id}", dto.Id);
                return false;
            }
        }
    }

    #endregion
}
