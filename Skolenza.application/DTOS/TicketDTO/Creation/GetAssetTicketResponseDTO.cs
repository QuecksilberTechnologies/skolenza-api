using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.DTOS.TicketDTO.Creation
{
    public class GetAssetTicketResponseDTO : BaseRequest
    {
      
        
        public int TicketTypeId { get; set; }   // Required - Linked with TicketType table

        public int AssetTypeId { get; set; }    // Required - Linked with AssetType table
        public string? AssetName { get; set; }

        public int? ResponsibleRoleId { get; set; } // - May or may not be assigned
        public string? ResponsibleRoleName { get; set; }
        public bool IsActive { get; set; } = true;  // Default active when added
        

        // Audit fields
        public long AddedById { get; set; }
        public DateTime AddedDateTime { get; set; } 

        public long? UpdatedById { get; set; }
        public DateTime? UpdatedDateTime { get; set; }

        public long? SoftDeletedById { get; set; }
        public DateTime? SoftDeletedTime { get; set; }

    }
}
