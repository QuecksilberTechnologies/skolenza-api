// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the lightweight Institution staffMember runtime authorization bootstrap response.
// ================================================================

using skolenza.application.DTOs.RoleModulePermission;

namespace skolenza.application.DTOS.UserAccess;

/// <summary>
/// Represents the current Institution-scoped operational navigation available to an authenticated staffMember.
/// </summary>
public sealed class InstitutionUserAccessResponseDTO
{
    #region Properties

    /// <summary>
    /// Gets or sets the current effective operational menu hierarchy.
    /// </summary>
    public IReadOnlyCollection<MainModuleDto> OperationalMenus { get; init; }
        = Array.Empty<MainModuleDto>();

    #endregion
}
