// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Represents editable host-role details.
// ================================================================

namespace skolenza.application.DTOS.Host
{
    /// <summary>
    /// Represents the host-role details accepted by an update operation.
    /// </summary>
    public class UpdateHostRoleRequestDTO
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

        #endregion
    }
}
