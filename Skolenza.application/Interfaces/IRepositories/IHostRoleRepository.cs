// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines persistence operations for host roles.
// ================================================================

using skolenza.domain.Entity;
using System.Collections.Generic;

namespace skolenza.application.Interfaces.IRepositories
{
    /// <summary>
    /// Defines persistence operations for host roles.
    /// </summary>
    public interface IHostRoleRepository
    {
        #region Host Role Methods

        /// <summary>
        /// Retrieves a non-soft-deleted host role by identifier.
        /// </summary>
        /// <param name="id">The host-role identifier.</param>
        /// <returns>The matching host role, or <see langword="null"/> when it is not found.</returns>
        Task<HostRole?> GetByIdAsync(long id);

        /// <summary>
        /// Retrieves all non-soft-deleted host roles.
        /// </summary>
        /// <returns>A list of host roles, which is empty when no matching roles exist.</returns>
        Task<List<HostRole>> GetAllAsync();

        /// <summary>
        /// Retrieves a non-soft-deleted host role by name.
        /// </summary>
        /// <param name="roleName">The host-role name to search for.</param>
        /// <returns>The matching host role, or <see langword="null"/> when it is not found.</returns>
        Task<HostRole?> GetByRoleNameAsync(string roleName);

        /// <summary>
        /// Adds a host role.
        /// </summary>
        /// <param name="entity">The host-role entity to add.</param>
        /// <returns>The persisted host-role entity.</returns>
        Task<HostRole> AddAsync(HostRole entity);

        /// <summary>
        /// Persists updates to a host role.
        /// </summary>
        /// <param name="entity">The host-role entity to update.</param>
        /// <returns>The persisted host-role entity.</returns>
        Task<HostRole> UpdateAsync(HostRole entity);

        /// <summary>
        /// Persists a host role's prepared soft-delete state.
        /// </summary>
        /// <param name="entity">The host-role entity marked for soft deletion.</param>
        /// <returns><see langword="true"/> when persistence succeeds; otherwise, <see langword="false"/>.</returns>
        Task<bool> DeleteAsync(HostRole entity);

        #endregion
    }
}
