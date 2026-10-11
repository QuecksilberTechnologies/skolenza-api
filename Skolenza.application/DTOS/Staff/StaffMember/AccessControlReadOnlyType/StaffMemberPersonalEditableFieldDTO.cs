using skolenza.application.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Staff.AccessControlReadOnlyType
{
    public class StaffMemberPersonalEditableFieldDTO
    {
        [AccessControl(ReadOnly = true)]
        public long InstitutionId { get; set; }
        [AccessControl(ReadOnly = true)]
        public long StaffMemberId { get; set; }
        [AccessControl(ReadOnly = false)]

        public string? AadhaarNumber { get; set; }
        [AccessControl(ReadOnly = false)]

        public string? PanNumber { get; set; }
        [AccessControl(ReadOnly = false)]

        public string? PassportNumber { get; set; }
        [AccessControl(ReadOnly = false)]

        public string? DrivingLicenseNumber { get; set; }
        [AccessControl(ReadOnly = false)]

        public string? VoterId { get; set; }
        [AccessControl(ReadOnly = false)]

        public string? BloodGroup { get; set; }
        [AccessControl(ReadOnly = false)]

        public string? MaritalStatus { get; set; }
        [AccessControl(ReadOnly = false)]

        public string? Nationality { get; set; }
        [AccessControl(ReadOnly = false)]

        public string? EmergencyContactName { get; set; }
        [AccessControl(ReadOnly = false)]

        public string? EmergencyContactNumber { get; set; }

 
      
 
    }
}
