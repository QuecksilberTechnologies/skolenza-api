// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines current Host-role module and operation permission queries for runtime authorization.
// ================================================================

using skolenza.application.DTOS.Host;
using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IRepositories
{
    /// <summary>
    /// Defines read operations over permissions assigned to a Host role.
    /// </summary>
    public interface IHostRolePermissionRepository
    {
        #region Host Permission Operations

        /// <summary>
        /// Retrieves the authenticated Host user's currently effective module and operation permissions
        /// from the Host authorization model.
        /// </summary>
        /// <param name="hostRoleId">The Host-role primary key whose effective permissions are requested.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The currently effective Host module-operation grants.</returns>
        Task<List<HostUserPermissionResponseDTO>> GetHostUserPermissionsAsync(
            long hostRoleId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves every persisted permission assignment for a Host role as tracked entities.
        /// </summary>
        /// <param name="hostRoleId">The Host-role primary key whose assignments are requested.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>All active and soft-deactivated assignments for the Host role.</returns>
        Task<List<HostRoleModuleAndPermission>> GetByHostRoleIdAsync(
            long hostRoleId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves tracked, non-soft-deleted Host-role permissions for the supplied Module identifiers.
        /// </summary>
        /// <param name="moduleIds">The affected Module identifiers.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The matching Host-role permissions that may be synchronized with Module status.</returns>
        Task<List<HostRoleModuleAndPermission>> GetNonDeletedByModuleIdsAsync(
            IReadOnlyCollection<int> moduleIds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines whether an Operation is assigned to any non-soft-deleted Host-role permission.
        /// </summary>
        /// <param name="operationId">The Operation identifier to check.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns><see langword="true"/> when the Operation is assigned to a current Host-role permission.</returns>
        Task<bool> IsOperationAssignedToNonDeletedPermissionAsync(
            int operationId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds new Host-role permission assignments to the current unit-of-work change set.
        /// </summary>
        /// <param name="permissions">The new unique permission assignments to add.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>A task that completes when the assignments have been staged for persistence.</returns>
        Task BulkInsertAsync(
            List<HostRoleModuleAndPermission> permissions,
            CancellationToken cancellationToken = default);

        #endregion
    }
}
