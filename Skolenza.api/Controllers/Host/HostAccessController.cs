// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Exposes authenticated Host-user authorization and Host operational-access bootstrap endpoints.
// ================================================================

using skolenza.application.Features.HostCmd.Handler;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Host;

/// <summary>
/// Exposes the current Host user's module and operation access bootstrap.
/// </summary>
[ApiController]
[Route("api/HostAccess")]
[Route("api/PlatformAccess")]
[Authorize]
public sealed class HostAccessController : ControllerBase
{
    #region Fields

    private readonly IMediator _mediator;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="HostAccessController"/> class.
    /// </summary>
    /// <param name="mediator">Dispatches Host access bootstrap queries.</param>
    public HostAccessController(IMediator mediator)
    {
        _mediator = mediator;
    }

    #endregion

    #region Host Access Endpoints
    /// <summary>
    /// Used-In-Angular: retrieves host bootstrap.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves host access.</para>
    /// <para>Handler flow: GetHostAccessQuery is processed by GetHostAccessQueryHandler; operation(s): GetByIdAsync, GetHostUserPermissionsAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); HostAccessResponseDTO: Modules (IReadOnlyCollection&lt;HostAccessModuleResponseDTO&gt;)</para>
    /// <para>Angular function(s): AccessBootstrapApi.getHostBootstrap (app/core/services/access-bootstrap-api.ts:28).</para>
    /// <para>Angular purpose: retrieves host bootstrap.</para>
    /// <para>Integrated UI page(s): /app/policies/attendance-policies; /auth/login; /app/admin-dashboard; /app/departments; /app/designations; /app/device-masters; /app/modules/module-operations; /app/modules/operations</para>
    /// <para>Angular UI component(s): CurrentUserPermissionsStore (app/core/stores/current-user-permissions.store.ts); hasModuleOperationGuard (app/core/guards/has-module-operation-guard.ts); hasModulePermissionGuard (app/core/guards/has-module-permission-guard.ts); superAdminGuard (app/core/guards/super-admin-guard.ts); AttendancePolicies (app/features/attendance-policies/attendance-policies.ts); Login (app/features/authentication/login/login.ts); DashboardAdmin (app/features/dashboard/dashboard-admin/dashboard-admin.ts); LocationsCard (app/features/dashboard/dashboard-admin/locations-card/locations-card.ts)</para>
    /// </remarks>

    [HttpGet("bootstrap")]
    public async Task<IActionResult> GetBootstrap(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetHostAccessQuery(), cancellationToken);
        return Ok(result);
    }

    #endregion
}
