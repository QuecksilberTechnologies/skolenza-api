// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Exposes authenticated Host administration endpoints for Host-user, Host-role, and Host-module management.
// ================================================================

using skolenza.application.DTOS.Host;
using skolenza.application.Features.HostCmd.Handler;
using skolenza.application.Interfaces.ILogger;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Host
{
    /// <summary>
    /// Provides authenticated HTTP endpoints for Host administration requests.
    /// </summary>
    [Authorize]
    [Route("api/Host")]
    [Route("api/Platform")]
    [ApiController]
    public class HostController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILoggerService _logger;  // Logger service ka declaration

    /// <summary>
    /// Initializes a new instance of the <see cref="HostController"/> class.
    /// </summary>
    /// <param name="mediator">The mediator used to dispatch Host requests.</param>
    /// <param name="logger">The logger used to record controller activity.</param>
    public HostController(IMediator mediator, ILoggerService logger)
    {
        _mediator = mediator;
        _logger = logger;  // Logger service ko inject karna
    }

        /// <summary>
        /// Used-In-Angular: creates host user.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: creates host user.</para>
        /// <para>Handler flow: CreateHostUserCommand is processed by CreateHostUserCommandHandler; operation(s): AddAsync, SaveChangesAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); CreateHostUserResponseDTO: Id (long), HostRoleId (long), Name (string), LoginId (string), Email (string?), MobileNumber (string?), IsActive (bool), RoleName (string?), Permissions (List&lt;HostUserPermissionResponseDTO&gt;)</para>
        /// <para>Angular function(s): HostApi.createHostUser (app/core/services/host-api.ts:72).</para>
        /// <para>Angular purpose: creates host user.</para>
        /// <para>Integrated UI page(s): /app/host-users</para>
        /// <para>Angular UI component(s): HostUserManageDialog (app/shared/components/host-user/host-user-manage-dialog/host-user-manage-dialog.ts); HostUsers (app/features/host/users/host-users.ts)</para>
        /// </remarks>
        [HttpPost("create-host-user")]
        public async Task<IActionResult> CreateHostUser([FromBody] CreateHostUserRequestDTO institutionCreateRequestDTO)
        {
            _logger.LogInfo("Received request for register a new Institution" + institutionCreateRequestDTO.ToString());
            var command = new CreateHostUserCommand(institutionCreateRequestDTO);
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        /// <summary>
        /// Used-In-Angular: creates host role.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: creates host role.</para>
        /// <para>Handler flow: CreateHostRoleCommand is processed by CreateHostRoleCommandHandler; operation(s): AddAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); CreateHostRoleResponseDTO: Id (long), Name (string), Description (string?), IsActive (bool), Permissions (List&lt;HostRolePermissionResponseDTO&gt;)</para>
        /// <para>Angular function(s): HostApi.createHostRole (app/core/services/host-api.ts:102).</para>
        /// <para>Angular purpose: creates host role.</para>
        /// <para>Integrated UI page(s): /app/host-roles</para>
        /// <para>Angular UI component(s): HostRoleManageDialog (app/shared/components/host-role/host-role-manage-dialog/host-role-manage-dialog.ts); HostRoles (app/features/host/roles/host-roles.ts)</para>
        /// </remarks>
        [HttpPost("create-host-role")]
        public async Task<IActionResult> CreateHostRole( [FromBody] CreateHostRoleRequestDTO hostRoleRequestDTO)
        {
            _logger.LogInfo(
                "Received request to create a new Host Role.");

            var command = new CreateHostRoleCommand(hostRoleRequestDTO);

            var result = await _mediator.Send(command);

            return Ok(result);
        }

        #region Host User CRUD

        /// <summary>
        /// Used-In-Angular: retrieves host user by id.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves host user by id.</para>
        /// <para>Handler flow: GetHostUserByIdQuery is processed by GetHostUserByIdQueryHandler; operation(s): GetByIdAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetHostUserResponseDTO: Id (long), HostRoleId (long), HostRoleName (string?), Name (string), LoginId (string), Email (string?), MobileNumber (string?), IsActive (bool), AddedDateTime (DateTime?), UpdatedDateTime (DateTime?)</para>
        /// <para>Angular function(s): HostApi.getHostUserById (app/core/services/host-api.ts:46).</para>
        /// <para>Angular purpose: retrieves host user by id.</para>
        /// <para>Integrated UI page(s): /app/policies/attendance-policies; /auth/login; /app/admin-dashboard; /app/departments; /app/designations; /app/device-masters; /app/modules/module-operations; /app/modules/operations</para>
        /// <para>Angular UI component(s): isLogoutMenuItem (app/core/stores/auth.store.ts); CurrentUserPermissionsStore (app/core/stores/current-user-permissions.store.ts); UserMenu (app/layout/user-menu/user-menu.ts); hasModuleOperationGuard (app/core/guards/has-module-operation-guard.ts); hasModulePermissionGuard (app/core/guards/has-module-permission-guard.ts); superAdminGuard (app/core/guards/super-admin-guard.ts); AttendancePolicies (app/features/attendance-policies/attendance-policies.ts); Login (app/features/authentication/login/login.ts)</para>
        /// </remarks>
        [HttpGet("get-host-user-by-id/{id}")]
        public async Task<IActionResult> GetHostUserById(long id)
        {
            _logger.LogInfo(
                $"Received request to get host user by Id: {id}");

            var query = new GetHostUserByIdQuery(id);
            var result = await _mediator.Send(query);

            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: retrieves host users.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves all host users.</para>
        /// <para>Handler flow: GetAllHostUsersQuery is processed by GetAllHostUsersQueryHandler; operation(s): GetPagedAsync.</para>
        /// <para>Response DTO property analysis: PagedApiResponse: IsSucceeded (bool), Message (string), Data (List&lt;T&gt;), TotalCount (int), PageNumber (int), PageSize (int), TotalPages (int), HasPrevious (bool), HasNext (bool), HasUploadedAll (bool?), IsPrimaryMarked (bool?), CompletionPercentage (double?); GetHostUserResponseDTO: Id (long), HostRoleId (long), HostRoleName (string?), Name (string), LoginId (string), Email (string?), MobileNumber (string?), IsActive (bool), AddedDateTime (DateTime?), UpdatedDateTime (DateTime?)</para>
        /// <para>Angular function(s): HostApi.getHostUsers (app/core/services/host-api.ts:38).</para>
        /// <para>Angular purpose: retrieves host users.</para>
        /// <para>Integrated UI page(s): /app/host-dashboard; /app/host-users</para>
        /// <para>Angular UI component(s): DashboardHostStore (app/features/dashboard/dashboard-host/dashboard-host.store.ts); HostUsersStore (app/features/host/users/host-users.store.ts); DashboardHost (app/features/dashboard/dashboard-host/dashboard-host.ts); HostUsers (app/features/host/users/host-users.ts)</para>
        /// </remarks>
        [HttpGet("get-all-host-users")]
        public async Task<IActionResult> GetAllHostUsers(
            [FromQuery] bool? isActive = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            _logger.LogInfo($"Received Host-user list request. IsActive: {isActive}; PageNumber: {pageNumber}; PageSize: {pageSize}.");

            var query = new GetAllHostUsersQuery(isActive, pageNumber, pageSize);
            var result = await _mediator.Send(query);

            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: updates host user.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates host user.</para>
        /// <para>Handler flow: UpdateHostUserCommand is processed by UpdateHostUserCommandHandler; operation(s): GetByIdAsync, GetByLoginIdAsync, UpdateAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); UpdateHostUserResponseDTO: Id (long), HostRoleId (long), HostRoleName (string?), Name (string), LoginId (string), Email (string?), MobileNumber (string?), IsActive (bool), UpdatedDateTime (DateTime?)</para>
        /// <para>Angular function(s): HostApi.updateHostUser (app/core/services/host-api.ts:78).</para>
        /// <para>Angular purpose: updates host user.</para>
        /// <para>Integrated UI page(s): /app/host-users; /app/profile</para>
        /// <para>Angular UI component(s): HostUsersStore (app/features/host/users/host-users.store.ts); HostProfileStore (app/features/user-menu/host-profile/host-profile.store.ts); HostUserManageDialog (app/shared/components/host-user/host-user-manage-dialog/host-user-manage-dialog.ts); HostUsers (app/features/host/users/host-users.ts); HostProfile (app/features/user-menu/host-profile/host-profile.ts)</para>
        /// </remarks>
        [HttpPost("update-host-user")]
        public async Task<IActionResult> UpdateHostUser(
            [FromBody] UpdateHostUserRequestDTO requestDTO)
        {
            _logger.LogInfo(
                $"Received request to update host user. Id: {requestDTO?.Id}");

            var command = new UpdateHostUserCommand(requestDTO);
            var result = await _mediator.Send(command);

            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: deletes host user.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: deletes host user.</para>
        /// <para>Handler flow: DeleteHostUserCommand is processed by DeleteHostUserCommandHandler; operation(s): GetByIdAsync, DeleteAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): HostApi.deleteHostUser (app/core/services/host-api.ts:84).</para>
        /// <para>Angular purpose: deletes host user.</para>
        /// <para>Integrated UI page(s): /app/host-users</para>
        /// <para>Angular UI component(s): HostUsersStore (app/features/host/users/host-users.store.ts); HostUsers (app/features/host/users/host-users.ts)</para>
        /// </remarks>
        [HttpPost("delete-host-user")]
        public async Task<IActionResult> DeleteHostUser(
            [FromBody] DeleteHostUserRequestDTO requestDTO)
        {
            _logger.LogInfo(
                $"Received request to delete host user. Id: {requestDTO?.Id}");

            var command = new DeleteHostUserCommand(requestDTO);
            var result = await _mediator.Send(command);

            return Ok(result);
        }

        #endregion

        #region Host User Password Management

        /// <summary>
        /// Used-In-Angular: updates host user password.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates host user password.</para>
        /// <para>Handler flow: ChangeHostUserPasswordCommand is processed by ChangeHostUserPasswordCommandHandler; operation(s): GetByIdAsync, UpdateAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): HostApi.changeHostUserPassword (app/core/services/host-api.ts:96).</para>
        /// <para>Angular purpose: updates host user password.</para>
        /// <para>Integrated UI page(s): /app/update-password</para>
        /// <para>Angular UI component(s): UpdatePassword (app/features/user-menu/update-password/update-password.ts)</para>
        /// </remarks>
        [HttpPost("change-host-user-password")]
        public async Task<IActionResult> ChangeHostUserPassword(
            [FromBody] ChangeHostUserPasswordRequestDTO requestDTO)
        {
            _logger.LogInfo(
                $"Received request to change HostUser password. HostUserId: {requestDTO?.HostUserId}");

            var command = new ChangeHostUserPasswordCommand(requestDTO);
            var result = await _mediator.Send(command);

            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: performs the Angular function reset host user password.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: performs the Angular function reset host user password.</para>
        /// <para>Handler flow: ResetHostUserPasswordCommand is processed by ResetHostUserPasswordCommandHandler; operation(s): GetByIdAsync, UpdateAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): HostApi.resetHostUserPassword (app/core/services/host-api.ts:90).</para>
        /// <para>Angular purpose: performs the Angular function reset host user password.</para>
        /// <para>Integrated UI page(s): /app/host-users</para>
        /// <para>Angular UI component(s): HostUserResetPasswordDialog (app/shared/components/host-user/host-user-reset-password-dialog/host-user-reset-password-dialog.ts); HostUsers (app/features/host/users/host-users.ts)</para>
        /// </remarks>
        [HttpPost("reset-host-user-password")]
        public async Task<IActionResult> ResetHostUserPassword(
            [FromBody] ResetHostUserPasswordRequestDTO requestDTO)
        {
            _logger.LogInfo(
                $"Received request to reset HostUser password. HostUserId: {requestDTO?.HostUserId}");

            var command = new ResetHostUserPasswordCommand(requestDTO);
            var result = await _mediator.Send(command);

            return Ok(result);
        }

        #endregion

        #region Host Role CRUD

        /// <summary>
        /// Used-In-Angular: retrieves host role by id.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves host role by id.</para>
        /// <para>Handler flow: GetHostRoleByIdQuery is processed by GetHostRoleByIdQueryHandler; operation(s): GetByIdAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetHostRoleResponseDTO: Id (long), Name (string), Description (string?), IsActive (bool), AddedDateTime (DateTime?), UpdatedDateTime (DateTime?)</para>
        /// <para>Angular function(s): HostApi.getHostRoleById (app/core/services/host-api.ts:58).</para>
        /// <para>Angular purpose: retrieves host role by id.</para>
        /// <para>Integrated UI page(s): /app/host-roles/permissions/:hostRoleId</para>
        /// <para>Angular UI component(s): HostRolePermissionsStore (app/features/host/roles/host-role-permissions/host-role-permissions.store.ts); HostRolePermissions (app/features/host/roles/host-role-permissions/host-role-permissions.ts)</para>
        /// </remarks>
        [HttpGet("get-host-role-by-id/{id}")]
        public async Task<IActionResult> GetHostRoleById(long id)
        {
            _logger.LogInfo(
                $"Received request to get host role by Id: {id}");

            var query = new GetHostRoleByIdQuery(id);
            var result = await _mediator.Send(query);

            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: retrieves host roles.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves all host roles.</para>
        /// <para>Handler flow: GetAllHostRolesQuery is processed by GetAllHostRolesQueryHandler; operation(s): GetAllAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetHostRoleResponseDTO: Id (long), Name (string), Description (string?), IsActive (bool), AddedDateTime (DateTime?), UpdatedDateTime (DateTime?)</para>
        /// <para>Angular function(s): HostApi.getHostRoles (app/core/services/host-api.ts:52).</para>
        /// <para>Angular purpose: retrieves host roles.</para>
        /// <para>Integrated UI page(s): /app/host-dashboard; /app/host-roles; /app/host-users</para>
        /// <para>Angular UI component(s): DashboardHostStore (app/features/dashboard/dashboard-host/dashboard-host.store.ts); HostRolesStore (app/features/host/roles/host-roles.store.ts); HostUserManageDialog (app/shared/components/host-user/host-user-manage-dialog/host-user-manage-dialog.ts); DashboardHost (app/features/dashboard/dashboard-host/dashboard-host.ts); HostRoles (app/features/host/roles/host-roles.ts); HostUsers (app/features/host/users/host-users.ts)</para>
        /// </remarks>
        [HttpGet("get-all-host-roles")]
        public async Task<IActionResult> GetAllHostRoles()
        {
            _logger.LogInfo("Received request to get all host roles.");

            var query = new GetAllHostRolesQuery();
            var result = await _mediator.Send(query);

            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: updates host role.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: updates host role.</para>
        /// <para>Handler flow: UpdateHostRoleCommand is processed by UpdateHostRoleCommandHandler; operation(s): GetByIdAsync, GetByRoleNameAsync, UpdateAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); UpdateHostRoleResponseDTO: Id (long), Name (string), Description (string?), IsActive (bool), UpdatedDateTime (DateTime?)</para>
        /// <para>Angular function(s): HostApi.updateHostRole (app/core/services/host-api.ts:108).</para>
        /// <para>Angular purpose: updates host role.</para>
        /// <para>Integrated UI page(s): /app/host-roles</para>
        /// <para>Angular UI component(s): HostRolesStore (app/features/host/roles/host-roles.store.ts); HostRoleManageDialog (app/shared/components/host-role/host-role-manage-dialog/host-role-manage-dialog.ts); HostRoles (app/features/host/roles/host-roles.ts)</para>
        /// </remarks>
        [HttpPost("update-host-role")]
        public async Task<IActionResult> UpdateHostRole(
            [FromBody] UpdateHostRoleRequestDTO requestDTO)
        {
            _logger.LogInfo(
                $"Received request to update host role. Id: {requestDTO?.Id}");

            var command = new UpdateHostRoleCommand(requestDTO);
            var result = await _mediator.Send(command);

            return Ok(result);
        }

        /// <summary>
        /// Used-In-Angular: deletes host role.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: deletes host role.</para>
        /// <para>Handler flow: DeleteHostRoleCommand is processed by DeleteHostRoleCommandHandler; operation(s): GetByIdAsync, DeleteAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
        /// <para>Angular function(s): HostApi.deleteHostRole (app/core/services/host-api.ts:114).</para>
        /// <para>Angular purpose: deletes host role.</para>
        /// <para>Integrated UI page(s): /app/host-roles</para>
        /// <para>Angular UI component(s): HostRolesStore (app/features/host/roles/host-roles.store.ts); HostRoles (app/features/host/roles/host-roles.ts)</para>
        /// </remarks>
        [HttpPost("delete-host-role")]
        public async Task<IActionResult> DeleteHostRole(
            [FromBody] DeleteHostRoleRequestDTO requestDTO)
        {
            _logger.LogInfo(
                $"Received request to delete host role. Id: {requestDTO?.Id}");

            var command = new DeleteHostRoleCommand(requestDTO);
            var result = await _mediator.Send(command);

            return Ok(result);
        }

        #endregion

        #region Host Module Queries

        /// <summary>
        /// Used-In-Angular: retrieves host modules.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves host modules.</para>
        /// <para>Handler flow: GetHostModulesQuery is processed by GetHostModulesQueryHandler; operation(s): GetHostModulesAsync.</para>
        /// <para>Response DTO property analysis: PagedApiResponse: IsSucceeded (bool), Message (string), Data (List&lt;T&gt;), TotalCount (int), PageNumber (int), PageSize (int), TotalPages (int), HasPrevious (bool), HasNext (bool), HasUploadedAll (bool?), IsPrimaryMarked (bool?), CompletionPercentage (double?); GetHostModuleResponseDTO: Id (int), InstitutionId (long?), ModuleCode (string?), ModuleName (string), DisplayName (string?), Urlpath (string?), ParentModuleId (int?), IsLeafNode (bool?), IsModuleDisplayInUi (bool), IsCommonMenu (bool), ModuleScope (short), IsActive (bool), ImageIconWeb (string?), ImageIconMobile (string?), ItemPriority (int?), Remark (string?), AddedById (long?), AddedDateTime (DateTime?), UpdatedById (long?), UpdatedDateTime (DateTime?)</para>
        /// <para>Angular function(s): HostApi.getHostModules (app/core/services/host-api.ts:66).</para>
        /// <para>Angular purpose: retrieves host modules.</para>
        /// <para>Integrated UI page(s): No static Angular route was resolved; see Angular UI component(s).</para>
        /// <para>Angular UI component(s): No consuming Angular component was statically resolved.</para>
        /// </remarks>
        [HttpGet("get-host-modules")]
        public async Task<IActionResult> GetHostModules(
            [FromQuery] bool? isActive = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            _logger.LogInfo($"Received Host-module list request. IsActive: {isActive}; PageNumber: {pageNumber}; PageSize: {pageSize}.");

            var query = new GetHostModulesQuery(isActive, pageNumber, pageSize);
            var result = await _mediator.Send(query);

            return Ok(result);
        }
                /// <summary>
                /// Not-Used-In-Angular.
                /// </summary>
                /// <remarks>
                /// <para>Angular usage status: Not-Used-In-Angular.</para>
                /// <para>API endpoint purpose: retrieves host module by id.</para>
                /// <para>Handler flow: GetHostModuleByIdQuery is processed by GetHostModuleByIdQueryHandler; operation(s): GetHostModuleByIdAsync.</para>
                /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetHostModuleResponseDTO: Id (int), InstitutionId (long?), ModuleCode (string?), ModuleName (string), DisplayName (string?), Urlpath (string?), ParentModuleId (int?), IsLeafNode (bool?), IsModuleDisplayInUi (bool), IsCommonMenu (bool), ModuleScope (short), IsActive (bool), ImageIconWeb (string?), ImageIconMobile (string?), ItemPriority (int?), Remark (string?), AddedById (long?), AddedDateTime (DateTime?), UpdatedById (long?), UpdatedDateTime (DateTime?)</para>
                /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
                /// <para>Backend endpoint: GET /api/host/get-host-module-by-id/{}.</para>
                /// </remarks>

                [HttpGet("get-host-module-by-id/{id:int}")]
                public async Task<IActionResult> GetHostModuleById(
                    int id,
                    [FromQuery] bool? isActive = null)
                {
                    _logger.LogInfo($"Received request to get Host module. Id: {id}, IsActive: {isActive}");

                    var query = new GetHostModuleByIdQuery(id, isActive);
                    var result = await _mediator.Send(query);

                    return Ok(result);
                }

        #endregion

    }
}
