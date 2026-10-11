// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for creating Bank.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Pagination;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace skolenza.application.DTOS.Staff.Bank
{
    /// <summary>
    /// StaffMember create request
    /// </summary>
    /// 
    /// <summary> InstitutionId Required</summary>
   
    public class CreateBankRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO
    {
        /// <summary> InstitutionId Required</summary>

        [Required]
        public string StaffMemberId { get; set; } = string.Empty;      
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? IFSCCode { get; set; }
        public string? BranchName { get; set; }
        public string? AccountType { get; set; }
        public string? UPIId { get; set; }
        public bool IsPrimaryAccount { get; set; }= false;        
        public IFormFile? CancelledChequeFile { get; set; }


    }


}
