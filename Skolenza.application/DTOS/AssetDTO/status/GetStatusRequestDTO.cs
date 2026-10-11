// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines client filters for retrieving asset statuses.
// ================================================================

using skolenza.application.DTOS.Pagination;

namespace skolenza.application.DTOS.AssetDTO.status;

/// <summary>
/// Represents client-supplied filters for retrieving asset statuses.
/// </summary>
public class GetStatusRequestDTO : BaseRequest
{
    /// <summary>
    /// Gets or sets an optional asset status identifier filter.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets whether only active statuses are requested.
    /// </summary>
    public bool IsActive { get; set; }
}
