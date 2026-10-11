using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.DTOS.TicketDTO.Creation
{
    public class UpdateAssetTicketRequestDTO
    {
        
            public long StaffMemberId { get; set; }
            public int RoleId { get; set; }
            public long? InstitutionId { get; set; }
            public long? Id { get; set; }  // Optional, mainly used for updates

            public int TicketTypeId { get; set; }   // Required - Linked with TicketType table

            public int AssetTypeId { get; set; }    // Required - Linked with AssetType table

            public int? ResponsibleRoleId { get; set; } // Optional - May or may not be assigned

            public bool IsActive { get; set; } = true;  // Default active when added

            // Soft delete info
            public bool? IsSoftDeleted { get; set; } = false;

            // Audit fields
            public long AddedById { get; set; }
            public DateTime AddedDateTime { get; set; } = DateTime.UtcNow;

            public long? UpdatedById { get; set; }
            public DateTime? UpdatedDateTime { get; set; }

            public long? SoftDeletedById { get; set; }
            public DateTime? SoftDeletedTime { get; set; }

        
      
    }
}
