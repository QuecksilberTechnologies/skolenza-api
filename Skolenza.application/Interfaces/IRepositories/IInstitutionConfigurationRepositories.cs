// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines institution-isolated persistence contracts for InstitutionConfiguration resources.
// ================================================================

using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.InstitutionConfiguration;
using skolenza.application.DTOS.Host;
using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IRepositories;

/// <summary>Defines Institution-location persistence and dependency checks.</summary>
public interface ICampusRepository
{
    /// <summary>Gets a non-soft-deleted Institution location by its trusted Institution and identifier.</summary>
    Task<Campus?> GetByIdAsync(long institutionId, long id, CancellationToken cancellationToken);
    /// <summary>Gets a tracked non-soft-deleted Institution location for a state change.</summary>
    Task<Campus?> GetForUpdateAsync(long institutionId, long id, CancellationToken cancellationToken);
    /// <summary>Gets the tracked canonical initial location, preferring the head office and then the oldest row.</summary>
    Task<Campus?> GetInitialForUpdateAsync(long institutionId, CancellationToken cancellationToken);
    /// <summary>Gets a database-paged Institution-location result.</summary>
    Task<PagedResponseDTO<Campus>> GetPagedAsync(long institutionId, CampusFilterRequestDTO filter, CancellationToken cancellationToken);
    /// <summary>Gets a database-paged Host-visible Institution-location result.</summary>
    Task<PagedResponseDTO<Campus>> GetHostPagedAsync(CampusFilterRequestDTO filter, CancellationToken cancellationToken);
    /// <summary>Determines whether the LocationCode is already live for the Institution.</summary>
    Task<bool> LocationCodeExistsAsync(long institutionId, string locationCode, long? excludeId, CancellationToken cancellationToken);
    /// <summary>Validates the active country, state, and Locality geographic hierarchy.</summary>
    Task<bool> IsValidGeographyAsync(int countryId, int? stateId, int? districtId, int? localityId, CancellationToken cancellationToken);
    /// <summary>Determines whether live active dependent configuration blocks deactivation.</summary>
    Task<bool> HasLiveActiveDependenciesAsync(long institutionId, long locationId, CancellationToken cancellationToken);
    /// <summary>Determines whether any live dependent configuration blocks soft deletion.</summary>
    Task<bool> HasAnyDependenciesAsync(long institutionId, long locationId, CancellationToken cancellationToken);
    /// <summary>Adds a prepared Institution location.</summary>
    Task AddAsync(Campus entity, CancellationToken cancellationToken);
}

/// <summary>Defines AttendancePolicy persistence and dependency checks.</summary>
public interface IAttendancePolicyRepository
{
    /// <summary>Gets a non-soft-deleted attendance policy scoped to a Institution.</summary>
    Task<AttendancePolicy?> GetByIdAsync(long institutionId, int id, CancellationToken cancellationToken);
    /// <summary>Gets a tracked attendance policy scoped to a Institution.</summary>
    Task<AttendancePolicy?> GetForUpdateAsync(long institutionId, int id, CancellationToken cancellationToken);
    /// <summary>Gets a database-paged attendance-policy result.</summary>
    Task<PagedResponseDTO<AttendancePolicy>> GetPagedAsync(long institutionId, AttendancePolicyFilterRequestDTO filter, CancellationToken cancellationToken);
    /// <summary>Determines whether the policy name is already live for the Institution.</summary>
    Task<bool> PolicyNameExistsAsync(long institutionId, string policyName, int? excludeId, CancellationToken cancellationToken);
    /// <summary>Determines whether the supplied PolicyType is active and owned by the Institution.</summary>
    Task<bool> IsEligiblePolicyTypeAsync(long institutionId, int policyTypeId, CancellationToken cancellationToken);
    /// <summary>Determines whether an active work arrangement blocks deactivation.</summary>
    Task<bool> HasActiveWorkArrangementsAsync(long institutionId, int policyId, CancellationToken cancellationToken);
    /// <summary>Determines whether any live work arrangement blocks soft deletion.</summary>
    Task<bool> HasAnyWorkArrangementsAsync(long institutionId, int policyId, CancellationToken cancellationToken);
    /// <summary>Adds a prepared attendance policy.</summary>
    Task AddAsync(AttendancePolicy entity, CancellationToken cancellationToken);
}

