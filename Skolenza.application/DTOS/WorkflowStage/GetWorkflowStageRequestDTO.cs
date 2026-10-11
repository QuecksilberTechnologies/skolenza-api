using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.WorkflowStage
{
    public class GetWorkflowStageRequestDTO
    {
        public long StaffMemberId { get; set; }
        public int RoleId { get; set; }
      
        public bool? IsActive { get; set; }
     
    }
}
