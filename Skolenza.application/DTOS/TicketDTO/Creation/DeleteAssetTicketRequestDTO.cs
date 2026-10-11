using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using skolenza.domain.Entity;
using MediatR;

namespace skolenza.application.DTOS.TicketDTO.Creation
{
    public class DeleteAssetTicketRequestDTO
    {
        public long StaffMemberId { get; set; }
        public int RoleId { get; set; }
        public long? InstitutionId { get; set; }
        public long? Id { get; set; }   //filter by id if provided
         
             

        

    }
}
