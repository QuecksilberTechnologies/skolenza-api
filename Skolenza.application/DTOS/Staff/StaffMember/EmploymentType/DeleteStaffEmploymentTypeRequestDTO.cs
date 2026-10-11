// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines the permission-aware request for institution StaffEmploymentType deletion.
// ================================================================

using skolenza.application.DTOs.BaseDTO;

namespace skolenza.application.DTOS.Staff.Type;

/// <summary>
/// Represents a request to soft delete one institution-owned StaffEmploymentType.
/// </summary>
public sealed class DeleteStaffEmploymentTypeRequestDTO : PermissionRequestDTO
{
    #region Properties

    /// <summary>
    /// Gets or sets the StaffEmploymentType identifier to delete.
    /// </summary>
    public required int Id { get; set; }

    #endregion
}
