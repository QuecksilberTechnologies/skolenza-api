// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Exposes HostRole module-operation permission assignment APIs.
// ================================================================

using skolenza.application.DTOS.Host;
using skolenza.application.Features.HostCmd.Handler;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Host;

/// <summary>
/// Coordinates HostRole module-operation permission requests.
/// </summary>
[ApiController]
[Route("api/HostRolePermission")]
[Route("api/PlatformRolePermission")]
[Authorize]
public class HostRolePermissionController : ControllerBase
{
    #region Fields

    private readonly IMediator _mediator;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="HostRolePermissionController"/> class.
    /// </summary>
    /// <param name="mediator">Dispatches HostRole permission commands and queries.</param>
    public HostRolePermissionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    #endregion

    #region HostRole Module Permissions

    /// <summary>
    /// Used-In-Angular: retrieves host role module permissions.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves host role module permissions.</para>
    /// <para>Handler flow: GetHostRoleModulePermissionsQuery is processed by GetHostRoleModulePermissionsQueryHandler; operation(s): GetByIdAsync, GetAllModuleOperationMappingsAsync, GetHostUserPermissionsAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); GetHostRoleModulePermissionsResponseDTO: HostRoleId (long), HostRoleName (string), Modules (List&lt;HostRoleModulePermissionsModuleResponseDTO&gt;)</para>
    /// <para>Angular function(s): HostApi.getHostRoleModulePermissions (app/core/services/host-api.ts:125).</para>
    /// <para>Angular purpose: retrieves host role module permissions.</para>
    /// <para>Integrated UI page(s): /app/policies/attendance-policies; /auth/login; /app/admin-dashboard; /app/departments; /app/designations; /app/device-masters; /app/modules/module-operations; /app/modules/operations</para>
    /// <para>Angular UI component(s): CurrentUserPermissionsStore (app/core/stores/current-user-permissions.store.ts); HostRolePermissionsStore (app/features/host/roles/host-role-permissions/host-role-permissions.store.ts); hasModuleOperationGuard (app/core/guards/has-module-operation-guard.ts); hasModulePermissionGuard (app/core/guards/has-module-permission-guard.ts); superAdminGuard (app/core/guards/super-admin-guard.ts); AttendancePolicies (app/features/attendance-policies/attendance-policies.ts); Login (app/features/authentication/login/login.ts); DashboardAdmin (app/features/dashboard/dashboard-admin/dashboard-admin.ts)</para>
    /// </remarks>
    [HttpGet("get-role-module-permissions/{hostRoleId:long}")]
    public async Task<IActionResult> GetRoleModulePermissions(
        long hostRoleId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetHostRoleModulePermissionsQuery(hostRoleId),
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Used-In-Angular: updates host role module permissions.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: updates host role module permissions.</para>
    /// <para>Handler flow: SaveHostRoleModulePermissionsCommand is processed by SaveHostRoleModulePermissionsCommandHandler; operation(s): GetByIdAsync, GetAllModuleOperationMappingsAsync, GetByHostRoleIdAsync, SaveChangesAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
    /// <para>Angular function(s): HostApi.saveHostRoleModulePermissions (app/core/services/host-api.ts:140).</para>
    /// <para>Angular purpose: updates host role module permissions.</para>
    /// <para>Integrated UI page(s): /app/host-roles/permissions/:hostRoleId</para>
    /// <para>Angular UI component(s): HostRolePermissionsStore (app/features/host/roles/host-role-permissions/host-role-permissions.store.ts); HostRolePermissions (app/features/host/roles/host-role-permissions/host-role-permissions.ts)</para>
    /// </remarks>
    [HttpPost("save-role-module-permissions")]
    public async Task<IActionResult> SaveRoleModulePermissions(
        [FromBody] SaveHostRoleModulePermissionsRequestDTO? dto,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new SaveHostRoleModulePermissionsCommand(dto),
            cancellationToken);

        return Ok(result);
    }

    #endregion
}
