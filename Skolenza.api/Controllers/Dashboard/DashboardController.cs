using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Dashboard;
using skolenza.application.Features.DashboardCmd;
using skolenza.application.Interfaces.ILogger;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Dashboard;

/// <summary>
/// Supplies role-specific dashboard data through the existing permission pipeline.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class DashboardController(IMediator mediator, ILoggerService logger) : ControllerBase
{
    /// <summary>Returns the complete dashboard data for the requested authenticated RoleType code.</summary>
    [HttpGet("{roleTypeCode}")]
    [ProducesResponseType(typeof(ApiResponse<DashboardDataDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Data(
        [FromRoute] string roleTypeCode,
        [FromQuery] PermissionRequestDTO permission,
        CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(
            new GetDashboardDataQuery(roleTypeCode, permission),
            cancellationToken));
    }

    /// <summary>Returns Institution Super-Admin dashboard totals.</summary>
    [HttpGet("SuperAdmin/Summary")]
    public async Task<IActionResult> Summary(
        [FromQuery] PermissionRequestDTO permission,
        CancellationToken cancellationToken)
    {
        logger.LogInfo("Received Super-Admin dashboard summary request.");
        return Ok(await mediator.Send(new GetSuperAdminSummaryQuery(permission), cancellationToken));
    }

    /// <summary>Returns active, inactive, and currently-on-leave staffMember totals.</summary>
    [HttpGet("SuperAdmin/StaffMemberOverview")]
    public async Task<IActionResult> StaffMemberOverview(
        [FromQuery] PermissionRequestDTO permission,
        CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new GetSuperAdminStaffMemberOverviewQuery(permission), cancellationToken));
    }

    /// <summary>Returns upcoming birthdays with department, Position, and role context.</summary>
    [HttpGet("SuperAdmin/Birthdays")]
    public async Task<IActionResult> Birthdays(
        [FromQuery] PermissionRequestDTO permission,
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        return Ok(await mediator.Send(new GetSuperAdminBirthdaysQuery(permission, days), cancellationToken));
    }

    /// <summary>Returns the most recently onboarded staffMembers.</summary>
    [HttpGet("SuperAdmin/Onboarding")]
    public async Task<IActionResult> Onboarding(
        [FromQuery] PermissionRequestDTO permission,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        return Ok(await mediator.Send(new GetSuperAdminOnboardingQuery(permission, limit), cancellationToken));
    }

    /// <summary>Returns active staffMember headcount grouped by active department.</summary>
    [HttpGet("SuperAdmin/DepartmentHeadcount")]
    public async Task<IActionResult> DepartmentHeadcount(
        [FromQuery] PermissionRequestDTO permission,
        CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new GetSuperAdminDepartmentHeadcountQuery(permission), cancellationToken));
    }

    /// <summary>Returns staffMembers whose approved, non-cancelled leave includes today.</summary>
    [HttpGet("SuperAdmin/CurrentlyOnLeave")]
    public async Task<IActionResult> CurrentlyOnLeave(
        [FromQuery] PermissionRequestDTO permission,
        CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new GetSuperAdminLeaveQuery(permission), cancellationToken));
    }

    /// <summary>Returns institution location totals and a limited location list.</summary>
    [HttpGet("SuperAdmin/Locations")]
    public async Task<IActionResult> Locations(
        [FromQuery] PermissionRequestDTO permission,
        [FromQuery] int limit = 3,
        CancellationToken cancellationToken = default)
    {
        return Ok(await mediator.Send(new GetSuperAdminLocationsQuery(permission, limit), cancellationToken));
    }

    /// <summary>Returns temporary storage values until authoritative storage metering is available.</summary>
    [HttpGet("SuperAdmin/Storage")]
    public async Task<IActionResult> Storage(
        [FromQuery] PermissionRequestDTO permission,
        CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new GetSuperAdminStorageQuery(permission), cancellationToken));
    }

    /// <summary>Returns temporary hiring values until the recruitment module is available.</summary>
    [HttpGet("SuperAdmin/HiringPipeline")]
    public async Task<IActionResult> HiringPipeline(
        [FromQuery] PermissionRequestDTO permission,
        CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new GetSuperAdminHiringPipelineQuery(permission), cancellationToken));
    }
}
