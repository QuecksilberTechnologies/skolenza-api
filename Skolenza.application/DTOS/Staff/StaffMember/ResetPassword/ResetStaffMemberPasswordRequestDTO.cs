// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the authorized Institution administrator request for
//           resetting a selected StaffMember login password.
// ================================================================

using skolenza.application.DTOs.BaseDTO;
using System.ComponentModel.DataAnnotations;

namespace skolenza.application.DTOS.Staff.ResetPassword;

/// <summary>
/// Represents an administrator-initiated password reset for one StaffMember in
/// the authenticated Institution. The staffMember identifier is client-safe encoded.
/// </summary>
public sealed class ResetStaffMemberPasswordRequestDTO : PermissionRequestDTO
{
    /// <summary>
    /// Gets or sets the client-safe encoded StaffMember identifier whose password
    /// is being reset.
    /// </summary>
    [Required]
    public string StaffMemberId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the replacement password.
    /// </summary>
    [Required]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the confirmation of the replacement password.
    /// </summary>
    [Required]
    public string ConfirmPassword { get; set; } = string.Empty;
}
