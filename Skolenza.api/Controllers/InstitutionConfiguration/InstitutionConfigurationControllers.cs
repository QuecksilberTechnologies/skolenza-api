// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Exposes authenticated InstitutionConfiguration endpoint groups.
// ================================================================

using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.InstitutionConfiguration;
using skolenza.application.Features.StaffMemberCmd.StaffMemberDeviceEnrollment.Handlers;
using skolenza.application.Features.StaffMemberCmd.StaffMemberWorkInfo.Handlers;
using skolenza.application.Features.InstitutionConfigurationCmd.Handlers;
using skolenza.application.Interfaces.ILogger;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.InstitutionConfiguration;

/// <summary>Provides common dependencies for InstitutionConfiguration endpoint groups.</summary>
[Authorize]
[ApiController]
public abstract class InstitutionConfigurationControllerBase(IMediator mediator, ILoggerService logger) : ControllerBase
{
    /// <summary>Dispatches InstitutionConfiguration requests.</summary>
    protected IMediator Mediator { get; } = mediator;
    /// <summary>Records InstitutionConfiguration endpoint activity.</summary>
    protected ILoggerService Logger { get; } = logger;
}

/// <summary>Exposes Institution campus configuration endpoints.</summary>
[Route("api/Campus")]
public sealed class CampusController(IMediator mediator, ILoggerService logger) : InstitutionConfigurationControllerBase(mediator, logger)
{
    #region Institution Location CRUD
    [HttpPost("create")] public async Task<IActionResult> Create([FromBody] CreateCampusRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received Campus create request."); return Ok(await Mediator.Send(new CreateCampusCommand(dto), ct)); }
    [HttpGet("get-by-id/{id:long}")] public async Task<IActionResult> GetById(long id, [FromQuery] CampusAccessRequestDTO accessRequest, CancellationToken ct) { Logger.LogInfo("Received Campus get-by-id request."); return Ok(await Mediator.Send(new GetCampusByIdQuery(id, accessRequest), ct)); }
    [HttpGet("get-all")] public async Task<IActionResult> GetAll([FromQuery] CampusFilterRequestDTO filter, CancellationToken ct) { Logger.LogInfo("Received Campus list request."); return Ok(await Mediator.Send(new GetCampussQuery(filter), ct)); }
    [HttpPost("update")] public async Task<IActionResult> Update([FromBody] UpdateCampusRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received Campus update request."); return Ok(await Mediator.Send(new UpdateCampusCommand(dto), ct)); }
    [HttpPost("update-status")] public async Task<IActionResult> UpdateStatus([FromBody] UpdateCampusStatusRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received Campus status request."); return Ok(await Mediator.Send(new UpdateCampusStatusCommand(dto), ct)); }
    [HttpDelete("delete/{id:long}")] public async Task<IActionResult> Delete(long id, [FromQuery] CampusAccessRequestDTO accessRequest, CancellationToken ct) { Logger.LogInfo("Received Campus delete request."); return Ok(await Mediator.Send(new DeleteCampusCommand(id, accessRequest), ct)); }
    #endregion
}

#if false // LEGACY POLICY API: incompatible old AttendancePolicy surface; use InstitutionPolicyController.
/// <summary>Exposes attendance-policy configuration endpoints.</summary>
[Route("api/[controller]")]
public sealed class AttendancePolicyController(IMediator mediator, ILoggerService logger) : InstitutionConfigurationControllerBase(mediator, logger)
{
    #region Attendance Policy CRUD
    [HttpPost("create")] public async Task<IActionResult> Create([FromBody] CreateAttendancePolicyRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received AttendancePolicy create request."); return Ok(await Mediator.Send(new CreateAttendancePolicyCommand(dto), ct)); }
    [HttpGet("get-by-id/{id:int}")] public async Task<IActionResult> GetById(int id, CancellationToken ct) { Logger.LogInfo("Received AttendancePolicy get-by-id request."); return Ok(await Mediator.Send(new GetAttendancePolicyByIdQuery(id), ct)); }
    [HttpGet("get-all")] public async Task<IActionResult> GetAll([FromQuery] AttendancePolicyFilterRequestDTO filter, CancellationToken ct) { Logger.LogInfo("Received AttendancePolicy list request."); return Ok(await Mediator.Send(new GetAttendancePoliciesQuery(filter), ct)); }
    [HttpPost("update")] public async Task<IActionResult> Update([FromBody] UpdateAttendancePolicyRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received AttendancePolicy update request."); return Ok(await Mediator.Send(new UpdateAttendancePolicyCommand(dto), ct)); }
    [HttpPost("update-status")] public async Task<IActionResult> UpdateStatus([FromBody] UpdateAttendancePolicyStatusRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received AttendancePolicy status request."); return Ok(await Mediator.Send(new UpdateAttendancePolicyStatusCommand(dto), ct)); }
    [HttpDelete("delete/{id:int}")] public async Task<IActionResult> Delete(int id, CancellationToken ct) { Logger.LogInfo("Received AttendancePolicy delete request."); return Ok(await Mediator.Send(new DeleteAttendancePolicyCommand(id), ct)); }
    #endregion
}
#endif

/// <summary>Exposes staffMember-location-assignment configuration endpoints.</summary>
[Route("api/StaffCampusAssignment")]
public sealed class StaffCampusAssignmentController(IMediator mediator, ILoggerService logger) : InstitutionConfigurationControllerBase(mediator, logger)
{
    #region StaffMember Location Assignment CRUD
    [HttpPost("create")] public async Task<IActionResult> Create([FromBody] CreateStaffMemberLocationAssignmentRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received StaffMemberLocationAssignment create request."); return Ok(await Mediator.Send(new CreateStaffMemberLocationAssignmentCommand(dto), ct)); }
    [HttpGet("get-by-id/{id:long}")] public async Task<IActionResult> GetById(long id, [FromQuery] PermissionRequestDTO permissionRequest, CancellationToken ct) { Logger.LogInfo("Received StaffMemberLocationAssignment get-by-id request."); return Ok(await Mediator.Send(new GetStaffMemberLocationAssignmentByIdQuery(id, permissionRequest), ct)); }
    [HttpGet("get-all")] public async Task<IActionResult> GetAll([FromQuery] StaffMemberLocationAssignmentFilterRequestDTO filter, CancellationToken ct) { Logger.LogInfo("Received StaffMemberLocationAssignment list request."); return Ok(await Mediator.Send(new GetStaffMemberLocationAssignmentsQuery(filter), ct)); }
    [HttpPost("update")] public async Task<IActionResult> Update([FromBody] UpdateStaffMemberLocationAssignmentRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received StaffMemberLocationAssignment update request."); return Ok(await Mediator.Send(new UpdateStaffMemberLocationAssignmentCommand(dto), ct)); }
    [HttpPost("update-status")] public async Task<IActionResult> UpdateStatus([FromBody] UpdateStaffMemberLocationAssignmentStatusRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received StaffMemberLocationAssignment status request."); return Ok(await Mediator.Send(new UpdateStaffMemberLocationAssignmentStatusCommand(dto), ct)); }
    [HttpDelete("delete/{id:long}")] public async Task<IActionResult> Delete(long id, [FromQuery] PermissionRequestDTO permissionRequest, CancellationToken ct) { Logger.LogInfo("Received StaffMemberLocationAssignment delete request."); return Ok(await Mediator.Send(new DeleteStaffMemberLocationAssignmentCommand(id, permissionRequest), ct)); }
    #endregion
}

/// <summary>Exposes staff member device enrollment configuration endpoints.</summary>
[Route("api/StaffDeviceEnrollment")]
public sealed class StaffDeviceEnrollmentController(IMediator mediator, ILoggerService logger) : InstitutionConfigurationControllerBase(mediator, logger)
{
    #region StaffMember Device Enrollment CRUD
    /// <summary>Creates the staffMember-to-device mapping required before face, card, or PIN can be sent to a physical device.</summary>
    [HttpPost("create")] public async Task<IActionResult> Create([FromBody] CreateStaffMemberDeviceEnrollmentRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received StaffMemberDeviceEnrollment create request."); return Ok(await Mediator.Send(new CreateStaffMemberDeviceEnrollmentCommand(dto), ct)); }
    /// <summary>Reads one safe device-user view so Angular can refresh credential and command-confirmation status without exposing device enroll IDs.</summary>
    [HttpGet("get-by-id/{id}")] public async Task<IActionResult> GetById(string id, [FromQuery] PermissionRequestDTO permissionRequest, CancellationToken ct) { Logger.LogInfo("Received StaffMemberDeviceEnrollment get-by-id request."); return Ok(await Mediator.Send(new GetStaffMemberDeviceEnrollmentByIdQuery(id, permissionRequest), ct)); }
    /// <summary>Lists staffMember device users for the device-user screen and filtered refreshes.</summary>
    [HttpGet("get-all")] public async Task<IActionResult> GetAll([FromQuery] StaffMemberDeviceEnrollmentFilterRequestDTO filter, CancellationToken ct) { Logger.LogInfo("Received StaffMemberDeviceEnrollment list request."); return Ok(await Mediator.Send(new GetStaffMemberDeviceEnrollmentsQuery(filter), ct)); }
    /// <summary>Changes only staffMember-specific access validity and working windows; it deliberately cannot enable or disable a physical device user.</summary>
    [HttpPost("update")] public async Task<IActionResult> Update([FromBody] UpdateStaffMemberDeviceEnrollmentRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received StaffMemberDeviceEnrollment update request."); return Ok(await Mediator.Send(new UpdateStaffMemberDeviceEnrollmentCommand(dto), ct)); }
    /// <summary>Enables or disables the existing device user through the durable queue, keeping face, card, and PIN credentials intact.</summary>
    [HttpPost("update-status")] public async Task<IActionResult> UpdateStatus([FromBody] UpdateStaffMemberDeviceEnrollmentStatusRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received StaffMemberDeviceEnrollment status request."); return Ok(await Mediator.Send(new UpdateStaffMemberDeviceEnrollmentStatusCommand(dto), ct)); }
    /// <summary>Queues a selected Angular JPEG or PNG face file for an already enrolled staffMember; raw biometric data is never persisted in the business table.</summary>
    [HttpPost("face/upsert")] [Consumes("multipart/form-data")] public async Task<IActionResult> UpsertFace([FromForm] UpsertStaffMemberDeviceFaceRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received staffMember device face enrollment request."); return Ok(await Mediator.Send(new UpsertStaffMemberDeviceFaceCommand(dto), ct)); }
    /// <summary>Queues a write-only staffMember PIN for an existing device user; use this instead of storing or reading a PIN in Angular.</summary>
    [HttpPost("pin/upsert")] public async Task<IActionResult> UpsertPin([FromBody] UpsertStaffMemberDevicePinRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received staffMember device PIN enrollment request."); return Ok(await Mediator.Send(new UpsertStaffMemberDevicePinCommand(dto), ct)); }
    /// <summary>Binds one Host-issued Available card to the staffMember device user and queues the physical card enrollment.</summary>
    [HttpPost("card/bind")] public async Task<IActionResult> BindCard([FromBody] BindStaffMemberDeviceCardRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received staffMember device card bind request."); return Ok(await Mediator.Send(new BindStaffMemberDeviceCardCommand(dto), ct)); }
    /// <summary>Queues removal of exactly one staff member credential, preserving the other credentials and the staff member-device mapping.</summary>
    [HttpPost("credential/remove")] public async Task<IActionResult> RemoveCredential([FromBody] RemoveStaffMemberDeviceCredentialRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received staffMember device credential removal request."); return Ok(await Mediator.Send(new RemoveStaffMemberDeviceCredentialCommand(dto), ct)); }
    /// <summary>Queues complete removal of a staff member from one physical device and soft-deletes only that staff member-device mapping.</summary>
    [HttpDelete("delete/{id}")] public async Task<IActionResult> Delete(string id, [FromQuery] PermissionRequestDTO permissionRequest, CancellationToken ct) { Logger.LogInfo("Received StaffMemberDeviceEnrollment delete request."); return Ok(await Mediator.Send(new DeleteStaffMemberDeviceEnrollmentCommand(id, permissionRequest), ct)); }
    #endregion
}

/// <summary>Exposes staffMember work-arrangement configuration endpoints.</summary>
[Route("api/StaffWorkArrangement")]
public sealed class StaffWorkArrangementController(IMediator mediator, ILoggerService logger) : InstitutionConfigurationControllerBase(mediator, logger)
{
    #region StaffMember Work Arrangement Dropdowns
    /// <summary>Gets effective Published Attendance policy versions for the authenticated Institution.</summary>
    /// <remarks>
    /// This read-only dropdown endpoint validates the bearer token and trusted Institution context only;
    /// it does not require ModuleId or OperationId. The category is resolved by the stable
    /// ATTENDANCE category code, and institution-generated Policy Type identifiers are never hard-coded.
    /// EffectiveOn is optional and defaults to the server's current UTC date. The UI should send
    /// the Work Arrangement Effective From value automatically when the user selects it. For each
    /// policy, the highest Published version whose effective window contains that date is returned.
    /// An empty successful response tells the UI to ask the user to create and publish an Attendance policy first.
    /// </remarks>
    [HttpGet("attendance-policy-options")]
    public async Task<IActionResult> GetAttendancePolicyOptions(
        [FromQuery] AttendancePolicyOptionRequestDTO request,
        CancellationToken cancellationToken)
    {
        Logger.LogInfo("Received StaffMemberWorkArrangement attendance-policy-options request.");
        return Ok(await Mediator.Send(new GetAttendancePolicyOptionsQuery(request), cancellationToken));
    }
    #endregion

    #region StaffMember Work Arrangement CRUD
    /// <summary>Creates the staffMember's long-running Office, Remote, Hybrid, or WFH attendance arrangement.</summary>
    [HttpPost("create")] public async Task<IActionResult> Create([FromBody] CreateStaffMemberWorkArrangementRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received StaffMemberWorkArrangement create request."); return Ok(await Mediator.Send(new CreateStaffMemberWorkArrangementCommand(dto), ct)); }
    /// <summary>Reads one staffMember work arrangement for edit or detail display.</summary>
    [HttpGet("get-by-id/{id:long}")] public async Task<IActionResult> GetById(long id, [FromQuery] PermissionRequestDTO permissionRequest, CancellationToken ct) { Logger.LogInfo("Received StaffMemberWorkArrangement get-by-id request."); return Ok(await Mediator.Send(new GetStaffMemberWorkArrangementByIdQuery(id, permissionRequest), ct)); }
    /// <summary>Lists work arrangements for staffMember profile and institution administration screens.</summary>
    [HttpGet("get-all")] public async Task<IActionResult> GetAll([FromQuery] StaffMemberWorkArrangementFilterRequestDTO filter, CancellationToken ct) { Logger.LogInfo("Received StaffMemberWorkArrangement list request."); return Ok(await Mediator.Send(new GetStaffMemberWorkArrangementsQuery(filter), ct)); }
    /// <summary>Changes an existing staffMember work arrangement while retaining its server-owned audit information.</summary>
    [HttpPost("update")] public async Task<IActionResult> Update([FromBody] UpdateStaffMemberWorkArrangementRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received StaffMemberWorkArrangement update request."); return Ok(await Mediator.Send(new UpdateStaffMemberWorkArrangementCommand(dto), ct)); }
    /// <summary>Activates or deactivates an arrangement without deleting its historical record.</summary>
    [HttpPost("update-status")] public async Task<IActionResult> UpdateStatus([FromBody] UpdateStaffMemberWorkArrangementStatusRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received StaffMemberWorkArrangement status request."); return Ok(await Mediator.Send(new UpdateStaffMemberWorkArrangementStatusCommand(dto), ct)); }
    /// <summary>Soft-deletes a work arrangement when it is no longer valid for the staffMember.</summary>
    [HttpDelete("delete/{id:long}")] public async Task<IActionResult> Delete(long id, [FromQuery] PermissionRequestDTO permissionRequest, CancellationToken ct) { Logger.LogInfo("Received StaffMemberWorkArrangement delete request."); return Ok(await Mediator.Send(new DeleteStaffMemberWorkArrangementCommand(id, permissionRequest), ct)); }
    #endregion
}

/// <summary>Exposes staffMember work-pattern configuration endpoints.</summary>
[Route("api/StaffWorkPattern")]
public sealed class StaffWorkPatternController(IMediator mediator, ILoggerService logger) : InstitutionConfigurationControllerBase(mediator, logger)
{
    #region StaffMember Work Pattern CRUD
    /// <summary>Creates a weekday-specific work pattern beneath a staff member's main work arrangement.</summary>
    [HttpPost("create")] public async Task<IActionResult> Create([FromBody] CreateStaffMemberWorkPatternRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received StaffMemberWorkPattern create request."); return Ok(await Mediator.Send(new CreateStaffMemberWorkPatternCommand(dto), ct)); }
    /// <summary>Reads one weekday work pattern for edit or detail display.</summary>
    [HttpGet("get-by-id/{id:long}")] public async Task<IActionResult> GetById(long id, [FromQuery] PermissionRequestDTO permissionRequest, CancellationToken ct) { Logger.LogInfo("Received StaffMemberWorkPattern get-by-id request."); return Ok(await Mediator.Send(new GetStaffMemberWorkPatternByIdQuery(id, permissionRequest), ct)); }
    /// <summary>Lists weekday patterns used to render a staff member's recurring schedule.</summary>
    [HttpGet("get-all")] public async Task<IActionResult> GetAll([FromQuery] StaffMemberWorkPatternFilterRequestDTO filter, CancellationToken ct) { Logger.LogInfo("Received StaffMemberWorkPattern list request."); return Ok(await Mediator.Send(new GetStaffMemberWorkPatternsQuery(filter), ct)); }
    /// <summary>Updates a weekday work pattern without changing the parent arrangement.</summary>
    [HttpPost("update")] public async Task<IActionResult> Update([FromBody] UpdateStaffMemberWorkPatternRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received StaffMemberWorkPattern update request."); return Ok(await Mediator.Send(new UpdateStaffMemberWorkPatternCommand(dto), ct)); }
    /// <summary>Activates or deactivates a weekday pattern without deleting schedule history.</summary>
    [HttpPost("update-status")] public async Task<IActionResult> UpdateStatus([FromBody] UpdateStaffMemberWorkPatternStatusRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received StaffMemberWorkPattern status request."); return Ok(await Mediator.Send(new UpdateStaffMemberWorkPatternStatusCommand(dto), ct)); }
    /// <summary>Soft-deletes a weekday pattern that should no longer be applied.</summary>
    [HttpDelete("delete/{id:long}")] public async Task<IActionResult> Delete(long id, [FromQuery] PermissionRequestDTO permissionRequest, CancellationToken ct) { Logger.LogInfo("Received StaffMemberWorkPattern delete request."); return Ok(await Mediator.Send(new DeleteStaffMemberWorkPatternCommand(id, permissionRequest), ct)); }
    #endregion
}

/// <summary>Exposes staffMember temporary work-mode override endpoints.</summary>
[Route("api/StaffWorkModeOverride")]
public sealed class StaffWorkModeOverrideController(IMediator mediator, ILoggerService logger) : InstitutionConfigurationControllerBase(mediator, logger)
{
    #region StaffMember Work Mode Override CRUD
    /// <summary>Creates a temporary staffMember Office, Remote, Hybrid, or WFH deviation for a defined date range.</summary>
    [HttpPost("create")] public async Task<IActionResult> Create([FromBody] CreateStaffMemberWorkModeOverrideRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received StaffMemberWorkModeOverride create request."); return Ok(await Mediator.Send(new CreateStaffMemberWorkModeOverrideCommand(dto), ct)); }
    /// <summary>Reads one temporary work-mode override together with its server-owned approval state.</summary>
    [HttpGet("get-by-id/{id:long}")] public async Task<IActionResult> GetById(long id, [FromQuery] PermissionRequestDTO permissionRequest, CancellationToken ct) { Logger.LogInfo("Received StaffMemberWorkModeOverride get-by-id request."); return Ok(await Mediator.Send(new GetStaffMemberWorkModeOverrideByIdQuery(id, permissionRequest), ct)); }
    /// <summary>Lists temporary work-mode requests for staffMember and manager workflow screens.</summary>
    [HttpGet("get-all")] public async Task<IActionResult> GetAll([FromQuery] StaffMemberWorkModeOverrideFilterRequestDTO filter, CancellationToken ct) { Logger.LogInfo("Received StaffMemberWorkModeOverride list request."); return Ok(await Mediator.Send(new GetStaffMemberWorkModeOverridesQuery(filter), ct)); }
    /// <summary>Updates a temporary work-mode request without exposing approval fields to the caller.</summary>
    [HttpPost("update")] public async Task<IActionResult> Update([FromBody] UpdateStaffMemberWorkModeOverrideRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received StaffMemberWorkModeOverride update request."); return Ok(await Mediator.Send(new UpdateStaffMemberWorkModeOverrideCommand(dto), ct)); }
    /// <summary>Activates or deactivates an override without deleting its audit history.</summary>
    [HttpPost("update-status")] public async Task<IActionResult> UpdateStatus([FromBody] UpdateStaffMemberWorkModeOverrideStatusRequestDTO dto, CancellationToken ct) { Logger.LogInfo("Received StaffMemberWorkModeOverride status request."); return Ok(await Mediator.Send(new UpdateStaffMemberWorkModeOverrideStatusCommand(dto), ct)); }
    /// <summary>Approves or rejects a pending override after revalidating its dates and references.</summary>
    /// <summary>Approves a pending work-mode override after revalidating its dates and dependencies.</summary>
    [HttpPost("approve")]
    public async Task<IActionResult> Approve([FromBody] StaffMemberWorkModeOverrideDecisionRequestDTO dto, CancellationToken ct)
    {
        Logger.LogInfo("Received StaffMemberWorkModeOverride approval request.");
        return Ok(await Mediator.Send(new DecideStaffMemberWorkModeOverrideCommand(dto, true), ct));
    }

    /// <summary>Rejects a pending work-mode override and records the mandatory rejection remark.</summary>
    [HttpPost("reject")]
    public async Task<IActionResult> Reject([FromBody] StaffMemberWorkModeOverrideDecisionRequestDTO dto, CancellationToken ct)
    {
        Logger.LogInfo("Received StaffMemberWorkModeOverride rejection request.");
        return Ok(await Mediator.Send(new DecideStaffMemberWorkModeOverrideCommand(dto, false), ct));
    }
    /// <summary>Soft-deletes a temporary work-mode request that is no longer applicable.</summary>
    [HttpDelete("delete/{id:long}")] public async Task<IActionResult> Delete(long id, [FromQuery] PermissionRequestDTO permissionRequest, CancellationToken ct) { Logger.LogInfo("Received StaffMemberWorkModeOverride delete request."); return Ok(await Mediator.Send(new DeleteStaffMemberWorkModeOverrideCommand(id, permissionRequest), ct)); }
    #endregion
}
