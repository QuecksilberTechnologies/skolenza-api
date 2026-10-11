using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.application.DTOS.AssetDTO.asset
{
    public class GetNewAssetRequestsResponseDTO
    {
        public string AssetId { get; set; }           // encoded id
        public string AssetName { get; set; }         // Dell Latitude
        public string Product { get; set; }           // Software License
        public string ProductType { get; set; }       // Adobe CC

        public string TicketId { get; set; }

        public string RequestedBy { get; set; }
        public string RecommendedBy { get; set; }

        public string ManagerStatus { get; set; }     // Pending / Approved / Rejected
        public string ManagerStatusCode { get; set; } // for UI icons

        public string CommentsThreadId { get; set; }  // Thread open karne ke liye

        public bool IsApproved { get; set; }
        public bool IsRecommended { get; set; }
        public bool IsRepairable { get; set; } =false;


        public DateTime RequestedDate { get; set; }
    }
}
