using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using skolenza.application.DTOS.Staff;
using skolenza.application.Features.StaffMemberCmd.ProfessionalStructure.Academic;

namespace skolenza.api.Controllers.Staff;

/// <summary>Manages curriculum, lesson planning, reviewed delivery and calculated syllabus progress.</summary>
[ApiController]
[Authorize]
[Route("api/Staff/curriculum-delivery")]
public sealed class StaffCurriculumDeliveryController(IMediator mediator) : ControllerBase
{
    [HttpGet("workflow-statuses")]
    public async Task<IActionResult> GetWorkflowStatuses([FromQuery] CurriculumDeliveryPermissionRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new GetAcademicWorkflowStatusesQuery(dto), token));
    [HttpGet("frameworks")]
    /// <summary>Returns one page; defaults to PageNumber=1 and PageSize=10.</summary>
    public async Task<IActionResult> GetFrameworks([FromQuery] CurriculumDeliveryPagedRequestDTO dto, CancellationToken token)
    {
        return Ok(await mediator.Send(new GetCurriculumFrameworksQuery(dto), token));
    }
    [HttpPost("frameworks")]
    public async Task<IActionResult> CreateFramework([FromBody] CurriculumFrameworkRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new SaveCurriculumFrameworkCommand(dto), token));
    [HttpPut("frameworks/{id:long}")]
    public async Task<IActionResult> UpdateFramework(long id, [FromBody] CurriculumFrameworkRequestDTO dto, CancellationToken token) { dto.Id = id; return Ok(await mediator.Send(new SaveCurriculumFrameworkCommand(dto), token)); }
    [HttpPut("frameworks/{id:long}/review")]
    public async Task<IActionResult> ReviewFramework(long id, [FromBody] WorkflowReviewRequestDTO dto, CancellationToken token)
    {
        dto.Id = id;
        return Ok(await mediator.Send(new ReviewCurriculumFrameworkCommand(dto), token));
    }
    [HttpDelete("frameworks/{id:long}")]
    public async Task<IActionResult> DeleteFramework(
        long id,
        [FromQuery] CurriculumDeliveryPermissionRequestDTO dto,
        CancellationToken token)
    {
        return Ok(await mediator.Send(new DeleteCurriculumFrameworkCommand(id, dto), token));
    }
    [HttpGet("frameworks/{frameworkId:long}/units")]
    /// <summary>Returns one page; defaults to PageNumber=1 and PageSize=10.</summary>
    public async Task<IActionResult> GetUnits(long frameworkId, [FromQuery] CurriculumDeliveryPagedRequestDTO dto, CancellationToken token)
    {
        return Ok(await mediator.Send(new GetCurriculumUnitsQuery(frameworkId, dto), token));
    }
    [HttpPost("units")]
    public async Task<IActionResult> CreateUnit([FromBody] CurriculumUnitRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new SaveCurriculumUnitCommand(dto), token));
    [HttpPut("units/{id:long}")]
    public async Task<IActionResult> UpdateUnit(long id, [FromBody] CurriculumUnitRequestDTO dto, CancellationToken token) { dto.Id = id; return Ok(await mediator.Send(new SaveCurriculumUnitCommand(dto), token)); }
    [HttpDelete("units/{id:long}")]
    public async Task<IActionResult> DeleteUnit(
        long id,
        [FromQuery] CurriculumDeliveryPermissionRequestDTO dto,
        CancellationToken token)
    {
        return Ok(await mediator.Send(new DeleteCurriculumUnitCommand(id, dto), token));
    }
    /// <summary>
    /// Returns an independently paged topic list for an institution-owned unit.
    /// Defaults to PageNumber=1 and PageSize=10; uses the existing View permission pipeline.
    /// </summary>
    [HttpGet("units/{unitId:long}/topics")]
    public async Task<IActionResult> GetTopics(
        long unitId,
        [FromQuery] CurriculumDeliveryPagedRequestDTO dto,
        CancellationToken token)
    {
        return Ok(await mediator.Send(new GetCurriculumTopicsQuery(unitId, dto), token));
    }

    [HttpPost("topics")]
    public async Task<IActionResult> CreateTopic([FromBody] CurriculumTopicRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new SaveCurriculumTopicCommand(dto), token));
    [HttpPut("topics/{id:long}")]
    public async Task<IActionResult> UpdateTopic(long id, [FromBody] CurriculumTopicRequestDTO dto, CancellationToken token) { dto.Id = id; return Ok(await mediator.Send(new SaveCurriculumTopicCommand(dto), token)); }
    [HttpDelete("topics/{id:long}")]
    public async Task<IActionResult> DeleteTopic(
        long id,
        [FromQuery] CurriculumDeliveryPermissionRequestDTO dto,
        CancellationToken token)
    {
        return Ok(await mediator.Send(new DeleteCurriculumTopicCommand(id, dto), token));
    }
    [HttpGet("lesson-plans")]
    /// <summary>Returns one page; defaults to PageNumber=1 and PageSize=10.</summary>
    public async Task<IActionResult> GetLessonPlans([FromQuery] CurriculumDeliveryPagedRequestDTO dto, CancellationToken token)
    {
        return Ok(await mediator.Send(new GetLessonPlansQuery(dto), token));
    }
    [HttpGet("teaching-assignments")]
    public async Task<IActionResult> GetTeachingAssignments([FromQuery] CurriculumDeliveryPermissionRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new GetCurriculumTeachingAssignmentsQuery(dto), token));
    [HttpPost("lesson-plans")]
    public async Task<IActionResult> CreateLessonPlan([FromBody] LessonPlanRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new SaveLessonPlanCommand(dto), token));
    [HttpPut("lesson-plans/{id:long}")]
    public async Task<IActionResult> UpdateLessonPlan(long id, [FromBody] LessonPlanRequestDTO dto, CancellationToken token) { dto.Id = id; return Ok(await mediator.Send(new SaveLessonPlanCommand(dto), token)); }
    [HttpPut("lesson-plans/{id:long}/review")]
    public async Task<IActionResult> ReviewLessonPlan(long id, [FromBody] WorkflowReviewRequestDTO dto, CancellationToken token) { dto.Id = id; return Ok(await mediator.Send(new ReviewLessonPlanCommand(dto), token)); }
    [HttpDelete("lesson-plans/{id:long}")]
    public async Task<IActionResult> DeleteLessonPlan(
        long id,
        [FromQuery] CurriculumDeliveryPermissionRequestDTO dto,
        CancellationToken token)
    {
        return Ok(await mediator.Send(new DeleteLessonPlanCommand(id, dto), token));
    }
    [HttpGet("lesson-deliveries")]
    /// <summary>Returns one page; defaults to PageNumber=1 and PageSize=10.</summary>
    public async Task<IActionResult> GetLessonDeliveries([FromQuery] CurriculumDeliveryPagedRequestDTO dto, CancellationToken token)
    {
        return Ok(await mediator.Send(new GetLessonDeliveriesQuery(dto), token));
    }
    [HttpPost("lesson-deliveries")]
    public async Task<IActionResult> CreateLessonDelivery([FromBody] LessonDeliveryRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new SaveLessonDeliveryCommand(dto), token));
    [HttpPut("lesson-deliveries/{id:long}")]
    public async Task<IActionResult> UpdateLessonDelivery(long id, [FromBody] LessonDeliveryRequestDTO dto, CancellationToken token) { dto.Id = id; return Ok(await mediator.Send(new SaveLessonDeliveryCommand(dto), token)); }
    [HttpPut("lesson-deliveries/{id:long}/review")]
    public async Task<IActionResult> ReviewLessonDelivery(long id, [FromBody] WorkflowReviewRequestDTO dto, CancellationToken token) { dto.Id = id; return Ok(await mediator.Send(new ReviewLessonDeliveryCommand(dto), token)); }
    [HttpDelete("lesson-deliveries/{id:long}")]
    public async Task<IActionResult> DeleteLessonDelivery(
        long id,
        [FromQuery] CurriculumDeliveryPermissionRequestDTO dto,
        CancellationToken token)
    {
        return Ok(await mediator.Send(new DeleteLessonDeliveryCommand(id, dto), token));
    }
    [HttpGet("syllabus-progress")]
    /// <summary>Returns one page; defaults to PageNumber=1 and PageSize=10.</summary>
    public async Task<IActionResult> GetSyllabusProgress([FromQuery] long? courseOfferingId, [FromQuery] CurriculumDeliveryPagedRequestDTO dto, CancellationToken token)
    {
        return Ok(await mediator.Send(new GetSyllabusProgressQuery(courseOfferingId, dto), token));
    }
}
