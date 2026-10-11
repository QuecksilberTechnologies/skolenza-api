using skolenza.application.DTOS.Pagination;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Operation
{
   
    public class GetHasAccessOperationDTO : BaseRequest
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public bool? Status { get; set; }
    }
}
