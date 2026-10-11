using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.ModuleOperation
{
    public class GetModuleOperationMappingResponseDTO: BaseRequest
    {
        public int ModuleId { get; set; }
        public List<int> OperationIds { get; set; } = new();
        public string? Message { get; set; }
        public bool Success { get; set; }
    }

}
