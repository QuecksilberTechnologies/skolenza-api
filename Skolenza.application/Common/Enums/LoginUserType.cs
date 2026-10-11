// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Identifies the principal type that owns a common refresh token.
// ============================================================================

namespace skolenza.application.Common.Enums
{
    /// <summary>
    /// Identifies the authenticated principal type associated with a refresh-token row.
    /// </summary>
    public enum LoginUserType : short
    {
        /// <summary>
        /// Identifies a Institution StaffMember refresh token.
        /// </summary>
        InstitutionStaffMember = 1,

        /// <summary>
        /// Identifies a Host User refresh token.
        /// </summary>
        Host = 2
    }
}
