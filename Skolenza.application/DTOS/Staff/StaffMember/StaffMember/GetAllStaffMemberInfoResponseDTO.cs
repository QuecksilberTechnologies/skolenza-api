// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines StaffMember-list row data including the configured maximum role-assignment limit.
// ================================================================

namespace skolenza.application.DTOS.Staff.BaseStaffMember
{
    /// <summary>
    /// Represents one StaffMember row returned by the existing paged StaffMember-list endpoint.
    /// </summary>
    public class GetAllStaffMemberInfoResponseDTO
    {
                  
            public string? StaffMemberId { get; set; }
            public string? EmployementCode { get; set; } 
            public string? LastName { get; set; } = string.Empty;
            public string? MiddleName { get; set; }
             public string? FirstName { get; set; } = string.Empty;
            public int GenderId { get; set; }
            public string? GenderName { get; set; }
              public int CountryId { get; set; }
              public string? CountryCode { get; set; }
             public string? MobileNumber { get; set; }
          public string Nationality { get; set; } = string.Empty;           
            public string? DateOfBirth { get; set; }
            public string? DateOfOnBoarding { get; set; }
            public int DepartmentId { get; set; }
            public string? DepartmentName { get; set; }
            public int? StaffPositionMasterId { get; set; }
            public string? PositionName { get; set; }
            public int StaffEmploymentTypeId { get; set; }
            public short? StaffCategoryId { get; set; }
            public string? StaffEmploymentTypeName { get; set; }
            public string? OfficialEmail { get; set; }          
            public string? EmergencyContactPerson { get; set; }          
            public bool IsActive { get; set; } = true;          
            public string? StaffMemberImagePath { get; set; }           
         //   public   <List>AsignedAsset AsignedAssetList { get;set} 
            public double? CompletionPercentage { get; set; }
            public bool? HasImagePicUploaded { get; set; }

            /// <summary>
            /// Gets or sets the configured maximum number of roles that may be assigned to this StaffMember.
            /// </summary>
            public int MaxRoleAssigned { get; set; }

            /// <summary>
            /// Gets the StaffMember's current active role assignments for the same Institution.
            /// </summary>
            public List<AssignedStaffMemberRoleDTO> AssignedRoles { get; set; } = new();
        //   public SummaryStaffMemberInfo SummaryStaffMemberInfo { get; set; } = new SummaryStaffMemberInfo();



    }

    /// <summary>
    /// Represents one active role assigned to an StaffMember-list row.
    /// </summary>
    public class AssignedStaffMemberRoleDTO
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

  
 
    public class StaffMemberProfileSummaryInfo
    {   
        public string? StaffMemberId { get; set; }
        public string? FullName { get; set; }
        public int? StaffPositionMasterId { get; set; }
        public string? PositionName { get; set; }
        public int? Department { get; set; }
        public string? DepartmentType { get; set; }
        public string? StaffMemberCode { get; set; }       
        public string? MobileNumber { get; set; }
        public string? OffilcialEmail { get; set; }
        




    }
    public class SummaryStaffMemberInfo
    {   
        public string? StaffMemberId { get; set; }
        public string? EmergencyContactPerson { get; set; }
        public string? EmergencyContactNumber { get; set; }
        public string? StaffMemberCode { get; set; }
        public string? BloodGroup { get; set; }
        public string? MobileNumber { get; set; }
        public string? PersonalEmail { get; set; }
        public string? CountryCode { get; set; }
        public int? Relation { get; set; }
        public string? RelationName  {  get; set;  }
        public bool IsActive { get; set; }
        public bool? IsMarried { get; set; }
        public int? OnlineStatus { get; set; }
        public DateTime? LastLoginDateTime { get; set; }
        public int CurrentSalaryStatusId { get; set; }
        public string? CurrentSalaryStatusRemark { get; set; }
        public int RoleId { get; set; }
        public int? RoleType { get; set; }
        public string? RoleName { get; set; } = null;
        public int StaffPositionMasterId { get; set; }
        public string? PositionName { get; set; }
        public int DepartmentId { get; set; }
        public string? Department { get; set; }
        public string? ProfileImage { get; set; }
        public string? City { get; set; }
        public string? Address { get; set; }
        public DateTime? DateOfJoining { get; set; } 
        public int StaffEmploymentTypeId { get; set; } 
        public string? StaffEmploymentTypeName { get; set; }




    }

}
 
