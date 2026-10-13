using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using skolenza.application.DTOS.Staff;
using skolenza.application.Features.StaffMemberCmd.ProfessionalStructure.Academic;

namespace skolenza.api.Controllers.Staff;

/// <summary>Manages institution timetable, substitute workflow and staff communication.</summary>
[ApiController]
[Authorize]
[Route("api/Staff/operations")]
public sealed class StaffTimetableAndCommunicationController(IMediator mediator) : ControllerBase
{
    [HttpGet("options")]
    public async Task<IActionResult> GetOptions(
        [FromQuery] StaffOperationsPermissionRequestDTO dto,
        CancellationToken token) =>
        Ok(await mediator.Send(new GetStaffOperationsOptionsQuery(dto), token));
    [HttpGet("timetable/periods")]
    public async Task<IActionResult> GetPeriods([FromQuery] StaffOperationsPermissionRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new GetTimetablePeriodsQuery(dto), token));
    [HttpPost("timetable/periods")]
    public async Task<IActionResult> CreatePeriod([FromBody] TimetablePeriodRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new SaveTimetablePeriodCommand(dto), token));
    [HttpPut("timetable/periods/{id:long}")]
    public async Task<IActionResult> UpdatePeriod(long id, [FromBody] TimetablePeriodRequestDTO dto, CancellationToken token) { dto.Id = id; return Ok(await mediator.Send(new SaveTimetablePeriodCommand(dto), token)); }
    [HttpGet("timetable/entries")]
    public async Task<IActionResult> GetEntries([FromQuery] StaffOperationsPermissionRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new GetTimetableEntriesQuery(dto), token));
    [HttpPost("timetable/entries")]
    public async Task<IActionResult> CreateEntry([FromBody] TimetableEntryRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new SaveTimetableEntryCommand(dto), token));
    [HttpPut("timetable/entries/{id:long}")]
    public async Task<IActionResult> UpdateEntry(long id, [FromBody] TimetableEntryRequestDTO dto, CancellationToken token) { dto.Id = id; return Ok(await mediator.Send(new SaveTimetableEntryCommand(dto), token)); }
    [HttpGet("substitute-workflows")]
    public async Task<IActionResult> GetWorkflows([FromQuery] StaffOperationsPermissionRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new GetSubstituteWorkflowsQuery(dto), token));
    [HttpPost("substitute-workflows")]
    public async Task<IActionResult> CreateWorkflow([FromBody] SubstituteWorkflowRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new SaveSubstituteWorkflowCommand(dto), token));
    [HttpPut("substitute-workflows/{id:long}")]
    public async Task<IActionResult> UpdateWorkflow(long id, [FromBody] SubstituteWorkflowRequestDTO dto, CancellationToken token) { dto.Id = id; return Ok(await mediator.Send(new SaveSubstituteWorkflowCommand(dto), token)); }
    [HttpGet("absence-impacts")]
    public async Task<IActionResult> GetAbsenceImpacts(
        [FromQuery] StaffOperationsPermissionRequestDTO dto,
        CancellationToken token) =>
        Ok(await mediator.Send(new GetStaffAbsenceImpactsQuery(dto), token));
    [HttpPost("absence-impacts/leave/{leaveRequestId:long}/synchronize")]
    public async Task<IActionResult> SynchronizeLeaveImpacts(
        long leaveRequestId,
        [FromBody] StaffOperationsPermissionRequestDTO dto,
        CancellationToken token) =>
        Ok(await mediator.Send(new SynchronizeLeaveImpactsCommand(leaveRequestId, dto), token));
    [HttpPost("substitute-assignments")]
    public async Task<IActionResult> AssignSubstitute(
        [FromBody] SubstituteAssignmentRequestDTO dto,
        CancellationToken token) =>
        Ok(await mediator.Send(new AssignSubstituteCommand(dto), token));
    [HttpPost("substitute-assignments/{id:long}/actions")]
    public async Task<IActionResult> ActOnSubstitute(
        long id,
        [FromBody] SubstituteActionRequestDTO dto,
        CancellationToken token) =>
        Ok(await mediator.Send(new ActOnSubstituteCommand(id, dto), token));
    [HttpGet("communication/settings")]
    public async Task<IActionResult> GetCommunicationSetting([FromQuery] StaffOperationsPermissionRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new GetCommunicationSettingQuery(dto), token));
    [HttpPut("communication/settings")]
    public async Task<IActionResult> SaveCommunicationSetting([FromBody] CommunicationSettingDTO dto, CancellationToken token) => Ok(await mediator.Send(new SaveCommunicationSettingCommand(dto), token));
    [HttpGet("communication/query-categories")]
    public async Task<IActionResult> GetQueryCategories(
        [FromQuery] StaffOperationsPermissionRequestDTO dto,
        [FromQuery] bool activeOnly,
        CancellationToken token) =>
        Ok(await mediator.Send(new GetStaffQueryCategoriesQuery(dto, activeOnly), token));
    [HttpPost("communication/query-categories")]
    public async Task<IActionResult> CreateQueryCategory(
        [FromBody] StaffQueryCategoryRequestDTO dto,
        CancellationToken token) =>
        Ok(await mediator.Send(new SaveStaffQueryCategoryCommand(dto), token));
    [HttpPut("communication/query-categories/{id:long}")]
    public async Task<IActionResult> UpdateQueryCategory(
        long id,
        [FromBody] StaffQueryCategoryRequestDTO dto,
        CancellationToken token)
    {
        dto.Id = id;
        return Ok(await mediator.Send(new SaveStaffQueryCategoryCommand(dto), token));
    }
    [HttpDelete("communication/query-categories/{id:long}")]
    public async Task<IActionResult> DeactivateQueryCategory(
        long id,
        [FromQuery] StaffOperationsPermissionRequestDTO dto,
        CancellationToken token) =>
        Ok(await mediator.Send(new DeactivateStaffQueryCategoryCommand(id, dto), token));
    [HttpGet("communication/conversations")]
    public async Task<IActionResult> GetConversations([FromQuery] StaffOperationsPermissionRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new GetStaffConversationsQuery(dto), token));
    [HttpGet("communication/conversations/{id:long}")]
    public async Task<IActionResult> GetConversation(
        long id,
        [FromQuery] StaffOperationsPermissionRequestDTO dto,
        CancellationToken token) =>
        Ok(await mediator.Send(new GetStaffConversationQuery(id, dto), token));
    [HttpPost("communication/conversations")]
    public async Task<IActionResult> CreateConversation([FromBody] StaffConversationRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new CreateStaffConversationCommand(dto), token));
    [HttpPost("communication/conversations/{id:long}/messages")]
    public async Task<IActionResult> AddMessage(long id, [FromBody] StaffMessageRequestDTO dto, CancellationToken token) => Ok(await mediator.Send(new AddStaffConversationMessageCommand(id, dto), token));
    [HttpPost("communication/conversations/{id:long}/read")]
    public async Task<IActionResult> MarkConversationRead(
        long id,
        [FromBody] StaffOperationsPermissionRequestDTO dto,
        CancellationToken token) =>
        Ok(await mediator.Send(new MarkStaffConversationReadCommand(id, dto), token));
    [HttpPost("communication/conversations/{id:long}/close")]
    public async Task<IActionResult> CloseConversation(
        long id,
        [FromBody] StaffOperationsPermissionRequestDTO dto,
        CancellationToken token) =>
        Ok(await mediator.Send(new CloseStaffConversationCommand(id, dto), token));
}