/// <summary>Defines staffMember-location-assignment persistence and validation checks.</summary>
public interface IStaffMemberLocationAssignmentRepository
{
    /// <summary>Gets a non-soft-deleted staffMember-location assignment scoped to a Institution.</summary>
    Task<StaffMemberLocationAssignment?> GetByIdAsync(long institutionId, long id, CancellationToken cancellationToken);
    /// <summary>Gets a tracked staffMember-location assignment scoped to a Institution.</summary>
    Task<StaffMemberLocationAssignment?> GetForUpdateAsync(long institutionId, long id, CancellationToken cancellationToken);
    /// <summary>Gets a database-paged staffMember-location-assignment result.</summary>
    Task<PagedResponseDTO<StaffMemberLocationAssignment>> GetPagedAsync(
        long institutionId,
        StaffMemberLocationAssignmentFilterRequestDTO filter,
        long requestingStaffMemberId,
        int requestingRoleTypeId,
        CancellationToken cancellationToken);
    /// <summary>Determines whether the StaffMember is active and owned by the Institution.</summary>
    Task<bool> IsEligibleStaffMemberAsync(long institutionId, long staffMemberId, CancellationToken cancellationToken);
    /// <summary>Determines whether the location is active and owned by the Institution.</summary>
    Task<bool> IsEligibleLocationAsync(long institutionId, long locationId, CancellationToken cancellationToken);
    /// <summary>Determines whether an overlapping live assignment already uses the StaffMember/location pair.</summary>
    Task<bool> AssignmentExistsAsync(long institutionId, long staffMemberId, long locationId, DateOnly effectiveFrom, DateOnly? effectiveTo, long? excludeId, CancellationToken cancellationToken);
    /// <summary>Determines whether another overlapping live primary assignment exists for the StaffMember.</summary>
    Task<bool> PrimaryAssignmentExistsAsync(long institutionId, long staffMemberId, DateOnly effectiveFrom, DateOnly? effectiveTo, long? excludeId, CancellationToken cancellationToken);
    /// <summary>Determines whether a proposed assignment change would invalidate a live work arrangement that uses it as primary.</summary>
    Task<bool> WouldInvalidatePrimaryWorkArrangementAsync(long institutionId, long staffMemberId, long locationId, bool isPrimary, bool isAttendanceAllowed, bool isActive, DateOnly effectiveFrom, DateOnly? effectiveTo, CancellationToken cancellationToken);
    /// <summary>Adds a prepared staffMember-location assignment.</summary>
    Task AddAsync(StaffMemberLocationAssignment entity, CancellationToken cancellationToken);
}

