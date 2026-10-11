// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Manages Host-controlled physical Institution device registration, configuration, and lifecycle.
// ================================================================

using skolenza.application.DTOS.Host;
using skolenza.application.Features.HostDeviceCmd.Handlers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.HostDevice;

/// <summary>Provides authenticated Host and Institution endpoints for physical Institution device administration.</summary>
[Authorize]
[ApiController]
[Route("api/InstitutionDevice")]
public sealed class InstitutionDeviceController(IMediator mediator, ILogger<InstitutionDeviceController> logger) : ControllerBase
{
    #region Institution Device Endpoints

    /// <summary>
    /// Used-In-Angular: creates institution device.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: creates institution device.</para>
    /// <para>Required permission fields in the JSON body: ModuleId and OperationId.
    /// Use the device assignment module and its granted assignment action from MyMenu.
    /// OperationId is the persisted action ID, not the OperationType enum value.
    /// An omitted or zero OperationId is rejected with HTTP 400 before device insertion.</para>
    /// <para>Handler flow: CreateInstitutionDeviceCommand is processed by CreateInstitutionDeviceCommandHandler; operation(s): GetForUpdateAsync, AddAsync, SaveChangesAsync, GetByIdAsync.</para>
    /// <para>Response DTO property analysis: CreateInstitutionDeviceRequestDTO: No public properties were statically resolved.; ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); InstitutionDeviceResponseDTO: InstitutionId (string), CampusId (long), DeviceMasterId (long), DeviceCode (string), DeviceName (string?), InstalledDateTime (DateTime?), InstalledBy (long?), InstallationRemark (string?), IsAttendanceDevice (bool), Description (string?), Remark (string?), IsActive (bool), Id (long), InstitutionName (string?), CampusName (string?), LocationCode (string?), DeviceMasterName (string?), DeviceMasterModelNo (string?), HasConfiguration (bool), AddedDateTime (DateTime), UpdatedDateTime (DateTime?)</para>
    /// <para>Angular function(s): InstitutionDeviceApi.addInstitutionDevice (app/core/services/institution-device-api.ts:43).</para>
    /// <para>Angular purpose: creates institution device.</para>
    /// <para>Integrated UI page(s): /app/institution-devices/new; /app/institution-devices/:institutionDeviceId/edit</para>
    /// <para>Angular UI component(s): InstitutionDeviceForm (app/features/host/institution-devices/institution-device-form/institution-device-form.ts)</para>
    /// </remarks>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateInstitutionDeviceRequestDTO dto, CancellationToken cancellationToken)
    { logger.LogInformation("Received InstitutionDevice create request for Institution {InstitutionId}.", dto.InstitutionId); return Ok(await mediator.Send(new CreateInstitutionDeviceCommand(dto), cancellationToken)); }

    /// <summary>
    /// Used-In-Angular: retrieves institution device.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves institution device by id.</para>
    /// <para>Handler flow: GetInstitutionDeviceByIdQuery is processed by GetInstitutionDeviceByIdQueryHandler; operation(s): GetByIdAsync.</para>
    /// <para>Response DTO property analysis: InstitutionDeviceAccessRequestDTO: InstitutionId (string?); ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); InstitutionDeviceResponseDTO: InstitutionId (string), CampusId (long), DeviceMasterId (long), DeviceCode (string), DeviceName (string?), InstalledDateTime (DateTime?), InstalledBy (long?), InstallationRemark (string?), IsAttendanceDevice (bool), Description (string?), Remark (string?), IsActive (bool), Id (long), InstitutionName (string?), CampusName (string?), LocationCode (string?), DeviceMasterName (string?), DeviceMasterModelNo (string?), HasConfiguration (bool), AddedDateTime (DateTime), UpdatedDateTime (DateTime?)</para>
    /// <para>Angular function(s): InstitutionDeviceApi.getInstitutionDevice (app/core/services/institution-device-api.ts:37).</para>
    /// <para>Angular purpose: retrieves institution device.</para>
    /// <para>Integrated UI page(s): /app/institution-devices/new; /app/institution-devices/:institutionDeviceId/edit</para>
    /// <para>Angular UI component(s): InstitutionDeviceForm (app/features/host/institution-devices/institution-device-form/institution-device-form.ts)</para>
    /// </remarks>
    [HttpGet("get-by-id/{id}")]
    public async Task<IActionResult> GetById(string id, [FromQuery] InstitutionDeviceAccessRequestDTO accessRequest, CancellationToken cancellationToken)
    { logger.LogInformation("Received InstitutionDevice get-by-id request."); return Ok(await mediator.Send(new GetInstitutionDeviceByIdQuery(id, accessRequest), cancellationToken)); }

