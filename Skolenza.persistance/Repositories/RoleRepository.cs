// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Persists institution roles and role query projections.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers.Converters;
using skolenza.application.Constants;
using skolenza.application.DTOs.Role;
using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Role;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IRepositories;
using skolenza.persistance.Data.Context;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data; using skolenza.domain.Entity;

namespace skolenza.persistance.Repositories;

/// <summary>
/// Provides persistence operations for Role records.
/// </summary>
public class RoleRepository : IRoleRepository
{

    private readonly SkolenzaDbContext _context;

    private readonly IMapper _mapper;
    private readonly ILogger<RoleRepository> _logger;
    private readonly IEncryptionService _encryptionService;

    public RoleRepository(
 SkolenzaDbContext context,
 ILogger<RoleRepository> logger,
 IMapper mapper,

 IEncryptionService encryptionService)
    {
        _context = context;
        _logger = logger;
        _mapper = mapper;

        _encryptionService = encryptionService;
    }


    public async Task<Role?> GetInstitutionAdminRoleAsync(long institutionId)
    {
        try
        {
            return await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r =>
                    r.InstitutionId == institutionId &&
                    r.RoleType == ConstantValues.RoleTypeAdmin &&
                    r.IsActive == true &&
                    r.IsSoftDeleted == false &&
                    r.IsSystemDefault == false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching institution admin role for InstitutionId: {InstitutionId}", institutionId);
            return null;
        }
    }
    public async Task<int> AutoCreateUserRoleAndAutomatedRolePermissionMappingAsync(long? institutionId, long staffMemberId, int role)
    {
        try
        {
            var enabledOperations = await _context.InstitutionEnabledOperations
                .Where(x => x.InstitutionId == institutionId)
                .ToListAsync();

            var rolePermissions = enabledOperations.Select(op => new RoleModuleAndPermission
            {
                RoleId = role,
                ModuleId = op.ModuleId,
                OperationId = op.OperationId,
                HasAccess = true,
                IsActive = op.IsEnabled,
                Remark = "System Generate Permission for user",
                IsOperational = true,
                AddedById = institutionId,
                AddedDateTime = DateTime.UtcNow,
                IsSoftDeleted = false
            }).ToList();

            await _context.RoleModuleAndPermissions.AddRangeAsync(rolePermissions);

            _logger.LogInformation(
                "RoleModuleAndPermission entries added to DbContext successfully. Count: {Count}, InstitutionId: {InstitutionId}, RoleId: {RoleId}",
                rolePermissions.Count,
                institutionId,
                role);

            return rolePermissions.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AutoCreateUserRoleAndAutomatedRolePermissionMappingAsync");
            throw;
        }
    }




    /// <summary>
    /// Retrieves all active roles for a given institution.
    /// </summary>
    /// <param name="role">The request DTO containing institution and filter information.</param>
    /// <returns>
    /// A list of <see cref="GetRoleResponseDTO"/> representing all active roles for the institution.
    /// </returns>
    ///





    /// <summary>
    /// Creates a new role for the specified institution.
    /// </summary>
    /// <param name="dto">The request DTO containing role creation details.</param>
    /// <returns>
    /// A list containing the created role details, mapped to <see cref="GetRoleResponseDTO"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when the request DTO is null.</exception>
    /// <exception cref="Exception">Logs and rethrows any exception during creation.</exception>

    /// <summary>
    /// Updates an existing role with values from the DTO.
    /// Only updates fields that are provided; keeps existing values for null/empty fields.
    /// </summary>
    /// <param name="dto">DTO containing the updated role information.</param>
    /// <returns>Returns true if update succeeds, false otherwise.</returns>




