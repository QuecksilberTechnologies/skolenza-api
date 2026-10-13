// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the request model for creating StaffMember.
// ================================================================

using skolenza.application.DTOS.Common;
using skolenza.application.DTOS.Pagination;
using skolenza.domain.Entity; using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace skolenza.application.DTOS.Staff.BaseStaffMember
{
    /// <summary>
    /// StaffMember create request
    /// </summary>
    public class CreateBaseStaffMemberRequestDTO : skolenza.application.DTOs.BaseDTO.PermissionRequestDTO

    {



        public int? StaffMemberDocumentId { get; set; }

        [Required]
        [MaxLength(100)]
        public required string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public required string LastName { get; set; }

        [Required]
        public DateTime? DateOfBirth { get; set; }

        [Required]
        public int DesignationId { get; set; }

        [Required]

        public  int CountryId { get; set; }
        [Required]
        public int DepartmentId { get; set; }

        [Required]
        public int StaffEmploymentTypeId { get; set; }

        [Required]
        public int InstitutionStaffCategoryMasterId { get; set; }

        [Required]
        public int StaffPositionMasterId { get; set; }

        [Required]
        public int GenderId { get; set; }

        //[Required]
        public required bool HasPermanent { get; set; }

        //[Required]
        public required bool IsActive { get; set; }

        [Required]
        [EmailAddress]
        public required string OfficialEmail { get; set; }

        //  Optional Fields
        [MaxLength(100)]
        public string? MiddleName { get; set; }

        public DateTime? DateOfOnBoarding { get; set; }

        public int RoleId { get; set; }





    }


}