    /// <summary>
    /// Used-In-Angular: retrieves institution devices.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves all institution devices.</para>
    /// <para>Handler flow: GetAllInstitutionDevicesQuery is processed by GetAllInstitutionDevicesQueryHandler; operation(s): GetPagedAsync, GetHostPagedAsync.</para>
    /// <para>Response DTO property analysis: GetInstitutionDeviceListRequestDTO: Search (string?), CampusId (long?), DeviceMasterId (long?), IsAttendanceDevice (bool?), IsActive (bool?), PageNumber (int), PageSize (int); ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); InstitutionDeviceResponseDTO: InstitutionId (string), CampusId (long), DeviceMasterId (long), DeviceCode (string), DeviceName (string?), InstalledDateTime (DateTime?), InstalledBy (long?), InstallationRemark (string?), IsAttendanceDevice (bool), Description (string?), Remark (string?), IsActive (bool), Id (long), InstitutionName (string?), CampusName (string?), LocationCode (string?), DeviceMasterName (string?), DeviceMasterModelNo (string?), HasConfiguration (bool), AddedDateTime (DateTime), UpdatedDateTime (DateTime?)</para>
    /// <para>Angular function(s): InstitutionDeviceApi.getInstitutionDevices (app/core/services/institution-device-api.ts:28).</para>
    /// <para>Angular purpose: retrieves institution devices.</para>
    /// <para>Integrated UI page(s): /app/institution-device-configurations/new; /app/institution-device-configurations/:institutionDeviceConfigurationId/edit; /app/institution-device-configurations; /app/institution-devices; /app/profile/device-enrollment-info</para>
    /// <para>Angular UI component(s): InstitutionDeviceConfigurationForm (app/features/host/institution-device-configurations/institution-device-configuration-form/institution-device-configuration-form.ts); InstitutionDeviceConfigurationsStore (app/features/host/institution-device-configurations/institution-device-configurations.store.ts); InstitutionDevicesStore (app/features/host/institution-devices/institution-devices.store.ts); StaffMemberDeviceEnrollmentForm (app/features/user-menu/staff-member-profile/staff member device enrollment-info/staff member device enrollment-form/staff member device enrollment-form.ts); InstitutionDeviceConfigurations (app/features/host/institution-device-configurations/institution-device-configurations.ts); InstitutionDevices (app/features/host/institution-devices/institution-devices.ts); StaffMemberDeviceEnrollmentInfo (app/features/user-menu/staff-member-profile/staff member device enrollment-info/staff member device enrollment-info.ts)</para>
    /// </remarks>
    [HttpGet("get-all")]
    public async Task<IActionResult> GetAll([FromQuery] GetInstitutionDeviceListRequestDTO filter, CancellationToken cancellationToken)
    { logger.LogInformation("Received InstitutionDevice list request."); return Ok(await mediator.Send(new GetAllInstitutionDevicesQuery(filter), cancellationToken)); }

