// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Provides current Host-role module and operation permission queries for runtime authorization.
// ================================================================

using skolenza.application.DTOS.Host;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Repositories
{
    /// <summary>
    /// Provides read operations over <c>HostRoleModuleAndPermission</c> assignments.
    /// </summary>
    public class HostRolePermissionRepository : IHostRolePermissionRepository
    {
        #region Fields

        private readonly SkolenzaDbContext _context;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="HostRolePermissionRepository"/> class.
        /// </summary>
        /// <param name="context">The database context used to read Host permissions.</param>
        public HostRolePermissionRepository(
            SkolenzaDbContext context)
        {
            _context = context;
        }

        #endregion

        #region Host Permission Operations

        /// <summary>
        /// Retrieves the authenticated Host user's currently effective module and operation permissions
        /// from the Host authorization model.
        /// </summary>
        /// <param name="hostRoleId">The validated current Host role identifier.</param>
        /// <param name="cancellationToken">Token used to cancel the database operation.</param>
        /// <returns>The currently effective Host module-operation grants.</returns>
        public Task<List<HostUserPermissionResponseDTO>> GetHostUserPermissionsAsync(
            long hostRoleId,
            CancellationToken cancellationToken = default)
        {
            // Preserve the legacy Host permission semantics used by the Host login response.
            return _context.HostRoleModuleAndPermissions
                .AsNoTracking()
                .Where(permission =>
                    permission.HostRoleId == hostRoleId &&
                    permission.IsActive &&
                    !permission.IsSoftDeleted)
                .OrderBy(permission => permission.Module.ModuleName)
                .ThenBy(permission => permission.Operation.OperationName)
                .Select(permission => new HostUserPermissionResponseDTO
                {
                    ModuleId = permission.ModuleId,
                    ModuleName = permission.Module.ModuleName,
                    DisplayName = permission.Module.DisplayName,
                    OperationId = permission.OperationId,
                    OperationName = permission.Operation.OperationName
                })
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc />
        public Task<List<HostRoleModuleAndPermission>> GetByHostRoleIdAsync(
            long hostRoleId,
            CancellationToken cancellationToken = default)
        {
            return _context.HostRoleModuleAndPermissions
                .Where(permission => permission.HostRoleId == hostRoleId)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc />
        public Task<List<HostRoleModuleAndPermission>> GetNonDeletedByModuleIdsAsync(
            IReadOnlyCollection<int> moduleIds,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(moduleIds);

            if (moduleIds.Count == 0)
            {
                return Task.FromResult(new List<HostRoleModuleAndPermission>());
            }

            return _context.HostRoleModuleAndPermissions
                .Where(permission =>
                    moduleIds.Contains(permission.ModuleId) &&
                    !permission.IsSoftDeleted)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc />
        public Task<bool> IsOperationAssignedToNonDeletedPermissionAsync(
            int operationId,
            CancellationToken cancellationToken = default)
        {
            return _context.HostRoleModuleAndPermissions
                .AnyAsync(permission =>
                    permission.OperationId == operationId &&
                    !permission.IsSoftDeleted,
                    cancellationToken);
        }

        /// <inheritdoc />
        public async Task BulkInsertAsync(
            List<HostRoleModuleAndPermission> permissions,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(permissions);

            if (permissions.Count == 0)
            {
                return;
            }

            await _context.HostRoleModuleAndPermissions.AddRangeAsync(permissions, cancellationToken);
        }

        #endregion
    }
}
