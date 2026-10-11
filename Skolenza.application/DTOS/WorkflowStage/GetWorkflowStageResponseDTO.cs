
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.WorkflowStage
{
    public class GetWorkflowStageResponseDTO
    {
    public int Id { get; set; }

      
        public string? StageName { get; set; }
        public string? ColorKey { get; set; }

        public int? StageOrder { get; set; }

        public string? Description { get; set; }

        public bool? IsActive { get; set; }
        public bool? IsSoftDeleted { get; set; }

        public long? AddedById { get; set; }
    
        public DateTime? AddedDateTime { get; set; }

        public long? UpdatedById { get; set; }

        public DateTime? UpdatedDateTime { get; set; }
        public long? SoftDeletedById { get; set; }
        public DateTime? DeletedDateTime { get; set; }


    }
}
