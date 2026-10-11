using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Leave
{
    public class GetLeaveTypeRequestDTO: BaseRequest
    {

        public long StaffMemberId { get; set; }
        public long InstitutionId { get; set; }
        public int RoleId { get; set; }
        public bool IsActive { get; set; }     
        public int LeaveTypeId { get; set; }



    }
}
