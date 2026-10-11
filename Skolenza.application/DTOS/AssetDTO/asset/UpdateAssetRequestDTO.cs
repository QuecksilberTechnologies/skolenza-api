// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines client input for updating an asset.
// ================================================================

using skolenza.application.DTOs.BaseDTO;
using Microsoft.AspNetCore.Http;

namespace skolenza.application.DTOS.AssetDTO.asset;

/// <summary>
/// Represents the client-editable values for an asset update.
/// </summary>
public class UpdateAssetRequestDTO : PermissionRequestDTO
{
    public long Id { get; set; }
    public string? AssetName { get; set; }
    public int AssetTypeId { get; set; }
    public string? Company { get; set; }
    public string? ModelNo { get; set; }
    public string? Size { get; set; }
    public string? Weight { get; set; }
    public string? Color { get; set; }
    public bool IsRepairable { get; set; }
    public decimal Price { get; set; }
    public string? SerialNumber { get; set; }
    public string? Barcode { get; set; }

    private DateTime? _purchaseDate;

    public DateTime? PurchaseDate
    {
        get => _purchaseDate;
        set => _purchaseDate = value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
    }

    private DateTime? _warrantyExpiryDate;

    public DateTime? WarrantyExpiryDate
    {
        get => _warrantyExpiryDate;
        set => _warrantyExpiryDate = value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
    }

    public int AssetImageType { get; set; } = 1;
    public string? AssetImagePath { get; set; }
    public int AssetStatusId { get; set; }
    public bool IsAssigned { get; set; }
    public bool IsActive { get; set; }
    public IFormFile? AssetImageFile { get; set; }
}
