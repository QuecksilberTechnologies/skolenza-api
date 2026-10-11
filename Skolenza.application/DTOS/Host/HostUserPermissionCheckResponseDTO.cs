// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Represents the database result of a current-state Host user module-operation permission check.
// ================================================================

namespace skolenza.application.DTOS.Host;

/// <summary>
/// Represents the Host authorization result returned by <c>CheckHostUserPermission</c>.
/// </summary>
public sealed class HostUserPermissionCheckResponseDTO
{
    /// <summary>
    /// Gets or sets the authorization result code.
    /// </summary>
    public int ResultCode { get; set; }

    /// <summary>
    /// Gets or sets the machine-readable authorization result key.
    /// </summary>
    public string ResultKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current Host role identifier resolved from the database.
    /// </summary>
    public long? CurrentHostRoleId { get; set; }

    /// <summary>
    /// Gets or sets the Host role identifier that granted the requested operation.
    /// </summary>
    public long? GrantedHostRoleId { get; set; }
}
