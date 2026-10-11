// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for updating Bank.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOs.BaseDTO;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace skolenza.application.DTOS.Staff.Bank
{
    /// <summary>
    /// Update Bank request
    /// </summary>
    /// 
    /// <summary> InstitutionId Required</summary>

    public class UpdateBankReqestDTO : PermissionRequestDTO
    {
        /// <summary> UserStaffMemberId Required</summary>

        [Required]
        public string UserStaffMemberId { get; set; } = string.Empty;
        [Required]
        public int Id { get; set; }

        [Required]
        public string StaffMemberId { get; set; } = string.Empty;
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? IFSCCode { get; set; }
        public string? BranchName { get; set; }
        public string? AccountType { get; set; }
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public string? UPIId { get; set; }
        public bool IsPrimaryAccount { get; set; }= false;        
        public IFormFile? CancelledChequeFile { get; set; }








    }


}