/// <summary>Defines staffMember-device-enrollment persistence and read-only InstitutionDevice validation.</summary>
public interface IStaffMemberDeviceEnrollmentRepository
{
    /// <summary>Gets a non-soft-deleted staffMember-device enrollment scoped to a Institution.</summary>
    Task<StaffMemberDeviceEnrollment?> GetByIdAsync(long institutionId, long id, CancellationToken cancellationToken);
    /// <summary>Gets a tracked staffMember-device enrollment scoped to a Institution.</summary>
    Task<StaffMemberDeviceEnrollment?> GetForUpdateAsync(long institutionId, long id, CancellationToken cancellationToken);
    /// <summary>Gets a database-paged staffMember-device-enrollment result.</summary>
    Task<PagedResponseDTO<StaffMemberDeviceEnrollment>> GetPagedAsync(
        long institutionId,
        StaffMemberDeviceEnrollmentFilterRequestDTO filter,
        long requestingStaffMemberId,
        int requestingRoleTypeId,
        CancellationToken cancellationToken);
    /// <summary>Determines whether the StaffMember is active and owned by the Institution.</summary>
    Task<bool> IsEligibleStaffMemberAsync(long institutionId, long staffMemberId, CancellationToken cancellationToken);
    /// <summary>Determines whether a Host-managed InstitutionDevice is eligible for enrollment.</summary>
    Task<bool> IsEligibleInstitutionDeviceAsync(long institutionId, long institutionDeviceId, CancellationToken cancellationToken);
    /// <summary>Determines whether the staffMember is already live on the physical Institution device.</summary>
    Task<bool> EnrollmentExistsAsync(long institutionId, long staffMemberId, long institutionDeviceId, long? excludeId, CancellationToken cancellationToken);
    /// <summary>Gets an enrollment-eligible Institution device with its authoritative location and configuration.</summary>
    Task<InstitutionDevice?> GetEligibleInstitutionDeviceAsync(long institutionId, long institutionDeviceId, CancellationToken cancellationToken);
    /// <summary>Determines whether the staffMember has an active attendance-allowed assignment for the device location.</summary>
    Task<bool> HasEligibleStaffMemberLocationAssignmentAsync(long institutionId, long staffMemberId, long campusId, DateOnly effectiveOn, CancellationToken cancellationToken);
    /// <summary>Gets a non-soft-deleted active card for a trusted Institution scope.</summary>
    Task<InstitutionCardMaster?> GetActiveCardAsync(long institutionId, long cardId, CancellationToken cancellationToken);
    /// <summary>Gets a tracked card for safe bind/unbind state transitions.</summary>
    Task<InstitutionCardMaster?> GetCardForUpdateAsync(long institutionId, long cardId, CancellationToken cancellationToken);
    /// <summary>Adds a prepared staffMember-device enrollment.</summary>
    Task AddAsync(StaffMemberDeviceEnrollment entity, CancellationToken cancellationToken);
}

/// <summary>Defines Host-only Institution card-inventory persistence and assignment safety checks.</summary>
public interface IInstitutionCardMasterRepository
{
    Task<InstitutionCardMaster?> GetByIdAsync(long institutionId, long id, CancellationToken cancellationToken);
    Task<InstitutionCardMaster?> GetForUpdateAsync(long institutionId, long id, CancellationToken cancellationToken);
    Task<PagedResponseDTO<InstitutionCardMaster>> GetPagedAsync(long institutionId, InstitutionCardMasterFilterRequestDTO filter, CancellationToken cancellationToken);
    Task<bool> CardHashExistsAsync(long institutionId, string lookupHash, long? excludeId, CancellationToken cancellationToken);
    Task<bool> HasActiveEnrollmentAssignmentAsync(long institutionId, long cardId, CancellationToken cancellationToken);
    Task AddAsync(InstitutionCardMaster entity, CancellationToken cancellationToken);
}

