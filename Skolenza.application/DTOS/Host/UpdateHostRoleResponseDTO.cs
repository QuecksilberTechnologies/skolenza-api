// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Represents host-role details returned after an update.
// ================================================================

namespace skolenza.application.DTOS.Host
{
    /// <summary>
    /// Represents the host-role details returned after an update operation.
    /// </summary>
    public class UpdateHostRoleResponseDTO
    {
        #region Properties

        /// <summary>
        /// Gets or sets the host-role identifier.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the host-role name.
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gets or sets the host-role description.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the host role is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the date and time at which the host role was last updated.
        /// </summary>
        public DateTime? UpdatedDateTime { get; set; }

        #endregion
    }
}
