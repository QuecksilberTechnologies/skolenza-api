namespace skolenza.application.Constants;

/// <summary>Existing StaffMember leaf codes which share institution self-service defaults.</summary>
public static class StaffMemberOperationalSections
{
    public static IReadOnlyList<string> ModuleCodes { get; } = Array.AsReadOnly(new[]
    {
        "STAFF_CAMPUS_ASSIGNMENTS", "STAFF_DEVICE_ENROLMENTS", "STAFF_WORK_ARRANGEMENTS",
        "STAFF_WORK_PATTERNS", "STAFF_WORK_MODE_OVERRIDES"
    });
}
