using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; 
using skolenza.domain.Entity; 
using MediatR;
using skolenza.application.DTOS.Common;
using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOS.TicketDTO.Classification
{
    public class GetClassificationRequestDTO : PermissionRequestDTO
    {

        public int Id { get; set; }  
         


    }
}
