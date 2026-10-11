using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Module.ParentModule
{
    public class ModuleResponseDTO
    {

        public int ModuleId { get; set; }
        public int? ParentModuleId { get; set; }
        public string? ModuleName { get; set; }
         public List<ModuleOperationResponseDTO> ModuleOperations { get; set; } = new()  ;
           


    }

    public class ModuleOperationResponseDTO
    {

        public int OperationId { get; set; }
  
        public string? OperationName { get; set; }


    }

}
