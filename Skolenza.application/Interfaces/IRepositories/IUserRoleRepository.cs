
// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines persistence operations for Institution staffMember role assignments.
// ================================================================

using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IRepositories
{
    /// <summary>
    /// Defines persistence operations for Institution staffMember role assignments.
    /// </summary>
    public interface IUserRoleRepository
    {
        #region Effective Role Queries

        /// <summary>
        /// Retrieves non-deleted role assignments for an staffMember.
        /// </summary>
        /// <param name="userId">The staffMember identifier.</param>
        /// <returns>The staffMember's persisted role assignments.</returns>
        Task<List<UserRole>> GetUsersRoleByIdAsync(long userId);

        /// <summary>
        /// Retrieves the current active, non-deleted role assignments for a Institution staffMember.
        /// </summary>
        /// <param name="staffMemberId">The authenticated staffMember identifier.</param>
        /// <param name="institutionId">The validated Institution identifier that owns the effective roles.</param>
        /// <param name="cancellationToken">A token used to cancel the database operation.</param>
        /// <returns>The current effective assignments with their active Institution roles.</returns>
        Task<List<UserRole>> GetStaffMemberRolesWithDetailsByIdAsync(
            long staffMemberId,
            long? institutionId,
            CancellationToken cancellationToken = default);

        #endregion

        #region Role Assignment Writes

        /// <summary>
        /// Adds role assignments to the current unit-of-work change set.
        /// </summary>
        /// <param name="entities">The role assignments to add.</param>
        /// <returns>A task that completes after the assignments are staged.</returns>
        Task AddRangeAsync(List<UserRole> entities);

        /// <summary>
        /// Marks role assignments for update in the current unit-of-work change set.
        /// </summary>
        /// <param name="entities">The role assignments to update.</param>
        void UpdateRange(List<UserRole> entities);

        #endregion
    }
     

}
