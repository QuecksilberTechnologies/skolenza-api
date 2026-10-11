// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Represents InstitutionConfiguration tables and the shared read-only InstitutionDevice model.
// ================================================================


namespace skolenza.domain.Entity;

/// <summary>Represents a Institution-owned physical work location.</summary>
public partial class Campus
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public string LocationCode { get; set; } = null!;
    public string LocationName { get; set; } = null!;
    public short LocationType { get; set; }
    public int CountryId { get; set; }
    public int? StateId { get; set; }
    public int? DistrictId { get; set; }
    public int? LocalityId { get; set; }
    public string? Address { get; set; }
    public string? Landmark { get; set; }
    public string? PostalCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int? GeoFenceRadiusMeters { get; set; }
    public string TimeZoneId { get; set; } = null!;
    public bool IsHeadOffice { get; set; }
    public bool IsGeoFenceEnabled { get; set; }
    public bool IsAttendanceAllowed { get; set; }
    public bool IsBiometricEnabled { get; set; }
    public bool IsActive { get; set; }
    public bool IsSoftDeleted { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public long? SoftDeletedById { get; set; }
    public DateTime? SoftDeletedDateTime { get; set; }
    public virtual Institution Institution { get; set; } = null!;
    public virtual Country Country { get; set; } = null!;
    public virtual District? District { get; set; }
    public virtual Locality? Locality { get; set; }
    public virtual ICollection<InstitutionDevice> InstitutionDevice { get; set; } = new List<InstitutionDevice>();
    public virtual ICollection<StaffMemberLocationAssignment> StaffMemberLocationAssignment { get; set; } = new List<StaffMemberLocationAssignment>();
    public virtual ICollection<StaffMemberWorkArrangement> StaffMemberWorkArrangement { get; set; } = new List<StaffMemberWorkArrangement>();
    public virtual ICollection<StaffMemberWorkPattern> StaffMemberWorkPattern { get; set; } = new List<StaffMemberWorkPattern>();
    public virtual ICollection<StaffMemberWorkModeOverrideRequest> StaffMemberWorkModeOverrideRequest { get; set; } = new List<StaffMemberWorkModeOverrideRequest>();
}

