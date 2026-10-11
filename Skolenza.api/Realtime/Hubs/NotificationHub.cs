// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : Sr. Software Engineer
// Purpose     : Hosts authenticated, server-assigned SignalR notification
//               connections for institution staffMembers and Host users.
// ============================================================================

using skolenza.api.Realtime.Identity;
using skolenza.application.Interfaces.IRealTimeNotification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace skolenza.api.Realtime.Hubs
{
    /// <summary>Provides the authenticated connection boundary for real-time notifications.</summary>
    [Authorize]
    public sealed class NotificationHub : Hub
    {
        #region Fields

        private readonly SignalRConnectionIdentityResolver _identityResolver;
        private readonly IRealTimeConnectionMembershipResolver _membershipResolver;
        private readonly ILogger<NotificationHub> _logger;

        #endregion

        #region Constructor

        /// <summary>Initializes a new instance of the <see cref="NotificationHub"/> class.</summary>
        /// <param name="identityResolver">Resolves trusted JWT claims into server-owned groups.</param>
        /// <param name="membershipResolver">Resolves current backend-authoritative institution memberships.</param>
        /// <param name="logger">Logs lifecycle events without credentials or payloads.</param>
        public NotificationHub(
            SignalRConnectionIdentityResolver identityResolver,
            IRealTimeConnectionMembershipResolver membershipResolver,
            ILogger<NotificationHub> logger)
        {
            _identityResolver = identityResolver;
            _membershipResolver = membershipResolver;
            _logger = logger;
        }

        #endregion

        #region Connection lifecycle

        /// <summary>Assigns the connection exclusively to authenticated and backend-authoritative groups.</summary>
        /// <returns>A task that completes when SignalR finishes the connection callback.</returns>
        public override async Task OnConnectedAsync()
        {
            if (!_identityResolver.TryResolve(Context.User, out var identity))
            {
                // Security: InstitutionId and UserId come only from Context.User's authenticated JWT claims.
                // Clients cannot choose a group, institution, role, department, or geography through this hub.
                _logger.LogWarning(
                    "Rejected SignalR connection with invalid authenticated identity. ConnectionId: {ConnectionId}",
                    Context.ConnectionId);
                Context.Abort();
                return;
            }

            var groupNames = await ResolveConnectionGroupsAsync(identity);
            if (groupNames == null)
            {
                Context.Abort();
                return;
            }

            foreach (var groupName in groupNames)
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName, Context.ConnectionAborted);

            _logger.LogInformation(
                "SignalR {PrincipalType} connection established. ConnectionId: {ConnectionId}, GroupCount: {GroupCount}",
                identity.PrincipalType,
                Context.ConnectionId,
                groupNames.Count);

            await base.OnConnectedAsync();
        }

        /// <summary>Records a disconnected connection without retaining connection state in the database.</summary>
        /// <param name="exception">The disconnect exception, when one occurred.</param>
        /// <returns>A task that completes when SignalR finishes the disconnect callback.</returns>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation(
                exception,
                "SignalR notification connection disconnected. ConnectionId: {ConnectionId}",
                Context.ConnectionId);

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Resolves only groups proven by the authenticated identity and current authoritative membership data.
        /// </summary>
        /// <param name="identity">The authenticated connection identity.</param>
        /// <returns>A distinct group collection, or <see langword="null"/> when the connection must be rejected.</returns>
        private async Task<IReadOnlyCollection<string>?> ResolveConnectionGroupsAsync(SignalRConnectionIdentity identity)
        {
            if (identity.PrincipalType == SignalRPrincipalType.HostUser)
                return new[] { identity.PrimaryGroup };

            if (identity.InstitutionId is not > 0 || identity.StaffMemberId is not > 0)
            {
                _logger.LogWarning(
                    "Rejected institution SignalR connection without a complete authenticated identity. ConnectionId: {ConnectionId}",
                    Context.ConnectionId);
                return null;
            }

            RealTimeConnectionMembership? membership;
            try
            {
                membership = await _membershipResolver.ResolveInstitutionUserMembershipAsync(
                    identity.InstitutionId.Value,
                    identity.StaffMemberId.Value,
                    Context.ConnectionAborted);
            }
            catch
            {
                // Security: do not expose persistence details to a connected client when membership cannot be verified.
                _logger.LogError(
                    "Unable to verify current institution SignalR membership. ConnectionId: {ConnectionId}",
                    Context.ConnectionId);
                return null;
            }

            if (membership == null)
            {
                _logger.LogWarning(
                    "Rejected institution SignalR connection with inactive or unavailable membership. ConnectionId: {ConnectionId}",
                    Context.ConnectionId);
                return null;
            }

            if (membership.InstitutionId != identity.InstitutionId.Value ||
                membership.StaffMemberId != identity.StaffMemberId.Value)
            {
                _logger.LogWarning(
                    "Rejected institution SignalR connection with mismatched authoritative membership. ConnectionId: {ConnectionId}",
                    Context.ConnectionId);
                return null;
            }

            if (membership.DepartmentIds.Count == 0)
                _logger.LogDebug("Institution SignalR connection has no active department group. ConnectionId: {ConnectionId}", Context.ConnectionId);

            if (membership.CountryId is not > 0)
                _logger.LogDebug("Institution SignalR connection has no active country group. ConnectionId: {ConnectionId}", Context.ConnectionId);

            return SignalRGroupNameFactory.GetInstitutionConnectionGroups(membership);
        }

        #endregion
    }
}
