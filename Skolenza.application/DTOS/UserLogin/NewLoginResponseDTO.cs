// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the compact Host and Institution StaffMember session bootstrap response returned by the NewLogin endpoint.
// ================================================================

using System.Text.Json.Serialization;

namespace skolenza.application.DTOs.UserLogin;

/// <summary>
/// Represents the compact session bootstrap returned after a Host user or Institution StaffMember authenticates successfully.
/// </summary>
public sealed class NewLoginResponseDTO
{
    #region Principal Type

    /// <summary>
    /// Gets or sets the existing authenticated principal type that owns the session.
    /// </summary>
    public string UserType { get; set; } = string.Empty;

    #endregion

    #region Token Information

    /// <summary>
    /// Gets or sets the signed access token issued for the authenticated Institution StaffMember.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the opaque refresh token issued for the authenticated Institution StaffMember.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC expiration timestamp embedded in the issued access token.
    /// </summary>
    public DateTime AccessTokenExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC expiration timestamp persisted for the issued refresh token.
    /// </summary>
    public DateTime RefreshTokenExpiresAtUtc { get; set; }

    #endregion

    #region User Context

    /// <summary>
    /// Gets or sets the minimal Institution StaffMember context needed to bootstrap an HRMS session.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public NewLoginUserContextDTO? User { get; set; }

    /// <summary>
    /// Gets or sets the minimal Host-user context when the authenticated principal is a Host user.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public NewLoginHostUserContextDTO? HostUser { get; set; }

    /// <summary>
    /// Gets or sets the active Host role when the authenticated principal is a Host user.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public NewLoginHostRoleDTO? HostRole { get; set; }

    #endregion
}

/// <summary>
/// Represents the minimal Institution StaffMember context needed immediately after authentication.
/// </summary>
public sealed class NewLoginUserContextDTO
{
    #region Identity

    /// <summary>
    /// Gets or sets the client-safe encoded staffMember identifier.
    /// </summary>
    public string StaffMemberId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the staffMember's display name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the staffMember's official email address.
    /// </summary>
    public string? OfficialEmail { get; set; }

    /// <summary>
    /// Gets or sets the resolved profile-image URL when a profile image exists.
    /// </summary>
    public string? ProfileImageUrl { get; set; }

    #endregion

    #region Residential Geography

    /// <summary>
    /// Gets or sets the StaffMember residential country identifier. This is intentionally unavailable until the residential profile source is connected.
    /// </summary>
    public int? CountryId { get; set; }

    /// <summary>
    /// Gets or sets the StaffMember residential country name. This is intentionally unavailable until the residential profile source is connected.
    /// </summary>
    public string? CountryName { get; set; }

    /// <summary>
    /// Gets or sets the StaffMember residential state identifier. This is intentionally unavailable until the residential profile source is connected.
    /// </summary>
    public int? StateId { get; set; }

    /// <summary>
    /// Gets or sets the StaffMember residential state name. This is intentionally unavailable until the residential profile source is connected.
    /// </summary>
    public string? StateName { get; set; }

    /// <summary>
    /// Gets or sets the StaffMember residential city identifier. This is intentionally unavailable until the residential profile source is connected.
    /// </summary>
    public int? CityId { get; set; }

    /// <summary>
    /// Gets or sets the StaffMember residential city name. This is intentionally unavailable until the residential profile source is connected.
    /// </summary>
    public string? CityName { get; set; }

    #endregion

    #region Institution Context

    /// <summary>
    /// Gets or sets the client-safe encoded institution identifier.
    /// </summary>
    public string InstitutionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authenticated institution's company name.
    /// </summary>
    public string InstitutionName { get; set; } = string.Empty;

    #endregion

    #region Employment Context

    /// <summary>
    /// Gets or sets the single effective primary role assigned to the staffMember.
    /// </summary>
    public NewLoginRoleDTO PrimaryRole { get; set; } = null!;

    /// <summary>
    /// Gets or sets the effective non-primary roles assigned to the staffMember.
    /// The collection is always present and does not include navigation or permission data.
    /// </summary>
    public IReadOnlyCollection<NewLoginRoleDTO> SecondaryRoles { get; set; }
        = Array.Empty<NewLoginRoleDTO>();

    /// <summary>
    /// Gets or sets the staff-employment-type identifier.
    /// </summary>
    public int StaffEmploymentTypeId { get; set; }

    /// <summary>
    /// Gets or sets the staff-employment-type name.
    /// </summary>
    public string? StaffEmploymentTypeName { get; set; }

    /// <summary>
    /// Gets or sets the department identifier when the staffMember has an active department assignment.
    /// </summary>
    public int? DepartmentId { get; set; }

    /// <summary>
    /// Gets or sets the department name when the staffMember has an active department assignment.
    /// </summary>
    public string? DepartmentName { get; set; }

    /// <summary>
    /// Gets or sets the Position identifier when the staffMember has an active Position assignment.
    /// </summary>
    public int? StaffPositionMasterId { get; set; }

    /// <summary>
    /// Gets or sets the Position name when the staffMember has an active Position assignment.
    /// </summary>
    public string? PositionName { get; set; }

    #endregion

    #region Session Flags

    /// <summary>
    /// Gets or sets whether the staffMember must change the current password.
    /// </summary>
    public bool IsPasswordChangeRequired { get; set; }

    /// <summary>
    /// Gets or sets whether the staffMember has completed onboarding.
    /// </summary>
    public bool IsOnboard { get; set; }

    #endregion
}

/// <summary>
/// Represents one effective Institution-scoped role returned in the NewLogin bootstrap response.
/// </summary>
public sealed class NewLoginRoleDTO
{
    #region Role Identity

    /// <summary>
    /// Gets or sets the role identifier.
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Gets or sets the role name.
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the role-type identifier.
    /// </summary>
    public int RoleTypeId { get; set; }

    /// <summary>
    /// Gets or sets the role-type name used by the existing Institution login contract.
    /// </summary>
    public string RoleTypeName { get; set; } = string.Empty;

    #endregion
}

/// <summary>
/// Represents the minimal Host-user context returned after Host authentication.
/// </summary>
public sealed class NewLoginHostUserContextDTO
{
    #region Host User Identity

    /// <summary>
    /// Gets or sets the Host-user identifier.
    /// </summary>
    public long HostUserId { get; set; }

    /// <summary>
    /// Gets or sets the Host user's display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Host user's login identifier.
    /// </summary>
    public string LoginId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Host user's email address when it is available.
    /// </summary>
    public string? Email { get; set; }

    #endregion
}

/// <summary>
/// Represents the active Host role returned after Host authentication.
/// </summary>
public sealed class NewLoginHostRoleDTO
{
    #region Host Role Identity

    /// <summary>
    /// Gets or sets the Host-role identifier.
    /// </summary>
    public long HostRoleId { get; set; }

    /// <summary>
    /// Gets or sets the Host-role name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    #endregion
}