/// <summary>Represents executable attendance configuration for a Institution policy type.</summary>
public partial class AttendancePolicy
{
    public int Id { get; set; }
    public long InstitutionId { get; set; }
    public int PolicyTypeId { get; set; }
    public string PolicyName { get; set; } = null!;
    public string? Description { get; set; }
    public string? Remark { get; set; }
    public short AttendanceLocationScope { get; set; }
    public bool AllowBiometric { get; set; }
    public bool AllowMobile { get; set; }
    public bool AllowWeb { get; set; }
    public bool AllowManualAttendance { get; set; }
    public bool AllowWorkFromHome { get; set; }
    public bool RequireGeoFenceForOffice { get; set; }
    public bool RequireGpsForRemote { get; set; }
    public bool AllowOutsideLocationWithApproval { get; set; }
    public bool IsActive { get; set; }
    public bool IsSoftDeleted { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public long? SoftDeletedById { get; set; }
    public DateTime? SoftDeletedDateTime { get; set; }
    public virtual Institution Institution { get; set; } = null!;
    public virtual PolicyType PolicyType { get; set; } = null!;
    public virtual ICollection<StaffMemberWorkArrangement> StaffMemberWorkArrangement { get; set; } = new List<StaffMemberWorkArrangement>();
}

/// <summary>Represents an staffMember's eligible Institution work location.</summary>
public partial class StaffMemberLocationAssignment
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StaffMemberId { get; set; }
    public long CampusId { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsAttendanceAllowed { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public bool IsSoftDeleted { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public long? SoftDeletedById { get; set; }
    public DateTime? SoftDeletedDateTime { get; set; }
    public virtual Institution Institution { get; set; } = null!;
    public virtual StaffMember StaffMember { get; set; } = null!;
    public virtual Campus Campus { get; set; } = null!;
}

/// <summary>Represents an staffMember's enrollment identifier on a Host-managed Institution device.</summary>
public partial class StaffMemberDeviceEnrollment
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StaffMemberId { get; set; }
    public long InstitutionDeviceId { get; set; }
    /// <summary>Location snapshot taken from the InstitutionDevice at enrollment time.</summary>
    public long CampusId { get; set; }
    /// <summary>Optional Host-issued active card bound to this device user.</summary>
    public long? InstitutionCardMasterId { get; set; }
    public string EnrollId { get; set; } = null!;
    /// <summary>Legacy display-only field. New credential flows use InstitutionCardMasterId.</summary>
    public string? CardNumber { get; set; }
    /// <summary>SHA-256 of the last submitted face file; raw biometric image is never persisted here.</summary>
    public string? FaceImageHash { get; set; }
    public bool IsFaceEnrolled { get; set; }
    public bool IsFingerprintEnrolled { get; set; }
    public bool IsCardEnrolled { get; set; }
    public short FaceDeploymentStatus { get; set; }
    public short CardDeploymentStatus { get; set; }
    public short PinDeploymentStatus { get; set; }
    public long? FaceDeviceCommandId { get; set; }
    public long? CardDeviceCommandId { get; set; }
    public long? PinDeviceCommandId { get; set; }
    /// <summary>Most recent enable/disable command. Its status is the device-confirmation source for IsActive.</summary>
    public long? UserActivationDeviceCommandId { get; set; }
    public DateTime? AccessEffectiveFromDateTime { get; set; }
    public DateTime? AccessEffectiveToDateTime { get; set; }
    public DateTime? LastSyncedDateTime { get; set; }
    public bool IsActive { get; set; }
    public bool IsSoftDeleted { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public long? SoftDeletedById { get; set; }
    public DateTime? SoftDeletedDateTime { get; set; }
    public virtual Institution Institution { get; set; } = null!;
    public virtual StaffMember StaffMember { get; set; } = null!;
    public virtual InstitutionDevice InstitutionDevice { get; set; } = null!;
    public virtual Campus Campus { get; set; } = null!;
    public virtual InstitutionCardMaster? InstitutionCardMaster { get; set; }
    public virtual DeviceCommand? FaceDeviceCommand { get; set; }
    public virtual DeviceCommand? CardDeviceCommand { get; set; }
    public virtual DeviceCommand? PinDeviceCommand { get; set; }
    public virtual DeviceCommand? UserActivationDeviceCommand { get; set; }
    public virtual ICollection<StaffMemberDeviceAccessWindow> StaffMemberDeviceAccessWindows { get; set; } = new List<StaffMemberDeviceAccessWindow>();
}

/// <summary>
/// Represents one weekly access window for an staffMember on one physical device.
/// It is deliberately a child of StaffMemberDeviceEnrollment rather than a shared
/// master because the user requested one per-device staffMember configuration.
/// </summary>
public partial class StaffMemberDeviceAccessWindow
{
    public long Id { get; set; }
    public long StaffMemberDeviceEnrollmentId { get; set; }
    public short DayOfWeek { get; set; }
    public TimeOnly StartLocalTime { get; set; }
    public TimeOnly EndLocalTime { get; set; }
    public bool IsActive { get; set; }
    public bool IsSoftDeleted { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public long? SoftDeletedById { get; set; }
    public DateTime? SoftDeletedDateTime { get; set; }
    public virtual StaffMemberDeviceEnrollment StaffMemberDeviceEnrollment { get; set; } = null!;
}

/// <summary>Represents one Host-managed physical card held in a Institution's card inventory.</summary>
public partial class InstitutionCardMaster
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public string CardNumberEncrypted { get; set; } = null!;
    public string CardNumberLookupHash { get; set; } = null!;
    public string? CardReference { get; set; }
    public short CardStatus { get; set; }
    public string PurchaseCurrencyCode { get; set; } = null!;
    public decimal UnitPurchasePriceExcludingTax { get; set; }
    public int? SupplierCountryId { get; set; }
    public int? SupplierStateId { get; set; }
    public int? PlaceOfSupplyCountryId { get; set; }
    public int? PlaceOfSupplyStateId { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierTaxRegistrationNumber { get; set; }
    public string? PurchaseInvoiceNumber { get; set; }
    public DateOnly? PurchaseInvoiceDate { get; set; }
    public short TaxTreatment { get; set; }
    public decimal CgstRate { get; set; }
    public decimal CgstAmount { get; set; }
    public decimal SgstRate { get; set; }
    public decimal SgstAmount { get; set; }
    public decimal IgstRate { get; set; }
    public decimal IgstAmount { get; set; }
    public string? ForeignTaxLabel { get; set; }
    public decimal ForeignTaxRate { get; set; }
    public decimal ForeignTaxAmount { get; set; }
    public decimal CustomsDutyAmount { get; set; }
    public decimal FreightAmount { get; set; }
    public decimal LandedCost { get; set; }
    public bool IsActive { get; set; }
    public bool IsSoftDeleted { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public long? SoftDeletedById { get; set; }
    public DateTime? SoftDeletedDateTime { get; set; }
    public virtual Institution Institution { get; set; } = null!;
    public virtual ICollection<StaffMemberDeviceEnrollment> StaffMemberDeviceEnrollments { get; set; } = new List<StaffMemberDeviceEnrollment>();
}

/// <summary>Represents an staffMember's active or historical work arrangement.</summary>
public partial class StaffMemberWorkArrangement
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StaffMemberId { get; set; }
    public int? AttendancePolicyId { get; set; }
    public long? PolicyVersionId { get; set; }
    public long? PrimaryCampusId { get; set; }
    public short WorkMode { get; set; }
    public short? HybridType { get; set; }
    public short? MinimumOfficeDaysPerWeek { get; set; }
    public short? MinimumOfficeDaysPerMonth { get; set; }
    public short? MaximumWFHDaysPerMonth { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public bool IsSoftDeleted { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public long? SoftDeletedById { get; set; }
    public DateTime? SoftDeletedDateTime { get; set; }
    public virtual Institution Institution { get; set; } = null!;
    public virtual StaffMember StaffMember { get; set; } = null!;
    public virtual AttendancePolicy? AttendancePolicy { get; set; }
    public virtual PolicyVersion? PolicyVersion { get; set; }
    public virtual Campus? PrimaryCampus { get; set; }
    public virtual ICollection<StaffMemberWorkPattern> StaffMemberWorkPattern { get; set; } = new List<StaffMemberWorkPattern>();
    public virtual ICollection<StaffMemberWorkModeOverrideRequest> StaffMemberWorkModeOverrideRequest { get; set; } = new List<StaffMemberWorkModeOverrideRequest>();
}

/// <summary>Represents one day in an staffMember work-arrangement pattern.</summary>
public partial class StaffMemberWorkPattern
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StaffMemberWorkArrangementId { get; set; }
    public short DayOfWeek { get; set; }
    public short WorkMode { get; set; }
    public long? CampusId { get; set; }
    public bool IsWorkingDay { get; set; }
    public bool IsActive { get; set; }
    public bool IsSoftDeleted { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public long? SoftDeletedById { get; set; }
    public DateTime? SoftDeletedDateTime { get; set; }
    public virtual Institution Institution { get; set; } = null!;
    public virtual StaffMemberWorkArrangement StaffMemberWorkArrangement { get; set; } = null!;
    public virtual Campus? Campus { get; set; }
}

/// <summary>Represents a temporary work-mode deviation pending a future approval workflow.</summary>
public partial class StaffMemberWorkModeOverrideRequest
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long StaffMemberId { get; set; }
    public long? StaffMemberWorkArrangementId { get; set; }
    public short RequestedWorkMode { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public long? CampusId { get; set; }
    public string Reason { get; set; } = null!;
    public short ApprovalStatus { get; set; }
    public long? ApprovedById { get; set; }
    public DateTime? ApprovedDateTime { get; set; }
    public string? ApprovalRemark { get; set; }
    public long? RejectedById { get; set; }
    public DateTime? RejectedDateTime { get; set; }
    public string? RejectionRemark { get; set; }
    public bool IsActive { get; set; }
    public bool IsSoftDeleted { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public long? SoftDeletedById { get; set; }
    public DateTime? SoftDeletedDateTime { get; set; }
    public virtual Institution Institution { get; set; } = null!;
    public virtual StaffMember StaffMember { get; set; } = null!;
    public virtual StaffMemberWorkArrangement? StaffMemberWorkArrangement { get; set; }
    public virtual Campus? Campus { get; set; }
}

/// <summary>Represents a Host-managed physical device installed for a Institution.</summary>
public partial class InstitutionDevice
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long CampusId { get; set; }
    public long DeviceMasterId { get; set; }
    public string DeviceCode { get; set; } = null!;
    public string? DeviceName { get; set; }
    public DateTime? InstalledDateTime { get; set; }
    public long? InstalledBy { get; set; }
    public string? InstallationRemark { get; set; }
    public bool IsAttendanceDevice { get; set; }
    public string? Description { get; set; }
    public string? Remark { get; set; }
    public bool IsActive { get; set; }
    public bool IsSoftDeleted { get; set; }
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public long? SoftDeletedById { get; set; }
    public DateTime? SoftDeletedDateTime { get; set; }
    public virtual Institution Institution { get; set; } = null!;
    public virtual Campus Campus { get; set; } = null!;
    public virtual DeviceMaster DeviceMaster { get; set; } = null!;
    public virtual InstitutionDeviceConfiguration? InstitutionDeviceConfiguration { get; set; }
}

/// <summary>Represents one connection and runtime configuration for a Institution device.</summary>
public partial class InstitutionDeviceConfiguration
{
    public long Id { get; set; }
    public long InstitutionDeviceId { get; set; }
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }
    public int? DevicePort { get; set; }
    /// <summary>
    /// Legacy connection transport retained only so existing HTTP device configuration
    /// values are not reinterpreted by the MQTT/MQTTS production stack.
    /// </summary>
    public short? CommunicationType { get; set; }
    /// <summary>
    /// Backward-compatible device-command transport column. Values are MQTT, MQTTS,
    /// or device-initiated HTTPS polling.
    /// </summary>
    public short? MqttTransport { get; set; }
    /// <summary>
    /// Generic command transport. New integrations use this field instead of the
    /// MQTT-named legacy column so HTTPS and WebSocket can coexist cleanly.
    /// </summary>
    public short? CommandTransport { get; set; }
    /// <summary>
    /// SHA-256 hash of the one-time HTTPS gateway bearer token. The plaintext token
    /// is deliberately never persisted or returned by configuration read endpoints.
    /// </summary>
    public string? HttpsIngressTokenHash { get; set; }
    /// <summary>
    /// SHA-256 hash for a replacement HTTPS gateway URL awaiting the device's
    /// first successful poll. The current gateway remains valid until promotion.
    /// </summary>
    public string? PendingHttpsIngressTokenHash { get; set; }
    /// <summary>Expiry for the pending replacement gateway token.</summary>
    public DateTime? PendingHttpsIngressTokenExpiresDateTime { get; set; }
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
    public long AddedById { get; set; }
    public DateTime AddedDateTime { get; set; }
    public long? UpdatedById { get; set; }
    public DateTime? UpdatedDateTime { get; set; }
    public virtual InstitutionDevice InstitutionDevice { get; set; } = null!;
}
