// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines Host-side filters for retrieving Institution management records.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOs.Institution;

/// <summary>
/// Represents optional filters and paging values for the Institution management list.
/// </summary>
public sealed class GetAllInstitutionsRequestDTO : PermissionRequestDTO
{
    #region Filter Properties

    /// <summary>
    /// Gets or sets the optional Institution active-state filter.
    /// A <see langword="null"/> value represents all active states.
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Gets or sets the optional Institution verification-state filter.
    /// A <see langword="null"/> value represents all verification states.
    /// </summary>
    public bool? IsVerified { get; set; }

    /// <summary>
    /// Gets or sets the optional text used to search Institution management records.
    /// </summary>
    public string? SearchKeyword { get; set; }

    /// <summary>
    /// Gets or sets the requested one-based page number.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the requested page size.
    /// </summary>
    public int PageSize { get; set; } = 10;

    #endregion
}
