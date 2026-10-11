// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines Host-managed DeviceMaster and InstitutionDevice API contracts.
// ================================================================

using System.Text.Json.Serialization;
using skolenza.application.DTOs.BaseDTO;
using skolenza.domain.Entity;

namespace skolenza.application.DTOS.Host;

/// <summary>Supplies editable model catalog values for a DeviceMaster record.</summary>
public class DeviceMasterRequestDTO
{
    #region Properties
    /// <summary>Gets or sets the manufacturer serial number for the device model.</summary>
    public string SNo { get; set; } = string.Empty;
    public string DeviceCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string ModelNo { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? BrandName { get; set; }
    public string? CountryOfOrigin { get; set; }
    public string? ManufacturerWebsite { get; set; }
    public string? SupportEmail { get; set; }
    public string? SupportContactNumber { get; set; }
    public DeviceType DeviceType { get; set; }
    public bool IsAttendanceDevice { get; set; } = true;
    public bool IsAccessControlDevice { get; set; }
    public bool SupportsFace { get; set; }
    public bool SupportsFingerprint { get; set; }
    public bool SupportsCard { get; set; }
    public bool SupportsPin { get; set; }
    public bool SupportsQrCode { get; set; }
    public bool SupportsTemperature { get; set; }
    public bool SupportsHttp { get; set; }
    public bool SupportsHttps { get; set; }
    public bool SupportsWebSocket { get; set; }
    public bool SupportsTcpIp { get; set; }
    public bool SupportsCloudApi { get; set; }
    public bool SupportsPushSdk { get; set; }
    public bool SupportsMqtt { get; set; }
    public bool SupportsMqtts { get; set; }
    public bool SupportsWifi { get; set; }
    public bool SupportsEthernet { get; set; }
    public bool SupportsUsb { get; set; }
    public string? SupportedTlsVersions { get; set; }
    public string? SupportedProtocols { get; set; }
    public int? MaxUserCapacity { get; set; }
    public int? MaxFaceCapacity { get; set; }
    public int? MaxFingerprintCapacity { get; set; }
    public int? MaxCardCapacity { get; set; }
    public int? MaxAttendanceRecordCapacity { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? SoftwareVersion { get; set; }
    public string? OperatingSystem { get; set; }
    public string? ProcessorInfo { get; set; }
    public string? MemoryInfo { get; set; }
    public string? StorageInfo { get; set; }
    public string? DisplayInfo { get; set; }
    public string? CameraInfo { get; set; }
    public string? SensorInfo { get; set; }
    public string? PowerRequirement { get; set; }
    public string? OperatingTemperature { get; set; }
    public string? Dimensions { get; set; }
    public string? Weight { get; set; }
    public string? Features { get; set; }
    public string? TechnicalSpecifications { get; set; }
    public decimal? Price { get; set; }
    public string? CurrencyKey { get; set; }
    public int? WarrantyPeriodMonths { get; set; }
    public string? WarrantyDescription { get; set; }
    public bool IsIntegrationSupported { get; set; }
    public string? IntegrationType { get; set; }
    public string? ApiDocumentationUrl { get; set; }
    public string? SdkDocumentationUrl { get; set; }
    public string? ImageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? BrochureUrl { get; set; }
    public string? ManualUrl { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? AdditionalInfo { get; set; }
    public string? Remark { get; set; }
    public bool IsRecommended { get; set; }
    public bool IsActive { get; set; } = true;
    #endregion
}

/// <summary>Creates a Host-managed device model.</summary>
public sealed class CreateDeviceMasterRequestDTO : DeviceMasterRequestDTO { }

/// <summary>Updates a Host-managed device model.</summary>
public sealed class UpdateDeviceMasterRequestDTO : DeviceMasterRequestDTO
{
    /// <summary>Gets or sets the DeviceMaster identifier.</summary>
    public long Id { get; set; }
    /// <summary>Gets or sets the occupied state before the device is registered with a Institution.</summary>
    public bool IsOccupied { get; set; }
}

/// <summary>Changes the active state of a Host-managed device model.</summary>
public sealed class UpdateDeviceMasterStatusRequestDTO
{
    /// <summary>Gets or sets the DeviceMaster identifier.</summary>
    public long Id { get; set; }
    /// <summary>Gets or sets the requested active state.</summary>
    public bool IsActive { get; set; }
}

/// <summary>Supplies server-side database paging and filtering for DeviceMaster records.</summary>
public sealed class GetDeviceMasterListRequestDTO
{
    /// <summary>Gets or sets a search term.</summary>
    public string? Search { get; set; }
    /// <summary>Gets or sets an optional functional device type filter.</summary>
    public DeviceType? DeviceType { get; set; }
    /// <summary>Gets or sets an optional active-state filter.</summary>
    public bool? IsActive { get; set; }
    /// <summary>Gets or sets an optional occupied-state filter.</summary>
    public bool? IsOccupied { get; set; }
    /// <summary>Gets or sets the requested page number.</summary>
    public int PageNumber { get; set; } = 1;
    /// <summary>Gets or sets the requested page size.</summary>
    public int PageSize { get; set; } = 10;
}

/// <summary>Represents a Host-managed device model returned by the API.</summary>
public sealed class DeviceMasterResponseDTO : DeviceMasterRequestDTO
{
    /// <summary>Gets or sets the DeviceMaster identifier.</summary>
    public long Id { get; set; }
    /// <summary>Gets or sets whether this device is already registered with a Institution.</summary>
    public bool IsOccupied { get; set; }
    /// <summary>Gets or sets the display name of the functional device type.</summary>
    public string DeviceTypeName { get; set; } = string.Empty;
    /// <summary>Gets or sets the creation timestamp.</summary>
    public DateTime AddedDateTime { get; set; }
    /// <summary>Gets or sets the most recent update timestamp.</summary>
    public DateTime? UpdatedDateTime { get; set; }
}

/// <summary>
/// Carries the encrypted Host-selected Institution identifier and permission metadata
/// required by InstitutionDevice endpoints.
/// </summary>
public class InstitutionDeviceAccessRequestDTO : PermissionRequestDTO
{
    /// <summary>
    /// Gets or sets the encrypted Institution identifier selected by a Host user.
    /// Institution StaffMember requests derive their Institution scope from the trusted token.
    /// </summary>
    public string? InstitutionId { get; set; }
}

/// <summary>Supplies editable installation values for a physical Institution device.</summary>
public class InstitutionDeviceRequestDTO : InstitutionDeviceAccessRequestDTO
{
    #region Properties
    public long CampusId { get; set; }
    public long DeviceMasterId { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public DateTime? InstalledDateTime { get; set; }
    public long? InstalledBy { get; set; }
    public string? InstallationRemark { get; set; }
    public bool IsAttendanceDevice { get; set; } = true;
    public string? Description { get; set; }
    public string? Remark { get; set; }
    public bool IsActive { get; set; } = true;
    #endregion
}

/// <summary>Creates a Host-managed physical Institution device.</summary>
public sealed class CreateInstitutionDeviceRequestDTO : InstitutionDeviceRequestDTO { }

/// <summary>Updates a Host-managed physical Institution device without altering runtime telemetry.</summary>
public sealed class UpdateInstitutionDeviceRequestDTO : InstitutionDeviceRequestDTO
{
    /// <summary>Gets or sets the encrypted InstitutionDevice identifier.</summary>
    public string Id { get; set; } = string.Empty;
}

/// <summary>Changes the active state of a physical Institution device.</summary>
public sealed class UpdateInstitutionDeviceStatusRequestDTO : InstitutionDeviceAccessRequestDTO
{
    /// <summary>Gets or sets the encrypted InstitutionDevice identifier.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Gets or sets the requested active state.</summary>
    public bool IsActive { get; set; }
}

/// <summary>Supplies database paging and filtering for physical Institution-device management.</summary>
public sealed class GetInstitutionDeviceListRequestDTO : InstitutionDeviceAccessRequestDTO
{
    public string? Search { get; set; }
    public long? CampusId { get; set; }
    public long? DeviceMasterId { get; set; }
    public bool? IsAttendanceDevice { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>Represents a physical Institution device returned by the API without exposing raw identifiers.</summary>
public sealed class InstitutionDeviceResponseDTO
{
    /// <summary>Gets or sets the encrypted Institution identifier.</summary>
    public string InstitutionId { get; set; } = string.Empty;
    public long CampusId { get; set; }
    /// <summary>Host-only DeviceMaster catalogue identifier. Omitted from Institution responses.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? DeviceMasterId { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public DateTime? InstalledDateTime { get; set; }
    public long? InstalledBy { get; set; }
    public string? InstallationRemark { get; set; }
    public bool IsAttendanceDevice { get; set; }
    public string? Description { get; set; }
    public string? Remark { get; set; }
    public bool IsActive { get; set; }
    /// <summary>Gets or sets the encrypted InstitutionDevice identifier.</summary>
    public string Id { get; set; } = string.Empty;
    public string? InstitutionName { get; set; }
    public string? CampusName { get; set; }
    public string? LocationCode { get; set; }
    /// <summary>Host-only DeviceMaster catalogue name. Omitted from Institution responses.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DeviceMasterName { get; set; }

    /// <summary>Host-only DeviceMaster catalogue model. Omitted from Institution responses.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DeviceMasterModelNo { get; set; }
    public bool HasConfiguration { get; set; }
    public DateTime AddedDateTime { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}

/// <summary>Supplies editable connection settings for a Institution device.</summary>
public class InstitutionDeviceConfigurationRequestDTO : InstitutionDeviceAccessRequestDTO
{
    /// <summary>Gets or sets the encrypted InstitutionDevice identifier.</summary>
    public string InstitutionDeviceId { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }
    public int? DevicePort { get; set; }
    /// <summary>
    /// Legacy MQTT-only field. Existing UI clients may send it; new clients must
    /// use <see cref="CommandTransport"/> instead.
    /// </summary>
    public DeviceCommunicationProtocol? MqttTransport { get; set; }
    /// <summary>Generic transport selected for command dispatch and inbound device traffic.</summary>
    public DeviceCommunicationProtocol? CommandTransport { get; set; }
    public string? ServerHost { get; set; }
    public int? ServerPort { get; set; }
    public string? ServerPath { get; set; }
    public string? ServerUrl { get; set; }
    public string? PushMode { get; set; }
    public int? HeartbeatIntervalSeconds { get; set; }
    public string? TimeZoneId { get; set; }
    public string? Configuration { get; set; }
    public bool IsEnrollmentEnabled { get; set; } = true;
    public bool IsAttendancePushEnabled { get; set; } = true;
    public bool IsAutoSyncEnabled { get; set; } = true;
}

/// <summary>Creates one connection configuration for a Institution device.</summary>
public sealed class CreateInstitutionDeviceConfigurationRequestDTO : InstitutionDeviceConfigurationRequestDTO { }

/// <summary>Updates one connection configuration for a Institution device.</summary>
public sealed class UpdateInstitutionDeviceConfigurationRequestDTO : InstitutionDeviceConfigurationRequestDTO
{
    /// <summary>Gets or sets the encrypted InstitutionDeviceConfiguration identifier.</summary>
    public string Id { get; set; } = string.Empty;
}

/// <summary>
/// Requests a new one-time HTTPS device-gateway address. This endpoint is
/// authenticated and the returned bearer URL must be installed on the device
/// immediately; it is never returned by configuration read APIs.
/// </summary>
public sealed class RotateInstitutionDeviceHttpsIngressTokenRequestDTO : InstitutionDeviceAccessRequestDTO
{
    /// <summary>
    /// Gets or sets the encrypted InstitutionDeviceConfiguration identifier. Institution
    /// administrators use this value when rotating from a configuration screen.
    /// </summary>
    public string? InstitutionDeviceConfigurationId { get; set; }

    /// <summary>
    /// Gets or sets the encrypted InstitutionDevice identifier. Host users use this
    /// value to issue the gateway URL without reading Institution configuration.
    /// </summary>
    public string? InstitutionDeviceId { get; set; }
}

/// <summary>Contains the one-time HTTPS gateway URL to copy to the physical device.</summary>
public sealed class InstitutionDeviceHttpsIngressEndpointResponseDTO
{
    public string GatewayUrl { get; set; } = string.Empty;
}

#region Initial Device Provisioning and Runtime Configuration

/// <summary>Requests a short-lived Host-issued initial HTTPS route for an unassigned device.</summary>
public sealed class IssueInitialDeviceBootstrapRequestDTO : PermissionRequestDTO
{
    /// <summary>Gets or sets the selected unassigned HTTPS-capable device identifier.</summary>
    public long DeviceMasterId { get; set; }

    /// <summary>Gets or sets the bootstrap URL lifetime in minutes. Defaults to two hours.</summary>
    public int LifetimeMinutes { get; set; } = 120;
}

/// <summary>Contains the one-time initial URL returned to the Host provisioning user.</summary>
public sealed class InitialDeviceBootstrapResponseDTO
{
    /// <summary>Gets or sets the physical device serial number.</summary>
    public string DeviceSerialNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the one-time HTTPS route to copy into the physical device.</summary>
    public string InitialGatewayUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the initial poll interval in seconds.</summary>
    public int HeartbeatIntervalSeconds { get; set; }

    /// <summary>Gets or sets the UTC expiry time for the one-time route.</summary>
    public DateTime ExpiresDateTime { get; set; }
}

/// <summary>Supplies the approved Institution-admin runtime settings for one configured device.</summary>
public sealed class ApplyInstitutionDeviceRuntimeConfigurationRequestDTO : InstitutionDeviceAccessRequestDTO
{
    /// <summary>Gets or sets the encrypted physical Institution device identifier.</summary>
    public string InstitutionDeviceId { get; set; } = string.Empty;

    /// <summary>Gets or sets the current local WebServer password for immediate device command delivery.</summary>
    public string CurrentWebServerPassword { get; set; } = string.Empty;

    /// <summary>Gets or sets the requested device polling interval in seconds.</summary>
    public int HeartbeatIntervalSeconds { get; set; } = 20;

    /// <summary>Gets or sets the optional device volume between 0 and 15.</summary>
    public int? Volume { get; set; }

    /// <summary>
    /// Legacy field retained for request compatibility. Local Web UI/API access
    /// is now changed only through the typed settings/web-access endpoint.
    /// </summary>
    public bool DisableLocalWebServer { get; set; } = true;

    /// <summary>
    /// Legacy field retained for request compatibility. Use the typed
    /// settings/web-access endpoint to rotate the local Web UI/API password.
    /// </summary>
    public string? NewWebServerPassword { get; set; }

    /// <summary>Gets or sets whether the device should reboot after applying the queued configuration.</summary>
    public bool RebootAfterApply { get; set; } = true;
}

/// <summary>Requests an outbound HTTPS reboot for a configured Institution device.</summary>
public sealed class RebootInstitutionDeviceRequestDTO : InstitutionDeviceAccessRequestDTO
{
    /// <summary>Gets or sets the encrypted physical Institution device identifier.</summary>
    public string InstitutionDeviceId { get; set; } = string.Empty;
}

/// <summary>Contains durable server-side tracking identifiers for a queued runtime configuration.</summary>
public sealed class InstitutionDeviceRuntimeConfigurationResponseDTO
{
    /// <summary>Gets or sets the queued configuration command identifier.</summary>
    public long ConfigurationCommandId { get; set; }

    /// <summary>Gets or sets the queued configuration tracking identifier.</summary>
    public Guid ConfigurationTrackingId { get; set; }

    /// <summary>Gets or sets the optional queued reboot command identifier.</summary>
    public long? RebootCommandId { get; set; }

    /// <summary>Gets or sets the optional queued reboot tracking identifier.</summary>
    public Guid? RebootTrackingId { get; set; }

    /// <summary>Gets or sets the durable command status at queue time.</summary>
    public string Status { get; set; } = string.Empty;
}

#endregion

/// <summary>Supplies paging and optional filters for Institution device configurations.</summary>
public sealed class GetInstitutionDeviceConfigurationListRequestDTO : InstitutionDeviceAccessRequestDTO
{
    public string? Search { get; set; }
    /// <summary>Optional encrypted InstitutionDevice identifier filter.</summary>
    public string? InstitutionDeviceId { get; set; }

    /// <summary>Resolved server-only value used by the repository after authorization.</summary>
    [JsonIgnore]
    public long? ResolvedInstitutionDeviceId { get; set; }
    public DeviceCommunicationProtocol? MqttTransport { get; set; }
    public DeviceCommunicationProtocol? CommandTransport { get; set; }
    public bool? IsEnrollmentEnabled { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>Represents a Institution device connection configuration without exposing raw identifiers.</summary>
public sealed class InstitutionDeviceConfigurationResponseDTO
{
    public string InstitutionId { get; set; } = string.Empty;
    /// <summary>Gets or sets the encrypted InstitutionDeviceConfiguration identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the encrypted InstitutionDevice identifier.</summary>
    public string InstitutionDeviceId { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }
    public int? DevicePort { get; set; }
    public DeviceCommunicationProtocol? MqttTransport { get; set; }
    public DeviceCommunicationProtocol? CommandTransport { get; set; }
    public string? ServerHost { get; set; }
    public int? ServerPort { get; set; }
    public string? ServerPath { get; set; }
    public string? ServerUrl { get; set; }
    public string? PushMode { get; set; }
    public int? HeartbeatIntervalSeconds { get; set; }
    public string? TimeZoneId { get; set; }
    public string? Configuration { get; set; }
    public bool IsEnrollmentEnabled { get; set; }
    public bool IsAttendancePushEnabled { get; set; }
    public bool IsAutoSyncEnabled { get; set; }
    public DateTime? LastHeartbeatDateTime { get; set; }
    public DateTime? LastSyncDateTime { get; set; }
    public DateTime? LastAttendanceReceivedDateTime { get; set; }
    public DateTime? LastSuccessfulConnectionDateTime { get; set; }
    public DateTime? LastFailedConnectionDateTime { get; set; }
    public string? LastConnectionError { get; set; }
    public string? DeviceCode { get; set; }
    public string? DeviceName { get; set; }
    /// <summary>Host-only DeviceMaster catalogue name. Omitted from Institution responses.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DeviceMasterName { get; set; }

    /// <summary>Host-only DeviceMaster catalogue serial. Omitted from Institution responses.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DeviceMasterSNo { get; set; }
    public DateTime AddedDateTime { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
}
