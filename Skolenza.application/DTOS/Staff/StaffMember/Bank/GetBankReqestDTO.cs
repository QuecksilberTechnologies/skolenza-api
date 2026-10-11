// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for retrieving Bank.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Pagination;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.Bank
{
    /// <summary>
    /// Represents the GetBankReqestDTO data transfer model.
    /// </summary>
    public class GetBankReqestDTO: PermissionPagedRequestDTO
    {

     
        public string? StaffMemberId { get; set; } // e.g., Savings, Current, etc.
        public int? Id {get; set; } // e.g., Savings, Current, etc.
        public string? AccountType { get; set; } // e.g., Savings, Current, etc.      
        public bool? IsPrimaryAccount { get; set; }
        public bool HasChequeDocUploaded { get; set; }
        public bool IsActive { get; set; }
        public bool? IsInfoVerified { get; set; }
        public bool? IsEditAllowed { get; set; }


    }

}
