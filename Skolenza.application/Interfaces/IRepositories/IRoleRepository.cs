// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines persistence operations for institution roles and role projections.
// ================================================================

using skolenza.application.DTOs.Role;
using skolenza.application.DTOs.RoleModulePermission;
using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Role;
using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IRepositories
{
    /// <summary>
    /// Defines persistence operations for institution roles and role projections.
    /// </summary>
    public interface IRoleRepository
    {
        Task<GetSingleRoleResponseDTO?> GetByIdAsync1(GetSingleRoleRequestDTO dto);

        /// <summary>
        /// Gets role option projections for the trusted institution context.
        /// </summary>
        /// <param name="institutionId">The authenticated institution identifier.</param>
        /// <param name="dto">The client-editable role option query criteria.</param>
        /// <returns>The matching role option projections.</returns>
        Task<List<GetRoleOptionResponseDTO>> GetOptionAsync(long institutionId, GetRoleOptionRequestDTO dto);

        /// <summary>
        /// Soft deletes a institution role using trusted institution and actor identifiers.
        /// </summary>
        Task<bool> DeleteAsync(int id, long institutionId, long staffMemberId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines whether a Role still has non-soft-deleted staffMember assignments or module-operation permissions.
        /// Inactive dependent rows are intentionally included because they may be reactivated later.
        /// </summary>
        Task<bool> HasNonDeletedDependenciesAsync(
            int roleId,
            long institutionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists a prepared institution role entity.
        /// </summary>
        Task<Role?> CreateAsync(Role entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets paged role projections for the authenticated institution.
        /// </summary>
        /// <param name="institutionId">The authenticated institution identifier.</param>
        /// <param name="dto">The client-editable role query criteria.</param>
        /// <returns>A paged role projection result.</returns>
        Task<PagedResponseDTO<GetRoleResponseDTO>> GetAsync(long institutionId, GetRoleRequestDTO dto);
        Task<List<GetRoleResponseDTO>> GetRoleAsync(long institutionId, int roleTypeId, bool isActive);

        /// <summary>
        /// Gets a mutable role after enforcing institution ownership.
        /// </summary>
        Task<Role?> GetByIdForInstitutionAsync(int id, long institutionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists a prepared institution role entity.
        /// </summary>
        Task<bool> UpdateAsync(Role entity, CancellationToken cancellationToken = default);

        Task<Role> AutoCreatedSingleInstitutionRoleAsync(Role role);
        Task<bool> AutoCreatedForInstitutionRoleAsync(List<Role> roles);
        Task<int> AutoCreateUserRoleAndAutomatedRolePermissionMappingAsync(long? InstitutionId, long staffMemberId, int role);
        Task<Role?> GetInstitutionAdminRoleAsync(long institutionId);
    }
}
