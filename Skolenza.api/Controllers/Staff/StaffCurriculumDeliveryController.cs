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
    public async Task<IActionResult> GetFrameworks([FromQuery] CurriculumDeliveryPermissionRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new GetCurriculumFrameworksQuery(dto), token));
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
    [HttpGet("frameworks/{frameworkId:long}/units")]
    public async Task<IActionResult> GetUnits(long frameworkId, [FromQuery] CurriculumDeliveryPermissionRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new GetCurriculumUnitsQuery(frameworkId, dto), token));
    [HttpPost("units")]
    public async Task<IActionResult> CreateUnit([FromBody] CurriculumUnitRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new SaveCurriculumUnitCommand(dto), token));
    [HttpPut("units/{id:long}")]
    public async Task<IActionResult> UpdateUnit(long id, [FromBody] CurriculumUnitRequestDTO dto, CancellationToken token) { dto.Id = id; return Ok(await mediator.Send(new SaveCurriculumUnitCommand(dto), token)); }
    [HttpPost("topics")]
    public async Task<IActionResult> CreateTopic([FromBody] CurriculumTopicRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new SaveCurriculumTopicCommand(dto), token));
    [HttpPut("topics/{id:long}")]
    public async Task<IActionResult> UpdateTopic(long id, [FromBody] CurriculumTopicRequestDTO dto, CancellationToken token) { dto.Id = id; return Ok(await mediator.Send(new SaveCurriculumTopicCommand(dto), token)); }
    [HttpGet("lesson-plans")]
    public async Task<IActionResult> GetLessonPlans([FromQuery] CurriculumDeliveryPermissionRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new GetLessonPlansQuery(dto), token));
    [HttpGet("teaching-assignments")]
    public async Task<IActionResult> GetTeachingAssignments([FromQuery] CurriculumDeliveryPermissionRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new GetCurriculumTeachingAssignmentsQuery(dto), token));
    [HttpPost("lesson-plans")]
    public async Task<IActionResult> CreateLessonPlan([FromBody] LessonPlanRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new SaveLessonPlanCommand(dto), token));
    [HttpPut("lesson-plans/{id:long}")]
    public async Task<IActionResult> UpdateLessonPlan(long id, [FromBody] LessonPlanRequestDTO dto, CancellationToken token) { dto.Id = id; return Ok(await mediator.Send(new SaveLessonPlanCommand(dto), token)); }
    [HttpPut("lesson-plans/{id:long}/review")]
    public async Task<IActionResult> ReviewLessonPlan(long id, [FromBody] WorkflowReviewRequestDTO dto, CancellationToken token) { dto.Id = id; return Ok(await mediator.Send(new ReviewLessonPlanCommand(dto), token)); }
    [HttpGet("lesson-deliveries")]
    public async Task<IActionResult> GetLessonDeliveries([FromQuery] CurriculumDeliveryPermissionRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new GetLessonDeliveriesQuery(dto), token));
    [HttpPost("lesson-deliveries")]
    public async Task<IActionResult> CreateLessonDelivery([FromBody] LessonDeliveryRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new SaveLessonDeliveryCommand(dto), token));
    [HttpPut("lesson-deliveries/{id:long}")]
    public async Task<IActionResult> UpdateLessonDelivery(long id, [FromBody] LessonDeliveryRequestDTO dto, CancellationToken token) { dto.Id = id; return Ok(await mediator.Send(new SaveLessonDeliveryCommand(dto), token)); }
    [HttpPut("lesson-deliveries/{id:long}/review")]
    public async Task<IActionResult> ReviewLessonDelivery(long id, [FromBody] WorkflowReviewRequestDTO dto, CancellationToken token) { dto.Id = id; return Ok(await mediator.Send(new ReviewLessonDeliveryCommand(dto), token)); }
    [HttpGet("syllabus-progress")]
    public async Task<IActionResult> GetSyllabusProgress([FromQuery] long? courseOfferingId, [FromQuery] CurriculumDeliveryPermissionRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new GetSyllabusProgressQuery(courseOfferingId, dto), token));
}
