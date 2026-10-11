using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Designation
{
    public class GetDesignationOptionResponseDTO
    {
        public int Id {  get; set; }
        public int DepartmentId { get; set; }        
        public string? DesignationName { get; set; }       
       
    }
}
