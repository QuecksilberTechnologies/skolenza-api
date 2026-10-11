// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines persistence operations for host users.
// ================================================================

using skolenza.application.DTOS.Host;
using skolenza.application.DTOS.Pagination;
using skolenza.domain.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.Interfaces.IRepositories
{
    /// <summary>
    /// Defines persistence operations for host users.
    /// </summary>
    public interface IHostUserRepository
    {
        #region Host User Methods

        /// <summary>
        /// Retrieves an active, non-soft-deleted host user by login identifier.
        /// </summary>
        /// <param name="loginId">The login identifier to search for.</param>
        /// <returns>The matching host user, or <see langword="null"/> when no active match exists.</returns>
        Task<HostUser?> GetByLoginIdAsync(string loginId);

        /// <summary>
        /// Retrieves a non-soft-deleted host user by identifier.
        /// </summary>
        /// <param name="id">The host-user identifier.</param>
        /// <returns>The matching host user with its role, or <see langword="null"/> when it is not found.</returns>
        Task<HostUser?> GetByIdAsync(long id);

        /// <summary>
        /// Retrieves a tracked, non-soft-deleted host user for a state-changing operation.
        /// </summary>
        /// <param name="id">The host-user identifier.</param>
        /// <returns>The tracked host user with its role, or <see langword="null"/> when it is not found.</returns>
        Task<HostUser?> GetForUpdateAsync(long id);

        /// <summary>
        /// Retrieves all non-soft-deleted host users with their roles.
        /// </summary>
        /// <returns>A list of host users, which is empty when no matching users exist.</returns>
        Task<List<HostUser>> GetAllAsync();

        /// <summary>
        /// Retrieves a database-paged set of non-soft-deleted Host users with an optional active-status filter.
        /// </summary>
        /// <param name="isActive">When supplied, limits rows to the requested active status.</param>
        /// <param name="pageNumber">The normalized one-based page number.</param>
        /// <param name="pageSize">The normalized number of rows per page.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The requested Host-user page.</returns>
        Task<PagedResponseDTO<GetHostUserResponseDTO>> GetPagedAsync(
            bool? isActive,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken);

        /// <summary>
        /// Determines whether an active, non-soft-deleted host user is assigned to a host role.
        /// </summary>
        /// <param name="hostRoleId">The host-role identifier to check.</param>
        /// <returns><see langword="true"/> when at least one active host user is assigned; otherwise, <see langword="false"/>.</returns>
        Task<bool> AnyActiveUserByHostRoleIdAsync(long hostRoleId);

        /// <summary>
        /// Adds a host user.
        /// </summary>
        /// <param name="entity">The host-user entity to add.</param>
        /// <returns>The persisted host-user entity.</returns>
        Task<HostUser> AddAsync(HostUser entity);

        /// <summary>
        /// Persists updates to a host user.
        /// </summary>
        /// <param name="entity">The host-user entity to update.</param>
        /// <returns>The persisted host-user entity.</returns>
        Task<HostUser> UpdateAsync(HostUser entity);

        /// <summary>
        /// Persists a host user's prepared soft-delete state.
        /// </summary>
        /// <param name="entity">The host-user entity marked for soft deletion.</param>
        /// <returns><see langword="true"/> when persistence succeeds; otherwise, <see langword="false"/>.</returns>
        Task<bool> DeleteAsync(HostUser entity);

        #endregion

    }
}
