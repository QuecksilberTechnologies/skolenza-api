using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Institution
{
    public class GetModuleHierarchyResponseDTO
    {
        public long? InstitutionId { get; set; }
        public List<ModuleNodedto> Modules { get; set; } = new();
    }

    public class ModuleNodedto
    {
        public long Id { get; set; }
        public string? ModuleName { get; set; }
        public long? ParentModuleId { get; set; }
        public bool IsEnabled { get; set; }

        public List<ModuleNodedto> Children { get; set; } = new();
    }

}
