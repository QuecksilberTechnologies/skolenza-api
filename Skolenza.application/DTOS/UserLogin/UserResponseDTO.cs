using skolenza.application.DTOs.Staff;
using skolenza.application.DTOs.RoleModulePermission;
using skolenza.application.DTOS.Module.ParentModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using skolenza.domain.Entity;
using MediatR;
using skolenza.application.DTOS.UserRoles;

namespace skolenza.application.DTOS.UserLogin
{

    public class UserResponseDTO
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public string? Message { get; set; }
        public GetStaffMemberLoginInfoResponseDTO? StaffMemberInfo { get; set; }
        public string? Allroles { get; set; }
        public List<ModuleResponseDTO>? CommonItemList { get; set; }
        public List<MainModuleDto>? OperationalMenus { get; set; }

    }
    public class UserInfoDTO
    {
        public int StaffMemberId { get; set; }
        public string? StaffMemberCode { get; set; }    
        public long? InstitutionId { get; set; }
        public string? InstitutionName { get; set; }
        public bool? IsPasswordChangeRequired { get; set; }
        public int? StaffPositionMasterId { get; set; }
        public int? DepartmentId { get; set; }
        public int StaffEmploymentTypeId { get; set; }
        public string? OfficialEmail { get; set; }
        public string? StaffMemberFullName { get; set; }
        public UserRoleDTO? UserPrimaryRole { get; set; }
        public List<UserRoleDTO>? UserSecondryRoles { get; set; }


    }
}
