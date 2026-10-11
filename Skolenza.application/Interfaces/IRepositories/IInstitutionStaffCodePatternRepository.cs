using skolenza.application.DTOS.Institution;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Interfaces.IRepositories
{
    public interface IInstitutionStaffCodePatternRepository
    {
        /// <summary>
        /// Gets the active staffMember code pattern for a specific institution.
        /// </summary>
        Task<GetStaffMemberCodePatternResponseDTO?> GetInstitutionStaffMemberCodePatternAsync(long institutionId ,bool IsActive);

        /// <summary>
        /// Gets the active staffMember code pattern as a tracked entity for a Host-managed Institution update.
        /// </summary>
        Task<StaffMemberCodePattern?> GetActivePatternForUpdateAsync(long institutionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new staffMember code pattern for a institution.
        /// </summary>
        Task<bool> CreatePatternAsync(StaffMemberCodePattern entity);

        /// <summary>
        /// Updates an existing staffMember code pattern.
        /// </summary>
        Task<bool> UpdatePatternAsync(StaffMemberCodePattern entity);

        /// <summary>
        /// Deactivates all existing patterns for a institution.
        /// (Ensures only one active pattern exists)
        /// </summary>
        Task<bool> DeactivateExistingPatternsAsync(long institutionId);

        /// <summary>
        /// Increments the running number and returns the updated sequence atomically.
        /// (This avoids race conditions when generating staffMember codes)
        /// </summary>
        Task<int> IncrementAndGetNextRunningNumberAsync(long institutionId);

        /// <summary>
        /// Generates staffMember code from pattern (prefix/year/month/etc.)
        /// </summary>
        Task<string> GenerateStaffMemberCodeAsync(long institutionId, int? departmentId = null, DateTime? joiningDate = null);

        /// <summary>Previews or confirms a institution pattern and staffMember-code change atomically.</summary>
        Task<SaveStaffMemberCodePatternResponseDTO> SaveWithStaffMemberCodesAsync(
            long institutionId, long actorId, StaffMemberCodePattern proposed,
            bool create, bool confirm, string? previewHash, CancellationToken cancellationToken);
    }

}
