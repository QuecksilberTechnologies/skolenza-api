
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.WorkflowStage
{
    public class UpdateWorkflowStageRequestDTO
    {
        public long Id { get; set; }             // Existing TicketType Id
        public long StaffMemberId { get; set; }     // Id of the staffMember making the update
        public int RoleId { get; set; }          // Role of the staffMember
    
        public string? ColorKey { get; set; }

        public string? StageName { get; set; }

        public int? StageOrder { get; set; }

        public string? Description { get; set; }

        public bool? IsActive { get; set; }

        



    }
}
