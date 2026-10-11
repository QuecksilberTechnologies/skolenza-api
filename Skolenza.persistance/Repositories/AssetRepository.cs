// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Persists and retrieves institution-owned assets and their images.
// ================================================================

using AutoMapper;
using skolenza.application.Constants;
using skolenza.application.DTOS.AssetDTO.asset;
using skolenza.application.DTOS.Pagination;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Interfaces.IQRService;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data; 
namespace skolenza.persistance.Repositories
{
    
 

    public class AssetRepository : IAssetRepository
    {
        private SkolenzaDbContext _context;

        private ILogger<AssetRepository> _logger;
        private readonly IQRService _qrService;
        private readonly IFileStorageService _fileStorageService;
       
        private readonly IMapper _mapper;


        public AssetRepository(SkolenzaDbContext context, ILogger<AssetRepository> logger, IQRService qrService, IFileStorageService fileStorageService,
            IMapper mapper)
        {
            _context = context;
            _logger = logger;
            _qrService = qrService;
            _fileStorageService = fileStorageService;
            _mapper = mapper;
            
        }

        public async Task<GetAssetResponseDTO> UpdateAsync(Asset asset, string? path)
        {
            try
            {
                // ===============================
                // 1️⃣ UPDATE ASSET
                // ===============================
                _context.Assets.Update(asset);
                await _context.SaveChangesAsync();

                // ===============================
                // 2️⃣ HANDLE IMAGE (IMPORTANT)
                // ===============================
                if (!string.IsNullOrWhiteSpace(path))
                {
                    // 🔹 Existing primary image deactivate
                    var existingImages = await _context.AssetImages
                        .Where(x => x.AssetId == asset.Id && x.IsActive && (x.IsSoftDeleted == false || x.IsSoftDeleted == null))
                        .ToListAsync();

                    foreach (var img in existingImages)
                    {
                        img.IsActive = false;
                        img.IsPrimary = false;
                        img.UpdatedDateTime = DateTime.UtcNow;
                    }

                    // 🔹 Add new image
                    var newImage = new AssetImage
                    {
                        AssetId = asset.Id,
                        AssetImagePath = path,
                        IsPrimary = true,
                        IsActive = true,
                        IsSoftDeleted = false,
                        AddedDateTime = DateTime.UtcNow,
                        AddedById = asset.UpdatedById
                    };

                    await _context.AssetImages.AddAsync(newImage);
                    await _context.SaveChangesAsync();
                }

                // ===============================
                // 3️⃣ FETCH UPDATED DATA (WITH JOIN)
                // ===============================
                var result = await _context.Assets
                    .AsNoTracking()
                    .Where(a => a.Id == asset.Id)
                    .Select(a => new GetAssetResponseDTO
                    {
                        AssetId = a.Id,
                        AssetName = a.AssetName,

                        AssetTypeId = a.AssetTypeId,
                        TypeName = a.AssetType.TypeName,

                        CategoryId = a.AssetType.AssetCategoryId,
                        CategoryName = a.AssetType.AssetCategory != null
                            ? a.AssetType.AssetCategory.CategoryName
                            : null,

                        AssetStatusId = a.AssetStatusId,
                        StatusName = a.AssetStatus.StatusName,
                        ColorKey = a.AssetStatus.ColorKey,

                        SerialNumber = a.SerialNumber,
                        ModelNo = a.ModelNo,

                        Company = a.Company,
                        Color = a.Color,
                        Price = a.Price,

                        IsActive = (bool)a.IsActive,
                        IsAssigned = (bool)a.IsAssigned,

                        PurchaseDate = a.PurchaseDate,
                        WarrantyExpiryDate = a.WarrantyExpiryDate,

                        AssetImagePath = a.AssetImage
                            .Where(i => i.IsPrimary && i.IsActive && (i.IsSoftDeleted !=true))
                            .Select(i => i.AssetImagePath)
                            .FirstOrDefault()
                    })
                    .FirstOrDefaultAsync();

                return result!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ AssetRepository UpdateAsync failed");
                throw;
            }
        }
        #region 🔹 Common Helpers



        private async Task<AssetImage?> GetAssetImageAsync(long assetId)
        {
            return await _context.AssetImages
                .FirstOrDefaultAsync(i =>
                    i.AssetId == assetId &&
                    i.IsSoftDeleted != true);
        }

