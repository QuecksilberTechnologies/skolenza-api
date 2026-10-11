// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines strongly typed values persisted by InstitutionConfiguration domain records.
// ================================================================

namespace skolenza.domain.Entity;

/// <summary>Defines the allowed attendance-location scopes for an attendance policy.</summary>
public enum AttendanceLocationScope : short
{
    /// <summary>Attendance is allowed only at the staffMember's primary location.</summary>
    PrimaryLocationOnly = 1,
    /// <summary>Attendance is allowed at locations assigned to the staffMember.</summary>
    AssignedLocations = 2,
    /// <summary>Attendance is allowed at any active location of the institution.</summary>
    AnyCampus = 3,
    /// <summary>Attendance is allowed remotely without a institution location.</summary>
    RemoteAnywhere = 4
}

/// <summary>Defines the permanent or temporary mode in which an staffMember works.</summary>
public enum WorkMode : short
{
    /// <summary>Work is performed from an office location.</summary>
    Office = 1,
    /// <summary>Work is performed remotely from home.</summary>
    WorkFromHome = 2,
    /// <summary>Work is split between office and remote modes.</summary>
    Hybrid = 3,
    /// <summary>Work is performed in the field.</summary>
    Field = 4,
    /// <summary>Work is performed at a client site.</summary>
    ClientSite = 5
}

/// <summary>Defines the configured form of a hybrid work arrangement.</summary>
public enum HybridType : short
{
    /// <summary>Hybrid days follow a fixed schedule.</summary>
    Fixed = 1,
    /// <summary>Hybrid days may be selected flexibly.</summary>
    Flexible = 2
}

/// <summary>Defines work-pattern days using the database's Monday-first numbering.</summary>
public enum WorkPatternDay : short
{
    /// <summary>Monday.</summary>
    Monday = 1,
    /// <summary>Tuesday.</summary>
    Tuesday = 2,
    /// <summary>Wednesday.</summary>
    Wednesday = 3,
    /// <summary>Thursday.</summary>
    Thursday = 4,
    /// <summary>Friday.</summary>
    Friday = 5,
    /// <summary>Saturday.</summary>
    Saturday = 6,
    /// <summary>Sunday.</summary>
    Sunday = 7
}

/// <summary>Defines the approval lifecycle states stored for a work-mode override request.</summary>
public enum WorkModeOverrideApprovalStatus : short
{
    /// <summary>The override is awaiting a future approval workflow.</summary>
    Pending = 1,
    /// <summary>The override was approved by the future approval workflow.</summary>
    Approved = 2,
    /// <summary>The override was rejected by the future approval workflow.</summary>
    Rejected = 3,
    /// <summary>The override was cancelled by the future approval workflow.</summary>
    Cancelled = 4
}

/// <summary>Defines the supported categories of Institution work locations.</summary>
public enum CampusType : short
{
    /// <summary>Institution headquarters.</summary>
    HeadOffice = 1,
    /// <summary>Institution branch.</summary>
    Branch = 2,
    /// <summary>Institution office.</summary>
    Office = 3,
    /// <summary>Institution plant.</summary>
    Plant = 4,
    /// <summary>Institution warehouse.</summary>
    Warehouse = 5,
    /// <summary>Client location.</summary>
    ClientSite = 6,
    /// <summary>Project location.</summary>
    ProjectSite = 7,
    /// <summary>Campus location.</summary>
    Campus = 8,
    /// <summary>Remote office.</summary>
    RemoteOffice = 9
}

/// <summary>Describes the operational state of a physical access card in Institution inventory.</summary>
public enum InstitutionCardStatus : short
{
    Available = 1,
    Assigned = 2,
    Blocked = 3,
    Lost = 4,
    Returned = 5,
    Retired = 6
}

/// <summary>Describes the tax basis recorded from the actual card procurement invoice.</summary>
public enum InstitutionCardTaxTreatment : short
{
    DomesticIntraState = 1,
    DomesticInterState = 2,
    Import = 3,
    ForeignLocalTax = 4
}

/// <summary>Describes a credential's most recently submitted device deployment state.</summary>
public enum DeviceCredentialDeploymentStatus : short
{
    NotConfigured = 0,
    Queued = 1,
    Completed = 2,
    Failed = 3,
    Removed = 4
}
