// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Defines authoritative, transport-neutral institution connection
//               membership data for real-time group assignment.
// ============================================================================

namespace skolenza.application.Interfaces.IRealTimeNotification
{
    /// <summary>
    /// Resolves the current backend-authoritative memberships required to group a institution user connection.
    /// </summary>
    public interface IRealTimeConnectionMembershipResolver
    {
        #region Membership resolution

        /// <summary>
        /// Resolves active institution, staffMember, role, department, and country memberships for one connection.
        /// Country/state targeting remains deferred until Institution has an authoritative State relationship.
        /// </summary>
        /// <param name="institutionId">The institution identifier already validated from the authenticated token.</param>
        /// <param name="staffMemberId">The staffMember identifier already validated from the authenticated token.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The current membership projection, or <see langword="null"/> when the institution staffMember is no longer active.
        /// </returns>
        Task<RealTimeConnectionMembership?> ResolveInstitutionUserMembershipAsync(
            long institutionId,
            long staffMemberId,
            CancellationToken cancellationToken = default);

        #endregion
    }

    /// <summary>
    /// Contains only the IDs required for server-controlled real-time group membership.
    /// </summary>
    public sealed class RealTimeConnectionMembership
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RealTimeConnectionMembership"/> class.
        /// </summary>
        /// <param name="institutionId">The active institution identifier.</param>
        /// <param name="staffMemberId">The active staffMember identifier.</param>
        /// <param name="roleIds">The staffMember's distinct active institution role identifiers.</param>
        /// <param name="departmentIds">The staffMember's distinct active institution department identifiers.</param>
        /// <param name="departmentRoleMemberships">The staffMember's authoritative department-role intersections.</param>
        /// <param name="countryId">The active institution country identifier, when available.</param>
        public RealTimeConnectionMembership(
            long institutionId,
            long staffMemberId,
            IReadOnlyCollection<int> roleIds,
            IReadOnlyCollection<int> departmentIds,
            IReadOnlyCollection<RealTimeDepartmentRoleMembership> departmentRoleMemberships,
            int? countryId)
        {
            InstitutionId = institutionId;
            StaffMemberId = staffMemberId;
            RoleIds = roleIds;
            DepartmentIds = departmentIds;
            DepartmentRoleMemberships = departmentRoleMemberships;
            CountryId = countryId;
        }

        /// <summary>Gets the active institution identifier.</summary>
        public long InstitutionId { get; }

        /// <summary>Gets the active staffMember identifier.</summary>
        public long StaffMemberId { get; }

        /// <summary>Gets the staffMember's distinct active institution role identifiers.</summary>
        public IReadOnlyCollection<int> RoleIds { get; }

        /// <summary>Gets the staffMember's distinct active institution department identifiers.</summary>
        public IReadOnlyCollection<int> DepartmentIds { get; }

        /// <summary>Gets only department-role intersections proven for this staffMember.</summary>
        public IReadOnlyCollection<RealTimeDepartmentRoleMembership> DepartmentRoleMemberships { get; }

        /// <summary>Gets the active institution country identifier, when available.</summary>
        public int? CountryId { get; }

    }

    /// <summary>
    /// Represents one department and role combination currently held by the same institution staffMember.
    /// </summary>
    public sealed class RealTimeDepartmentRoleMembership
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RealTimeDepartmentRoleMembership"/> class.
        /// </summary>
        /// <param name="departmentId">The active institution department identifier.</param>
        /// <param name="roleId">The active institution role identifier.</param>
        public RealTimeDepartmentRoleMembership(int departmentId, int roleId)
        {
            DepartmentId = departmentId;
            RoleId = roleId;
        }

        /// <summary>Gets the active institution department identifier.</summary>
        public int DepartmentId { get; }

        /// <summary>Gets the active institution role identifier.</summary>
        public int RoleId { get; }
    }
}
