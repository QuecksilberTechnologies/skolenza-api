// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Creates server-owned SignalR group and user identifiers for
//               institution staffMembers and Host users.
// ============================================================================

using skolenza.application.Interfaces.IRealTimeNotification;

namespace skolenza.api.Realtime.Identity
{
    /// <summary>Creates stable, institution-safe SignalR identifiers from trusted server values.</summary>
    public static class SignalRGroupNameFactory
    {
        #region Group and user names

        /// <summary>Gets the group containing all authenticated connections for a institution.</summary>
        public static string Institution(long institutionId)
        {
            EnsurePositiveIdentifier(institutionId, nameof(institutionId));
            return $"institution:{institutionId}";
        }

        /// <summary>Gets the group containing all authenticated Host-user connections.</summary>
        public static string Host() => "host";

        /// <summary>Gets the unique identifier for an authenticated institution staffMember.</summary>
        public static string InstitutionUser(long institutionId, long userId)
        {
            EnsurePositiveIdentifier(userId, nameof(userId));
            return $"{Institution(institutionId)}:user:{userId}";
        }

        /// <summary>Gets the unique identifier for an authenticated Host user.</summary>
        public static string HostUser(long hostUserId)
        {
            EnsurePositiveIdentifier(hostUserId, nameof(hostUserId));
            return $"host:user:{hostUserId}";
        }

        /// <summary>
        /// Gets the group containing institution connections with one active role.
        /// </summary>
        /// <param name="institutionId">The trusted institution identifier.</param>
        /// <param name="roleId">The authoritative role identifier.</param>
        /// <returns>The institution role group name.</returns>
        public static string InstitutionRole(long institutionId, int roleId)
        {
            EnsurePositiveIdentifier(roleId, nameof(roleId));
            // Security: InstitutionId is mandatory so equal role IDs cannot cross institution boundaries.
            return $"{Institution(institutionId)}:role:{roleId}";
        }

        /// <summary>
        /// Gets the group containing institution connections in one active department.
        /// </summary>
        /// <param name="institutionId">The trusted institution identifier.</param>
        /// <param name="departmentId">The authoritative department identifier.</param>
        /// <returns>The institution department group name.</returns>
        public static string InstitutionDepartment(long institutionId, int departmentId)
        {
            EnsurePositiveIdentifier(departmentId, nameof(departmentId));
            // Security: InstitutionId is mandatory so equal department IDs cannot cross institution boundaries.
            return $"{Institution(institutionId)}:department:{departmentId}";
        }

        /// <summary>
        /// Gets the group containing institution connections with one proven department-role membership.
        /// </summary>
        /// <param name="institutionId">The trusted institution identifier.</param>
        /// <param name="departmentId">The authoritative department identifier.</param>
        /// <param name="roleId">The authoritative role identifier.</param>
        /// <returns>The institution department-role group name.</returns>
        public static string InstitutionDepartmentRole(long institutionId, int departmentId, int roleId)
        {
            return $"{InstitutionDepartment(institutionId, departmentId)}:role:{EnsurePositiveIdentifier(roleId, nameof(roleId))}";
        }

        /// <summary>
        /// Gets the group containing authenticated connections from every active institution.
        /// </summary>
        /// <returns>The all-institutions group name.</returns>
        public static string AllInstitutions()
        {
            // Security: this group excludes Host users and is used instead of Clients.All for institution data.
            return "institutions:all";
        }

        /// <summary>
        /// Gets the group containing institution connections in one country.
        /// Country/state targeting is deferred until Institution has an authoritative State relationship.
        /// </summary>
        /// <param name="countryId">The authoritative country identifier.</param>
        /// <returns>The country institution group name.</returns>
        public static string CountryInstitutions(int countryId)
        {
            EnsurePositiveIdentifier(countryId, nameof(countryId));
            return $"institutions:country:{countryId}";
        }

        /// <summary>
        /// Creates all institution-only groups proven by an authoritative membership projection.
        /// </summary>
        /// <param name="membership">The authoritative membership projection.</param>
        /// <returns>A distinct collection of server-owned group names.</returns>
        public static IReadOnlyCollection<string> GetInstitutionConnectionGroups(RealTimeConnectionMembership membership)
        {
            ArgumentNullException.ThrowIfNull(membership);

            // Security: this accepts only the backend membership projection; clients never supply group identifiers.

            var groups = new HashSet<string>(StringComparer.Ordinal)
            {
                Institution(membership.InstitutionId),
                AllInstitutions()
            };

            foreach (var roleId in membership.RoleIds.Where(roleId => roleId > 0).Distinct())
                groups.Add(InstitutionRole(membership.InstitutionId, roleId));

            foreach (var departmentId in membership.DepartmentIds.Where(departmentId => departmentId > 0).Distinct())
                groups.Add(InstitutionDepartment(membership.InstitutionId, departmentId));

            foreach (var departmentRole in membership.DepartmentRoleMemberships
                         .Where(item => item.DepartmentId > 0 && item.RoleId > 0)
                         .DistinctBy(item => (item.DepartmentId, item.RoleId)))
            {
                groups.Add(InstitutionDepartmentRole(
                    membership.InstitutionId,
                    departmentRole.DepartmentId,
                    departmentRole.RoleId));
            }

            if (membership.CountryId is > 0)
                groups.Add(CountryInstitutions(membership.CountryId.Value));

            return groups.ToArray();
        }

        #endregion

        #region Validation

        /// <summary>Prevents invalid values from becoming server-owned group or user identifiers.</summary>
        /// <param name="identifier">The identifier to validate.</param>
        /// <param name="parameterName">The source parameter name.</param>
        private static long EnsurePositiveIdentifier(long identifier, string parameterName)
        {
            if (identifier <= 0)
                throw new ArgumentOutOfRangeException(parameterName, "A positive identifier is required.");

            return identifier;
        }

        #endregion
    }
}
