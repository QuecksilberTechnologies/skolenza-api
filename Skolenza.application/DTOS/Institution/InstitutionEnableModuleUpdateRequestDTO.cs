using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Institution
{
    public class InstitutionEnableModuleUpdateRequestDTO
    {

        public List<ModuleNodeDTO> RootModules { get; set; } = new List<ModuleNodeDTO>();

    }

    public class ModuleNodeDTO
    {
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public bool IsLeafNode { get; set; }
        public int Level { get; set; }
        public string Breadcrumb { get; set; }
        public List<ModuleNodeDTO> Children { get; set; } = new List<ModuleNodeDTO>();
        public List<OperationDTO> Operations { get; set; } = new List<OperationDTO>();
    }
    public class OperationDTO
    {
        public int OperationId { get; set; }
        public string OperationName { get; set; }
        public bool IsApplied { get; set; }
        public string Breadcrumb { get; set; }
    }
   

}
