using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.TicketDTO.Header
{
    public class GetHeaderRequestDTO 
    {
        public long InstitutionId { get; set; }
        public int Id { get; set; }
        public long StaffMemberId { get; set; }
        public int RoleId { get; set; } // RoleId of the requester
      
        public string? HeaderName { get; set; } // Filter by header name (optional)
        public string? Description { get; set; } // Filter by description (optional)
        public bool? IsActive { get; set; }    // Filter by active status (optional)
        public int? TicketClassificationId { get; set; }// Foreign key to TicketClassification (optional)
        public bool? IsAssetRelated { get; set; } // Indicates if the header is related to an asset (optional)




    }
}
