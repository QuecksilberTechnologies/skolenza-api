using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.RoleModulePermission
{
    [Keyless]
    public class RoleModuleOperationResponseDTO
    {
        public int RoleId { get; set; }
        public long InstitutionId { get; set; }
        public int ModuleId { get; set; }  
        public string? ModuleName { get; set; }
        public string? URLPath { get; set; } // ✅ Add this
        public string? DisplayName { get; set; } // ✅ From Module
        public string? ImageIconWeb { get; set; } // ✅ From Module
        public string? ImageIconMobile { get; set; } // ✅ From Module

        public string? SubModuleName { get; set; }
        /// <summary>Root modules have no parent; preserve SQL NULL rather than failing materialization.</summary>
        public int? ParentModuleId { get; set; }

        public string? MainModuleName { get; set; }
        public int? MainModuleId { get; set; }

        public int OperationId { get; set; }
        public string? OperationName { get; set; }

   

        public int? DataViewStructureId { get; set; }
        public string? DisplayOn { get; set; }
    }

    public class MainModuleDto
    {
        public int? MainModuleId { get; set; }
        public string? MainModuleName { get; set; }
        public List<SubModuleDto>? SubModules { get; set; }
    }

    public class SubModuleDto
    {
        public int? SubModuleId { get; set; }
        public string? SubModuleName { get; set; }
        public List<ModuleDto>? Modules { get; set; }
    }

    public class ModuleDto
    {
        public int? ModuleId { get; set; }
        public string? ModuleName { get; set; }

        public string? DisplayName { get; set; }  // ✅ New
        public string? ImageIconWeb { get; set; } // ✅ New
        public string? ImageIconMobile { get; set; } // ✅ New

        public string? Path { get; set; } // ✅ Add this
        public string? SubModuleURL { get; set; } // ✅ Add this
        public string? IconURL { get; set; }

        public int? DataViewStructureId { get; set; }
        public string? DisplayOn { get; set; }

        public List<OperationDto>? Operations { get; set; }
    }

    public class OperationDto
    {
        public int OperationId { get; set; }
        public string? OperationName { get; set; }
    }

    public class RoleModuleOperationResponseDTONew
    {
        public int RoleId { get; set; }
        public long InstitutionId { get; set; }
        public int ModuleId { get; set; }
        public string? ModuleName { get; set; }
        public string? URLPath { get; set; }
        public string? DisplayName { get; set; }
        public string? ImageIconWeb { get; set; }
        public string? ImageIconMobile { get; set; }
        public int ParentModuleId { get; set; }
        public string? SubModuleName { get; set; }
        public int MainModuleId { get; set; }
        public string? MainModuleName { get; set; }
        public int OperationId { get; set; }
        public string? OperationName { get; set; }
        public int DataViewStructureId { get; set; }
        public string? DisplayOn { get; set; }
    }



}
