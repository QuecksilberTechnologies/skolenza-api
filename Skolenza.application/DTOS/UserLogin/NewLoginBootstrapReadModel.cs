// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the minimal persistence read model used to construct a NewLogin Institution StaffMember session.
// ================================================================

namespace skolenza.application.DTOs.UserLogin;

/// <summary>
/// Represents the validated, minimal Institution StaffMember data projected by the login repository for a NewLogin session.
/// </summary>
public sealed class NewLoginBootstrapReadModel
{
    #region Trusted Identity Data

    /// <summary>
    /// Gets or sets the internal staffMember identifier used only for trusted server-side processing.
    /// </summary>
    public long StaffMemberId { get; set; }

    /// <summary>
    /// Gets or sets the internal institution identifier used only for trusted server-side processing.
    /// </summary>
    public long InstitutionId { get; set; }

    /// <summary>
    /// Gets or sets the staffMember's first name.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the staffMember's middle name.
    /// </summary>
    public string? MiddleName { get; set; }

    /// <summary>
    /// Gets or sets the staffMember's last name.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the staffMember's official email address.
    /// </summary>
    public string? OfficialEmail { get; set; }

    /// <summary>
    /// Gets or sets the active institution's company name.
    /// </summary>
    public string InstitutionName { get; set; } = string.Empty;

    #endregion

    #region Employment Data

    /// <summary>
    /// Gets or sets the staff-employment-type identifier.
    /// </summary>
    public int StaffEmploymentTypeId { get; set; }

    /// <summary>
    /// Gets or sets the staff-employment-type name.
    /// </summary>
    public string? StaffEmploymentTypeName { get; set; }

    /// <summary>
    /// Gets or sets the active department identifier when assigned.
    /// </summary>
    public int? DepartmentId { get; set; }

    /// <summary>
    /// Gets or sets the active department name when assigned.
    /// </summary>
    public string? DepartmentName { get; set; }

    /// <summary>
    /// Gets or sets the active designation identifier when assigned.
    /// </summary>
    public int? DesignationId { get; set; }

    /// <summary>
    /// Gets or sets the active designation name when assigned.
    /// </summary>
    public string? DesignationName { get; set; }

    #endregion

    #region Token and Session Data

    /// <summary>
    /// Gets or sets the staffMember's gender identifier used by the existing Institution access-token claim set.
    /// </summary>
    public int GenderId { get; set; }

    /// <summary>
    /// Gets or sets the staffMember's gender name used by the existing Institution access-token claim set.
    /// </summary>
    public string? GenderName { get; set; }

    /// <summary>
    /// Gets or sets whether the staffMember has permanent employment status for the existing Institution access-token claim set.
    /// </summary>
    public bool HasPermanent { get; set; }

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
