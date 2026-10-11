// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines encrypted-Institution filters for Institution Parent Module header retrieval.
// ================================================================

namespace skolenza.application.DTOS.Module.InstitutionParentModule;

/// <summary>
/// Represents the Host-managed filters used to retrieve a Institution's entitled module-header tree.
/// </summary>
public sealed class InstitutionParentModuleHeaderRequestDTO
{
    /// <summary>
    /// Gets or sets the encrypted Institution identifier.
    /// </summary>
    public required string InstitutionId { get; set; }

    /// <summary>
    /// Gets or sets the optional Institution entitlement enabled-state filter.
    /// </summary>
    public bool? IsEnabled { get; set; }
}
