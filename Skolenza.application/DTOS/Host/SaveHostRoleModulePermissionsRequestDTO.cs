// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the HostRole module-operation permission selection request.
// ================================================================

namespace skolenza.application.DTOS.Host;

/// <summary>
/// Represents the complete selected module-operation permissions for one Host role.
/// </summary>
public class SaveHostRoleModulePermissionsRequestDTO
{
    #region Properties

    /// <summary>
    /// Gets or sets the Host-role identifier receiving the selected permissions.
    /// </summary>
    public long HostRoleId { get; set; }

    /// <summary>
    /// Gets or sets the selected module-operation pairs. An empty list removes all current permissions.
    /// </summary>
    public List<HostRoleModulePermissionRequestDTO>? Permissions { get; set; } = new();

    #endregion
}

/// <summary>
/// Represents one selected module-operation pair for a Host role.
/// </summary>
public class HostRoleModulePermissionRequestDTO
{
    #region Properties

    /// <summary>
    /// Gets or sets the module identifier.
    /// </summary>
    public int ModuleId { get; set; }

    /// <summary>
    /// Gets or sets the operation identifier.
    /// </summary>
    public int OperationId { get; set; }

    #endregion
}
