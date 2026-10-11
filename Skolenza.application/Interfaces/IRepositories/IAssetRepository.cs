

// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines persistence operations for institution-owned assets.
// ================================================================

using skolenza.application.DTOS.AssetDTO.asset;
using skolenza.application.DTOS.Pagination;
using skolenza.domain.Entity;
namespace skolenza.application.Interfaces.IRepositories
{
    public interface IAssetRepository
    {


        #region asset
        Task<GetAssetResponseDTO> UpdateAsync(Asset asset, string path);
        Task<GetAssetResponseDTO> AddAsync(Asset asset, string path);
    
        Task UpdateQrCodeAsync(long Id, string qrJson);
        // All assets
        Task<List<GetAssetResponseDTO>> GetAllAsync(long institutionId, bool Isactive);
       
        public Task<Asset> GetSingleRecordAsync(long Id, bool? IsActive);  // Ensure this returns 

        Task<List<GetAssetResponseDTO>> GetInsertedAssetAsync(long institutionId, bool Isactive);
        Task<PagedResponseDTO<GetAssetResponseDTO>> GetAssetsByFilterAsync(
            long institutionId,
            GetAssetRequestDTO asset,
            CancellationToken cancellationToken = default);

        Task<Asset?> GetSingleRecordForInstitutionAsync(
            long id,
            long institutionId,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteAssetAsync(
            long id,
            long institutionId,
            long staffMemberId,
            CancellationToken cancellationToken = default);
        
        #endregion
        //Task<List<AssetCategory>> AddAssetCategoryAsync(AddCategoryRequestDTO asset);
 
        

         
    }
}
