using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.AssetDTO.common
{
    public class GetAssetTypeByCategoryResponseDTO : BaseRequest
    {
         
        public long AssetId { get; set; }
        
        public long? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? AssetName { get; set; }
        public int? AssetTypeId { get; set; }
        public string? TypeName { get; set; }
        public string? Company { get; set; }
        public string? ModelNo { get; set; }
        public string? Size { get; set; }
        public string? Weight { get; set; }
        public string? Color { get; set; }
        public bool? IsRepairable { get; set; }
        public decimal? Price { get; set; }
        public string? SerialNumber { get; set; }
        public string? Barcode { get; set; }
        public string? Qrcode { get; set; }
        public string? ModelNumber { get; set; }       
        public string? AssetImagePath { get; set; }
        public string? QRCodeImagePath { get; set; }
        public int? QRImageType { get; set; } 
        public int? AssetImageType { get; set; }
             
        public DateTime? PurchaseDate { get; set; }
        public DateTime? WarrantyExpiryDate { get; set; }
        public int? AssetStatusId { get; set; }
        public bool? IsAssigned { get; set; }
        public bool IsActive { get; set; }
    }
}
