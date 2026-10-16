using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Role
{

    public class GetActiveRoleRequestDTO
    {
        public long? InstitutionId { get; set; }
        public long StaffMemberId { get; set; }
        public int RoleId { get; set; }
        public int StaffPositionMasterId { get; set; }
        


    }
}
