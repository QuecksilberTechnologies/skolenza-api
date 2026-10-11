// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Persists and projects institution-scoped designation data.
// ================================================================

using AutoMapper;
using skolenza.application.DTOs.Designation;
using skolenza.application.DTOS.Designation;
using skolenza.application.DTOS.Pagination;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace skolenza.persistance.Repositories
{
    /// <summary>
    /// Provides institution-scoped persistence and projection operations for designations.
    /// </summary>
    public class DesignationRepository : IDesignationRepository
    {
        private readonly SkolenzaDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<DesignationRepository> _logger;

        /// <summary>
        /// Initializes repository dependencies.
        /// </summary>
        public DesignationRepository(
            SkolenzaDbContext context,
            ILogger<DesignationRepository> logger,
            IMapper mapper)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
        }

        #region Create

        /// <summary>
        /// Persists designation seed entities and returns the executive-office designation identifier.
        /// </summary>
        public async Task<int> AutoCreateDesignationAsync(List<Designation> designations, int departmentId)
        {
            try
            {
                if (designations == null || !designations.Any())
                {
                    _logger.LogWarning("Designation seed list is null or empty. Seeding aborted.");
                    return 0;
                }

                var institutionId = designations.First().InstitutionId;
                await _context.Designations.AddRangeAsync(designations);
                var savedCount = await _context.SaveChangesAsync();

                if (savedCount != designations.Count)
                {
                    _logger.LogWarning(
                        "Designation seed count mismatch. Expected: {Expected}; saved: {Saved}.",
                        designations.Count,
                        savedCount);
                    return 0;
                }

                return designations.FirstOrDefault(
                    designation => designation.Department?.InstitutionId == institutionId &&
                                   designation.Department.IsExecutiveOffice)?.Id ?? 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating designation seeds.");
                return 0;
            }
        }

        /// <summary>
        /// Persists a designation entity after enforcing institution and department constraints.
        /// </summary>
        public async Task<Designation?> CreateAsync(
            Designation entity,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            var departmentExists = await _context.Departments.AnyAsync(
                department => department.Id == entity.DepartmentId &&
                              department.InstitutionId == entity.InstitutionId &&
                              department.IsActive == true &&
                              department.IsSoftDeleted != true,
                cancellationToken);

            if (!departmentExists)
            {
                _logger.LogWarning(
                    "Department {DepartmentId} is unavailable for InstitutionId {InstitutionId}.",
                    entity.DepartmentId,
                    entity.InstitutionId);
                return null;
            }

            entity.DesignationName = entity.DesignationName.Trim();
            var exists = await CheckDuplicateValueAsync(
                entity.InstitutionId,
                entity.DepartmentId,
                entity.DesignationName,
                cancellationToken: cancellationToken);

            if (exists)
            {
                _logger.LogWarning(
                    "Designation {DesignationName} already exists for InstitutionId {InstitutionId}.",
                    entity.DesignationName,
                    entity.InstitutionId);
                return null;
            }

            await _context.Designations.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return entity;
        }

        #endregion

        #region Update

        /// <summary>
        /// Gets a mutable designation entity after enforcing institution ownership.
        /// </summary>
        public Task<Designation?> GetByIdForInstitutionAsync(
            int id,
            long institutionId,
            CancellationToken cancellationToken = default)
        {
            return _context.Designations.FirstOrDefaultAsync(
                designation => designation.Id == id &&
                               designation.InstitutionId == institutionId &&
                               designation.IsSoftDeleted != true,
                cancellationToken);
        }

        /// <summary>
        /// Persists changes to a prepared designation entity.
        /// </summary>
        public async Task<bool> UpdateDesignationAsync(
            Designation entity,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            var existing = await _context.Designations.FirstOrDefaultAsync(
                designation => designation.Id == entity.Id &&
                               designation.InstitutionId == entity.InstitutionId &&
                               designation.IsSoftDeleted != true,
                cancellationToken);

            if (existing == null)
                return false;

            var departmentExists = await _context.Departments.AnyAsync(
                department => department.Id == entity.DepartmentId &&
                              department.InstitutionId == entity.InstitutionId &&
                              department.IsActive == true &&
                              department.IsSoftDeleted != true,
                cancellationToken);

            if (!departmentExists)
                return false;

            if (await CheckDuplicateValueAsync(
                    entity.InstitutionId,
                    entity.DepartmentId,
                    entity.DesignationName,
                    entity.Id,
                    cancellationToken))
            {
                return false;
            }

            existing.DesignationName = entity.DesignationName;
            existing.Description = entity.Description;
            existing.DepartmentId = entity.DepartmentId;
            existing.IsActive = entity.IsActive;
            existing.UpdatedById = entity.UpdatedById;
            existing.UpdatedDateTime = entity.UpdatedDateTime;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        #endregion

        #region Delete

        /// <summary>
        /// Determines whether the Designation remains assigned to an staffMember in the same institution.
        /// The IsActive flag is deliberately excluded: an inactive staffMember is still a valid
        /// dependency until that staffMember record is soft deleted.
        /// </summary>
        public Task<bool> HasNonDeletedStaffMembersAsync(
            int designationId,
            long institutionId,
            CancellationToken cancellationToken = default)
        {
            return _context.StaffMembers
                .AsNoTracking()
                .AnyAsync(
                    staffMember => staffMember.InstitutionId == institutionId &&
                                staffMember.DesignationId == designationId &&
                                !staffMember.IsSoftDeleted,
                    cancellationToken);
        }

        /// <summary>
        /// Soft deletes a designation after enforcing institution ownership.
        /// </summary>
        public async Task<bool> DeleteDesignationAsync(
            int id,
            long institutionId,
            long staffMemberId,
            CancellationToken cancellationToken = default)
        {
            var entity = await _context.Designations.FirstOrDefaultAsync(
                designation => designation.Id == id &&
                               designation.InstitutionId == institutionId &&
                               designation.IsSoftDeleted != true,
                cancellationToken);

            if (entity == null)
                return false;

            entity.IsSoftDeleted = true;
            entity.IsActive = false;
            entity.SoftDeletedById = staffMemberId;
            entity.SoftDeletedDateTime = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        #endregion

        #region Queries

        /// <summary>
        /// Matches normalized designation names within one institution-owned department.
        /// </summary>
        public Task<bool> CheckDuplicateValueAsync(
            long institutionId,
            int departmentId,
            string value,
            int? excludeId = null,
            CancellationToken cancellationToken = default)
        {
            return _context.Designations.AnyAsync(
                designation => designation.InstitutionId == institutionId &&
                               designation.DepartmentId == departmentId &&
                               (!excludeId.HasValue || designation.Id != excludeId.Value) &&
                               designation.IsSoftDeleted != true &&
                               designation.DesignationName.Trim().ToLower() == value.Trim().ToLower(),
                cancellationToken);
        }

        /// <summary>
        /// Gets paged designation projections for a trusted institution.
        /// </summary>
        public async Task<PagedResponseDTO<GetDesignationResponseDTO>> GetAsync(
            GetDesignationRequestDTO request,
            long institutionId,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var query =
                from designation in _context.Designations.AsNoTracking()
                join department in _context.Departments.AsNoTracking()
                    on designation.DepartmentId equals department.Id into departments
                from department in departments.DefaultIfEmpty()
                where designation.InstitutionId == institutionId &&
                      designation.IsSoftDeleted != true &&
                      (department == null || (department.IsSoftDeleted != true && department.IsActive == true))
                select new
                {
                    Designation = designation,
                    DepartmentName = department != null ? department.DepartmentName : string.Empty
                };

            if (request.DepartmentId > 0)
                query = query.Where(item => item.Designation.DepartmentId == request.DepartmentId);
            if (!string.IsNullOrWhiteSpace(request.DesignationName))
                query = query.Where(item => item.Designation.DesignationName.ToLower().Contains(request.DesignationName.ToLower()));
            if (request.IsActive.HasValue)
                query = query.Where(item => item.Designation.IsActive == request.IsActive.Value);

            query = request.SortBy?.ToLower() switch
            {
                "designationname" => request.SortOrder?.ToLower() == "asc"
                    ? query.OrderBy(item => item.Designation.DesignationName)
                    : query.OrderByDescending(item => item.Designation.DesignationName),
                "departmentname" => request.SortOrder?.ToLower() == "asc"
                    ? query.OrderBy(item => item.DepartmentName)
                    : query.OrderByDescending(item => item.DepartmentName),
                _ => query.OrderByDescending(item => item.Designation.Id)
            };

            var totalCount = await query.CountAsync(cancellationToken);
            var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
            var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;
            var data = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(item => new GetDesignationResponseDTO
                {
                    Id = item.Designation.Id,
                    DepartmentId = item.Designation.DepartmentId,
                    DesignationName = item.Designation.DesignationName,
                    DepartmentName = item.DepartmentName,
                    Description = item.Designation.Description,
                    IsActive = item.Designation.IsActive
                })
                .ToListAsync(cancellationToken);

            return new PagedResponseDTO<GetDesignationResponseDTO>(data, totalCount, pageNumber, pageSize);
        }

        /// <summary>
        /// Gets a designation projection by identifier.
        /// </summary>
        public async Task<GetSingleDesignationResponseDTO?> GetByIdAsync(
            GetSingleDesignationRequestDTO dto,
            long institutionId,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var designation = await _context.Designations
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item => item.Id == dto.Id &&
                            item.InstitutionId == institutionId &&
                            item.IsSoftDeleted != true,
                    cancellationToken);

            return designation == null
                ? null
                : _mapper.Map<GetSingleDesignationResponseDTO>(designation);
        }

        /// <summary>
        /// Gets active designation options for a department within a trusted institution.
        /// </summary>
        public Task<List<GetDesignationOptionResponseDTO>> GetOptionAsync(
            int departmentId,
            long institutionId,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Designations
                .AsNoTracking()
                .Where(designation => designation.InstitutionId == institutionId &&
                                      designation.IsSoftDeleted != true &&
                                      designation.IsActive == true);

            if (departmentId > 0)
                query = query.Where(designation => designation.DepartmentId == departmentId);

            return query
                .OrderBy(designation => designation.DesignationName)
                .Select(designation => new GetDesignationOptionResponseDTO
                {
                    Id = designation.Id,
                    DepartmentId = designation.DepartmentId,
                    DesignationName = designation.DesignationName
                })
                .ToListAsync(cancellationToken);
        }

        #endregion
    }
}
