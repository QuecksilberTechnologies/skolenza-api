using skolenza.application.DTOs.Operation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.ProjectModule
{
    public class ProjectChildModuleDetailDTO
    {
        public int Id { get; set; }
        public int ModuleId { get; set; }
        public string ModuleName { get; set; }
        public int SubModuleId { get; set; }
        public string SubModuleName { get; set; }
        public byte[] IconImage { get; set; }
        public string ChildModuleName { get; set; }
        public string ChildModuleURL { get; set; }
        public bool IsActive { get; set; }
        public string Remark { get; set; }
        public bool IsOperational { get; set; }  // True if master permission exists
        public List<OperationDTO> Operations { get; set; }  // List of available operations
    }

}
