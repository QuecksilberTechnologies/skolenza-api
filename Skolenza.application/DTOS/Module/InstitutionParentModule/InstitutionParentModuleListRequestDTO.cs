// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines minimal filtering and paging for Institution Parent Module listing.
// ================================================================

namespace skolenza.application.DTOS.Module.InstitutionParentModule;

/// <summary>
/// Represents the minimal Host-managed filters and paging values for the Institution Parent Module list endpoint.
/// </summary>
public sealed class InstitutionParentModuleListRequestDTO
{
    /// <summary>
    /// Gets or sets the optional active-state filter.
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Gets or sets the requested one-based page number.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the requested page size.
    /// </summary>
    public int PageSize { get; set; } = 10;
}