    public async Task<Role> AutoCreatedSingleInstitutionRoleAsync(Role role)
    {
        try
        {
            // await using var context = await _contextFactory.CreateDbContextAsync();

            if (role == null)
            {
                _logger.LogWarning("AutoCreatedForInstitutionRoleAsync: Received null role object.");
                throw new ArgumentNullException(nameof(role), "Role object cannot be null.");
            }

            // Logging input
            _logger.LogInformation("Creating new Role for InstitutionId: {InstitutionId}, RoleName: {RoleName}", role.InstitutionId, role.RoleName);

            role.AddedDateTime = DateTime.Now;

            await _context.Roles.AddAsync(role);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Role created successfully with Id: {RoleId}", role.Id);

            // Optionally reload from DB if you want latest tracking
            var latestRole = await _context.Roles
                .OrderByDescending(r => r.Id)
                .FirstOrDefaultAsync(r => r.Id == role.Id); // ensure you get the one just created

            return latestRole ?? role;
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Database update failed while creating Role.");
            throw; // Let it bubble up or wrap in custom exception
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred in AutoCreatedForInstitutionRoleAsync.");
            throw;
        }
    }
    public async Task<bool> AutoCreatedForInstitutionRoleAsync(List<Role> roles)
    {
        if (roles == null || !roles.Any())
        {
            _logger.LogWarning("AutoCreatedForInstitutionRoleAsync called with null or empty role list.");
            return false;
        }

        try
        {
            var validRoles = new List<Role>();

            foreach (var dto in roles)
            {
                if (dto.InstitutionId <= 0 || string.IsNullOrWhiteSpace(dto.RoleName))
                {
                    _logger.LogWarning(
                        "Skipping invalid role entry. InstitutionId: {InstitutionId}, RoleName: {RoleName}",
                        dto.InstitutionId,
                        dto.RoleName);
                    continue;
                }

                bool exists = await _context.Roles.AnyAsync(r =>
                    r.InstitutionId == dto.InstitutionId &&
                    r.RoleName.ToLower() == dto.RoleName.ToLower() &&
                    r.IsSoftDeleted == false);

                if (exists)
                {
                    _logger.LogInformation(
                        "Role '{RoleName}' already exists for InstitutionId {InstitutionId}. Skipping insert.",
                        dto.RoleName,
                        dto.InstitutionId);
                    continue;
                }

                validRoles.Add(new Role
                {
                    InstitutionId = dto.InstitutionId,
                    RoleName = dto.RoleName,
                    RoleType = dto.RoleType,
                    IsSystemDefault = false,
                    IsActive = true,
                    IsSoftDeleted = false,
                    Remark = string.IsNullOrWhiteSpace(dto.Remark)
                        ? ConstantValues.GetRoleTypeRemark(dto.RoleType)
                        : dto.Remark,
                    AddedById = dto.InstitutionId ?? 0,
                    AddedDateTime = DateTime.UtcNow
                });
            }

            if (!validRoles.Any())
            {
                _logger.LogWarning("No valid roles to insert after validation.");
                return false;
            }

            await _context.Roles.AddRangeAsync(validRoles);

            _logger.LogInformation("Roles added to DbContext successfully. Count: {Count}", validRoles.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred in AutoCreatedForInstitutionRoleAsync.");
            return false;
        }
    }


    #region Update

    /// <summary>
    /// Gets a mutable role after enforcing institution ownership.
    /// </summary>
    public Task<Role?> GetByIdForInstitutionAsync(
        int id,
        long institutionId,
        CancellationToken cancellationToken = default)
    {
        return _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(
            role => role.Id == id &&
                    role.InstitutionId == institutionId &&
                    role.IsSoftDeleted != true,
            cancellationToken);
    }

    /// <summary>
    /// Persists a prepared institution role entity.
    /// </summary>
    public async Task<bool> UpdateAsync(Role entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            _logger.LogWarning("UpdateAsync called with a null Role entity.");
            return false;
        }

        try
        {

            var existingRole = await _context.Roles
                .FirstOrDefaultAsync(
                    role => role.Id == entity.Id &&
                            role.InstitutionId == entity.InstitutionId &&
                            role.IsSoftDeleted != true,
                    cancellationToken);

            if (existingRole == null)
            {
                _logger.LogWarning("Role with ID {RoleId} was not found for the institution.", entity.Id);
                return false;
            }

            bool isModified = false;

            //  RoleName
            if (!string.IsNullOrWhiteSpace(entity.RoleName) &&
                entity.RoleName.Trim() != existingRole.RoleName)
            {
                existingRole.RoleName = entity.RoleName.Trim();
                isModified = true;
            }

            //  RoleType
            if (entity.RoleType != existingRole.RoleType)
            {
                existingRole.RoleType = entity.RoleType;
                isModified = true;
            }

            //  Remark
            if (entity.Remark != existingRole.Remark)
            {
                existingRole.Remark = entity.Remark;
                isModified = true;
            }

            //  IsActive
            if (entity.IsActive != existingRole.IsActive)
            {
                existingRole.IsActive = entity.IsActive;
                isModified = true;
            }


            //  NEW — Update audit fields only when something changes
            if (isModified)
            {
                existingRole.UpdatedById = entity.UpdatedById;
                existingRole.UpdatedDateTime = entity.UpdatedDateTime;

                var affected = await _context.SaveChangesAsync();

                if (affected > 0)
                {
                    _logger.LogInformation(
                        "✅ Role updated successfully. ID: {RoleId}, UpdatedBy: {UpdatedById}",
                        entity.Id, entity.UpdatedById
                    );
                    return true;
                }

                _logger.LogInformation("ℹ️ Update called but no DB changes detected. ID: {RoleId}", entity.Id);
            }
            else
            {
                _logger.LogInformation("🔹 No changes found for Role ID {RoleId}.", entity.Id);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
                _logger.LogError(ex, "Error while updating Role ID {RoleId}", entity.Id);
            return false;
        }
    }

    #endregion


    public async Task<GetSingleRoleResponseDTO?> GetByIdAsync1(GetSingleRoleRequestDTO dto)
    {
        try
        {
            if (dto == null)
            {
                _logger.LogWarning("⚠️ GetByIdAsync1 called with null DTO.");
                return null;
            }


            _logger.LogInformation("🔍 Fetching role details for RoleId: {RoleId}, InstitutionId:", dto.Id);

            //  Fetch Role by Id with Institution check and SoftDelete filter
            var role = await _context.Roles
                .Where(r =>
                    r.Id == dto.Id &&
                    r.IsSoftDeleted != true)
                .FirstOrDefaultAsync();

            if (role == null)
            {
                _logger.LogWarning("⚠️ No Role found for RoleId: {RoleId} and InstitutionId: ", dto.Id);
                return null;
            }

            //  Map entity to response DTO
            var mappedRole = _mapper.Map<GetSingleRoleResponseDTO>(role);

            _logger.LogInformation("✅ Successfully retrieved role details for RoleId: {RoleId}", dto.Id);

            return mappedRole;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error occurred while fetching role details for RoleId: {RoleId}", dto?.Id);
            return null;
        }
    }




    #region Delete

    /// <summary>
    /// Determines whether the Role is still referenced by a current staffMember-role assignment
    /// or a current module-operation permission. The IsActive flag is deliberately not part
    /// of this guard: an inactive dependency can be reactivated and must retain its Role.
    /// </summary>
    public Task<bool> HasNonDeletedDependenciesAsync(
        int roleId,
        long institutionId,
        CancellationToken cancellationToken = default)
    {
        return _context.Roles
            .AsNoTracking()
            .Where(role => role.Id == roleId && role.InstitutionId == institutionId)
            .AnyAsync(
                role => role.UserRole.Any(userRole => userRole.IsSoftDeleted != true) ||
                        role.RoleModuleAndPermission.Any(permission => permission.IsSoftDeleted != true),
                cancellationToken);
    }

    /// <summary>
    /// Soft deletes a role after enforcing institution ownership.
    /// </summary>
    public async Task<bool> DeleteAsync(
        int id,
        long institutionId,
        long staffMemberId,
        CancellationToken cancellationToken = default)
    {
        try
        {


            //  Step 3️⃣ - Fetch Role
            var role = await _context.Roles
                .FirstOrDefaultAsync(
                    role => role.Id == id &&
                            role.InstitutionId == institutionId &&
                            role.IsSoftDeleted != true,
                    cancellationToken);

            if (role == null)
            {
                _logger.LogWarning("Delete failed: Role not found for Id: {Id}", id);
                return false;
            }

            //  Step 4️⃣ - Apply Soft Delete
            role.IsSoftDeleted = true;
            role.IsActive = false;
            role.SoftDeletedById = staffMemberId;
            role.DeletedDateTime = DateTime.UtcNow;
            role.UpdatedById = staffMemberId;
            role.UpdatedDateTime = DateTime.UtcNow;

            //  Step 5️⃣ - Update Entity
            _context.Roles.Update(role);

            //  Step 6️⃣ - Commit Changes
            var result = await _context.SaveChangesAsync();

            if (result > 0)
            {
                _logger.LogInformation("Role deleted successfully. Id: {Id}", id);
                return true;
            }

            _logger.LogWarning("No changes saved for Role Id: {Id}", id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while deleting Role. Id: {Id}", id);
            return false;
        }
    }

    #endregion

    #region Create

    /// <summary>
    /// Persists a prepared institution role entity.
    /// </summary>
    public async Task<Role?> CreateAsync(Role entity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (entity == null)
            {
                _logger.LogWarning("CreateAsync called with a null Role entity.");
                return null;
            }

            //  Duplicate check
            bool isDuplicate = await _context.Roles
                .AnyAsync(
                    role => role.RoleName.ToLower() == entity.RoleName.ToLower() &&
                            role.InstitutionId == entity.InstitutionId &&
                            role.IsSoftDeleted != true,
                    cancellationToken);

            if (isDuplicate)
            {
                _logger.LogWarning("Role '{RoleName}' already exists for InstitutionId: {InstitutionId}", entity.RoleName, entity.InstitutionId);
                return null;
            }

            await _context.Roles.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Role '{RoleName}' created for InstitutionId: {InstitutionId}.", entity.RoleName, entity.InstitutionId);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role '{RoleName}' for InstitutionId: {InstitutionId}", entity?.RoleName, entity?.InstitutionId);
            throw;
        }
    }

