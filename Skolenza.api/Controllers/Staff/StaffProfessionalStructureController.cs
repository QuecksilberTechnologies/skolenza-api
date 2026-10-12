using skolenza.application.DTOS.Staff;
using skolenza.application.Features.StaffMemberCmd.ProfessionalStructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Staff;

/// <summary>Manages institution professional masters and effective-dated StaffMember assignments.</summary>
[ApiController]
[Authorize]
[Route("api/Staff/professional-structure")]
public sealed class StaffProfessionalStructureController(IMediator mediator) : ControllerBase
{
    [HttpGet("masters/{masterType}")]
    public async Task<IActionResult> GetMasters(
        StaffProfessionalMasterType masterType,
        [FromQuery] bool activeOnly,
        [FromQuery] StaffProfessionalPermissionRequestDTO dto,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetStaffProfessionalMastersQuery(masterType, activeOnly, dto), cancellationToken));

    [HttpPost("masters/{masterType}")]
    public async Task<IActionResult> CreateMaster(
        StaffProfessionalMasterType masterType,
        [FromBody] StaffProfessionalMasterRequestDTO dto,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new SaveStaffProfessionalMasterCommand(masterType, dto), cancellationToken));

    [HttpPut("masters/{masterType}/{id:int}")]
    public async Task<IActionResult> UpdateMaster(
        StaffProfessionalMasterType masterType,
        int id,
        [FromBody] StaffProfessionalMasterRequestDTO dto,
        CancellationToken cancellationToken)
    {
        dto.Id = id;
        return Ok(await mediator.Send(new SaveStaffProfessionalMasterCommand(masterType, dto), cancellationToken));
    }

    [HttpDelete("masters/{masterType}/{id:int}")]
    public async Task<IActionResult> DeleteMaster(
        StaffProfessionalMasterType masterType,
        int id,
        [FromQuery] StaffProfessionalDeleteRequestDTO dto,
        CancellationToken cancellationToken)
    {
        dto.Id = id;
        return Ok(await mediator.Send(new DeleteStaffProfessionalMasterCommand(masterType, dto), cancellationToken));
    }

    [HttpGet("positions")]
    public async Task<IActionResult> GetPositions(
        [FromQuery] bool activeOnly,
        [FromQuery] short? staffCategoryId,
        [FromQuery] StaffProfessionalPermissionRequestDTO dto,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetStaffPositionsQuery(activeOnly, staffCategoryId, dto), cancellationToken));

    [HttpPost("positions")]
    public async Task<IActionResult> CreatePosition([FromBody] StaffPositionMasterRequestDTO dto, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new SaveStaffPositionCommand(dto), cancellationToken));

    [HttpPut("positions/{id:int}")]
    public async Task<IActionResult> UpdatePosition(int id, [FromBody] StaffPositionMasterRequestDTO dto, CancellationToken cancellationToken)
    {
        dto.Id = id;
        return Ok(await mediator.Send(new SaveStaffPositionCommand(dto), cancellationToken));
    }

    [HttpDelete("positions/{id:int}")]
    public async Task<IActionResult> DeletePosition(int id, [FromQuery] StaffProfessionalDeleteRequestDTO dto, CancellationToken cancellationToken)
    {
        dto.Id = id;
        return Ok(await mediator.Send(new DeleteStaffPositionCommand(dto), cancellationToken));
    }

    [HttpGet("staff/{staffMemberId}")]
    public async Task<IActionResult> GetStaffProfile(
        string staffMemberId,
        [FromQuery] StaffProfessionalProfileRequestDTO dto,
        CancellationToken cancellationToken)
    {
        dto.StaffMemberId = staffMemberId;
        return Ok(await mediator.Send(new GetStaffProfessionalProfileQuery(dto), cancellationToken));
    }

    [HttpPut("capability-verification/{assignmentId:long}")]
    public async Task<IActionResult> VerifyCapability(
        long assignmentId,
        [FromBody] VerifyStaffCapabilityRequestDTO dto,
        CancellationToken cancellationToken)
    {
        dto.AssignmentId = assignmentId;
        return Ok(await mediator.Send(new VerifyStaffCapabilityCommand(dto), cancellationToken));
    }
}
