// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines client-editable fields for a institution StaffEmploymentType update.
// ================================================================

namespace skolenza.application.DTOS.Staff.Type;

/// <summary>
/// Represents the editable values for an existing institution-owned StaffEmploymentType.
/// </summary>
public sealed class UpdateStaffEmploymentTypeRequestDTO : CreateStaffEmploymentTypeDTO
{
    #region Properties

    /// <summary>
    /// Gets or sets the StaffEmploymentType identifier to update.
    /// </summary>
    public required int Id { get; set; }

    #endregion
}
