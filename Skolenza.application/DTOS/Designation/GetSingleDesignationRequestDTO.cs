using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.application.DTOs.BaseDTO; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Designation
{
    public class GetSingleDesignationRequestDTO : PermissionRequestDTO
    {
        public required string UsertId { get; set; }
        public required int Id { get; set; } 
     
    }
}
