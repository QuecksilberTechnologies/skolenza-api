// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Provides current Institution staffMember role-assignment persistence queries.
// ================================================================

using skolenza.application.Interfaces.IRepositories;

using skolenza.domain.Entity;

using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace skolenza.persistance.Repositories;

/// <summary>
/// Provides persistence operations for Institution staffMember role assignments.
/// </summary>
public class UserRoleRepository : IUserRoleRepository
{
    private readonly SkolenzaDbContext _context;
    private readonly ILogger<UserRoleRepository>? _logger;

    public UserRoleRepository(
        SkolenzaDbContext context,
        ILogger<UserRoleRepository>? logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Effective Role Queries

    /// <summary>
    /// Retrieves non-deleted role assignments for an staffMember.
    /// </summary>
    /// <param name="userId">The staffMember identifier.</param>
    /// <returns>The staffMember's persisted role assignments.</returns>
    public async Task<List<UserRole>> GetUsersRoleByIdAsync(long userId)
    {
        try
        {
            _logger?.LogInformation("Fetching roles for user with ID: {UserId}", userId);

            var userRoles = await _context.UserRoles
                .Where(ur => ur.StaffMemberId == userId && ur.IsSoftDeleted != true)
                .ToListAsync();

            return userRoles ?? new List<UserRole>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching roles for userId: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves current active, non-deleted role assignments for an authenticated Institution staffMember.
    /// </summary>
    /// <param name="staffMemberId">The trusted staffMember identifier.</param>
    /// <param name="institutionId">The trusted Institution identifier.</param>
    /// <param name="cancellationToken">A token used to cancel the database operation.</param>
    /// <returns>The effective role assignments with active, non-deleted Institution roles.</returns>
    public async Task<List<UserRole>> GetStaffMemberRolesWithDetailsByIdAsync(
        long staffMemberId,
        long? institutionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger?.LogInformation("Fetching roles for StaffMemberId: {StaffMemberId}", staffMemberId);

            var userRoles = await _context.UserRoles
                .AsNoTracking()
                .Include(x => x.Role)
                .Where(x =>
                    x.StaffMemberId == staffMemberId &&
                    x.IsActive == true &&
                    x.IsSoftDeleted != true &&
                    x.Role != null &&
                    x.Role.IsActive == true &&
                    x.Role.IsSoftDeleted != true &&
                    x.Role.InstitutionId == institutionId)
                .ToListAsync(cancellationToken);

            return userRoles ?? new List<UserRole>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error fetching role details for StaffMemberId: {StaffMemberId}", staffMemberId);
            throw;
        }
    }

    #endregion

    #region Role Assignment Writes

    /// <summary>
    /// Adds role assignments to the current unit-of-work change set.
    /// </summary>
    /// <param name="entities">The role assignments to add.</param>
    /// <returns>A task that completes after the assignments are staged.</returns>
    public async Task AddRangeAsync(List<UserRole> entities)
    {
        try
        {
            if (entities == null || !entities.Any())
                return;

            await _context.UserRoles.AddRangeAsync(entities);

            _logger?.LogInformation("Bulk Insert: {Count} roles", entities.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in bulk insert UserRoles");
            throw;
        }
    }

    /// <summary>
    /// Marks role assignments for update in the current unit-of-work change set.
    /// </summary>
    /// <param name="entities">The role assignments to update.</param>
    public void UpdateRange(List<UserRole> entities)
    {
        try
        {
            if (entities == null || !entities.Any())
                return;

            _context.UserRoles.UpdateRange(entities);

            _logger?.LogInformation("Bulk Update: {Count} roles", entities.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in bulk update UserRoles");
            throw;
        }
    }

    #endregion
}
