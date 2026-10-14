// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Exposes the centralized Staff Category catalogue.
// ================================================================

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using skolenza.application.Features.HostStaffCategoryCmd.Handlers;
using skolenza.application.Features.StaffMemberCmd.StaffCategory.Handlers;

namespace skolenza.api.Controllers.Staff;

[ApiController]
[Route("api/Staff/Category")]
public sealed class StaffCategoryController(IMediator mediator) : ControllerBase
{
    /// <summary>Used-In-Angular: Host Staff Category Catalogue list.</summary>
    [Authorize]
    [HttpGet("host")]
    public async Task<IActionResult> GetHostCategories([FromQuery] skolenza.application.DTOs.BaseDTO.PermissionRequestDTO permission,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetHostStaffCategoriesQuery(permission), cancellationToken));

    /// <summary>Used-In-Angular: Host creates or updates a platform Staff Category.</summary>
    [Authorize]
    [HttpPost("host")]
    public async Task<IActionResult> SaveHostCategory([FromBody] skolenza.application.DTOS.Staff.SaveHostStaffCategoryRequestDTO request,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new SaveHostStaffCategoryCommand(request), cancellationToken));

    /// <summary>Used-In-Angular: Host updates a platform Staff Category.</summary>
    [Authorize]
    [HttpPut("host/{id}")]
    public async Task<IActionResult> UpdateHostCategory(short id,
        [FromBody] skolenza.application.DTOS.Staff.SaveHostStaffCategoryRequestDTO request,
        CancellationToken cancellationToken)
    {
        request.Id = id;
        return Ok(await mediator.Send(new SaveHostStaffCategoryCommand(request), cancellationToken));
    }

    /// <summary>Used-In-Angular: retrieves active platform Staff Category options.</summary>
    /// <remarks>
    /// Bearer authentication is required. This catalogue lookup accepts no ModuleId or OperationId.
    /// Angular function: StaffCategoriesAPI.getOptions; StaffMember create/edit form only.
    /// Institution registration does not expose this selector and assigns Administrative in the backend.
    /// </remarks>
    [Authorize]
    [HttpGet("options")]
    public async Task<IActionResult> GetOptions(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetStaffCategoryOptionsQuery(), cancellationToken);
        return Ok(response);
    }
}