    #endregion

    public async Task<List<GetRoleResponseDTO>> GetRoleAsync(  long institutionId,   int roleTypeId,   bool isActive)
    {
        try
        {

            var query = _context.Roles
                .AsNoTracking()
                .Where(r =>
                    r.InstitutionId == institutionId &&
                    r.IsActive == isActive &&
                    (r.IsSoftDeleted !=true));

            // Optional filter
            if (roleTypeId > 0)
                query = query.Where(r => r.RoleType == roleTypeId);

            var result = await query
                .Select(r => new GetRoleResponseDTO
                {
                    Id = r.Id,
                    RoleName = r.RoleName,
                    RoleType = r.RoleType,
                    IsActive = r.IsActive
                })
                .ToListAsync();

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Error fetching roles for InstitutionId: {InstitutionId}",
                institutionId);

            return new List<GetRoleResponseDTO>();
        }
    }

    /// <summary>
    /// Projects paged roles for the supplied trusted institution identifier.
    /// </summary>
    public async Task<PagedResponseDTO<GetRoleResponseDTO>> GetAsync(long institutionId, GetRoleRequestDTO dto)
    {
        var response = new PagedResponseDTO<GetRoleResponseDTO>();

        try
        {
            if (dto == null)
            {
                _logger.LogWarning("⚠️ GetAsync called with null request DTO.");
                return response;
            }


            int roleType = dto.RoleType;

            var query = _context.Roles
                .Where(r => r.InstitutionId == institutionId && (r.IsSoftDeleted != true))
                .AsQueryable();

            //  Optional Filters
            if (dto.Id > 0)
                query = query.Where(r => r.Id == dto.Id);



            if (!string.IsNullOrWhiteSpace(dto.RoleName))
                query = query.Where(r => r.RoleName.ToLower().Contains(dto.RoleName.ToLower()));

            if (dto.IsActive == true)
                query = query.Where(r => r.IsActive == dto.IsActive);

            if (roleType > 0)
                query = query.Where(r => r.RoleType == roleType);

            //  Sorting

            query = dto.SortBy?.ToLower() switch
            {
                "rolename" => dto.SortOrder?.ToLower() == "asc"
                    ? query.OrderBy(x => x.RoleName)
                    : query.OrderByDescending(x => x.RoleName),

                "roletype" => dto.SortOrder?.ToLower() == "asc"
                    ? query.OrderBy(x => x.RoleType)
                    : query.OrderByDescending(x => x.RoleType),

                _ => query.OrderByDescending(x => x.Id) // default sort by Id
            };

            //  Pagination
            var totalRecords = await query.CountAsync();
            var roles = await query
                .Skip((dto.PageNumber - 1) * dto.PageSize)
                .Take(dto.PageSize)
                .ToListAsync();

            var mappedList = _mapper.Map<List<GetRoleResponseDTO>>(roles);

            response.Data = mappedList;
            response.TotalCount = totalRecords;
            response.PageNumber = dto.PageNumber;
            response.PageSize = dto.PageSize;
            response.TotalPages = (int)Math.Ceiling((double)totalRecords / dto.PageSize);

            _logger.LogInformation("Retrieved {Count} roles for InstitutionId: {InstitutionId}", mappedList.Count, institutionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching roles for InstitutionId: {InstitutionId}", institutionId);
            new List<GetRoleResponseDTO>();
        }

        return response;
    }

    #region Option Queries
    /// <summary>
    /// Projects role options for the trusted institution without constructing an API response.
    /// </summary>
    /// <param name="institutionId">The authenticated institution identifier.</param>
    /// <param name="dto">The client-editable role option query criteria.</param>
    /// <returns>The matching role option projections.</returns>
    public async Task<List<GetRoleOptionResponseDTO>> GetOptionAsync(long institutionId, GetRoleOptionRequestDTO dto)
    {
        try
        {

            var query = _context.Roles
                .Where(x => x.InstitutionId == institutionId && x.IsSoftDeleted != true && x.IsActive);

            if (dto.RoleType > 0)
                query = query.Where(x => x.RoleType == dto.RoleType);

            return await query
                .OrderBy(x => x.RoleName)
                .Select(r => new GetRoleOptionResponseDTO
                {
                    Id = r.Id,
                    RoleName = r.RoleName,
                    RoleType = r.RoleType,
                    IsActive = r.IsActive

                })
                .AsNoTracking()
                .ToListAsync();

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching role options for institution {InstitutionId}.", institutionId);
            throw;
        }
    }

    #endregion
}
