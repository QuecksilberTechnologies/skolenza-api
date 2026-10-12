namespace skolenza.application.Constants;

/// <summary>
/// Authoritative bootstrap departments created for every new institution.
/// Institution-managed departments remain database master data and are not enums.
/// </summary>
public static class DepartmentConstants
{
    public sealed record DepartmentSeedDefinition(
        string Name,
        string Description,
        string Remark,
        bool IsExecutiveOffice);

    public const string ExecutiveOfficeName = "Executive Office";

    public static IReadOnlyList<DepartmentSeedDefinition> DefaultDepartments { get; } =
    [
        new(
            ExecutiveOfficeName,
            "Handles institution-wide strategy and leadership.",
            "Default administrative department.",
            true)
    ];
}
