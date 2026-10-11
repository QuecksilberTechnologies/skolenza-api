// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines a selectable Institution role-type option.
// ================================================================

namespace skolenza.application.DTOS.Role
{
    /// <summary>
    /// Represents a role type published for Role create, edit and filtering controls.
    /// </summary>
    public sealed class RoleTypeOptionResponseDTO
    {
        /// <summary>Gets or sets the persisted numeric role type.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the stable role-type code used across environments.</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Gets or sets the user-facing option name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Gets or sets the optional user-facing option description.</summary>
        public string Description { get; set; } = string.Empty;
    }
}
