// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines encrypted-Institution filters for Institution Parent Module retrieval by global Module identifier.
// ================================================================

namespace skolenza.application.DTOS.Module.InstitutionParentModule;

/// <summary>
/// Represents the Host-managed Institution and Module-scope filters required to retrieve one entitled Header Module.
/// </summary>
public sealed class InstitutionParentModuleByIdRequestDTO
{
    /// <summary>
    /// Gets or sets the encrypted Institution identifier.
    /// </summary>
    public required string InstitutionId { get; set; }

    /// <summary>
    /// Gets or sets the required global Module scope.
    /// </summary>
    public short ModuleScope { get; set; }
}
