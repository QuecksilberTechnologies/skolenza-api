// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Represents the details required to change a host-user password.
// ================================================================

namespace skolenza.application.DTOS.Host
{
    /// <summary>
    /// Represents the details required for a host user to change a password.
    /// </summary>
    public class ChangeHostUserPasswordRequestDTO
    {
        #region Properties

        /// <summary>
        /// Gets or sets the host-user identifier.
        /// </summary>
        public long HostUserId { get; set; }

        /// <summary>
        /// Gets or sets the current password to verify.
        /// </summary>
        public string OldPassword { get; set; } = null!;

        /// <summary>
        /// Gets or sets the new password.
        /// </summary>
        public string NewPassword { get; set; } = null!;

        /// <summary>
        /// Gets or sets the confirmation for the new password.
        /// </summary>
        public string ConfirmPassword { get; set; } = null!;

        #endregion
    }
}
