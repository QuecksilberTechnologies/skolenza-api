using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Module.ManualModule
{
    public class GetModuleChildInversResponseDTO
    {
        public int Id { get; set; }
        public string ModuleName { get; set; }
        public string? SubModuleUrl { get; set; }
        public string? DisplayName { get; set; }
        public bool? IsLeafNode { get; set; }
        public string? URLPath { get; set; }
        public string? ImageIconWeb { get; set; }
        public string? ImageIconMobile { get; set; }
        public int? ItemPriority { get; set; }
        public List<GetModuleChildInversResponseDTO> Children { get; set; } = new();
    }
}
