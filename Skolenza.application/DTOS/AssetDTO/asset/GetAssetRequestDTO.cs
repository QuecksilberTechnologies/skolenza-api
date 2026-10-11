// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines client filters for retrieving assets.
// ================================================================

using skolenza.application.DTOS.Pagination;

namespace skolenza.application.DTOS.AssetDTO.asset;

/// <summary>
/// Represents client-supplied filters for retrieving assets.
/// </summary>
public class GetAssetRequestDTO : BaseRequest
{
    public long? AssetId { get; set; }
    public int? AssetTypeId { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? PurchasedDateTime { get; set; }
    public DateTime? InBetweenPurchaseDate { get; set; }
    public DateTime? WarrantyExpiryDateTime { get; set; }
    public string? ModelNumber { get; set; }
    public int? AssetStatusId { get; set; }
    public bool? IsAssigned { get; set; }
    public long? TypeId { get; set; }
    public bool? IsActive { get; set; }
    public bool IsRepairable { get; set; }
}
