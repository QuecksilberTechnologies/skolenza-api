// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Persists common refresh tokens owned by Institution StaffMembers
//               or Host users.
// ============================================================================

namespace skolenza.domain.Entity;

/// <summary>
/// Represents an opaque, hashed refresh token.
/// The authenticated owner is determined by <see cref="UserType"/>.
/// </summary>
public partial class RefreshToken
{
    #region Primary Key

    /// <summary>
    /// Gets or sets the refresh-token primary key.
    /// </summary>
    public long Id { get; set; }

    #endregion

    #region Owner Information

    /// <summary>
    /// Gets or sets the login identifier of the authenticated user.
    /// </summary>
    public string LoginId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the authenticated user type.
    /// 1 = Institution User,
    /// 2 = Host User.
    /// </summary>
    public short UserType { get; set; }

    /// <summary>
    /// Gets or sets the Institution LoginCredential identifier
    /// when the refresh token belongs to a institution user.
    /// </summary>
    public long? LoginCredentialId { get; set; }

    /// <summary>
    /// Gets or sets the HostUser identifier
    /// when the refresh token belongs to a host user.
    /// </summary>
    public long? HostUserId { get; set; }

    #endregion

    #region Token Information

    /// <summary>
    /// Gets or sets the SHA-256 hash of the opaque refresh token.
    /// </summary>
    public string Token { get; set; } = null!;

    /// <summary>
    /// Gets or sets the token expiration date and time.
    /// </summary>
    public DateTime ExpiryDate { get; set; }

    /// <summary>
    /// Gets or sets whether the refresh token has been revoked.
    /// </summary>
    public bool? IsRevoked { get; set; }

    /// <summary>
    /// Gets or sets the hash of the successor refresh token
    /// created during token rotation.
    /// </summary>
    public string? ReplacedByToken { get; set; }

    #endregion

    #region Audit Information

    /// <summary>
    /// Gets or sets the date and time when the refresh token was created.
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the refresh token was created.
    /// </summary>
    public string? CreatedByIp { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the refresh token was revoked.
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the refresh token was revoked.
    /// </summary>
    public string? RevokedByIp { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Gets or sets the related host user when
    /// <see cref="UserType"/> represents a Host user.
    /// </summary>
    public virtual HostUser? HostUser { get; set; }

    /// <summary>
    /// Gets or sets the related institution login credential when
    /// <see cref="UserType"/> represents a Institution user.
    /// </summary>
    public virtual LoginCredential? LoginCredential { get; set; }

    #endregion
}