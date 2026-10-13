using skolenza.application.DTOS.Staff;
using skolenza.application.Features.StaffMemberCmd.ProfessionalStructure.Academic;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.Staff;

/// <summary>Manages institution academic structure and StaffMember academic assignments.</summary>
[ApiController]
[Authorize]
[Route("api/Staff/academic-structure")]
public sealed class StaffAcademicStructureController(IMediator mediator) : ControllerBase
{
    /// <summary>Returns the central teaching-assignment role catalogue.</summary>
    [HttpGet("teaching-assignment-types")]
    public async Task<IActionResult> GetTeachingAssignmentTypes([FromQuery] AcademicStructurePermissionRequestDTO dto, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetTeachingAssignmentTypesQuery(dto), cancellationToken));

    [HttpGet("masters/{masterType}")]
    public async Task<IActionResult> GetMasters(AcademicMasterType masterType, [FromQuery] bool activeOnly, [FromQuery] AcademicStructurePermissionRequestDTO dto, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetAcademicMastersQuery(masterType, activeOnly, dto), cancellationToken));

    [HttpPost("masters/{masterType}")]
    public async Task<IActionResult> CreateMaster(AcademicMasterType masterType, [FromBody] AcademicMasterRequestDTO dto, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new SaveAcademicMasterCommand(masterType, dto), cancellationToken));

    [HttpPut("masters/{masterType}/{id:long}")]
    public async Task<IActionResult> UpdateMaster(AcademicMasterType masterType, long id, [FromBody] AcademicMasterRequestDTO dto, CancellationToken cancellationToken)
    {
        dto.Id = id;
        return Ok(await mediator.Send(new SaveAcademicMasterCommand(masterType, dto), cancellationToken));
    }

    [HttpDelete("masters/{masterType}/{id:long}")]
    public async Task<IActionResult> DeleteMaster(AcademicMasterType masterType, long id, [FromQuery] AcademicMasterDeleteRequestDTO dto, CancellationToken cancellationToken)
    {
        dto.Id = id;
        return Ok(await mediator.Send(new DeleteAcademicMasterCommand(masterType, dto), cancellationToken));
    }

    [HttpGet("staff/{staffMemberId}")]
    public async Task<IActionResult> GetStaffProfile(string staffMemberId, [FromQuery] StaffAcademicProfileRequestDTO dto, CancellationToken cancellationToken)
    {
        dto.StaffMemberId = staffMemberId;
        return Ok(await mediator.Send(new GetStaffAcademicProfileQuery(dto), cancellationToken));
    }

    [HttpPost("teaching-assignments")]
    public async Task<IActionResult> CreateTeachingAssignment([FromBody] StaffTeachingAssignmentRequestDTO dto, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new SaveStaffTeachingAssignmentCommand(dto), cancellationToken));

    [HttpPut("teaching-assignments/{id:long}")]
    public async Task<IActionResult> UpdateTeachingAssignment(long id, [FromBody] StaffTeachingAssignmentRequestDTO dto, CancellationToken cancellationToken)
    {
        dto.Id = id;
        return Ok(await mediator.Send(new SaveStaffTeachingAssignmentCommand(dto), cancellationToken));
    }

    [HttpDelete("teaching-assignments/{id:long}")]
    public async Task<IActionResult> DeleteTeachingAssignment(long id, [FromQuery] AcademicStructurePermissionRequestDTO dto, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new DeleteStaffTeachingAssignmentCommand(id, dto), cancellationToken));

    [HttpPost("class-teacher-assignments")]
    public async Task<IActionResult> CreateClassTeacherAssignment([FromBody] ClassTeacherAssignmentRequestDTO dto, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new SaveClassTeacherAssignmentCommand(dto), cancellationToken));

    [HttpPut("class-teacher-assignments/{id:long}")]
    public async Task<IActionResult> UpdateClassTeacherAssignment(long id, [FromBody] ClassTeacherAssignmentRequestDTO dto, CancellationToken cancellationToken)
    {
        dto.Id = id;
        return Ok(await mediator.Send(new SaveClassTeacherAssignmentCommand(dto), cancellationToken));
    }

    [HttpDelete("class-teacher-assignments/{id:long}")]
    public async Task<IActionResult> DeleteClassTeacherAssignment(long id, [FromQuery] AcademicStructurePermissionRequestDTO dto, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new DeleteClassTeacherAssignmentCommand(id, dto), cancellationToken));
}
