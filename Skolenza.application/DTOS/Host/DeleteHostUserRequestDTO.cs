// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Identifies a host user for soft deletion.
// ================================================================

namespace skolenza.application.DTOS.Host
{
    /// <summary>
    /// Represents the host-user identifier accepted by a soft-delete operation.
    /// </summary>
    public class DeleteHostUserRequestDTO
    {
        #region Properties

        /// <summary>
        /// Gets or sets the host-user identifier.
        /// </summary>
        public long Id { get; set; }

        #endregion
    }
}
