using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.TicketDTO.TicketType
{
    public class GetTicketTypeByRoleIdRequestDTO
    {
        public long StaffMemberId { get; set; }
        public int ResponsibleRoleId { get; set; }
        public int RoleId { get; set; }  
     
        public long? InstitutionId { get; set; }     
        public bool IsActive { get; set; }
    }
}
