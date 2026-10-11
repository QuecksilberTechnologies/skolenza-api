// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines persistence operations for institution-owned asset types.
// ================================================================

using skolenza.application.DTOS.AssetDTO.type;
using skolenza.application.DTOS.Pagination;
using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IRepositories;

/// <summary>
/// Defines persistence operations for asset types.
/// </summary>
public interface IAssetTypeRepository
{
    #region Queries

    /// <summary>
    /// Retrieves asset types for the supplied institution and client filters.
    /// </summary>
    /// <param name="institutionId">The trusted institution identifier.</param>
    /// <param name="dto">The client-supplied filters.</param>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    /// <returns>A paged collection of asset types.</returns>
    Task<PagedResponseDTO<GetTypeResponseDTO>> GetAllAsync(
        long institutionId,
        GetTypeRequestDTO dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an active, non-deleted asset type owned by a institution.
    /// </summary>
    /// <param name="id">The asset type identifier.</param>
    /// <param name="institutionId">The trusted institution identifier.</param>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    /// <returns>The asset type when it exists for the institution; otherwise, <see langword="null"/>.</returns>
    Task<AssetType?> GetByIdForInstitutionAsync(
        long id,
        long institutionId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Create

    /// <summary>
    /// Persists a new asset type entity.
    /// </summary>
    /// <param name="entity">The fully prepared asset type entity.</param>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    /// <returns>The persisted asset type.</returns>
    Task<AssetType?> CreateAsync(AssetType entity, CancellationToken cancellationToken = default);

    #endregion

    #region Update

    /// <summary>
    /// Persists changes to an existing asset type entity.
    /// </summary>
    /// <param name="entity">The institution-owned asset type entity to update.</param>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    /// <returns><see langword="true"/> when changes were saved; otherwise, <see langword="false"/>.</returns>
    Task<bool> UpdateAsync(AssetType entity, CancellationToken cancellationToken = default);

    #endregion

    #region Delete

    /// <summary>
    /// Soft-deletes an asset type owned by a institution.
    /// </summary>
    /// <param name="id">The asset type identifier.</param>
    /// <param name="institutionId">The trusted institution identifier.</param>
    /// <param name="staffMemberId">The authenticated staffMember performing the deletion.</param>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    /// <returns><see langword="true"/> when the type was deleted; otherwise, <see langword="false"/>.</returns>
    Task<bool> DeleteAsync(
        long id,
        long institutionId,
        long staffMemberId,
        CancellationToken cancellationToken = default);

    #endregion
}
