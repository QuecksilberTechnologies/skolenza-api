using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.AssetDTO.asset
{
    public class GetAssetResponseDTO
    {
        
        public long AssetId { get; set; }
        public long ? AssetImageId { get; set; }   
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
        public bool IsRepairable { get; set; }
        public bool IsActive { get; set; }
        public bool HasMultipleUser { get; set; }
        public bool IsAssigned { get; set; }
        public decimal? Price { get; set; }
        public string? SerialNumber { get; set; }
        public string? Barcode { get; set; }
        public string? QRCode { get; set; }
       // public string? ModelNumber { get; set; }       
        public string? AssetImagePath { get; set; } 
        public int? AssetImageType { get; set; }             
        public DateTime? PurchaseDate { get; set; }
        public DateTime? WarrantyExpiryDate { get; set; }
        public int? AssetStatusId { get; set; }
        public string? ColorKey { get; set; }
        public string? StatusName { get; set; }

      
      //   public bool? IsAssigned { get; set; }
       // public bool IsActive { get; set; }
    }
}
