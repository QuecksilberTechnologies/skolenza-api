// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Adds InstitutionConfiguration navigation collections to existing domain entities.
// ================================================================

namespace skolenza.domain.Entity;

/// <summary>Adds InstitutionConfiguration relationships to a Institution.</summary>
public partial class Institution
{
    public virtual ICollection<Campus> Campuss { get; set; } = new List<Campus>();
    public virtual ICollection<AttendancePolicy> AttendancePolicies { get; set; } = new List<AttendancePolicy>();
    public virtual ICollection<StaffMemberLocationAssignment> StaffMemberLocationAssignments { get; set; } = new List<StaffMemberLocationAssignment>();
    public virtual ICollection<StaffMemberDeviceEnrollment> StaffMemberDeviceEnrollments { get; set; } = new List<StaffMemberDeviceEnrollment>();
    public virtual ICollection<InstitutionCardMaster> InstitutionCardMasters { get; set; } = new List<InstitutionCardMaster>();
    public virtual ICollection<StaffMemberWorkArrangement> StaffMemberWorkArrangements { get; set; } = new List<StaffMemberWorkArrangement>();
    public virtual ICollection<StaffMemberWorkPattern> StaffMemberWorkPatterns { get; set; } = new List<StaffMemberWorkPattern>();
    public virtual ICollection<StaffMemberWorkModeOverrideRequest> StaffMemberWorkModeOverrideRequests { get; set; } = new List<StaffMemberWorkModeOverrideRequest>();
    public virtual ICollection<InstitutionDevice> InstitutionDevices { get; set; } = new List<InstitutionDevice>();
}

/// <summary>Adds InstitutionConfiguration relationships to an StaffMember.</summary>
public partial class StaffMember
{
    public virtual ICollection<StaffMemberLocationAssignment> StaffMemberLocationAssignments { get; set; } = new List<StaffMemberLocationAssignment>();
    public virtual ICollection<StaffMemberDeviceEnrollment> StaffMemberDeviceEnrollments { get; set; } = new List<StaffMemberDeviceEnrollment>();
    public virtual ICollection<StaffMemberWorkArrangement> StaffMemberWorkArrangements { get; set; } = new List<StaffMemberWorkArrangement>();
    public virtual ICollection<StaffMemberWorkModeOverrideRequest> StaffMemberWorkModeOverrideRequests { get; set; } = new List<StaffMemberWorkModeOverrideRequest>();
}

/// <summary>Adds InstitutionConfiguration relationships to a PolicyType.</summary>
public partial class PolicyType
{
    public virtual ICollection<AttendancePolicy> AttendancePolicies { get; set; } = new List<AttendancePolicy>();
}

/// <summary>Adds Campus relationships to a Country.</summary>
public partial class Country
{
    public virtual ICollection<Campus> Campuss { get; set; } = new List<Campus>();
}

/// <summary>Adds Campus relationships to a Locality.</summary>
public partial class Locality
{
    public virtual ICollection<Campus> Campuss { get; set; } = new List<Campus>();
}

/// <summary>Adds device-enrollment navigation to a Institution location.</summary>
public partial class Campus
{
    public virtual ICollection<StaffMemberDeviceEnrollment> StaffMemberDeviceEnrollments { get; set; } = new List<StaffMemberDeviceEnrollment>();
    public virtual ICollection<Holiday> Holidays { get; set; } = new List<Holiday>();
}

/// <summary>Adds staffMember-enrollment navigation to a physical Institution device.</summary>
public partial class InstitutionDevice
{
    public virtual ICollection<StaffMemberDeviceEnrollment> StaffMemberDeviceEnrollments { get; set; } = new List<StaffMemberDeviceEnrollment>();
}
