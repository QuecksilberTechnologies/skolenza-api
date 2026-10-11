// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Provides persistence operations for Ticket Type records.
// ================================================================

using AutoMapper;
using skolenza.application.Constants;
using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.TicketDTO.TicketType;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace skolenza.persistance.Repositories
{

    /// <summary>
    /// Provides persistence operations for TicketType records.
    /// </summary>
    public class TicketTypeRepository : ITicketTypeRepository
    {
        #region Fields
        private readonly SkolenzaDbContext _context;
        private readonly ILogger<TicketTypeRepository> _logger;
        private readonly IMapper _mapper;

        #endregion



        #region Constructor
        public TicketTypeRepository(SkolenzaDbContext context, ILogger<TicketTypeRepository> logger, IMapper mapper
            )
        {
            this._context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        }


        #endregion

        #region Get Methods


        /// <summary>
        /// Retrieves all active Ticket Types for a given institution, including their associated
        /// responsible role and primary responsible staffMember (based on IsPrimary = true).
        /// </summary>
        /// <param name="dTO">
        /// The request DTO containing institution-specific filters such as <see cref="GetTicketTypeRequestDTO.InstitutionId"/>
        /// and <see cref="GetTicketTypeRequestDTO.IsActive"/>.
        /// </param>
        /// <returns>
        public async Task<PagedResponseDTO<GetTicketTypeResponseDTO>> AllAsync(long institutionId, GetTicketTypeRequestDTO dto)
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
                var baseQuery =
                    from t in _context.TicketTypes.AsNoTracking()

                    join th in _context.TicketHeaders.AsNoTracking()
                        on t.TicketHeaderId equals th.Id into headerGroup
                    from th in headerGroup.DefaultIfEmpty()

                    join r in _context.Roles.AsNoTracking()
                        on t.ResponsibleRoleId equals r.Id into roleGroup
                    from r in roleGroup.DefaultIfEmpty()

                    join ar in _context.Roles.AsNoTracking()
                        on t.ApprovalRoleId equals ar.Id into approvalGroup
                    from ar in approvalGroup.DefaultIfEmpty()

                    where t.InstitutionId == institutionId
                          && t.IsActive == true
                          && t.IsSoftDeleted != true

                    select new GetTicketTypeResponseDTO
                    {
                        Id = t.Id,
                        TicketTypeName = t.TicketTypeName,

                        TicketHeaderId = t.TicketHeaderId,
                        TicketHeaderName = th != null ? th.HeaderName : null,

                        InstitutionId = t.InstitutionId,

                        ResponsibleRoleId = r != null ? r.Id : null,
                        ResponsibleRoleName = r != null ? r.RoleName : null,

                        ApprovalId = t.ApprovalRoleId,
                        ApprovalRoleName = ar != null ? ar.RoleName : null,
                        //  FIX YEH HAI
                        ClassificationId = th != null ? th.TicketClassificationId : 0,

                        IsApprovalRequired = t.IsApprovalRequired,
                        AutoApproveIfSameRole = t.AutoApproveIfSameRole,
                        IsAttachmentRequired = t.IsAttachmentRequired,
                        SLAHours = t.SLAHours,
                        IsActiveForAllUsers = t.IsActiveForAllUsers,

                        Description = t.Description,
                        IsActive = t.IsActive
                    };

                // ===============================
                // 3️⃣ TOTAL COUNT (BEFORE PAGINATION)
                // ===============================
                var totalCount = await baseQuery.CountAsync();

                // ===============================
                // 4️⃣ SORTING
                // ===============================
                baseQuery = dto.SortBy?.ToLower() switch
                {
                    "name" => dto.SortOrder == "asc"
                        ? baseQuery.OrderBy(x => x.TicketTypeName)
                        : baseQuery.OrderByDescending(x => x.TicketTypeName),

                    _ => baseQuery.OrderByDescending(x => x.Id)
                };

                // ===============================
                // 5️⃣ PAGINATION
                // ===============================
                var data = await baseQuery
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // ===============================
                // 6️⃣ RESPONSE
                // ===============================
                return new PagedResponseDTO<GetTicketTypeResponseDTO>
                {
                    Data = data,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching TicketTypes for InstitutionId {InstitutionId}", institutionId);
                throw;
            }
        }

        public async Task<List<GetDDLTicketTypeResponseDTO>> GetDDLAsync(bool isActive, long institutionId)
        {
            try
            {
                _logger.LogInformation("Fetching TicketType DDL for InstitutionId: {InstitutionId}", institutionId);

                // ===============================
                // 1️⃣ BASE QUERY
                // ===============================
                var query = _context.TicketTypes
                    .AsNoTracking()
                    .Where(t => t.InstitutionId == institutionId &&
                                t.IsSoftDeleted != true);

                // ===============================
                // 2️⃣ OPTIONAL FILTER
                // ===============================

                    query = query.Where(t => t.IsActive == isActive);


                // ===============================
                // 3️⃣ PROJECTION (DDL)
                // ===============================
                var data = await query
                    .OrderBy(t => t.TicketTypeName)
                    .Select(t => new GetDDLTicketTypeResponseDTO
                    {
                        Id = t.Id,
                        TicketTypeName = t.TicketTypeName,
                        IsActive = isActive,
                        Description = t.Description

                    })
                    .ToListAsync();

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching TicketTypes for InstitutionId {InstitutionId}", institutionId);
                throw;
            }
        }

        public async Task<List<GetTicketTypeResponseDTO>> AllByHeaderIdAsync(long institutionId, GetTicketTypeByHeaderIdRequestDTO dTO)
        {
            try
            {
                var ticketTypes = await _context.TicketTypes
                    .Where(t => t.TicketHeaderId == dTO.TicketHeaderId && t.InstitutionId == institutionId && t.IsActive && (t.IsSoftDeleted !=true))
                    .ToListAsync();

                if (ticketTypes == null || !ticketTypes.Any())
                    return new List<GetTicketTypeResponseDTO>();

                return _mapper.Map<List<GetTicketTypeResponseDTO>>(ticketTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching all TicketTypes");
                throw;
            }
        }

        public async Task<GetTicketTypeResponseDTO?> GetByIdAsync(long id, bool isActive)
        {
            try
            {
                var entity = await _context.TicketTypes
                    .FirstOrDefaultAsync(t =>  t.Id == id && t.IsActive == isActive && (t.IsSoftDeleted != true));

                if (entity == null)
                {
                    _logger.LogWarning("TicketType with Id {Id} not found", id);
                    return null;
                }

                return _mapper.Map<GetTicketTypeResponseDTO>(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching TicketType by Id {Id}", id);
                throw;
            }
        }
        #endregion

        #region Add Method
        public async Task<TicketType?> AddAsync(TicketType entity)
        {
            try
            {
                await _context.TicketTypes.AddAsync(entity);
                await _context.SaveChangesAsync();

                return entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding new TicketType");
                throw;
            }
        }
        #endregion

        #region Update Method
        /// <summary>
        /// Updates an existing TicketType record with new values if changed.
        /// </summary>
        /// <param name="dto">DTO containing updated fields</param>
        /// <returns>True if update was successful or no changes were needed; false if record not found</returns>
        /// <summary>
        /// Updates an existing TicketType record. Only non-null values from DTO will be updated.
        /// </summary>
        public async Task<TicketType?> GetByIdForInstitutionAsync(long id, long institutionId)
        {
            return await _context.TicketTypes.FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.InstitutionId == institutionId &&
                x.IsSoftDeleted != true);
        }

        public async Task<TicketType?> UpdateAsync(TicketType entity)
        {
            _context.TicketTypes.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        #endregion

        #region Delete Method
        public async Task<bool> DeleteAsync(long id, long staffMemberId)
        {
            try
            {
                var entity = await _context.TicketTypes
                    .FirstOrDefaultAsync(x =>
                        x.Id == id && x.IsSoftDeleted != true && x.IsActive);

                if (entity == null)
                    throw new ValidationErrorException("TicketType not found.");

                //  SOFT DELETE
                entity.IsSoftDeleted = true;
                entity.IsActive = false;
                entity.SoftDeletedById = staffMemberId;
                entity.SoftDeletedTime = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting TicketType Id {Id}", id);
                throw;
            }
        }
        /// <summary>
        /// Fetches all Ticket Types based on the given RoleId.
        /// Each Ticket Type groups multiple responsible staffMembers (if any) under the same role.
        /// </summary>
        /// <param name="dTO">Request DTO containing RoleId.</param>
        /// <returns>List of Ticket Types with grouped staffMembers responsible for each ticket type.</returns>

        /// <summary>
        /// Fetch all TicketTypes for a given requester's RoleId along with responsible staffMembers.
        /// Each TicketType can have multiple responsible staffMembers.
        /// </summary>
        /// <param name="dTO">DTO containing the requester's RoleId</param>
        /// <returns>List of TicketType DTOs with StaffMember info</returns>
        public async Task<List<GetTicketTypeRoleResponseDTO>> AllByRoleIdAsync(GetTicketTypeByRoleIdRequestDTO dTO)
        {
            try
            {
                if (dTO == null || dTO.ResponsibleRoleId <= 0)
                {
                    _logger.LogWarning("AllByRoleIdAsync called with invalid RoleId: {RoleId}", dTO?.ResponsibleRoleId);
                    return new List<GetTicketTypeRoleResponseDTO>();
                }


                // 1️⃣ Flattened query: TicketType → ResponsibleRole → UserRole → StaffMember
                var flatData =
                    from ticketType in _context.TicketTypes.AsNoTracking()
                    join role in _context.Roles.AsNoTracking() on ticketType.ResponsibleRoleId equals role.Id
                    join userRole in _context.UserRoles.AsNoTracking() on role.Id equals userRole.RoleId
                    join emp in _context.StaffMembers.AsNoTracking() on userRole.StaffMemberId equals emp.Id
                    where role.IsActive
                          && userRole.IsActive
                          && emp.IsActive
                          && ticketType.IsActive && ticketType.InstitutionId==dTO.InstitutionId
                          && (ticketType.IsSoftDeleted == false || ticketType.IsSoftDeleted == null)
                    select new
                    {
                        TicketTypeId = ticketType.Id,
                        ticketType.TicketTypeName,
                        ticketType.Description,
                        ResponsibleRoleId = role.Id,
                        ResponsibleRoleName = role.RoleName,
                        StaffMemberId = emp.Id,
                        StaffMemberName = emp.FirstName + " " + emp.LastName,
                        StaffMemberEmail = emp.OfficialEmail,

                    };

                // 2️⃣ Group by TicketType to consolidate multiple staffMembers
                var grouped = await flatData
                    .GroupBy(t => new
                    {
                        t.TicketTypeId,
                        t.TicketTypeName,
                        t.Description,
                        t.ResponsibleRoleId,
                        t.ResponsibleRoleName
                    })
                    .Select(g => new GetTicketTypeRoleResponseDTO
                    {
                        Id = g.Key.TicketTypeId,
                        TicketTypeName = g.Key.TicketTypeName,
                        Description = g.Key.Description,
                        ResponsibleRoleId = g.Key.ResponsibleRoleId,
                        ResponsibleRoleName = g.Key.ResponsibleRoleName,

                        StaffMembers = g.Select(e => new StaffMemberMinInfoDTO
                        {
                            Id = e.StaffMemberId,
                            Name = e.StaffMemberName,
                            Email = e.StaffMemberEmail
                        }).ToList()
                    })
                    .ToListAsync();

                // 3️⃣ Filter only those TicketTypes where the requester's RoleId is allowed
                var filtered = grouped
                    .Where(t => t.ResponsibleRoleId == dTO.ResponsibleRoleId)
                    .ToList();

                _logger.LogInformation("Fetched {Count} grouped TicketTypes for RoleId {RoleId}", filtered.Count, dTO.ResponsibleRoleId);

                return filtered;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AllByRoleIdAsync for RoleId {RoleId}", dTO?.ResponsibleRoleId);
                throw;
            }
        }


        #endregion


    }
}
