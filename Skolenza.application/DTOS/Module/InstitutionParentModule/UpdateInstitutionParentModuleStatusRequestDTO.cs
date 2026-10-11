// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the Host-managed Institution Parent Module status update request.
// ================================================================

namespace skolenza.application.DTOS.Module.InstitutionParentModule;

/// <summary>
/// Represents the encrypted Institution identifier and requested enabled state for one Institution Parent Module status cascade.
/// </summary>
public sealed class UpdateInstitutionParentModuleStatusRequestDTO
{
    /// <summary>
    /// Gets or sets the encrypted Institution identifier.
    /// </summary>
    public required string InstitutionId { get; set; }

    /// <summary>
    /// Gets or sets the requested enabled state for the target Header Module and its descendants.
    /// </summary>
    public bool IsActive { get; set; }
}
