using skolenza.application.DTOS.Pagination;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Operation
{

    /// <summary>
    /// get-request to get all operation
    /// </summary>
    public class GetOperationRequestDTO : BaseRequest
    {
        /// <summary> StaffMember Id Required</summary>
       
        public required long StaffMemberId { get; set; }
        /// <summary> Role Id Required</summary>
        public required int RoleId { get; set; } 
        
    }
}
