// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Exposes the centralized Staff Category catalogue.
// ================================================================

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using skolenza.application.Features.StaffMemberCmd.StaffCategory.Handlers;

namespace skolenza.api.Controllers.Staff;

[ApiController]
[Route("api/Staff/Category")]
public sealed class StaffCategoryController(IMediator mediator) : ControllerBase
{
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
