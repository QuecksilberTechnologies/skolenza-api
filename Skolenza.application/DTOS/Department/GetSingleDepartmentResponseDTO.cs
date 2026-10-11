using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Department
{
    public class GetSingleDepartmentResponseDTO
    {
        public int Id { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? Description { get; set; }
        public string? Remark { get; set; }
        public string? AddedById { get; set; }
        public DateTime? AddedDateTime { get; set; }
        public string? UpdatedById { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
    }
}
