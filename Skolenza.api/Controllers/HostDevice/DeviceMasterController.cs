// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Exposes authenticated Host administration endpoints for the DeviceMaster catalog.
// ================================================================

using skolenza.application.DTOS.Host;
using skolenza.application.Features.HostDeviceCmd.Handlers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.HostDevice;

/// <summary>Provides authenticated Host endpoints for global device model administration.</summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class DeviceMasterController(IMediator mediator, ILogger<DeviceMasterController> logger) : ControllerBase
{
    #region Device Master Endpoints

    /// <summary>
    /// Used-In-Angular: creates device master.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: creates device master.</para>
    /// <para>Handler flow: CreateDeviceMasterCommand is processed by CreateDeviceMasterCommandHandler; operation(s): AddAsync, SaveChangesAsync.</para>
    /// <para>Response DTO property analysis: CreateDeviceMasterRequestDTO: No public properties were statically resolved.; ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); DeviceMasterResponseDTO: Id (long), IsOccupied (bool), DeviceTypeName (string), AddedDateTime (DateTime), UpdatedDateTime (DateTime?)</para>
    /// <para>Angular function(s): DeviceMasterApi.addDeviceMaster (app/core/services/device-master-api.ts:40).</para>
    /// <para>Angular purpose: creates device master.</para>
    /// <para>Integrated UI page(s): /app/device-masters/new; /app/device-masters/:deviceMasterId/edit</para>
    /// <para>Angular UI component(s): DeviceMasterForm (app/features/host/device-masters/device-master-form/device-master-form.ts)</para>
    /// </remarks>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateDeviceMasterRequestDTO dto, CancellationToken cancellationToken)
    { logger.LogInformation("Received DeviceMaster create request."); return Ok(await mediator.Send(new CreateDeviceMasterCommand(dto), cancellationToken)); }

    /// <summary>
    /// Used-In-Angular: retrieves device master.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves device master by id.</para>
    /// <para>Handler flow: GetDeviceMasterByIdQuery is processed by GetDeviceMasterByIdQueryHandler; operation(s): GetByIdAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); DeviceMasterResponseDTO: Id (long), IsOccupied (bool), DeviceTypeName (string), AddedDateTime (DateTime), UpdatedDateTime (DateTime?)</para>
    /// <para>Angular function(s): DeviceMasterApi.getDeviceMaster (app/core/services/device-master-api.ts:34).</para>
    /// <para>Angular purpose: retrieves device master.</para>
    /// <para>Integrated UI page(s): /app/device-masters/new; /app/device-masters/:deviceMasterId/edit</para>
    /// <para>Angular UI component(s): DeviceMasterForm (app/features/host/device-masters/device-master-form/device-master-form.ts)</para>
    /// </remarks>
    [HttpGet("get-by-id/{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    { logger.LogInformation("Received DeviceMaster get-by-id request for {DeviceMasterId}.", id); return Ok(await mediator.Send(new GetDeviceMasterByIdQuery(id), cancellationToken)); }

        /// <summary>
        /// Not-Used-In-Angular.
        /// </summary>
        /// <remarks>
        /// <para>Angular usage status: Not-Used-In-Angular.</para>
        /// <para>API endpoint purpose: retrieves device master info by sno.</para>
        /// <para>Handler flow: GetDeviceMasterInfoBySNoQuery is processed by GetDeviceMasterInfoBySNoQueryHandler; operation(s): GetBySNoAsync.</para>
        /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); DeviceMasterResponseDTO: Id (long), IsOccupied (bool), DeviceTypeName (string), AddedDateTime (DateTime), UpdatedDateTime (DateTime?)</para>
        /// <para>No active Angular HTTP call with the same HTTP method and normalized route was found in the scanned Angular source.</para>
        /// <para>Backend endpoint: GET /api/devicemaster/get-info-by-sno/{}.</para>
        /// </remarks>
        [HttpGet("get-info-by-sno/{sNo}")]
        public async Task<IActionResult> GetInfoBySNo(string sNo, CancellationToken cancellationToken)
        { logger.LogInformation("Received DeviceMaster get-info-by-sno request."); return Ok(await mediator.Send(new GetDeviceMasterInfoBySNoQuery(sNo), cancellationToken)); }

    /// <summary>
    /// Used-In-Angular: retrieves device masters.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves all device masters.</para>
    /// <para>Handler flow: GetAllDeviceMastersQuery is processed by GetAllDeviceMastersQueryHandler; operation(s): GetPagedAsync.</para>
    /// <para>Response DTO property analysis: GetDeviceMasterListRequestDTO: Search (string?), DeviceType (DeviceType?), IsActive (bool?), IsOccupied (bool?), PageNumber (int), PageSize (int); ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); DeviceMasterResponseDTO: Id (long), IsOccupied (bool), DeviceTypeName (string), AddedDateTime (DateTime), UpdatedDateTime (DateTime?)</para>
    /// <para>Angular function(s): DeviceMasterApi.getDeviceMasters (app/core/services/device-master-api.ts:28).</para>
    /// <para>Angular purpose: retrieves device masters.</para>
    /// <para>Integrated UI page(s): /app/institution-devices/new; /app/institution-devices/:institutionDeviceId/edit; /app/device-masters; /app/institution-devices</para>
    /// <para>Angular UI component(s): DeviceMastersStore (app/features/host/device-masters/device-masters.store.ts); InstitutionDeviceForm (app/features/host/institution-devices/institution-device-form/institution-device-form.ts); InstitutionDevicesStore (app/features/host/institution-devices/institution-devices.store.ts); DeviceMasters (app/features/host/device-masters/device-masters.ts); InstitutionDevices (app/features/host/institution-devices/institution-devices.ts)</para>
    /// </remarks>
    [HttpGet("get-all")]
    public async Task<IActionResult> GetAll([FromQuery] GetDeviceMasterListRequestDTO filter, CancellationToken cancellationToken)
    { logger.LogInformation("Received DeviceMaster list request."); return Ok(await mediator.Send(new GetAllDeviceMastersQuery(filter), cancellationToken)); }

    /// <summary>
    /// Used-In-Angular: updates device master.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: updates device master.</para>
    /// <para>Handler flow: UpdateDeviceMasterCommand is processed by UpdateDeviceMasterCommandHandler; operation(s): GetForUpdateAsync, Map, SaveChangesAsync.</para>
    /// <para>Response DTO property analysis: UpdateDeviceMasterRequestDTO: Id (long), IsOccupied (bool); ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); DeviceMasterResponseDTO: Id (long), IsOccupied (bool), DeviceTypeName (string), AddedDateTime (DateTime), UpdatedDateTime (DateTime?)</para>
    /// <para>Angular function(s): DeviceMasterApi.updateDeviceMaster (app/core/services/device-master-api.ts:47).</para>
    /// <para>Angular purpose: updates device master.</para>
    /// <para>Integrated UI page(s): /app/device-masters/new; /app/device-masters/:deviceMasterId/edit</para>
    /// <para>Angular UI component(s): DeviceMasterForm (app/features/host/device-masters/device-master-form/device-master-form.ts)</para>
    /// </remarks>
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody] UpdateDeviceMasterRequestDTO dto, CancellationToken cancellationToken)
    { logger.LogInformation("Received DeviceMaster update request for {DeviceMasterId}.", dto.Id); return Ok(await mediator.Send(new UpdateDeviceMasterCommand(dto), cancellationToken)); }

    /// <summary>
    /// Used-In-Angular: updates device master status.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: updates device master status.</para>
    /// <para>Handler flow: UpdateDeviceMasterStatusCommand is processed by UpdateDeviceMasterStatusCommandHandler; operation(s): GetForUpdateAsync, SaveChangesAsync.</para>
    /// <para>Response DTO property analysis: UpdateDeviceMasterStatusRequestDTO: Id (long), IsActive (bool); ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); DeviceMasterResponseDTO: Id (long), IsOccupied (bool), DeviceTypeName (string), AddedDateTime (DateTime), UpdatedDateTime (DateTime?)</para>
    /// <para>Angular function(s): DeviceMasterApi.setDeviceMasterStatus (app/core/services/device-master-api.ts:53).</para>
    /// <para>Angular purpose: updates device master status.</para>
    /// <para>Integrated UI page(s): /app/device-masters</para>
    /// <para>Angular UI component(s): DeviceMastersStore (app/features/host/device-masters/device-masters.store.ts); DeviceMasters (app/features/host/device-masters/device-masters.ts)</para>
    /// </remarks>
    [HttpPost("update-status")]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateDeviceMasterStatusRequestDTO dto, CancellationToken cancellationToken)
    { logger.LogInformation("Received DeviceMaster status request for {DeviceMasterId}.", dto.Id); return Ok(await mediator.Send(new UpdateDeviceMasterStatusCommand(dto), cancellationToken)); }

    /// <summary>
    /// Used-In-Angular: deletes device master.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: deletes device master.</para>
    /// <para>Handler flow: DeleteDeviceMasterCommand is processed by DeleteDeviceMasterCommandHandler; operation(s): GetForUpdateAsync, SaveChangesAsync.</para>
    /// <para>Response DTO property analysis: ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
    /// <para>Angular function(s): DeviceMasterApi.deleteDeviceMaster (app/core/services/device-master-api.ts:59).</para>
    /// <para>Angular purpose: deletes device master.</para>
    /// <para>Integrated UI page(s): /app/device-masters</para>
    /// <para>Angular UI component(s): DeviceMastersStore (app/features/host/device-masters/device-masters.store.ts); DeviceMasters (app/features/host/device-masters/device-masters.ts)</para>
    /// </remarks>
    [HttpDelete("delete/{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    { logger.LogInformation("Received DeviceMaster delete request for {DeviceMasterId}.", id); return Ok(await mediator.Send(new DeleteDeviceMasterCommand(id), cancellationToken)); }

    #endregion
}