        private static GetAssetResponseDTO MapToAssetDTO(
            Asset asset,
            AssetImage? image = null)
        {
            return new GetAssetResponseDTO
            {
                AssetId = asset.Id,
                AssetName = asset.AssetName,
                AssetTypeId = asset.AssetTypeId,
                Company = asset.Company,
                ModelNo = asset.ModelNo,
                Size = asset.Size,
                Weight = asset.Weight,
                Color = asset.Color,
                IsRepairable = asset.IsRepairable,
                Price = asset.Price,
                SerialNumber = asset.SerialNumber,
                Barcode = asset.Barcode,
                QRCode = asset.Qrcode,
               
                PurchaseDate = asset.PurchaseDate,
                WarrantyExpiryDate = asset.WarrantyExpiryDate,
                AssetStatusId = asset.AssetStatusId,
                IsAssigned = asset.IsAssigned ?? false,
                IsActive = asset.IsActive ?? false,
                HasMultipleUser = (bool)(asset.AssetType != null && asset.AssetType.AssetCategory != null ? asset.AssetType.AssetCategory.HasMultipleUser : false),
                AssetImageId = image?.Id,
                AssetImagePath = image?.AssetImagePath,
                AssetImageType = image?.AssetImageType
            };
        }
        public async Task UpdateQrCodeAsync(long assetId, string qrCode)
        {
            var asset = await _context.Assets
                .FirstOrDefaultAsync(a =>
                    a.Id == assetId &&
                    a.IsSoftDeleted != true);

            if (asset == null)
                throw new Exception("Asset not found for QR update.");

            asset.Qrcode = qrCode;
            await _context.SaveChangesAsync();
        }


        public async Task<GetAssetResponseDTO> AddAsync(Asset asset, string path)
        {
            try
            {
                _logger.LogInformation("Adding new asset for InstitutionId={InstitutionId}", asset.InstitutionId);

                // 🔥 Attach child entity using navigation property
                if (!string.IsNullOrEmpty(path))
                {
                    asset.AssetImage.Add(new AssetImage
                    {
                        InstitutionId = asset.InstitutionId,
                        AssetImageType = ConstantValues.Web,
                        AssetImagePath = path,
                        IsPrimary = true,
                        IsActive = true,
                        IsSoftDeleted = false,
                        AddedById = asset.AddedById,
                        AddedDateTime = DateTime.UtcNow
                    });
                }

                // 🔥 Single insert (Parent + Child)
                await _context.Assets.AddAsync(asset);
                await _context.SaveChangesAsync();

                // 🔥 Get inserted image (from navigation)
                var assetImage = asset.AssetImage.FirstOrDefault();

                return MapToAssetDTO(asset, assetImage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ AddAsync failed");
                throw;
            }
        }

        public async Task<List<GetAssetResponseDTO>> GetAllAsync(long institutionId, bool isActive)
        {
            try
            {
                _logger.LogInformation("Fetching assets for InstitutionId={InstitutionId}", institutionId);

                var assets = await _context.Assets
                    .Where(a => a.InstitutionId == institutionId &&
                                a.IsActive == isActive &&
                                a.IsSoftDeleted != true)
                    .OrderByDescending(a => a.Id)
                    .Take(10)
                    .ToListAsync();

                var response = new List<GetAssetResponseDTO>();

                foreach (var a in assets)
                {
                    var img = await GetAssetImageAsync(a.Id);
                    response.Add(MapToAssetDTO(a, img));
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ GetAllAsync failed");
                throw;
            }
        }

        public async Task<Asset?> GetSingleRecordAsync(long id, bool? isActive)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching single asset | Id={Id} | IsActive={IsActive}",
                    id, isActive);

                IQueryable<Asset> query =
                    _context.Assets.Where(a =>
                        a.Id == id &&  a.IsSoftDeleted != true);

                if (isActive.HasValue)
                    query = query.Where(a => a.IsActive == isActive.Value);

                return await query.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ GetSingleRecordAsync failed for AssetId={Id}", id);
                throw;
            }
        }



