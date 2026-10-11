using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Leave
{
    public class DeleteLeaveRequestDTO
    {
        public int Id { get; set; }
        public long RoleId { get; set; }
        public long StaffMemberId { get; set; }

    }
}
