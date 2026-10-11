// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Supplies the Host-specific identity data required for access-token generation.
// ============================================================================

namespace skolenza.application.DTOS.Token
{
    /// <summary>
    /// Represents the Host principal data that is safe and necessary to place in a Host access token.
    /// </summary>
    public class HostTokenInfoDTO
    {
        #region Identity

        /// <summary>
        /// Gets or sets the Host-user primary key used as the token subject.
        /// </summary>
        public long HostUserId { get; set; }

        /// <summary>
        /// Gets or sets the Host-role primary key assigned to the Host user.
        /// </summary>
        public long HostRoleId { get; set; }

        /// <summary>
        /// Gets or sets the Host user's login identifier.
        /// </summary>
        public string LoginId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Host user's display name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Host user's email address when one is available.
        /// </summary>
        public string? Email { get; set; }

        #endregion
    }
}
