using skolenza.application.DTOS.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.Bank
{
    public class DeleteBankRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {
 
      
        public int Id { get; set; }
        
    }
}
