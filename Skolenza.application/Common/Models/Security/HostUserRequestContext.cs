// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Represents the trusted Host JWT and current database context required for Host administration and permission validation.
// ================================================================

namespace skolenza.application.Common.Models.Security;

/// <summary>
/// Represents the authenticated Host identity, trusted principal type, login-time role snapshot, and current database role.
/// </summary>
public sealed class HostUserRequestContext
{
    /// <summary>
    /// Gets the validated Host user primary identifier.
    /// </summary>
    public long HostUserId { get; init; }

    /// <summary>
    /// Gets the Host role identifier captured when the JWT was issued.
    /// </summary>
    public long TokenHostRoleId { get; init; }

    /// <summary>
    /// Gets the current Host role identifier resolved from the active Host user database record.
    /// </summary>
    public long CurrentHostRoleId { get; init; }

    /// <summary>
    /// Gets the trusted Host principal type read from the signed JWT claim.
    /// </summary>
    public string UserType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the Host-scoped key carried in the established Institution encryption-key claim.
    /// </summary>
    public string InstitutionEncryptionKey { get; init; } = string.Empty;
}
