using skolenza.application.DTOS.Pagination;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Operation
{
    [Keyless]
    public class GetCheckOperationPermissionRequestDTO: BaseRequest
    {
        public string? RoleIds { get; set; }
       
        public int OperationId { get; set; }
        public bool IsActive { get; set; }
        public bool HasAccess { get; set; }

    }

}
