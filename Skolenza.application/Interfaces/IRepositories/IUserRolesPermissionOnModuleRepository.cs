using skolenza.application.DTOs.BasicAndRoleBaseMenu;
using skolenza.application.DTOs.ModuleOperation;
using skolenza.application.DTOs.Role;
using skolenza.application.DTOs.RoleModulePermission;
using skolenza.application.DTOs.Institution;
using skolenza.application.DTOS.RoleModulePermission;
using skolenza.domain.Entity; 
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; 
namespace skolenza.application.Interfaces.IRepositories
{
    public interface IUserRolesPermissionOnModuleRepository
    {
        public Task<IEnumerable<UserRolesPermissionOnModuleDTO>> GetModuleListAndOperationByRollIdAsync(List<RoleInfoDTO> roleList, int? forPlatform);
        Task<int> AdminAssignModulePermissionAsync(List<RoleModuleAndPermission> insertRoleModulePermissionsRequestDTO);
        Task<int> BulkInsertAsync(  List<RoleModuleAndPermission> rolePermissions);

        /// <summary>
        /// Adds only missing enabled module-operation permissions to the active Institution Admin role.
        /// Existing permission rows are retained and are never duplicated.
        /// </summary>
        Task<int> AddMissingInstitutionAdminPermissionsAsync(
            long institutionId,
            long addedById,
            CancellationToken cancellationToken = default);

        Task<InstitutionEnabledOperationsResponseDTO> GetAllInstitutionModuleWithOperation(InstitutionEnabledOperation dto);
        Task<bool> UpdateInstitutionModuleAndItsOperationsAsync(InstitutionModuleOperationsUpdateRequestDTO request);
        Task<List<InstitutionEnabledModule>> GetAllInstitutionEnabledModulesWithOperationsAsync(long? InstitutionId);
        Task<List<GetModuleOperationRolePermissionsResponseDTO>> GetInstitutionModulesOperationRolePermissionResponses(GetInstitutionModuleOperationRolePermissionsRequestDTO request);

        Task<List<RoleModuleAndPermission>> GetByRoleIdAsync(
            int roleId,
            long institutionId,
            CancellationToken cancellationToken = default);

        Task BulkDeleteAsync(List<RoleModuleAndPermission> list);

        // Retained for the existing institution-registration permission flow.
        Task<List<ModuleOperationMapping>> GetModuleOperationMappings(List<Module> modules);


    }
}
