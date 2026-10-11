using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.application.DTOs.BaseDTO; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Department
{
    public class GetSingleDepartmentRequestDTO : PermissionRequestDTO
    {
        
        public required int Id { get; set; } 
     
    }
}
