using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOS.Staff.BaseStaffMember
{
    public class GetMinimalStaffMemberResponseDTO
    {
        public long Id { get; set; }
        public long InstitutionId { get; set; }
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string EmployementCode { get; set; } = null!;
        public int StaffEmploymentTypeId { get; set; }
        public int CountryId { get; set; }
        public string? CountryCode { get; set; }
        public string? EmergencyContactPerson { get; set; }
        public string? EmergencyContactNumber { get; set; }
        public string? BloodGroup { get; set; }
        public string? MobileNumber { get; set; }
        public int? Relation { get; set; }
        public string? Nationality { get; set; }
        public string? StaffEmploymentTypeName { get; set; }
        public string? DepartmentName { get; set; }
        public string? PositionName { get; set; }
        public int DepartmentId { get; set; }
        public int StaffPositionMasterId { get; set; }
        public string? OfficialEmail { get; set; }
        public bool IsActive { get; set; }
       // public bool IsOnboard { get; set; }
        public bool IsSoftDeleted { get; set; }
        public bool? IsEditAllowed { get; set; }
        public bool HasPermanent { get; set; }
        public int GenderId { get; set; }
        public string? GenderName { get; set; }
        public string? Remark { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? DateOfOnBoarding { get; set; }
        public long? UpdateById { get; set; }
        public DateTime? UpdateByDateTime { get; set; }
       
      
    }

}
