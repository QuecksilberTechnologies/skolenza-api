using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Designation.Custom
{
    public class GetDepartmentAndDesignationResponseDTO
    {
        public long DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public List<DesignationInfoDTO>? DesignationInfo { get; set; }
    }

    public class DesignationInfoDTO
    {
        public long Id { get; set; }
        public long? DepartmentId { get; set; }
        public string? DesignationName { get; set; }
        public bool IsActive { get; set; }
    }

}
