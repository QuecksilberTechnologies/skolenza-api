// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Represents the trusted Host or Institution principal validated for a shared authenticated request.
// ================================================================

using skolenza.application.Common.Enums;

namespace skolenza.application.Common.Models.Security;

/// <summary>
/// Represents the Host or Institution principal that has passed the established request-validation path.
/// </summary>
public sealed class AuthenticatedRequestContext
{
    #region Principal Context

    /// <summary>
    /// Gets or sets the existing principal type that owns the authenticated session.
    /// </summary>
    public LoginUserType UserType { get; init; }

    /// <summary>
    /// Gets or sets the validated Host-user or Institution StaffMember identifier.
    /// </summary>
    public long AuthenticatedUserId { get; init; }

    /// <summary>
    /// Gets or sets the validated institution identifier for a Institution StaffMember request.
    /// </summary>
    public long? InstitutionId { get; init; }

    /// <summary>
    /// Gets or sets the validated primary role identifier for a Institution StaffMember request.
    /// </summary>
    public int? RoleId { get; init; }

    #endregion
}
