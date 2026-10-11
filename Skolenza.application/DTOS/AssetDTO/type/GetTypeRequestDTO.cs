// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines client filters for retrieving asset types.
// ================================================================

using skolenza.application.DTOS.Pagination;

namespace skolenza.application.DTOS.AssetDTO.type;

/// <summary>
/// Represents the client-supplied filters for retrieving asset types.
/// </summary>
public class GetTypeRequestDTO : BaseRequest
{
    /// <summary>
    /// Gets or sets an optional asset type identifier filter.
    /// </summary>
    public int? TypeId { get; set; }

    /// <summary>
    /// Gets or sets an optional asset category identifier filter.
    /// </summary>
    public long? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets an optional active-state filter.
    /// </summary>
    public bool? IsActive { get; set; }
}
