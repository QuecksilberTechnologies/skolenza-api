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
    public class GetAssetTicketFilterRequestDTO : BaseRequest
    {
       
        public int RoleId { get; set; }
 
    
        public int TicketTypeId { get; set; }   // Required - Linked with TicketType table

        public int AssetTypeId { get; set; }   // Required - Linked with AssetType table

        public int? ResponsibleRoleId { get; set; }  // Optional - May or may not be assigned

        public bool? IsActive { get; set; } = true; // Default active when added
             

        

    }
}
