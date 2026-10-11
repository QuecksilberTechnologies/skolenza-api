using skolenza.application.DTOS.UserRoles;

namespace skolenza.application.DTOs.Staff
{
    public class GetStaffMemberLoginInfoResponseDTO
    {
        public string? StaffMemberId { get; set; }         
        public int RoleTypeId { get; set; }
        public string? RoleTypeName { get; set; }
        public string? InstitutionName { get; set; }
        public bool? IsPasswordChangeRequired { get; set; }
        public string? DesignationName { get; set; }
        public int DesignationId { get; set; }
        public bool IsOnboard {  get; set; }
        public int DepartmentId { get; set; }
        public int? CountryId { get; set; }
        public string? Nationality { get; set; }
        public string? DepartmentName { get; set; }
        public int StaffEmploymentTypeId  { get; set; }
        public string? OfficialEmail  { get; set; }
        public string? StaffMemberFullName { get; set; }
        public UserRoleDTO? UserPrimaryRole { get; set; }
        public List<UserRoleDTO>? UserSecondryRoles { get; set; }

        public string? ProfileImageLink { get; set; } 

        }







}

