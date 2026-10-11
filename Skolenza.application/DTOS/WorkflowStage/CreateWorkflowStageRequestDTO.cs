using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.WorkflowStage
{
    public class CreateWorkflowStageRequestDTO
    {
        public long StaffMemberId { get; set; }
        public int RoleId { get; set; }      

        public string StageName { get; set; } = null!;
        public string? ColorKey { get; set; }        

        public int StageOrder { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; }


    }
}
