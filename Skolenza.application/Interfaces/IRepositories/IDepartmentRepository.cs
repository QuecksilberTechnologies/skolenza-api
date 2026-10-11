// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines persistence operations for institution-scoped departments.
// ================================================================

using skolenza.application.DTOs.Department;
using skolenza.application.DTOS.Department;
using skolenza.application.DTOS.Pagination;
using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IRepositories
{
    /// <summary>
    /// Defines persistence operations for institution-scoped departments.
    /// </summary>
    public interface IDepartmentRepository
    {
        /// <summary>
        /// Gets a department response by its identifier.
        /// </summary>
        Task<GetSingleDepartmentResponseDTO?> GetByIdAsync(
            GetSingleDepartmentRequestDTO dto,
            long institutionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets active department options for a trusted institution.
        /// </summary>
        Task<List<GetDepartmentOptionResponse>> GetOptionAsync(
            long institutionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Soft deletes a department owned by the supplied institution.
        /// </summary>
        Task<bool> DeleteAsync(
            int id,
            long institutionId,
            long staffMemberId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines whether a Department is still assigned to a non-soft-deleted staffMember.
        /// Inactive staffMembers are intentionally included because they can be reactivated later.
        /// </summary>
        Task<bool> HasNonDeletedStaffMembersAsync(
            int departmentId,
            long institutionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a department using the supplied domain entity.
        /// </summary>
        Task<Department?> CreateAsync(
            Department entity,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a paged department projection for a trusted institution.
        /// </summary>
        Task<PagedResponseDTO<GetDepartmentResponseDTO>> GetAsync(
            GetDepartmentRequestDTO request,
            long institutionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds department seed entities to the current persistence context.
        /// </summary>
        Task<bool> AutoCreateDepartmentSeedAsync(
            List<Department>? departments,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a mutable department entity after enforcing institution ownership.
        /// </summary>
        Task<Department?> GetByIdForInstitutionAsync(
            int id,
            long institutionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists changes to an existing department entity.
        /// </summary>
        Task<bool> UpdateAsync(
            Department entity,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines whether a department belongs to a institution.
        /// </summary>
        Task<bool> ExistsAsync(
            long id,
            long institutionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets department-name to identifier mappings for a institution.
        /// </summary>
        Task<Dictionary<string, int>> GetDepartmentNameIdMapAsync(
            long institutionId,
            CancellationToken cancellationToken = default);
    }
}
