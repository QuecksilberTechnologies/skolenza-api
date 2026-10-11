// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines persistence operations for institution-scoped designations.
// ================================================================

using skolenza.application.DTOs.Designation;
using skolenza.application.DTOS.Designation;
using skolenza.application.DTOS.Pagination;
using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IRepositories
{
    /// <summary>
    /// Defines persistence operations for institution-scoped designations.
    /// </summary>
    public interface IDesignationRepository
    {
        /// <summary>
        /// Gets active designation options for a department within a trusted institution.
        /// </summary>
        Task<List<GetDesignationOptionResponseDTO>> GetOptionAsync(
            int departmentId,
            long institutionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists designation seed entities and returns the executive-office designation identifier.
        /// </summary>
        Task<int> AutoCreateDesignationAsync(List<Designation> designations, int departmentId);

        /// <summary>
        /// Persists a prepared designation entity.
        /// </summary>
        Task<Designation?> CreateAsync(Designation entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Soft deletes a designation after enforcing institution ownership.
        /// </summary>
        Task<bool> DeleteDesignationAsync(
            int id,
            long institutionId,
            long staffMemberId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines whether a Designation is still assigned to a non-soft-deleted staffMember.
        /// Inactive staffMembers are intentionally included because they can be reactivated later.
        /// </summary>
        Task<bool> HasNonDeletedStaffMembersAsync(
            int designationId,
            long institutionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets paged designation projections for a trusted institution.
        /// </summary>
        Task<PagedResponseDTO<GetDesignationResponseDTO>> GetAsync(
            GetDesignationRequestDTO request,
            long institutionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a mutable designation entity after enforcing institution ownership.
        /// </summary>
        Task<Designation?> GetByIdForInstitutionAsync(
            int id,
            long institutionId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists a prepared designation entity.
        /// </summary>
        Task<bool> UpdateDesignationAsync(Designation entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines whether a live designation name exists in the same institution and department.
        /// </summary>
        Task<bool> CheckDuplicateValueAsync(
            long institutionId,
            int departmentId,
            string value,
            int? excludeId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a designation projection by identifier.
        /// </summary>
        Task<GetSingleDesignationResponseDTO?> GetByIdAsync(
            GetSingleDesignationRequestDTO dto,
            long institutionId,
            CancellationToken cancellationToken = default);
    }
}
