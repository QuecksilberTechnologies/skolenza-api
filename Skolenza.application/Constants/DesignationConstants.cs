namespace skolenza.application.Constants;

/// <summary>
/// Authoritative bootstrap designations created for every new institution.
/// Institution-managed designations remain database master data and are not enums.
/// </summary>
public static class DesignationConstants
{
    public sealed record DesignationSeedDefinition(
        string Name,
        string DepartmentName,
        string Description);

    public const string InstitutionAdministratorName = "Institution Administrator";

    public static IReadOnlyList<DesignationSeedDefinition> DefaultDesignations { get; } =
    [
        new(
            InstitutionAdministratorName,
            DepartmentConstants.ExecutiveOfficeName,
            "Initial administrative designation for the institution account owner.")
    ];
}
