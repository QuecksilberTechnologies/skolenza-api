// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines persistence operations for institution-owned asset statuses.
// ================================================================

using skolenza.application.DTOS.AssetDTO.status;
using skolenza.application.DTOS.Pagination;
using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IRepositories;

/// <summary>
/// Defines persistence operations for asset statuses.
/// </summary>
public interface IAssetStatusRepository
{
    #region Queries

    /// <summary>
    /// Retrieves asset statuses for the supplied institution and client filters.
    /// </summary>
    Task<PagedResponseDTO<GetStatusResponseDTO>> GetAllAsync(
        long institutionId,
        GetStatusRequestDTO dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a non-deleted asset status owned by a institution.
    /// </summary>
    Task<AssetStatus?> GetByIdForInstitutionAsync(
        int id,
        long institutionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a non-deleted asset status by its identifier for existing cross-module validation.
    /// </summary>
    Task<AssetStatus?> GetByIdAsync(int? id);

    #endregion

    #region Create

    /// <summary>
    /// Persists a new, fully prepared asset status entity.
    /// </summary>
    Task<AssetStatus?> CreateAsync(AssetStatus entity, CancellationToken cancellationToken = default);

    #endregion

    #region Update

    /// <summary>
    /// Persists changes to a institution-owned asset status entity.
    /// </summary>
    Task<bool> UpdateAsync(AssetStatus entity, CancellationToken cancellationToken = default);

    #endregion

    #region Delete

    /// <summary>
    /// Soft-deletes an asset status owned by a institution.
    /// </summary>
    Task<bool> DeleteAsync(
        int id,
        long institutionId,
        long staffMemberId,
        CancellationToken cancellationToken = default);

    #endregion
}
