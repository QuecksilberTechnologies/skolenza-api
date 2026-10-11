// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines request, filter, status, and response contracts for InstitutionConfiguration management.
// ================================================================

using skolenza.application.DTOs.BaseDTO;
using skolenza.domain.Entity;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;

namespace skolenza.application.DTOS.InstitutionConfiguration;

/// <summary>
/// Carries the encrypted Host-selected Institution identifier and the module-operation
/// permission context required by Campus endpoints.
/// </summary>
public class CampusAccessRequestDTO : PermissionRequestDTO
{
    /// <summary>
    /// Gets or sets the encrypted Institution identifier selected by a Host user.
    /// Institution StaffMember requests are always scoped from their trusted token and
    /// therefore do not use this value.
    /// </summary>
    public string? InstitutionId { get; set; }
}

/// <summary>Supplies client-editable values for a Institution location.</summary>
public class CreateCampusRequestDTO : CampusAccessRequestDTO
{
    #region Properties
    public string LocationCode { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public CampusType LocationType { get; set; }
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
    public string TimeZoneId { get; set; } = string.Empty;
    public bool IsHeadOffice { get; set; }
    public bool IsGeoFenceEnabled { get; set; }
    public bool IsAttendanceAllowed { get; set; } = true;
    public bool IsBiometricEnabled { get; set; }
    public bool IsActive { get; set; } = true;
    #endregion
}

/// <summary>Supplies client-editable values for an existing Institution location.</summary>
public sealed class UpdateCampusRequestDTO : CreateCampusRequestDTO
{
    public long Id { get; set; }
}

/// <summary>Supplies a Institution-location active-state change.</summary>
public sealed class UpdateCampusStatusRequestDTO : CampusAccessRequestDTO
{
    public long Id { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Defines database-side filters for Institution locations.</summary>
public sealed class CampusFilterRequestDTO : CampusAccessRequestDTO
{
    public string? Search { get; set; }
    public int? CountryId { get; set; }
    public int? StateId { get; set; }
    public int? DistrictId { get; set; }
    public int? LocalityId { get; set; }
    public CampusType? LocationType { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>Describes a Institution location with geographic display context.</summary>
public sealed class CampusResponseDTO
{
    public long Id { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public CampusType LocationType { get; set; }
    public string LocationTypeName { get; set; } = string.Empty;
    public int CountryId { get; set; }
    public string CountryName { get; set; } = string.Empty;
    public int? StateId { get; set; }
    public string? StateName { get; set; }
    public int? DistrictId { get; set; }
    public string? DistrictName { get; set; }
    public int? LocalityId { get; set; }
    public string? LocalityName { get; set; }
    public string? Address { get; set; }
    public string? Landmark { get; set; }
    public string? PostalCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int? GeoFenceRadiusMeters { get; set; }
    public string TimeZoneId { get; set; } = string.Empty;
    public bool IsHeadOffice { get; set; }
    public bool IsGeoFenceEnabled { get; set; }
    public bool IsAttendanceAllowed { get; set; }
    public bool IsBiometricEnabled { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Supplies client-editable values for an attendance policy.</summary>
public class CreateAttendancePolicyRequestDTO : PermissionRequestDTO
{
    #region Properties
    public int PolicyTypeId { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Remark { get; set; }
    public AttendanceLocationScope AttendanceLocationScope { get; set; }
    public bool AllowBiometric { get; set; } = true;
    public bool AllowMobile { get; set; }
    public bool AllowWeb { get; set; }
    public bool AllowManualAttendance { get; set; }
    public bool AllowWorkFromHome { get; set; }
    public bool RequireGeoFenceForOffice { get; set; } = true;
    public bool RequireGpsForRemote { get; set; } = true;
    public bool AllowOutsideLocationWithApproval { get; set; }
    public bool IsActive { get; set; } = true;
    #endregion
}

/// <summary>Supplies client-editable values for an existing attendance policy.</summary>
public sealed class UpdateAttendancePolicyRequestDTO : CreateAttendancePolicyRequestDTO
{
    public int Id { get; set; }
}

/// <summary>Supplies an attendance-policy active-state change.</summary>
public sealed class UpdateAttendancePolicyStatusRequestDTO : PermissionRequestDTO
{
    public int Id { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Defines database-side filters for attendance policies.</summary>
public sealed class AttendancePolicyFilterRequestDTO : PermissionRequestDTO
{
    public string? Search { get; set; }
    public int? PolicyTypeId { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>Describes an attendance policy and its configured location scope.</summary>
public sealed class AttendancePolicyResponseDTO
{
    public int Id { get; set; }
    public int PolicyTypeId { get; set; }
    public string PolicyTypeName { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Remark { get; set; }
    public AttendanceLocationScope AttendanceLocationScope { get; set; }
    public string AttendanceLocationScopeName { get; set; } = string.Empty;
    public bool AllowBiometric { get; set; }
    public bool AllowMobile { get; set; }
    public bool AllowWeb { get; set; }
    public bool AllowManualAttendance { get; set; }
    public bool AllowWorkFromHome { get; set; }
    public bool RequireGeoFenceForOffice { get; set; }
    public bool RequireGpsForRemote { get; set; }
    public bool AllowOutsideLocationWithApproval { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Supplies client-editable values for an staffMember-location assignment.</summary>
public class CreateStaffMemberLocationAssignmentRequestDTO : PermissionRequestDTO
{
    public string StaffMemberId { get; set; } = string.Empty;
    public long CampusId { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsAttendanceAllowed { get; set; } = true;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Supplies client-editable values for an existing staffMember-location assignment.</summary>
public sealed class UpdateStaffMemberLocationAssignmentRequestDTO : CreateStaffMemberLocationAssignmentRequestDTO
{
    public long Id { get; set; }
}

/// <summary>Supplies an staffMember-location-assignment active-state change.</summary>
public sealed class UpdateStaffMemberLocationAssignmentStatusRequestDTO : PermissionRequestDTO
{
    public long Id { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Defines database-side filters for staffMember-location assignments.</summary>
public sealed class StaffMemberLocationAssignmentFilterRequestDTO : PermissionRequestDTO
{
    public string? StaffMemberId { get; set; }
    [JsonIgnore] public long? ResolvedStaffMemberId { get; set; }
    public long? CampusId { get; set; }
    public bool? IsPrimary { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>Describes one staffMember-to-location configuration record.</summary>
public sealed class StaffMemberLocationAssignmentResponseDTO
{
    public long Id { get; set; }
    public string StaffMemberId { get; set; } = string.Empty;
    public string StaffMemberName { get; set; } = string.Empty;
    public string? StaffMemberCode { get; set; }
    public long CampusId { get; set; }
    public string CampusName { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public bool IsAttendanceAllowed { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Supplies client-editable values for an staffMember device enrollment.</summary>
public class CreateStaffMemberDeviceEnrollmentRequestDTO : PermissionRequestDTO
{
    public string StaffMemberId { get; set; } = string.Empty;
    /// <summary>Encrypted Institution device identifier. The server resolves the physical device and location.</summary>
    public string InstitutionDeviceId { get; set; } = string.Empty;
    /// <summary>Optional encrypted Host-issued card identifier. A typed card number is never accepted here.</summary>
    public string? InstitutionCardId { get; set; }
    /// <summary>Optional inclusive validity period for all configured weekly access windows.</summary>
    public DateTime? AccessEffectiveFromDateTime { get; set; }
    public DateTime? AccessEffectiveToDateTime { get; set; }
    /// <summary>Weekly staffMember/device access windows; the API owns their persistence and lifecycle.</summary>
    public List<StaffMemberDeviceAccessWindowRequestDTO> AccessWindows { get; set; } = new();
    public bool IsActive { get; set; } = true;
}

/// <summary>One local-time weekly access window submitted with an staffMember-device enrollment.</summary>
public sealed class StaffMemberDeviceAccessWindowRequestDTO
{
    public WorkPatternDay DayOfWeek { get; set; }
    public TimeOnly StartLocalTime { get; set; }
    public TimeOnly EndLocalTime { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Supplies client-editable values for an existing staffMember device enrollment.</summary>
public sealed class UpdateStaffMemberDeviceEnrollmentRequestDTO : CreateStaffMemberDeviceEnrollmentRequestDTO
{
    public string Id { get; set; } = string.Empty;
}

/// <summary>Supplies an staffMember-device-enrollment active-state change.</summary>
public sealed class UpdateStaffMemberDeviceEnrollmentStatusRequestDTO : PermissionRequestDTO
{
    public string Id { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>Defines database-side filters for staffMember device enrollments.</summary>
public sealed class StaffMemberDeviceEnrollmentFilterRequestDTO : PermissionRequestDTO
{
    public string? Search { get; set; }
    public string? StaffMemberId { get; set; }
    [JsonIgnore] public long? ResolvedStaffMemberId { get; set; }
    public string? InstitutionDeviceId { get; set; }
    [JsonIgnore] public long? ResolvedInstitutionDeviceId { get; set; }
    public long? CampusId { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>Describes an staffMember enrollment on a Host-managed physical device.</summary>
public sealed class StaffMemberDeviceEnrollmentResponseDTO
{
    public string Id { get; set; } = string.Empty;
    public string StaffMemberId { get; set; } = string.Empty;
    public string StaffMemberName { get; set; } = string.Empty;
    public string? StaffMemberCode { get; set; }
    public string InstitutionDeviceId { get; set; } = string.Empty;
    public string DeviceCode { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string CampusId { get; set; } = string.Empty;
    public string CampusName { get; set; } = string.Empty;
    /// <summary>Device-only global staffMember identifier. It is intentionally not returned to Angular.</summary>
    public string? InstitutionCardId { get; set; }
    public string? MaskedCardNumber { get; set; }
    public bool IsFaceEnrolled { get; set; }
    public bool IsFingerprintEnrolled { get; set; }
    public bool IsCardEnrolled { get; set; }
    public DeviceCredentialDeploymentStatus FaceDeploymentStatus { get; set; }
    public DeviceCredentialDeploymentStatus CardDeploymentStatus { get; set; }
    public DeviceCredentialDeploymentStatus PinDeploymentStatus { get; set; }
    public DeviceCommandStatus? FaceCommandStatus { get; set; }
    public DeviceCommandStatus? CardCommandStatus { get; set; }
    public DeviceCommandStatus? PinCommandStatus { get; set; }
    /// <summary>Live confirmation state for the most recent enable/disable command.</summary>
    public DeviceCommandStatus? UserActivationCommandStatus { get; set; }
    public DateTime? AccessEffectiveFromDateTime { get; set; }
    public DateTime? AccessEffectiveToDateTime { get; set; }
    public List<StaffMemberDeviceAccessWindowResponseDTO> AccessWindows { get; set; } = new();
    public DateTime? LastSyncedDateTime { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>One read-only weekly access window returned as part of its enrollment.</summary>
public sealed class StaffMemberDeviceAccessWindowResponseDTO
{
    public WorkPatternDay DayOfWeek { get; set; }
    public TimeOnly StartLocalTime { get; set; }
    public TimeOnly EndLocalTime { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Queues an uploaded face photo for one existing staffMember-device enrollment.</summary>
public sealed class UpsertStaffMemberDeviceFaceRequestDTO : PermissionRequestDTO
{
    public string EnrollmentId { get; set; } = string.Empty;
    public IFormFile? FaceImage { get; set; }
}

/// <summary>Queues a write-only staffMember device PIN create or replacement.</summary>
public sealed class UpsertStaffMemberDevicePinRequestDTO : PermissionRequestDTO
{
    public string EnrollmentId { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
}

/// <summary>Queues card association change for one existing staffMember-device enrollment.</summary>
public sealed class BindStaffMemberDeviceCardRequestDTO : PermissionRequestDTO
{
    public string EnrollmentId { get; set; } = string.Empty;
    public string InstitutionCardId { get; set; } = string.Empty;
}

/// <summary>Queues removal of one staffMember credential from a physical device.</summary>
public sealed class RemoveStaffMemberDeviceCredentialRequestDTO : PermissionRequestDTO
{
    public string EnrollmentId { get; set; } = string.Empty;
    public StaffMemberDeviceCredentialType CredentialType { get; set; }
}

/// <summary>Names only the staffMember credentials which can be remotely removed.</summary>
public enum StaffMemberDeviceCredentialType : short
{
    Face = 1,
    Card = 2,
    Pin = 3
}

/// <summary>Supplies client-editable values for an staffMember work arrangement.</summary>
public class CreateStaffMemberWorkArrangementRequestDTO : PermissionRequestDTO
{
    public string StaffMemberId { get; set; } = string.Empty;
    public long PolicyVersionId { get; set; }
    public long? PrimaryCampusId { get; set; }
    public WorkMode WorkMode { get; set; }
    public HybridType? HybridType { get; set; }
    public short? MinimumOfficeDaysPerWeek { get; set; }
    public short? MinimumOfficeDaysPerMonth { get; set; }
    public short? MaximumWFHDaysPerMonth { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Supplies client-editable values for an existing staffMember work arrangement.</summary>
public sealed class UpdateStaffMemberWorkArrangementRequestDTO : CreateStaffMemberWorkArrangementRequestDTO
{
    public long Id { get; set; }
}

/// <summary>Supplies an staffMember-work-arrangement active-state change.</summary>
public sealed class UpdateStaffMemberWorkArrangementStatusRequestDTO : PermissionRequestDTO
{
    public long Id { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Defines database-side filters for staffMember work arrangements.</summary>
public sealed class StaffMemberWorkArrangementFilterRequestDTO : PermissionRequestDTO
{
    public string? StaffMemberId { get; set; }
    [JsonIgnore] public long? ResolvedStaffMemberId { get; set; }
    public long? PolicyVersionId { get; set; }
    public long? PrimaryCampusId { get; set; }
    public WorkMode? WorkMode { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>Describes an staffMember work arrangement with policy and location context.</summary>
public sealed class StaffMemberWorkArrangementResponseDTO
{
    public long Id { get; set; }
    public string StaffMemberId { get; set; } = string.Empty;
    public string StaffMemberName { get; set; } = string.Empty;
    public long PolicyVersionId { get; set; }
    public long PolicyId { get; set; }
    public string AttendancePolicyName { get; set; } = string.Empty;
    public long? PrimaryCampusId { get; set; }
    public string? PrimaryCampusName { get; set; }
    public WorkMode WorkMode { get; set; }
    public string WorkModeName { get; set; } = string.Empty;
    public HybridType? HybridType { get; set; }
    public string? HybridTypeName { get; set; }
    public short? MinimumOfficeDaysPerWeek { get; set; }
    public short? MinimumOfficeDaysPerMonth { get; set; }
    public short? MaximumWFHDaysPerMonth { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Supplies the date and optional text used to find effective Attendance policy versions.</summary>
public sealed class AttendancePolicyOptionRequestDTO
{
    /// <summary>Optional policy-effective date; defaults to the server's current UTC date.</summary>
    public DateOnly? EffectiveOn { get; set; }
    public string? Search { get; set; }
}

/// <summary>Describes one effective Published Attendance policy version for dropdown selection.</summary>
public sealed class AttendancePolicyOptionResponseDTO
{
    public long PolicyId { get; set; }
    public long PolicyVersionId { get; set; }
    public string PolicyCode { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public int PolicyTypeId { get; set; }
    public string PolicyTypeCode { get; set; } = string.Empty;
    public string PolicyTypeName { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>Supplies client-editable values for an staffMember work-pattern day.</summary>
public class CreateStaffMemberWorkPatternRequestDTO : PermissionRequestDTO
{
    public long StaffMemberWorkArrangementId { get; set; }
    public WorkPatternDay DayOfWeek { get; set; }
    public WorkMode WorkMode { get; set; }
    public long? CampusId { get; set; }
    public bool IsWorkingDay { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

/// <summary>Supplies client-editable values for an existing staffMember work-pattern day.</summary>
public sealed class UpdateStaffMemberWorkPatternRequestDTO : CreateStaffMemberWorkPatternRequestDTO
{
    public long Id { get; set; }
}

/// <summary>Supplies an staffMember-work-pattern active-state change.</summary>
public sealed class UpdateStaffMemberWorkPatternStatusRequestDTO : PermissionRequestDTO
{
    public long Id { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Defines database-side filters for staffMember work-pattern days.</summary>
public sealed class StaffMemberWorkPatternFilterRequestDTO : PermissionRequestDTO
{
    public long? StaffMemberWorkArrangementId { get; set; }
    public WorkPatternDay? DayOfWeek { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>Describes one staffMember work-pattern day.</summary>
public sealed class StaffMemberWorkPatternResponseDTO
{
    public long Id { get; set; }
    public string StaffMemberId { get; set; } = string.Empty;
    public string StaffMemberName { get; set; } = string.Empty;
    public long StaffMemberWorkArrangementId { get; set; }
    public WorkPatternDay DayOfWeek { get; set; }
    public string DayOfWeekName { get; set; } = string.Empty;
    public WorkMode WorkMode { get; set; }
    public string WorkModeName { get; set; } = string.Empty;
    public long? CampusId { get; set; }
    public string? CampusName { get; set; }
    public bool IsWorkingDay { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Supplies client-editable values for an staffMember temporary work-mode override.</summary>
public class CreateStaffMemberWorkModeOverrideRequestDTO : PermissionRequestDTO
{
    public string StaffMemberId { get; set; } = string.Empty;
    public long? StaffMemberWorkArrangementId { get; set; }
    public WorkMode RequestedWorkMode { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public long? CampusId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

/// <summary>Supplies client-editable values for an existing temporary work-mode override.</summary>
public sealed class UpdateStaffMemberWorkModeOverrideRequestDTO : CreateStaffMemberWorkModeOverrideRequestDTO
{
    public long Id { get; set; }
}

/// <summary>Supplies an override record active-state change without exposing approval fields.</summary>
public sealed class UpdateStaffMemberWorkModeOverrideStatusRequestDTO : PermissionRequestDTO
{
    public long Id { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Supplies an approval or rejection decision for a pending work-mode override.</summary>
public sealed class StaffMemberWorkModeOverrideDecisionRequestDTO : PermissionRequestDTO
{
    public long Id { get; set; }
    public string? Remark { get; set; }
}

/// <summary>Defines database-side filters for staffMember work-mode overrides.</summary>
public sealed class StaffMemberWorkModeOverrideFilterRequestDTO : PermissionRequestDTO
{
    public string? Search { get; set; }
    public string? StaffMemberId { get; set; }
    [JsonIgnore] public long? ResolvedStaffMemberId { get; set; }
    public WorkMode? RequestedWorkMode { get; set; }
    public WorkModeOverrideApprovalStatus? ApprovalStatus { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>Describes a temporary work-mode override, including read-only approval state.</summary>
public sealed class StaffMemberWorkModeOverrideResponseDTO
{
    public long Id { get; set; }
    public string StaffMemberId { get; set; } = string.Empty;
    public string StaffMemberName { get; set; } = string.Empty;
    public long? StaffMemberWorkArrangementId { get; set; }
    public WorkMode RequestedWorkMode { get; set; }
    public string RequestedWorkModeName { get; set; } = string.Empty;
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public long? CampusId { get; set; }
    public string? CampusName { get; set; }
    public string Reason { get; set; } = string.Empty;
    public WorkModeOverrideApprovalStatus ApprovalStatus { get; set; }
    public string ApprovalStatusName { get; set; } = string.Empty;
    public string? ApprovalRemark { get; set; }
    public string? RejectionRemark { get; set; }
    public bool IsActive { get; set; }
}