        public async Task<List<GetAssetResponseDTO>> GetInsertedAssetAsync(long institutionId, bool isActive)
        {
            try
            {
                var assets = await _context.Assets
                    .Where(a =>
                        a.InstitutionId == institutionId &&
                        a.IsActive == isActive &&
                        a.IsSoftDeleted != true)
                    .OrderByDescending(a => a.Id)
                    .Take(10)
                    .ToListAsync();

                var result = new List<GetAssetResponseDTO>();

                foreach (var a in assets)
                {
                    var img = await GetAssetImageAsync(a.Id);
                    result.Add(MapToAssetDTO(a, img));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ GetInsertedAssetAsync failed");
                throw;
            }
        }
        public async Task<PagedResponseDTO<GetAssetResponseDTO>> GetAssetsByFilterAsync(
            long institutionId,
            GetAssetRequestDTO asset,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // ✅ Null-safe fallback
                if (asset == null)
                {
                    _logger.LogWarning("⚠️ GetAssetsByFilterAsync called with null filter");

                    return new PagedResponseDTO<GetAssetResponseDTO>(
                        new List<GetAssetResponseDTO>(),
                        0,
                        asset?.PageNumber ?? 1,
                        asset?.PageSize ?? 10
                    );
                }

                IQueryable<Asset> query = _context.Assets
                    .AsNoTracking()
                    .Where(a => a.InstitutionId == institutionId &&
                                (a.IsSoftDeleted == false || a.IsSoftDeleted == null));

                // 🔹 Filters
                if (asset.AssetId > 0)
                    query = query.Where(a => a.Id == asset.AssetId);

                if (asset.AssetTypeId > 0)
                    query = query.Where(a => a.AssetTypeId == asset.AssetTypeId);

                if (!string.IsNullOrWhiteSpace(asset.SerialNumber))
                    query = query.Where(a => a.SerialNumber != null &&
                                             a.SerialNumber.Contains(asset.SerialNumber));

                if (!string.IsNullOrWhiteSpace(asset.ModelNumber))
                    query = query.Where(a => a.ModelNo != null &&
                                             a.ModelNo.Contains(asset.ModelNumber));

                if (asset.IsActive.HasValue)
                    query = query.Where(a => a.IsActive == asset.IsActive);

                if (asset.IsAssigned.HasValue)
                    query = query.Where(a => a.IsAssigned == asset.IsAssigned);

                // 🔥 TOTAL COUNT (before pagination)
                var totalCount = await query.CountAsync(cancellationToken);

                // 🔥 PAGINATION
                var pageNumber = asset.PageNumber <= 0 ? 1 : asset.PageNumber;
                var pageSize = asset.PageSize <= 0 ? 10 : asset.PageSize;

                var data = await query
                    .OrderByDescending(a => a.Id)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new GetAssetResponseDTO
                    {
                        AssetId = a.Id,
                        AssetName = a.AssetName,

                        AssetTypeId = a.AssetTypeId,
                        TypeName = a.AssetType != null ? a.AssetType.TypeName : null,

                        CategoryId = a.AssetType != null ? a.AssetType.AssetCategoryId : null,
                        CategoryName = a.AssetType != null && a.AssetType.AssetCategory != null
                            ? a.AssetType.AssetCategory.CategoryName
                            : null,

                        HasMultipleUser = (bool)(a.AssetType != null && a.AssetType.AssetCategory != null
                            ? a.AssetType.AssetCategory.HasMultipleUser
                            : false),

                        SerialNumber = a.SerialNumber,
                        ModelNo = a.ModelNo,

                        AssetStatusId = a.AssetStatusId,
                        StatusName = a.AssetStatus != null ? a.AssetStatus.StatusName : null,
                        ColorKey = a.AssetStatus != null ? a.AssetStatus.ColorKey : null,
                        IsRepairable = a.IsRepairable,

                        Size = a.Size,
                        Weight = a.Weight,
                        Company = a.Company,
                        Color = a.Color,
                        Price = a.Price,

                        IsActive = (bool)a.IsActive,
                        IsAssigned = (bool)a.IsAssigned,

                        PurchaseDate = a.PurchaseDate,
                        WarrantyExpiryDate = a.WarrantyExpiryDate,

                        AssetImagePath = a.AssetImage
                            .Where(i => i.IsPrimary == true &&
                                        (i.IsSoftDeleted != true) &&
                                        (i.IsActive == true))
                            .Select(i => i.AssetImagePath)
                            .FirstOrDefault()
                    })
                    .ToListAsync(cancellationToken);

                // ✅ FINAL RESPONSE
                return new PagedResponseDTO<GetAssetResponseDTO>(
                    data,
                    totalCount,
                    pageNumber,
                    pageSize
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ GetAssetsByFilterAsync failed");
                throw;
            }
        }

        public async Task<Asset?> GetSingleRecordForInstitutionAsync(
            long id,
            long institutionId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Assets.FirstOrDefaultAsync(
                asset => asset.Id == id
                    && asset.InstitutionId == institutionId
                    && asset.IsSoftDeleted != true,
                cancellationToken);
        }

        public async Task<bool> DeleteAssetAsync(
            long id,
            long institutionId,
            long staffMemberId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 1️⃣ Fetch Existing Asset Entity
                var existingAsset = await _context.Assets
                    .FirstOrDefaultAsync(a =>
                        a.Id == id &&
                        a.InstitutionId == institutionId &&
                        a.IsSoftDeleted != true);

                if (existingAsset == null)
                {
                    _logger.LogWarning("⚠ Asset not found for Id={Id}", id);
                    return false;
                }

                // 2️⃣ Soft Delete Asset (ENTITY)
                existingAsset.IsSoftDeleted = true;
                existingAsset.IsActive = false;
                existingAsset.SoftDeletedById = staffMemberId;
                existingAsset.DeletedDateTime = DateTime.UtcNow;

                // 3️⃣ Fetch ALL Asset Images and Soft Delete
                var assetImages = await _context.AssetImages
                    .Where(i => i.AssetId == id && i.IsSoftDeleted != true)
                    .ToListAsync(cancellationToken);

                foreach (var img in assetImages)
                {
                    img.IsSoftDeleted = true;
                    img.IsActive = false;
                    img.SoftDeletedById = staffMemberId;
                    img.DeletedDateTime = DateTime.UtcNow;
                }

                // 4️⃣ Save All at Once
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("✅ Asset & Image soft-delete completed | AssetId={Id}", id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ DeleteAssetAsync failed");
                return false;
            }
        }


        #endregion





    }
}



