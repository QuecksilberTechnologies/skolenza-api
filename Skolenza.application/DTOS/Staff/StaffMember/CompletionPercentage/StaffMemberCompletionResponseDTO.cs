using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.CompletionPercentage
{
    public class StaffMemberCompletionResponseDTO
    {
        public long StaffMemberId { get; set; }
        public List<CompletionSectionDTO> Sections { get; set; } = new();
    }
}
