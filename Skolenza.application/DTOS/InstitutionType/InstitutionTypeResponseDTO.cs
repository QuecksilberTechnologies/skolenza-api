using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.InstitutionType
{
 
        public class InstitutionTypeResponseDTO
        {
            public int Id { get; set; }
            public string InstitutionTypeName { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string? Remark { get; set; }
            public bool IsActive { get; set; } 
          
        }
     

}
