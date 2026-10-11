using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.BaseDTO
{
    public class PermissionRequestDTO
    {        
            public int ModuleId { get; set; }
            public int OperationId { get; set; }

        
    }
}
