// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines Institution persistence operations, including Host-managed lifecycle transitions.
// ================================================================

using skolenza.application.DTOs.Registration;
using skolenza.application.DTOs.Institution;
using skolenza.application.DTOS.Pagination;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.Interfaces.IRepositories
{
    public interface IInstitutionRepository
    {
        Task<Institution?> GetByIdAsync(long? id, bool isActive);
        Task<bool> CheckInstitutionByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<Institution?> GetByCodeAsync(string institutionCode, CancellationToken cancellationToken = default);
        Task<List<Institution>> GetAllInstitutionBySubscriptionIdAsync(Institution institution, CancellationToken cancellationToken = default);

        Task AddInstitutionAsync(Institution institution, CancellationToken cancellationToken = default);
        Task AddInstitutionProfileAsync(InstitutionProfile institutionProfile, CancellationToken cancellationToken = default);

        Task<Institution?> UpdateInstitutionAsync(Institution institution, CancellationToken cancellationToken = default);
        Task DeleteInstitutionAsync(Institution institution, CancellationToken cancellationToken = default);

        #region Host Management

        /// <summary>
        /// Retrieves a filtered, paged, non-soft-deleted Institution collection for Host-side management.
        /// </summary>
        /// <param name="request">The Host Institution list filters and paging values.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The requested page of Institution entities.</returns>
        Task<PagedResponseDTO<Institution>> GetHostManagedInstitutionsAsync(
            GetAllInstitutionsRequestDTO request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a tracked, non-soft-deleted Institution for a Host-managed operation.
        /// </summary>
        /// <param name="institutionId">The authoritative Institution identifier.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The Institution entity, or <see langword="null"/> when unavailable.</returns>
        Task<Institution?> GetHostManagedInstitutionByIdAsync(long institutionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the safe Host-visible detail projection for one active, non-soft-deleted Institution and its active related records.
        /// </summary>
        /// <param name="institutionId">The authoritative Institution identifier.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The safe detail projection, or <see langword="null"/> when the Institution is unavailable.</returns>
        Task<HostInstitutionDetailResponseDTO?> GetHostManagedInstitutionDetailAsync(long institutionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the Institution's tracked profile for the Host-managed nested update flow.
        /// </summary>
        /// <param name="institutionId">The authoritative Institution identifier.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The tracked Institution profile, or <see langword="null"/> when unavailable.</returns>
        Task<InstitutionProfile?> GetInstitutionProfileForUpdateAsync(long institutionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines whether another non-soft-deleted Institution uses the supplied email address.
        /// </summary>
        /// <param name="institutionEmail">The email address to check.</param>
        /// <param name="excludedInstitutionId">The current Institution excluded from the check.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns><see langword="true"/> when another Institution uses the email.</returns>
        Task<bool> IsInstitutionEmailInUseAsync(string institutionEmail, long excludedInstitutionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Determines whether another non-soft-deleted Institution uses the supplied Institution code.
        /// </summary>
        /// <param name="institutionCode">The Institution code to check.</param>
        /// <param name="excludedInstitutionId">The current Institution excluded from the check.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns><see langword="true"/> when another Institution uses the code.</returns>
        Task<bool> IsInstitutionCodeInUseAsync(string institutionCode, long excludedInstitutionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stages a Host-managed Institution update prepared by the handler.
        /// </summary>
        /// <param name="institution">The tracked Institution entity with validated editable changes.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>A task representing the staging operation.</returns>
        Task StageHostManagedUpdateAsync(Institution institution, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stages a Institution status transition and synchronizes valid, non-soft-deleted credentials.
        /// </summary>
        /// <param name="institution">The tracked Institution whose active state was prepared by the handler.</param>
        /// <param name="hostUserId">The validated Host user used for credential audit fields.</param>
        /// <param name="utcNow">The single UTC audit timestamp captured for the Host request.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>A task representing the staging operation.</returns>
        Task SynchronizeInstitutionStatusAsync(
            Institution institution,
            long hostUserId,
            DateTime utcNow,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Stages Institution soft deletion and deactivates all related login credentials without deleting historical records.
        /// </summary>
        /// <param name="institution">The tracked Institution prepared for soft deletion.</param>
        /// <param name="hostUserId">The validated Host user used for credential audit fields.</param>
        /// <param name="utcNow">The single UTC audit timestamp captured for the Host request.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>A task representing the staging operation.</returns>
        Task SoftDeleteInstitutionAndDeactivateCredentialsAsync(
            Institution institution,
            long hostUserId,
            DateTime utcNow,
            CancellationToken cancellationToken = default);

        #endregion
    }

 }
