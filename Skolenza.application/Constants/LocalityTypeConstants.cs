// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines stable LocalityType identifiers and names.
// ================================================================

namespace skolenza.application.Constants;

/// <summary>Canonical LocalityType values shared by persistence and API behavior.</summary>
public static class LocalityTypeConstants
{
    public const int CityId = 1;
    public const int TownId = 2;
    public const int VillageId = 3;
    public const int OtherId = 4;

    public const string CityName = "City";
    public const string TownName = "Town";
    public const string VillageName = "Village";
    public const string OtherName = "Other / Unclassified";

    public static readonly int[] Ids = [CityId, TownId, VillageId, OtherId];

    public static readonly IReadOnlyDictionary<int, string> Values =
        new Dictionary<int, string>
        {
            [CityId] = CityName,
            [TownId] = TownName,
            [VillageId] = VillageName,
            [OtherId] = OtherName
        };
}
