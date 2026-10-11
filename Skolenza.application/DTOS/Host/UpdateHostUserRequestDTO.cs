// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Represents editable host-user details.
// ================================================================

namespace skolenza.application.DTOS.Host
{
    /// <summary>
    /// Represents the host-user details accepted by an update operation.
    /// </summary>
    public class UpdateHostUserRequestDTO
    {
        #region Properties

        /// <summary>
        /// Gets or sets the host-user identifier.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the associated host-role identifier.
        /// </summary>
        public long HostRoleId { get; set; }

        /// <summary>
        /// Gets or sets the host user's name.
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Gets or sets the host user's login identifier.
        /// </summary>
        public string LoginId { get; set; } = null!;

        /// <summary>
        /// Gets or sets the host user's email address.
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Gets or sets the host user's mobile number.
        /// </summary>
        public string? MobileNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the host user is active.
        /// </summary>
        public bool IsActive { get; set; }

        #endregion
    }
}
