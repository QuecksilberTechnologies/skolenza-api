using skolenza.application.Common.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;

namespace skolenza.application.DTOs.Staff.AccessControlReadOnlyType
{
    public class StaffMemberInfoEditableFieldsDTO
    {

        [AccessControl(ReadOnly = true)]
        public long InstitutionId { get; set; }

        [AccessControl(ReadOnly = true)]
        public int StaffMemberDocumentId { get; set; }

        [AccessControl(ReadOnly = true)]

        public required string OfficialEmail { get; set; }

        [AccessControl(ReadOnly = true)]
        public required string EmployementCode { get; set; }
        [AccessControl(ReadOnly = false)]
        public required string FirstName { get; set; }
        [Required]
        [AccessControl(ReadOnly = false)]
        [MaxLength(100)]
        public string? MiddleName { get; set; }


        [AccessControl(ReadOnly = false)]

        public required string LastName { get; set; }

        [AccessControl(ReadOnly = true)]
        public DateTime? DateOfOnBoarding { get; set; }


        [AccessControl(ReadOnly = false)]
        public DateTime? DateOfBirth { get; set; }

        [AccessControl(ReadOnly = true)]
        public int? DepartmentId { get; set; }    // Optional Dropdown

        [AccessControl(ReadOnly = true)]
        public int? DesignationId { get; set; }   // Optional Dropdown

        [AccessControl(ReadOnly = true)]
        public required int StaffEmploymentTypeId { get; set; }   // Dropdown: e.g., Intern, Full-Time, Contractor

        [AccessControl(ReadOnly = true)]
        public required bool HasPermanent { get; set; }    // Permanent employment flag


        [AccessControl(ReadOnly = true)]
        public required bool IsActive { get; set; }        // Active flag


    }

}
