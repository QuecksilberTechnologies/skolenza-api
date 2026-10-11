// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Exposes Host bootstrap plus Host-authorized and Institution-admin device connection configuration endpoints.
// ================================================================

using skolenza.application.DTOS.Host;
using skolenza.application.Features.HostDeviceCmd.Handlers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace skolenza.api.Controllers.HostDevice;

/// <summary>Provides Host initial provisioning plus Host-authorized and Institution-admin connection configuration endpoints.</summary>
[Authorize]
[ApiController]
[Route("api/InstitutionDeviceConfiguration")]
public sealed class InstitutionDeviceConfigurationController(IMediator mediator, ILogger<InstitutionDeviceConfigurationController> logger) : ControllerBase
{
    #region Initial Device Provisioning and Runtime Configuration

    /// <summary>
    /// Issues a short-lived initial HTTPS gateway URL so a technician can connect a new unassigned physical device without exposing a permanent credential.
    /// </summary>
    /// <remarks>
    /// Host provisioning permission is enforced by the HostDevice permission behavior.
    /// The returned URL is shown only once and must be entered on the physical device.
    /// </remarks>
    [HttpPost("issue-bootstrap-url")]
    public async Task<IActionResult> IssueBootstrapUrl(
        [FromBody] IssueInitialDeviceBootstrapRequestDTO dto,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received initial device bootstrap request for DeviceMaster {DeviceMasterId}.", dto.DeviceMasterId);
        return Ok(await mediator.Send(new IssueInitialDeviceBootstrapCommand(dto), cancellationToken));
    }

    /// <summary>
    /// Queues the approved Host-authorized or Institution-admin runtime settings so the device is configured through its outbound transport instead of a direct LAN call.
    /// </summary>
    [HttpPost("apply-runtime-configuration")]
    public async Task<IActionResult> ApplyRuntimeConfiguration(
        [FromBody] ApplyInstitutionDeviceRuntimeConfigurationRequestDTO dto,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received runtime configuration request for InstitutionDevice {InstitutionDeviceId}.", dto.InstitutionDeviceId);
        return Ok(await mediator.Send(new ApplyInstitutionDeviceRuntimeConfigurationCommand(dto), cancellationToken));
    }

    /// <summary>
    /// Queues a Host-authorized or Institution-admin device reboot so restart is auditable and works for both configured transports.
    /// </summary>
    [HttpPost("reboot")]
    public async Task<IActionResult> Reboot(
        [FromBody] RebootInstitutionDeviceRequestDTO dto,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received reboot request for InstitutionDevice {InstitutionDeviceId}.", dto.InstitutionDeviceId);
        return Ok(await mediator.Send(new RebootInstitutionDeviceCommand(dto), cancellationToken));
    }

    /// <summary>Queues one typed device time-settings change so the Time UI updates only this firmware configuration section.</summary>
    [HttpPost("settings/time")]
    public Task<IActionResult> UpdateTimeSettings([FromBody] UpdateInstitutionDeviceTimeSettingsRequestDTO dto, CancellationToken cancellationToken) =>
        SendSettingsAsync(dto, InstitutionDeviceSettingsSection.Time, cancellationToken);

