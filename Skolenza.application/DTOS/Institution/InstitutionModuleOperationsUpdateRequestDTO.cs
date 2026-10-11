using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Institution
{
    public class InstitutionModuleOperationsUpdateRequestDTO
    {
        public long? InstitutionId { get; set; }
        public List<ModuleToggleDTO> Modules { get; set; } = new();
    }

    public class ModuleToggleDTO
    {
        public int ModuleId { get; set; }
        public bool IsEnabled { get; set; }

        public List<OperationToggleDTO> Operations { get; set; } = new();
    }
    public class OperationToggleDTO
    {
        public int OperationId { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsOperationUsed { get; set; }
    }


}
