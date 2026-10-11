using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Pagination;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.Sensitive
{
    public class GetIdentityResponseDTO
    {

        public string? StaffMemberId { get; set; }
        //public string? AadhaarNumber { get; set; }
        //public string? PanNumber { get; set; }
        //public string? PassportNumber { get; set; }
        public string? DrivingLicenseNumber { get; set; }
        public string? EmergencyContactRelation { get; set; }
        public bool HasEPFAccount { get; set; }
        public string? UANNumber { get; set; }
        public string? VoterId { get; set; }
        public string? BloodGroup { get; set; }
        public bool? MaritalStatus { get; set; }
        public string? Nationality { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactNumber { get; set; }
        public bool? IsInfoVerified { get; set; }
        public bool IsEditAllowed { get; set; }

        //public bool hasPanIdUploaded { get; set; }
        //public bool hasAadharIdUploaded { get; set; }
        //public bool hasPassportIdUploaded { get; set; }

        //public string? passportFilePath { get; set; }
        //public string? panFilePath { get; set; }
        //public string? aadharFilePath { get; set; }
        public double? CompletionPercentage { get; set; }
    }

}
