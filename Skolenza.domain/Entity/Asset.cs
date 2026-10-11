using System;
using System.Collections.Generic;

namespace skolenza.domain.Entity;

public partial class Asset
{
    public long Id { get; set; }

    public long InstitutionId { get; set; }

    public string AssetName { get; set; } = null!;

    public int AssetTypeId { get; set; }

    public string Company { get; set; } = null!;

    public string? Color { get; set; }

    public bool IsRepairable { get; set; }

    public decimal? Price { get; set; }

    public string? SerialNumber { get; set; }

    public string? Barcode { get; set; }

    public DateTime? PurchaseDate { get; set; }

    public DateTime? WarrantyExpiryDate { get; set; }

    public string? Size { get; set; }

    public string? ModelNo { get; set; }

    public string? Weight { get; set; }

    public int AssetStatusId { get; set; }

    public bool? IsAssigned { get; set; }

    public bool? IsActive { get; set; }

    public bool IsSoftDeleted { get; set; }

    public long? SoftDeletedById { get; set; }

    public long AddedById { get; set; }

    public DateTime? AddedDateTime { get; set; }

    public long? UpdatedById { get; set; }

    public DateTime? UpdatedDateTime { get; set; }

    public DateTime? DeletedDateTime { get; set; }

    public string? Qrcode { get; set; }

    public virtual ICollection<AssetAssignment> AssetAssignment { get; set; } = new List<AssetAssignment>();

    public virtual ICollection<AssetImage> AssetImage { get; set; } = new List<AssetImage>();

    public virtual AssetStatus AssetStatus { get; set; } = null!;

    public virtual AssetType AssetType { get; set; } = null!;
}
