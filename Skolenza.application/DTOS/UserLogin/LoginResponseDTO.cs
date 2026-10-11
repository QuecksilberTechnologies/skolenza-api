// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Represents the additive login response for Institution StaffMember and Host principals.
// ============================================================================

using skolenza.application.DTOs.Staff;
using skolenza.application.DTOs.RoleModulePermission;
using skolenza.application.DTOS.Host;

namespace skolenza.application.DTOs.UserLogin
{
    /// <summary>
    /// Represents the login result while preserving the existing Institution StaffMember response fields.
    /// </summary>
    public class LoginResponseDTO
    {
        #region Shared Token Properties

        /// <summary>
        /// Gets or sets whether the login operation succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the existing access-token response field used by Institution StaffMember clients.
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        /// Gets or sets the explicit access-token field used by the Host login response.
        /// </summary>
        public string? AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the refresh token issued with the access token.
        /// </summary>
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Gets or sets the UTC expiration of the access token when the response is for a Host principal.
        /// </summary>
        public DateTime? TokenExpiry { get; set; }

        /// <summary>
        /// Gets or sets the application principal type when the response is for a Host principal.
        /// </summary>
        public string? UserType { get; set; }

        /// <summary>
        /// Gets or sets an optional response message.
        /// </summary>
        public string? Message { get; set; }

        #endregion

        #region Institution StaffMember Properties

        /// <summary>
        /// Gets or sets the Institution StaffMember information returned by the existing Institution login flow.
        /// </summary>
        public GetStaffMemberLoginInfoResponseDTO? StaffMemberInfo { get; set; }

        /// <summary>
        /// Gets or sets the comma-separated Institution role identifiers returned by the existing Institution login flow.
        /// </summary>
        public string? Allroles { get; set; }

        /// <summary>
        /// Gets or sets the common menu items returned by the existing Institution login flow.
        /// </summary>
        public List<ModuleDTO>? CommonItems { get; set; }

        /// <summary>
        /// Gets or sets the Institution operational menus returned by the existing Institution login flow.
        /// </summary>
        public List<MainModuleDto>? OperationalMenus { get; set; }

        #endregion

        #region Host Properties

        /// <summary>
        /// Gets or sets the authenticated Host user when the response is for a Host login.
        /// </summary>
        public GetHostUserResponseDTO? HostUser { get; set; }

        /// <summary>
        /// Gets or sets the active Host role when the response is for a Host login.
        /// </summary>
        public GetHostRoleResponseDTO? HostRole { get; set; }

        /// <summary>
        /// Gets or sets the effective Host permissions read from HostRoleModuleAndPermission.
        /// </summary>
        public List<HostUserPermissionResponseDTO>? HostPermissions { get; set; }

        #endregion
    }

    /// <summary>
    /// Represents a Institution common-menu node returned by the existing login response.
    /// </summary>
    public class ModuleDTO
    {
        /// <summary>
        /// Gets or sets the module identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the module name.
        /// </summary>
        public string ModuleName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the module URL path.
        /// </summary>
        public string? URLPath { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// Gets or sets whether this node is a leaf node.
        /// </summary>
        public bool? IsLeafNode { get; set; }

        /// <summary>
        /// Gets or sets the web icon path.
        /// </summary>
        public string? ImageIconWeb { get; set; }

        /// <summary>
        /// Gets or sets the mobile icon path.
        /// </summary>
        public string? ImageIconMobile { get; set; }

        /// <summary>
        /// Gets or sets the display priority.
        /// </summary>
        public int? ItemPriority { get; set; }

        /// <summary>
        /// Gets or sets the child modules.
        /// </summary>
        public List<ModuleDTO> Children { get; set; } = new();
    }
}
