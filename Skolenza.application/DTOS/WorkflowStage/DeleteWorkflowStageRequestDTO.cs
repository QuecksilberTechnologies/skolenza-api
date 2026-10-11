using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.WorkflowStage
{
    public class DeleteWorkflowStageRequestDTO
    {
        public long Id { get; set; }
        public long StaffMemberId { get; set; }
        public long RoleId { get; set; }
       

    }
}