    /// <summary>Queues an explicit device-clock synchronization when an admin needs the physical device clock corrected without changing time-format settings.</summary>
    [HttpPost("settings/time/sync")]
    public async Task<IActionResult> SyncTime([FromBody] SyncInstitutionDeviceTimeRequestDTO dto, CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new SyncInstitutionDeviceTimeCommand(dto), cancellationToken));

    /// <summary>Queues typed bell settings so the Bell UI can update output behavior without sending raw vendor JSON.</summary>
    [HttpPost("settings/bell")]
    public Task<IActionResult> UpdateBellSettings([FromBody] UpdateInstitutionDeviceBellSettingsRequestDTO dto, CancellationToken cancellationToken) =>
        SendSettingsAsync(dto, InstitutionDeviceSettingsSection.Bell, cancellationToken);

    /// <summary>Queues typed device display and recognition settings so the Device setup UI uses readable business properties.</summary>
    [HttpPost("settings/device-setup")]
    public Task<IActionResult> UpdateDeviceSetupSettings([FromBody] UpdateInstitutionDeviceSetupSettingsRequestDTO dto, CancellationToken cancellationToken) =>
        SendSettingsAsync(dto, InstitutionDeviceSettingsSection.DeviceSetup, cancellationToken);

    /// <summary>Queues typed advanced face, verification, privacy, and fill-light settings so these sensitive controls remain permission-checked.</summary>
    [HttpPost("settings/advanced")]
    public Task<IActionResult> UpdateAdvancedSettings([FromBody] UpdateInstitutionDeviceAdvancedSettingsRequestDTO dto, CancellationToken cancellationToken) =>
        SendSettingsAsync(dto, InstitutionDeviceSettingsSection.Advanced, cancellationToken);

    /// <summary>Queues typed door, Wiegand, and access-control settings so a lock configuration change is logged and transport-neutral.</summary>
    [HttpPost("settings/lock")]
    public Task<IActionResult> UpdateLockSettings([FromBody] UpdateInstitutionDeviceLockSettingsRequestDTO dto, CancellationToken cancellationToken) =>
        SendSettingsAsync(dto, InstitutionDeviceSettingsSection.Lock, cancellationToken);

    /// <summary>Queues typed serial port settings so the Serial UI uses validated device options.</summary>
    [HttpPost("settings/serial")]
    public Task<IActionResult> UpdateSerialSettings([FromBody] UpdateInstitutionDeviceSerialSettingsRequestDTO dto, CancellationToken cancellationToken) =>
        SendSettingsAsync(dto, InstitutionDeviceSettingsSection.Serial, cancellationToken);

    /// <summary>Queues typed Ethernet settings so network changes are delivered safely through the configured outbound transport; static addressing applies only when DHCP is disabled.</summary>
    [HttpPost("settings/ethernet")]
    public Task<IActionResult> UpdateEthernetSettings([FromBody] UpdateInstitutionDeviceEthernetSettingsRequestDTO dto, CancellationToken cancellationToken) =>
        SendSettingsAsync(dto, InstitutionDeviceSettingsSection.Ethernet, cancellationToken);

    /// <summary>Queues typed Wi-Fi network settings so the institution can manage connectivity without a direct browser call to the device; static addressing applies only when DHCP is disabled.</summary>
    [HttpPost("settings/wifi")]
    public Task<IActionResult> UpdateWifiSettings([FromBody] UpdateInstitutionDeviceWifiSettingsRequestDTO dto, CancellationToken cancellationToken) =>
        SendSettingsAsync(dto, InstitutionDeviceSettingsSection.Wifi, cancellationToken);

    /// <summary>Queues typed optional app-notification settings so third-party notification details stay protected inside the command queue.</summary>
    [HttpPost("settings/app-notification")]
    public Task<IActionResult> UpdateAppNotificationSettings([FromBody] UpdateInstitutionDeviceAppNotificationSettingsRequestDTO dto, CancellationToken cancellationToken) =>
        SendSettingsAsync(dto, InstitutionDeviceSettingsSection.AppNotification, cancellationToken);

    /// <summary>Queues local device Web UI/API enablement and password rotation so the Institution can lock or restore local access without exposing stored credentials.</summary>
    [HttpPost("settings/web-access")]
    public Task<IActionResult> UpdateWebAccess([FromBody] UpdateInstitutionDeviceWebAccessRequestDTO dto, CancellationToken cancellationToken) =>
        SendSettingsAsync(dto, InstitutionDeviceSettingsSection.WebAccess, cancellationToken);

    /// <summary>Queues the PIN required by the device physical System/Local Manager menu so unauthorized people cannot change settings at the device screen.</summary>
    [HttpPost("settings/screen-menu-pin")]
    public Task<IActionResult> UpdateScreenMenuPin([FromBody] UpdateInstitutionDeviceScreenMenuPinRequestDTO dto, CancellationToken cancellationToken) =>
        SendSettingsAsync(dto, InstitutionDeviceSettingsSection.ScreenMenuPin, cancellationToken);

    /// <summary>Gets the device cloud gateway base address and replacement state so Angular can show connection health without returning a bearer token.</summary>
    [HttpGet("gateway-address/{institutionDeviceId}")]
    public async Task<IActionResult> GetGatewayAddress(
        string institutionDeviceId,
        [FromQuery] InstitutionDeviceAccessRequestDTO dto,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetInstitutionDeviceGatewayAddressQuery(institutionDeviceId, dto), cancellationToken));

    /// <summary>
    /// Replaces the device HTTPS gateway remotely so the Institution can rotate the connection address without visiting the device. The current URL remains usable
    /// until the device polls through the new URL; heartbeat is not changed.
    /// </summary>
    [HttpPost("replace-https-gateway-url")]
    public async Task<IActionResult> ReplaceHttpsGatewayUrl(
        [FromBody] ReplaceInstitutionDeviceHttpsGatewayUrlRequestDTO dto,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new ReplaceInstitutionDeviceHttpsGatewayUrlCommand(dto), cancellationToken));

    /// <summary>
    /// Publishes the next command already in the MQTTS queue now when a Institution explicitly needs immediate broker delivery. HTTPS devices
    /// deliberately use their next outbound heartbeat instead.
    /// </summary>
    [HttpPost("dispatch-mqtts-now")]
    public async Task<IActionResult> DispatchMqttsNow(
        [FromBody] DispatchInstitutionDeviceMqttsNowRequestDTO dto,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new DispatchInstitutionDeviceMqttsNowCommand(dto), cancellationToken));

    private async Task<IActionResult> SendSettingsAsync(
        InstitutionDeviceSettingRequestDTO dto,
        InstitutionDeviceSettingsSection section,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new ApplyInstitutionDeviceSettingsCommand(dto, section), cancellationToken));

    #endregion

    #region Institution Device Configuration CRUD

    /// <summary>
    /// Used-In-Angular: creates institution device configuration.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: creates institution device configuration.</para>
    /// <para>Handler flow: CreateInstitutionDeviceConfigurationCommand is processed by CreateInstitutionDeviceConfigurationCommandHandler; operation(s): AddAsync, SaveChangesAsync, GetByIdAsync.</para>
    /// <para>Response DTO property analysis: CreateInstitutionDeviceConfigurationRequestDTO: No public properties were statically resolved.; ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); InstitutionDeviceConfigurationResponseDTO: InstitutionId (string), Id (long), InstitutionDeviceId (long), IpAddress (string?), MacAddress (string?), DevicePort (int?), CommunicationType (DeviceCommunicationType?), ServerHost (string?), ServerPort (int?), ServerPath (string?), ServerUrl (string?), PushMode (string?), HeartbeatIntervalSeconds (int?), TimeZoneId (string?), Configuration (string?), IsEnrollmentEnabled (bool), IsAttendancePushEnabled (bool), IsAutoSyncEnabled (bool), LastHeartbeatDateTime (DateTime?), LastSyncDateTime (DateTime?), LastAttendanceReceivedDateTime (DateTime?), LastSuccessfulConnectionDateTime (DateTime?), LastFailedConnectionDateTime (DateTime?), LastConnectionError (string?), DeviceCode (string?), DeviceName (string?), DeviceMasterName (string?), DeviceMasterSNo (string?), AddedDateTime (DateTime), UpdatedDateTime (DateTime?)</para>
    /// <para>Angular function(s): InstitutionDeviceConfigurationApi.addInstitutionDeviceConfiguration (app/core/services/institution-device-configuration-api.ts:57).</para>
    /// <para>Angular purpose: creates institution device configuration.</para>
    /// <para>Integrated UI page(s): /app/institution-device-configurations/new; /app/institution-device-configurations/:institutionDeviceConfigurationId/edit</para>
    /// <para>Angular UI component(s): InstitutionDeviceConfigurationForm (app/features/host/institution-device-configurations/institution-device-configuration-form/institution-device-configuration-form.ts)</para>
    /// </remarks>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateInstitutionDeviceConfigurationRequestDTO dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received InstitutionDeviceConfiguration create request for InstitutionDevice {InstitutionDeviceId}.", dto.InstitutionDeviceId);
        return Ok(await mediator.Send(new CreateInstitutionDeviceConfigurationCommand(dto), cancellationToken));
    }

    /// <summary>
    /// Used-In-Angular: retrieves institution device configuration.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves institution device configuration by id.</para>
    /// <para>Handler flow: GetInstitutionDeviceConfigurationByIdQuery is processed by GetInstitutionDeviceConfigurationByIdQueryHandler; operation(s): GetByIdAsync.</para>
    /// <para>Response DTO property analysis: InstitutionDeviceAccessRequestDTO: InstitutionId (string?); ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); InstitutionDeviceConfigurationResponseDTO: InstitutionId (string), Id (long), InstitutionDeviceId (long), IpAddress (string?), MacAddress (string?), DevicePort (int?), CommunicationType (DeviceCommunicationType?), ServerHost (string?), ServerPort (int?), ServerPath (string?), ServerUrl (string?), PushMode (string?), HeartbeatIntervalSeconds (int?), TimeZoneId (string?), Configuration (string?), IsEnrollmentEnabled (bool), IsAttendancePushEnabled (bool), IsAutoSyncEnabled (bool), LastHeartbeatDateTime (DateTime?), LastSyncDateTime (DateTime?), LastAttendanceReceivedDateTime (DateTime?), LastSuccessfulConnectionDateTime (DateTime?), LastFailedConnectionDateTime (DateTime?), LastConnectionError (string?), DeviceCode (string?), DeviceName (string?), DeviceMasterName (string?), DeviceMasterSNo (string?), AddedDateTime (DateTime), UpdatedDateTime (DateTime?)</para>
    /// <para>Angular function(s): InstitutionDeviceConfigurationApi.getInstitutionDeviceConfiguration (app/core/services/institution-device-configuration-api.ts:45).</para>
    /// <para>Angular purpose: retrieves institution device configuration.</para>
    /// <para>Integrated UI page(s): /app/institution-device-configurations/new; /app/institution-device-configurations/:institutionDeviceConfigurationId/edit</para>
    /// <para>Angular UI component(s): InstitutionDeviceConfigurationForm (app/features/host/institution-device-configurations/institution-device-configuration-form/institution-device-configuration-form.ts)</para>
    /// </remarks>
    [HttpGet("get-by-id/{id}")]
    public async Task<IActionResult> GetById(string id, [FromQuery] InstitutionDeviceAccessRequestDTO accessRequest, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received InstitutionDeviceConfiguration get-by-id request.");
        return Ok(await mediator.Send(new GetInstitutionDeviceConfigurationByIdQuery(id, accessRequest), cancellationToken));
    }

    /// <summary>
    /// Used-In-Angular: retrieves institution device configurations.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: retrieves all institution device configurations.</para>
    /// <para>Handler flow: GetAllInstitutionDeviceConfigurationsQuery is processed by GetAllInstitutionDeviceConfigurationsQueryHandler; operation(s): GetPagedAsync, GetHostPagedAsync.</para>
    /// <para>Response DTO property analysis: GetInstitutionDeviceConfigurationListRequestDTO: Search (string?), InstitutionDeviceId (long?), CommunicationType (DeviceCommunicationType?), IsEnrollmentEnabled (bool?), PageNumber (int), PageSize (int); ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); InstitutionDeviceConfigurationResponseDTO: InstitutionId (string), Id (long), InstitutionDeviceId (long), IpAddress (string?), MacAddress (string?), DevicePort (int?), CommunicationType (DeviceCommunicationType?), ServerHost (string?), ServerPort (int?), ServerPath (string?), ServerUrl (string?), PushMode (string?), HeartbeatIntervalSeconds (int?), TimeZoneId (string?), Configuration (string?), IsEnrollmentEnabled (bool), IsAttendancePushEnabled (bool), IsAutoSyncEnabled (bool), LastHeartbeatDateTime (DateTime?), LastSyncDateTime (DateTime?), LastAttendanceReceivedDateTime (DateTime?), LastSuccessfulConnectionDateTime (DateTime?), LastFailedConnectionDateTime (DateTime?), LastConnectionError (string?), DeviceCode (string?), DeviceName (string?), DeviceMasterName (string?), DeviceMasterSNo (string?), AddedDateTime (DateTime), UpdatedDateTime (DateTime?)</para>
    /// <para>Angular function(s): InstitutionDeviceConfigurationApi.getInstitutionDeviceConfigurations (app/core/services/institution-device-configuration-api.ts:29).</para>
    /// <para>Angular purpose: retrieves institution device configurations.</para>
    /// <para>Integrated UI page(s): /app/institution-device-configurations</para>
    /// <para>Angular UI component(s): InstitutionDeviceConfigurationsStore (app/features/host/institution-device-configurations/institution-device-configurations.store.ts); InstitutionDeviceConfigurations (app/features/host/institution-device-configurations/institution-device-configurations.ts)</para>
    /// </remarks>
    [HttpGet("get-all")]
    public async Task<IActionResult> GetAll([FromQuery] GetInstitutionDeviceConfigurationListRequestDTO filter, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received InstitutionDeviceConfiguration list request.");
        return Ok(await mediator.Send(new GetAllInstitutionDeviceConfigurationsQuery(filter), cancellationToken));
    }

    /// <summary>
    /// Used-In-Angular: updates institution device configuration.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: updates institution device configuration.</para>
    /// <para>Handler flow: UpdateInstitutionDeviceConfigurationCommand is processed by UpdateInstitutionDeviceConfigurationCommandHandler; operation(s): GetForUpdateAsync, Map, SaveChangesAsync, GetByIdAsync.</para>
    /// <para>Response DTO property analysis: UpdateInstitutionDeviceConfigurationRequestDTO: Id (long); ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?); InstitutionDeviceConfigurationResponseDTO: InstitutionId (string), Id (long), InstitutionDeviceId (long), IpAddress (string?), MacAddress (string?), DevicePort (int?), CommunicationType (DeviceCommunicationType?), ServerHost (string?), ServerPort (int?), ServerPath (string?), ServerUrl (string?), PushMode (string?), HeartbeatIntervalSeconds (int?), TimeZoneId (string?), Configuration (string?), IsEnrollmentEnabled (bool), IsAttendancePushEnabled (bool), IsAutoSyncEnabled (bool), LastHeartbeatDateTime (DateTime?), LastSyncDateTime (DateTime?), LastAttendanceReceivedDateTime (DateTime?), LastSuccessfulConnectionDateTime (DateTime?), LastFailedConnectionDateTime (DateTime?), LastConnectionError (string?), DeviceCode (string?), DeviceName (string?), DeviceMasterName (string?), DeviceMasterSNo (string?), AddedDateTime (DateTime), UpdatedDateTime (DateTime?)</para>
    /// <para>Angular function(s): InstitutionDeviceConfigurationApi.updateInstitutionDeviceConfiguration (app/core/services/institution-device-configuration-api.ts:66).</para>
    /// <para>Angular purpose: updates institution device configuration.</para>
    /// <para>Integrated UI page(s): /app/institution-device-configurations/new; /app/institution-device-configurations/:institutionDeviceConfigurationId/edit</para>
    /// <para>Angular UI component(s): InstitutionDeviceConfigurationForm (app/features/host/institution-device-configurations/institution-device-configuration-form/institution-device-configuration-form.ts)</para>
    /// </remarks>
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody] UpdateInstitutionDeviceConfigurationRequestDTO dto, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received InstitutionDeviceConfiguration update request for {InstitutionDeviceConfigurationId}.", dto.Id);
        return Ok(await mediator.Send(new UpdateInstitutionDeviceConfigurationCommand(dto), cancellationToken));
    }

    /// <summary>
    /// Generates a one-time opaque HTTPS device gateway URL. The caller must copy
    /// it to the physical device immediately; later reads intentionally omit it.
    /// </summary>
    [HttpPost("rotate-https-ingress-token")]
    public async Task<IActionResult> RotateHttpsIngressToken(
        [FromBody] RotateInstitutionDeviceHttpsIngressTokenRequestDTO dto,
        CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new RotateInstitutionDeviceHttpsIngressTokenCommand(dto), cancellationToken));

    /// <summary>
    /// Used-In-Angular: deletes institution device configuration.
    /// </summary>
    /// <remarks>
    /// <para>Angular usage status: Used-In-Angular.</para>
    /// <para>API endpoint purpose: deletes institution device configuration.</para>
    /// <para>Handler flow: DeleteInstitutionDeviceConfigurationCommand is processed by DeleteInstitutionDeviceConfigurationCommandHandler; operation(s): GetForUpdateAsync, Remove, SaveChangesAsync.</para>
    /// <para>Response DTO property analysis: InstitutionDeviceAccessRequestDTO: InstitutionId (string?); ApiResponse: IsSucceeded (bool), Message (string), Data (T), Errors (List&lt;string&gt;), ErrorCode (string?), PageNumber (int?), PageSize (int?), TotalRecords (int?), TotalPages (int?), IsPrimaryMarked (bool?), HasAllDocUploaded (bool?), CompletionPercentage (double?)</para>
    /// <para>Angular function(s): InstitutionDeviceConfigurationApi.deleteInstitutionDeviceConfiguration (app/core/services/institution-device-configuration-api.ts:77).</para>
    /// <para>Angular purpose: deletes institution device configuration.</para>
    /// <para>Integrated UI page(s): /app/institution-device-configurations</para>
    /// <para>Angular UI component(s): InstitutionDeviceConfigurationsStore (app/features/host/institution-device-configurations/institution-device-configurations.store.ts); InstitutionDeviceConfigurations (app/features/host/institution-device-configurations/institution-device-configurations.ts)</para>
    /// </remarks>
    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(string id, [FromQuery] InstitutionDeviceAccessRequestDTO accessRequest, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received InstitutionDeviceConfiguration delete request.");
        return Ok(await mediator.Send(new DeleteInstitutionDeviceConfigurationCommand(id, accessRequest), cancellationToken));
    }

    #endregion
}
