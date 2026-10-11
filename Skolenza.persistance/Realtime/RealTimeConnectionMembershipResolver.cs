// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Projects active institution staffMember memberships from persistence
//               data for server-controlled real-time grouping.
// ============================================================================

using skolenza.application.Interfaces.IRealTimeNotification;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Realtime
{
    /// <summary>
    /// Resolves the current active institution membership projection without returning tracked entities.
    /// </summary>
    public sealed class RealTimeConnectionMembershipResolver : IRealTimeConnectionMembershipResolver
    {
        #region Fields

        private readonly SkolenzaDbContext _context;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="RealTimeConnectionMembershipResolver"/> class.
        /// </summary>
        /// <param name="context">The workforce database context.</param>
        public RealTimeConnectionMembershipResolver(SkolenzaDbContext context)
        {
            _context = context;
        }

        #endregion

        #region Membership resolution

        /// <inheritdoc />
        public async Task<RealTimeConnectionMembership?> ResolveInstitutionUserMembershipAsync(
            long institutionId,
            long staffMemberId,
            CancellationToken cancellationToken = default)
        {
            if (institutionId <= 0 || staffMemberId <= 0)
                return null;

            var institutionStaffMember = await (
                from staffMember in _context.StaffMembers.AsNoTracking()
                join institution in _context.Institutions.AsNoTracking() on staffMember.InstitutionId equals institution.Id
                where staffMember.Id == staffMemberId &&
                      staffMember.InstitutionId == institutionId &&
                      staffMember.IsActive &&
                      staffMember.IsSoftDeleted != true &&
                      institution.IsActive &&
                      institution.IsSoftDeleted != true
                select new
                {
                    DepartmentId = _context.Departments
                        .AsNoTracking()
                        .Where(department =>
                            department.Id == staffMember.DepartmentId &&
                            department.InstitutionId == institution.Id &&
                            department.IsActive &&
                            department.IsSoftDeleted != true)
                        .Select(department => (int?)department.Id)
                        .FirstOrDefault(),
                    CountryId = _context.Countries
                        .AsNoTracking()
                        .Where(country => country.Id == institution.CountryId && country.IsActive == true)
                        .Select(country => (int?)country.Id)
                        .FirstOrDefault()
                }).FirstOrDefaultAsync(cancellationToken);

            if (institutionStaffMember == null)
                return null;

            var roleIds = await _context.UserRoles
                .AsNoTracking()
                .Where(userRole =>
                    userRole.StaffMemberId == staffMemberId &&
                    userRole.IsActive &&
                    userRole.IsSoftDeleted != true &&
                    userRole.RoleId.HasValue &&
                    userRole.Role != null &&
                    userRole.Role.IsActive &&
                    userRole.Role.IsSoftDeleted != true &&
                    userRole.Role.InstitutionId == institutionId)
                .Select(userRole => userRole.RoleId!.Value)
                .Distinct()
                .ToArrayAsync(cancellationToken);

            var departmentIds = institutionStaffMember.DepartmentId is > 0
                ? new[] { institutionStaffMember.DepartmentId.Value }
                : Array.Empty<int>();
            var departmentRoleMemberships = departmentIds
                .SelectMany(departmentId => roleIds.Select(roleId =>
                    new RealTimeDepartmentRoleMembership(departmentId, roleId)))
                .ToArray();

            // Country/state targeting is deferred until Institution has an authoritative State relationship.
            return new RealTimeConnectionMembership(
                institutionId,
                staffMemberId,
                roleIds,
                departmentIds,
                departmentRoleMemberships,
                institutionStaffMember.CountryId);
        }

        #endregion
    }
}
