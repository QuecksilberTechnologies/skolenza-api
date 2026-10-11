// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the HostRole module-operation permission selection response.
// ================================================================

namespace skolenza.application.DTOS.Host;

/// <summary>
/// Represents all available module-operation permissions for one Host role.
/// </summary>
public class GetHostRoleModulePermissionsResponseDTO
{
    #region Properties

    /// <summary>
    /// Gets or sets the Host-role identifier.
    /// </summary>
    public long HostRoleId { get; set; }

    /// <summary>
    /// Gets or sets the Host-role name.
    /// </summary>
    public string HostRoleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the modules and their available operations.
    /// </summary>
    public List<HostRoleModulePermissionsModuleResponseDTO> Modules { get; set; } = new();

    #endregion
}

/// <summary>
/// Represents one module and the operations available for Host-role assignment.
/// </summary>
public class HostRoleModulePermissionsModuleResponseDTO
{
    #region Properties

    /// <summary>
    /// Gets or sets the module identifier.
    /// </summary>
    public int ModuleId { get; set; }

    /// <summary>
    /// Gets or sets the module name.
    /// </summary>
    public string ModuleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the available operations for this module.
    /// </summary>
    public List<HostRoleModulePermissionsOperationResponseDTO> Operations { get; set; } = new();

    #endregion
}

/// <summary>
/// Represents one available operation and its selection state for a Host role.
/// </summary>
public class HostRoleModulePermissionsOperationResponseDTO
{
    #region Properties

    /// <summary>
    /// Gets or sets the module-operation mapping identifier.
    /// </summary>
    public int ModuleOperationMappingId { get; set; }

    /// <summary>
    /// Gets or sets the operation identifier.
    /// </summary>
    public int OperationId { get; set; }

    /// <summary>
    /// Gets or sets the operation name.
    /// </summary>
    public string OperationName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the Host role currently has this permission.
    /// </summary>
    public bool IsAllowed { get; set; }

    #endregion
}