/// <summary>Defines staffMember-work-arrangement persistence, validation, and lifecycle checks.</summary>
public interface IStaffMemberWorkArrangementRepository
{
    /// <summary>Gets a non-soft-deleted work arrangement scoped to a Institution.</summary>
    Task<StaffMemberWorkArrangement?> GetByIdAsync(long institutionId, long id, CancellationToken cancellationToken);
    /// <summary>Gets a tracked work arrangement scoped to a Institution.</summary>
    Task<StaffMemberWorkArrangement?> GetForUpdateAsync(long institutionId, long id, CancellationToken cancellationToken);
    /// <summary>Gets a database-paged staffMember-work-arrangement result.</summary>
    Task<PagedResponseDTO<StaffMemberWorkArrangement>> GetPagedAsync(
        long institutionId,
        StaffMemberWorkArrangementFilterRequestDTO filter,
        long requestingStaffMemberId,
        int requestingRoleTypeId,
        CancellationToken cancellationToken);
    /// <summary>Gets effective Published Attendance policy versions for a token-authenticated Institution.</summary>
    Task<List<AttendancePolicyOptionResponseDTO>> GetAttendancePolicyOptionsAsync(
        long institutionId,
        DateOnly effectiveOn,
        string? search,
        CancellationToken cancellationToken);
    /// <summary>Determines whether the StaffMember is active and owned by the Institution.</summary>
    Task<bool> IsEligibleStaffMemberAsync(long institutionId, long staffMemberId, CancellationToken cancellationToken);
    /// <summary>Gets a non-deleted StaffMember for status-specific validation.</summary>
    Task<StaffMember?> GetStaffMemberForValidationAsync(long institutionId, long staffMemberId, CancellationToken cancellationToken);
    /// <summary>Determines whether the version is an active, Published Attendance policy effective on the requested date.</summary>
    Task<bool> IsEligibleAttendancePolicyVersionAsync(long institutionId, long policyVersionId, DateOnly effectiveOn, CancellationToken cancellationToken);
    /// <summary>Gets the typed execution settings for an Attendance policy version.</summary>
    Task<AttendancePolicyVersionConfiguration?> GetAttendanceConfigurationAsync(long institutionId, long policyVersionId, CancellationToken cancellationToken);
    /// <summary>Determines whether the location is active and owned by the Institution.</summary>
    Task<bool> IsEligibleLocationAsync(long institutionId, long locationId, CancellationToken cancellationToken);
    /// <summary>Gets another active, non-deleted arrangement that overlaps the proposed effective window.</summary>
    Task<StaffMemberWorkArrangement?> GetOverlappingArrangementAsync(long institutionId, long staffMemberId, DateOnly effectiveFrom, DateOnly? effectiveTo, long? excludeId, CancellationToken cancellationToken);
    /// <summary>Determines whether the selected primary location has a primary, attendance-allowed assignment covering the full arrangement window.</summary>
    Task<bool> HasCoveringPrimaryLocationAssignmentAsync(long institutionId, long staffMemberId, long locationId, DateOnly effectiveFrom, DateOnly? effectiveTo, CancellationToken cancellationToken);
    /// <summary>Determines whether an active, attendance-allowed staffMember location assignment covers the full requested window.</summary>
    Task<bool> HasCoveringAttendanceLocationAssignmentAsync(long institutionId, long staffMemberId, long locationId, DateOnly effectiveFrom, DateOnly effectiveTo, CancellationToken cancellationToken);
    /// <summary>Gets non-deleted staffMember-location assignments used to explain a failed primary-location validation.</summary>
    Task<List<StaffMemberLocationAssignment>> GetLocationAssignmentsForValidationAsync(long institutionId, long staffMemberId, long locationId, CancellationToken cancellationToken);
    /// <summary>Gets a non-deleted Institution location for status and type validation.</summary>
    Task<Campus?> GetLocationForValidationAsync(long institutionId, long locationId, CancellationToken cancellationToken);
    /// <summary>Gets the active Institution location type used to validate the selected work mode.</summary>
    Task<CampusType?> GetEligibleLocationTypeAsync(long institutionId, long locationId, CancellationToken cancellationToken);
    /// <summary>Determines whether active children block deactivation.</summary>
    Task<bool> HasLiveActiveDependenciesAsync(long institutionId, long arrangementId, CancellationToken cancellationToken);
    /// <summary>Determines whether any live children block soft deletion.</summary>
    Task<bool> HasAnyDependenciesAsync(long institutionId, long arrangementId, CancellationToken cancellationToken);
    /// <summary>Adds a prepared staffMember work arrangement.</summary>
    Task AddAsync(StaffMemberWorkArrangement entity, CancellationToken cancellationToken);
}

