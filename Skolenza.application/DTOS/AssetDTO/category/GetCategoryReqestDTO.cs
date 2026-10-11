// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines client filters for retrieving asset categories.
// ================================================================

using skolenza.application.DTOS.Pagination;

namespace skolenza.application.DTOS.AssetDTO.category;

/// <summary>
/// Represents the client-supplied filters for retrieving asset categories.
/// </summary>
public class GetCategoryReqestDTO : BaseRequest
{
    /// <summary>Gets or sets an optional asset category identifier filter.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets an optional active-state filter.</summary>
    public bool? IsActive { get; set; }
}
