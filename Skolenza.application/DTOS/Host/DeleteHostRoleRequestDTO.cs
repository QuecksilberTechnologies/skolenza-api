// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Identifies a host role for soft deletion.
// ================================================================

namespace skolenza.application.DTOS.Host
{
    /// <summary>
    /// Represents the host-role identifier accepted by a soft-delete operation.
    /// </summary>
    public class DeleteHostRoleRequestDTO
    {
        #region Properties

        /// <summary>
        /// Gets or sets the host-role identifier.
        /// </summary>
        public long Id { get; set; }

        #endregion
    }
}
