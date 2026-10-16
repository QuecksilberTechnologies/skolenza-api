namespace skolenza.application.Common.Enums;

/// <summary>Supported master data preview targets; does not define role types.</summary>
public enum BulkImportMaster
{
    Department = 1,
    // Persisted jobs retain this numeric slot; it must never be reassigned or executed.
    RetiredWorkTitle = 2,
    Role = 3,
    StaffEmploymentType = 4,
    StaffMember = 5,
    DeviceMaster = 6,
    InstitutionCard = 7,
    HostModule = 8,
    HostSubModule = 9,
    HostOperation = 10,
    HostModuleOperation = 11,
    PolicyType = 12,
    PolicyDefinition = 13,
    PolicyAssignment = 14
}

/// <summary>Preview outcomes only; Ready is not a persisted record.</summary>
public enum BulkImportRowStatus
{
    Ready = 1,
    Existing = 2,
    Invalid = 3,
    Created = 4,
    Failed = 5
}

public enum BulkImportJobStatus
{
    Draft = 1,
    Queued = 2,
    Running = 3,
    Completed = 4,
    CompletedWithErrors = 5,
    Failed = 6,
    Cancelled = 7
}

public enum BulkImportAction
{
    Confirm = 1,
    Get = 2,
    List = 3,
    Retry = 4,
    Cancel = 5,
    Template = 6,
    SendInvitations = 7
}

/// <summary>Invitation delivery is separate from account creation and its retries.</summary>
public enum BulkImportInvitationStatus
{
    Pending = 1,
    Sending = 2,
    Sent = 3,
    Failed = 4,
    DeliveryUnknown = 5,
    NotRequired = 6
}
