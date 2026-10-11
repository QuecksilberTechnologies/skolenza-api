using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Common
{
   public class DeleteRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {    
        public long Id { get; set; }    
      
    
    }
}
