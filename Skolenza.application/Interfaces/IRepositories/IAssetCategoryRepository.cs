// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines persistence operations for institution-owned asset categories.
// ================================================================

using skolenza.application.DTOS.AssetDTO.category;
using skolenza.application.DTOS.Pagination;
using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IRepositories;

/// <summary>
/// Defines persistence operations for asset categories.
/// </summary>
public interface IAssetCategoryRepository
{
    #region Queries

    /// <summary>Retrieves asset categories for a trusted institution and client filters.</summary>
    Task<PagedResponseDTO<GetCategoryResponseDTO>> GetAllAsync(
        long institutionId,
        GetCategoryReqestDTO dto,
        CancellationToken cancellationToken = default);

    /// <summary>Retrieves an active, non-deleted asset category owned by a institution.</summary>
    Task<AssetCategory?> GetByIdForInstitutionAsync(
        long id,
        long institutionId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Create

    /// <summary>Persists a fully prepared asset category entity.</summary>
    Task<AssetCategory?> CreateAsync(AssetCategory entity, CancellationToken cancellationToken = default);

    #endregion

    #region Update

    /// <summary>Persists changes to a institution-owned asset category entity.</summary>
    Task<bool> UpdateAsync(AssetCategory entity, CancellationToken cancellationToken = default);

    #endregion

    #region Delete

    /// <summary>Soft-deletes an asset category owned by a institution.</summary>
    Task<bool> DeleteAsync(
        long id,
        long institutionId,
        long staffMemberId,
        CancellationToken cancellationToken = default);

    #endregion
}
