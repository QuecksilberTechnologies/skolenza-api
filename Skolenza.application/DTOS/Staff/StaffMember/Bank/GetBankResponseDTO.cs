using skolenza.application.DTOS.Common;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.Bank
{

    public class GetBankResponseDTO 
    {
        public int Id { get; set; }
        public string? StaffMemberId { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
        public string? AccountType { get; set; } // e.g., Savings, Current, etc.
        public bool IsPrimaryAccount { get; set; }
        public bool? IsInfoVerified { get; set; }
        public string? IFSCCode { get; set; }
        public string? BranchName { get; set; }
        public string? UPIId { get; set; }
        public bool HasChequeDocUploaded { get; set; }
        public string? FilePath { get; set; }
        public string? FileName { get; set; }
        public int? FileType { get; set; }
        public IFormFile? CancelledChequeFile { get; set; }      
        public bool? IsEditAllowed { get; set; }
        
        public bool IsActive { get; set; }
        public double CompletionPercentage { get; set; } 
       
      
    

    }

}
