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
    /// <summary>Used-In-Angular: returns institution-owned Staff Categories and dependency counts; defaults to page 1 / 10 records.</summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(
        [FromQuery] bool activeOnly,
        [FromQuery] StaffProfessionalPagedRequestDTO dto,
        CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new GetInstitutionStaffCategoriesQuery(activeOnly, dto), cancellationToken));
    }

    /// <summary>Used-In-Angular: creates an institution-owned Staff Category.</summary>
    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] InstitutionStaffCategoryRequestDTO dto, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new SaveInstitutionStaffCategoryCommand(dto), cancellationToken));

    /// <summary>Used-In-Angular: updates an institution-owned Staff Category.</summary>
    [HttpPut("categories/{id:int}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] InstitutionStaffCategoryRequestDTO dto, CancellationToken cancellationToken)
    {
        dto.Id = id;
        return Ok(await mediator.Send(new SaveInstitutionStaffCategoryCommand(dto), cancellationToken));
    }

    /// <summary>Used-In-Angular: replaces the active Job Family mappings for one Staff Category.</summary>
    [HttpPut("categories/{id:int}/job-families")]
    public async Task<IActionResult> SetCategoryJobFamilies(int id, [FromBody] StaffCategoryJobFamilyRequestDTO dto, CancellationToken cancellationToken)
    {
        dto.InstitutionStaffCategoryMasterId = id;
        return Ok(await mediator.Send(new SetStaffCategoryJobFamiliesCommand(dto), cancellationToken));
    }

    /// <summary>Used-In-Angular: dependency-safe Staff Category activation or deactivation.</summary>
    [HttpPut("categories/{id:int}/active/{isActive:bool}")]
    public async Task<IActionResult> SetCategoryActive(
        int id,
        bool isActive,
        [FromBody] StaffProfessionalPermissionRequestDTO dto,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new SetInstitutionStaffCategoryActiveCommand(id, isActive, dto), cancellationToken));

    /// <summary>Returns a tenant-scoped page, default PageNumber=1 and PageSize=10.</summary>
    [HttpGet("masters/{masterType}")]
    public async Task<IActionResult> GetMasters(
        StaffProfessionalMasterType masterType,
        [FromQuery] bool activeOnly,
        [FromQuery] StaffProfessionalPagedRequestDTO dto,
        CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new GetStaffProfessionalMastersQuery(masterType, activeOnly, dto), cancellationToken));
    }

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

    /// <summary>Used-In-Angular: dependency-safe deactivation; inactive records remain editable and can be reactivated.</summary>
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

    /// <summary>Returns a tenant-scoped page, default PageNumber=1 and PageSize=10.</summary>
    [HttpGet("positions")]
    public async Task<IActionResult> GetPositions(
        [FromQuery] bool activeOnly,
        [FromQuery] int? institutionStaffCategoryMasterId,
        [FromQuery] StaffProfessionalPagedRequestDTO dto,
        CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new GetStaffPositionsQuery(activeOnly, institutionStaffCategoryMasterId, dto), cancellationToken));
    }

    [HttpPost("positions")]
    public async Task<IActionResult> CreatePosition([FromBody] StaffPositionMasterRequestDTO dto, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new SaveStaffPositionCommand(dto), cancellationToken));

    [HttpPut("positions/{id:int}")]
    public async Task<IActionResult> UpdatePosition(int id, [FromBody] StaffPositionMasterRequestDTO dto, CancellationToken cancellationToken)
    {
        dto.Id = id;
        return Ok(await mediator.Send(new SaveStaffPositionCommand(dto), cancellationToken));
    }

    /// <summary>Used-In-Angular: dependency-safe deactivation; inactive positions remain editable and can be reactivated.</summary>
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
