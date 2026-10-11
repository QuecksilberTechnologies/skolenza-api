// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines filters for retrieving newly created assets.
// ================================================================

namespace skolenza.application.DTOS.AssetDTO.asset;

/// <summary>
/// Represents the client filter for newly created assets.
/// </summary>
public class GetNewAssetRequestDTO
{
    public bool IsUnread { get; set; } = true;
}
