using skolenza.application.DTOs.Module;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Institution
{
    public class PlanModuleMappingResponseDTO
    {
        public int SubscriptionPlanId { get; set; }

        public List<ModuleWithOperationsDTO> Modules { get; set; } = new();
    }
    public class ModuleWithOperationsDTO
    {
        public int? MainModuleId { get; set; }
        public string? MainModuleName { get; set; }
        public string? DisplayName { get; set; }
        public int ModuleId { get; set; }          
        public int? ParentModuleId { get; set; }
        public string? ModuleName { get; set; }
        public List<OperationResponseDTO> Operations { get; set; } = new();
    }

    public class OperationResponseDTO
    {
        public int OperationId { get; set; }
        
        public string? PageUrl { get; set; }
        public string ?IconUrl { get; set; }
    }

}
