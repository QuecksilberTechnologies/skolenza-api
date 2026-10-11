// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines stored-function and validated runtime authorization read operations.
// ================================================================

using skolenza.application.DTOs.Module.NewFolder;
using skolenza.application.DTOs.Operation;
using skolenza.application.DTOs.RoleModulePermission;
using skolenza.application.DTOs.UserLogin;
using skolenza.application.DTOS.Host;
using skolenza.application.DTOS.RoleModulePermission;
using skolenza.application.DTOS.StoreProcedures;
using skolenza.application.DTOS.StoreProcedures.DashboardSummeries;
using skolenza.application.DTOS.Institution;

namespace skolenza.application.Interfaces.IRepositories
{
    public interface IStoreProcedureRepository
    {
        Task<long> ValidateActiveUserLoginOnlyAsync(string loginId);
        Task<long> ValidateActiveUserCredentialOnlyAsync(string loginId);

        Task<int> ValidateUserPasswordAsync(string loginId);
        Task<bool> UpdateLoginCredential(LoginRequestDTO loginId);

        /// <summary>
        /// Checks whether the current Institution staffMember can execute the requested
        /// module operation using the staffMember's current Primary and Secondary roles.
        /// </summary>
        /// <param name="institutionId">Authenticated Institution identifier.</param>
        /// <param name="staffMemberId">Authenticated StaffMember identifier.</param>
        /// <param name="tokenRoleId">Primary Role identifier contained in the current JWT.</param>
        /// <param name="moduleId">Requested Module identifier.</param>
        /// <param name="operationId">Requested Operation identifier.</param>
        /// <param name="cancellationToken">Token used to cancel the database operation.</param>
        /// <returns>
        /// The current authorization result including stale-role detection and
        /// the Role that granted access.
        /// </returns>
        Task<InstitutionsUserPermissionCheckResponseDTO> CheckInstitutionStaffMemberPermissionAsync(
                long institutionId,
                long staffMemberId,
                int tokenRoleId,
                int moduleId,
                int operationId,
                CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether the current Host user can execute the requested module operation using the current Host role.
        /// </summary>
        /// <param name="hostUserId">The authenticated Host user identifier.</param>
        /// <param name="tokenHostRoleId">The Host role identifier captured in the signed access token.</param>
        /// <param name="moduleId">The requested Host module identifier.</param>
        /// <param name="operationId">The requested Host operation identifier.</param>
        /// <param name="cancellationToken">The token used to cancel the database operation.</param>
        /// <returns>The current Host authorization result including stale-role detection.</returns>
        Task<HostUserPermissionCheckResponseDTO> CheckHostUserPermissionAsync(
            long hostUserId,
            long tokenHostRoleId,
            int moduleId,
            int operationId,
            CancellationToken cancellationToken = default);

        Task<UpdateInstitutionEnabledOperationFromModuleOperationResponseDTO> UpdateInstitutionEnabledOperationFromModuleOperationRequestDTO(
            UpdateInstitutionEnabledOperationFromModuleOperationRequestDTO request);

        /// <summary>
        /// Retrieves the legacy Institution operational permission rows for a supplied internal role-ID set.
        /// </summary>
        /// <param name="request">The trusted Institution identifier and internal comma-separated role identifiers.</param>
        /// <returns>The legacy operational permission rows.</returns>
        Task<List<RoleModuleOperationResponseDTO>> GetActiveRoleModuleOperationsAsync(
            GetActiveRoleModuleOperationsRequestDTO request);

        /// <summary>
        /// Retrieves the authenticated institution staffMember's current effective module-operation access
        /// using the authoritative institution permission source.
        /// </summary>
        /// <param name="institutionId">The validated Institution identifier.</param>
        /// <param name="roleIds">The current effective role identifiers resolved by the application.</param>
        /// <param name="cancellationToken">A token used to cancel the database operation.</param>
        /// <returns>The current permitted operational rows used to construct navigation.</returns>
        Task<List<RoleModuleOperationResponseDTO>> GetCurrentInstitutionOperationalAccessAsync(
            long institutionId,
            IReadOnlyCollection<int> roleIds,
            CancellationToken cancellationToken = default);
        Task<List<GetModuleOperationRolePermissionsResponseDTO>> GetInstitutionModulesConfigurationResponses(GetInstitutionModuleOperationRolePermissionsRequestDTO request);
         

        Task<List<SubscribedModuleResponseDTO>> GetSubscribedModulesByInstitutionAsync(long institutionId);
        Task<bool> GetHasAccessOperation(GetCheckOperationPermissionRequestDTO checkOperationPermissionRequest);
        Task<bool> HasPermissionAsync(long userId, string permissionCode);
        Task<bool> IsInstitutionValidAsync(long userId, long? InstitutionId);

        Task<List<GetStaffMemberIdentitySp>> GetIdentityRecordAsync(long staffMemberId, int countryId, bool isActive);
        Task<StaffMemberCountResponseStatsSp> GetStaffMemberCountsAsync(long institutionId);
        Task<GetStaffMemberCodePatternResponseDTO?> GetInstitutionStaffMemberCodePatternAsync(
             StaffMemberCodePatternRequestDTO request);

        //   Task  <IUserRoleRepository> UpdateLoginCredential(LoginRequestDTO loginId);
        //  Task List<string> UpdateLoginCredential(LoginRequestDTO loginId);

    }
}

namespace skolenza.application.DTOS.StoreProcedures
{
    public class InstitutionUserPermissionCheckResponseDTO
    {
        // add properties according to your domain model
    }
}
