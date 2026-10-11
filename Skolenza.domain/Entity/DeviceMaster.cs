// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Represents a Host-managed global physical device model catalog record.
// ================================================================

namespace skolenza.domain.Entity;

/// <summary>Represents a global physical device model maintained by Host users.</summary>
public partial class DeviceMaster
{
    #region Properties

    public long Id { get; set; }
    public string SNo { get; set; } = null!;
    public string DeviceCode { get; set; } = null!;
    public string DeviceName { get; set; } = null!;
    public string ModelNo { get; set; } = null!;
    public string? ProductCode { get; set; }
    public string CompanyName { get; set; } = null!;
    public string? BrandName { get; set; }
    public string? CountryOfOrigin { get; set; }
    public string? ManufacturerWebsite { get; set; }
    public string? SupportEmail { get; set; }
    public string? SupportContactNumber { get; set; }
    public short DeviceType { get; set; }
    public bool IsAttendanceDevice { get; set; }
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
    public bool IsActive { get; set; }
    public bool IsOccupied { get; set; }
    public bool IsSoftDeleted { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public long? SoftDeletedById { get; set; }
    public DateTime? SoftDeletedDateTime { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>Gets physical Institution devices using this device model.</summary>
    public virtual ICollection<InstitutionDevice> InstitutionDevice { get; set; } = new List<InstitutionDevice>();

    #endregion
}