    /// <summary>
    /// Used-In-Angular: updates institution device.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: updates institution device.</para>
    /// <para>Handler flow: UpdateInstitutionDeviceCommand is processed by UpdateInstitutionDeviceCommandHandler; operation(s): GetForUpdateAsync, Map, SaveChangesAsync, GetByIdAsync.</para>
    /// <para>Response DTO property analysis: UpdateInstitutionDeviceRequestDTO: Id (long); ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); InstitutionDeviceResponseDTO: InstitutionId (string), CampusId (long), DeviceMasterId (long), DeviceCode (string), DeviceName (string?), InstalledDateTime (DateTime?), InstalledBy (long?), InstallationRemark (string?), IsAttendanceDevice (bool), Description (string?), Remark (string?), IsActive (bool), Id (long), InstitutionName (string?), CampusName (string?), LocationCode (string?), DeviceMasterName (string?), DeviceMasterModelNo (string?), HasConfiguration (bool), AddedDateTime (DateTime), UpdatedDateTime (DateTime?)</para>
    /// <para>Angular function(s): InstitutionDeviceApi.updateInstitutionDevice (app/core/services/institution-device-api.ts:50).</para>
    /// <para>Angular purpose: updates institution device.</para>
    /// <para>Integrated UI page(s): /app/institution-devices/new; /app/institution-devices/:institutionDeviceId/edit</para>
    /// <para>Angular UI component(s): InstitutionDeviceForm (app/features/host/institution-devices/institution-device-form/institution-device-form.ts)</para>
    /// </remarks>
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody] UpdateInstitutionDeviceRequestDTO dto, CancellationToken cancellationToken)
    { logger.LogInformation("Received InstitutionDevice update request for {InstitutionDeviceId}.", dto.Id); return Ok(await mediator.Send(new UpdateInstitutionDeviceCommand(dto), cancellationToken)); }

    /// <summary>
    /// Used-In-Angular: updates institution device status.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: updates institution device status.</para>
    /// <para>Handler flow: UpdateInstitutionDeviceStatusCommand is processed by UpdateInstitutionDeviceStatusCommandHandler; operation(s): GetForUpdateAsync, SaveChangesAsync, GetByIdAsync.</para>
    /// <para>Response DTO property analysis: UpdateInstitutionDeviceStatusRequestDTO: Id (long), IsActive (bool); ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); InstitutionDeviceResponseDTO: InstitutionId (string), CampusId (long), DeviceMasterId (long), DeviceCode (string), DeviceName (string?), InstalledDateTime (DateTime?), InstalledBy (long?), InstallationRemark (string?), IsAttendanceDevice (bool), Description (string?), Remark (string?), IsActive (bool), Id (long), InstitutionName (string?), CampusName (string?), LocationCode (string?), DeviceMasterName (string?), DeviceMasterModelNo (string?), HasConfiguration (bool), AddedDateTime (DateTime), UpdatedDateTime (DateTime?)</para>
    /// <para>Angular function(s): InstitutionDeviceApi.setInstitutionDeviceStatus (app/core/services/institution-device-api.ts:56).</para>
    /// <para>Angular purpose: updates institution device status.</para>
    /// <para>Integrated UI page(s): /app/institution-devices</para>
    /// <para>Angular UI component(s): InstitutionDevicesStore (app/features/host/institution-devices/institution-devices.store.ts); InstitutionDevices (app/features/host/institution-devices/institution-devices.ts)</para>
    /// </remarks>
    [HttpPost("update-status")]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateInstitutionDeviceStatusRequestDTO dto, CancellationToken cancellationToken)
    { logger.LogInformation("Received InstitutionDevice status request for {InstitutionDeviceId}.", dto.Id); return Ok(await mediator.Send(new UpdateInstitutionDeviceStatusCommand(dto), cancellationToken)); }

    /// <summary>
    /// Lets a Institution administrator move an already assigned device to another
    /// location without changing the Host-owned DeviceMaster assignment.
    /// </summary>
    [HttpPost("update-location")]
    public async Task<IActionResult> UpdateLocation(
        [FromBody] UpdateInstitutionDeviceLocationRequestDTO dto,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received InstitutionDevice location update for {InstitutionDeviceId}.", dto.InstitutionDeviceId);
        return Ok(await mediator.Send(new UpdateInstitutionDeviceLocationCommand(dto), cancellationToken));
    }

    /// <summary>
    /// Used-In-Angular: deletes institution device.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: deletes institution device.</para>
    /// <para>Handler flow: DeleteInstitutionDeviceCommand is processed by DeleteInstitutionDeviceCommandHandler; operation(s): GetForUpdateAsync, SaveChangesAsync.</para>
    /// <para>Response DTO property analysis: InstitutionDeviceAccessRequestDTO: InstitutionId (string?); ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
    /// <para>Angular function(s): InstitutionDeviceApi.deleteInstitutionDevice (app/core/services/institution-device-api.ts:64).</para>
    /// <para>Angular purpose: deletes institution device.</para>
    /// <para>Integrated UI page(s): /app/institution-devices</para>
    /// <para>Angular UI component(s): InstitutionDevicesStore (app/features/host/institution-devices/institution-devices.store.ts); InstitutionDevices (app/features/host/institution-devices/institution-devices.ts)</para>
    /// </remarks>
    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(string id, [FromQuery] InstitutionDeviceAccessRequestDTO accessRequest, CancellationToken cancellationToken)
    { logger.LogInformation("Received InstitutionDevice delete request."); return Ok(await mediator.Send(new DeleteInstitutionDeviceCommand(id, accessRequest), cancellationToken)); }

    #endregion
}
