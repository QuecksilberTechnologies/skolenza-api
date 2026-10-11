using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Module.NewFolder
{
    public class MainModuleResponseDTO
    {
        public int? Id { get; set; }

        public string? ModuleCode { get; set; }

        public string? ModuleName { get; set; }

        public string? DisplayName { get; set; }

        public string? Path { get; set; }

        public string? SubModuleURL { get; set; }

        public int? ParentModuleId { get; set; }

        
        public  bool IsLeafNode { get; set; }

        public bool? IsModuleDisplayInUI { get; set; }

        public bool? IsCommonMenu { get; set; }

        public bool? IsActive { get; set; }

        public string? ImageIconWeb { get; set; }

        public string? ImageIconMobile { get; set; }

        public int? ItemPriority { get; set; }

        public string? Remark { get; set; }

        public long? AddedById { get; set; }

        public DateTime? AddedDateTime { get; set; }

        public long? UpdatedById { get; set; }

        public DateTime? UpdatedDateTime { get; set; }
    }

}