/// <summary>Defines staffMember-work-pattern persistence and validation checks.</summary>
public interface IStaffMemberWorkPatternRepository
{
    /// <summary>Gets a non-soft-deleted work-pattern day scoped to a Institution.</summary>
    Task<StaffMemberWorkPattern?> GetByIdAsync(long institutionId, long id, CancellationToken cancellationToken);
    /// <summary>Gets a tracked work-pattern day scoped to a Institution.</summary>
    Task<StaffMemberWorkPattern?> GetForUpdateAsync(long institutionId, long id, CancellationToken cancellationToken);
    /// <summary>Gets a database-paged staffMember-work-pattern result.</summary>
    Task<PagedResponseDTO<StaffMemberWorkPattern>> GetPagedAsync(
        long institutionId,
        StaffMemberWorkPatternFilterRequestDTO filter,
        long requestingStaffMemberId,
        int requestingRoleTypeId,
        CancellationToken cancellationToken);
    /// <summary>Determines whether the work arrangement is active and owned by the Institution.</summary>
    Task<bool> IsEligibleArrangementAsync(long institutionId, long arrangementId, CancellationToken cancellationToken);
    /// <summary>Determines whether the location is active and owned by the Institution.</summary>
    Task<bool> IsEligibleLocationAsync(long institutionId, long locationId, CancellationToken cancellationToken);
    /// <summary>Determines whether a live work-pattern day already exists for the arrangement.</summary>
    Task<bool> PatternDayExistsAsync(long institutionId, long arrangementId, short dayOfWeek, long? excludeId, CancellationToken cancellationToken);
    /// <summary>Adds a prepared work-pattern day.</summary>
    Task AddAsync(StaffMemberWorkPattern entity, CancellationToken cancellationToken);
}

/// <summary>Defines staffMember work-mode-override persistence and validation checks.</summary>
public interface IStaffMemberWorkModeOverrideRequestRepository
{
    /// <summary>Gets a non-soft-deleted override request scoped to a Institution.</summary>
    Task<StaffMemberWorkModeOverrideRequest?> GetByIdAsync(long institutionId, long id, CancellationToken cancellationToken);
    /// <summary>Gets a tracked override request scoped to a Institution.</summary>
    Task<StaffMemberWorkModeOverrideRequest?> GetForUpdateAsync(long institutionId, long id, CancellationToken cancellationToken);
    /// <summary>Gets a database-paged work-mode-override result.</summary>
    Task<PagedResponseDTO<StaffMemberWorkModeOverrideRequest>> GetPagedAsync(
        long institutionId,
        StaffMemberWorkModeOverrideFilterRequestDTO filter,
        long requestingStaffMemberId,
        int requestingRoleTypeId,
        CancellationToken cancellationToken);
    /// <summary>Determines whether the StaffMember is active and owned by the Institution.</summary>
    Task<bool> IsEligibleStaffMemberAsync(long institutionId, long staffMemberId, CancellationToken cancellationToken);
    /// <summary>Determines whether the optional work arrangement is active and owned by the Institution.</summary>
    Task<bool> IsEligibleArrangementAsync(long institutionId, long arrangementId, CancellationToken cancellationToken);
    /// <summary>Determines whether the optional location is active and owned by the Institution.</summary>
    Task<bool> IsEligibleLocationAsync(long institutionId, long locationId, CancellationToken cancellationToken);
    /// <summary>Gets a competing active Pending or Approved override whose inclusive dates overlap the requested window.</summary>
    Task<StaffMemberWorkModeOverrideRequest?> GetOverlappingOverrideAsync(long institutionId, long staffMemberId, DateOnly fromDate, DateOnly toDate, long? excludeId, CancellationToken cancellationToken);
    /// <summary>Adds a prepared work-mode override request.</summary>
    Task AddAsync(StaffMemberWorkModeOverrideRequest entity, CancellationToken cancellationToken);
}
