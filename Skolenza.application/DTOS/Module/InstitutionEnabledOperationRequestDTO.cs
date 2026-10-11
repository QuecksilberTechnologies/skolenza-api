using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Module
{
    public class InstitutionEnabledOperationRequestDTO
    {
       
          
            public long? InstitutionId { get; set; }

            public int ModuleId { get; set; }

            public int OperationId { get; set; }

            public bool IsOperationUsed { get; set; }
            public bool IsEnabled { get; set; }

            public long? AddedById { get; set; }

            public DateTime? AddedDateTime { get; set; }

            public long? UpdatedById { get; set; }

            public DateTime? UpdatedDateTime { get; set; }

    
       

    }
}
