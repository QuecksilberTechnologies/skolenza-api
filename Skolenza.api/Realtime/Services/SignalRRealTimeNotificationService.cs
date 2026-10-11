// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Implements application real-time notification publishing with
//               authenticated, institution-safe SignalR recipients.
// ============================================================================

using skolenza.api.Realtime.Hubs;
using skolenza.api.Realtime.Identity;
using skolenza.application.Interfaces.IRealTimeNotification;
using Microsoft.AspNetCore.SignalR;

namespace skolenza.api.Realtime.Services
{
    /// <summary>Publishes application events to server-derived SignalR groups and users.</summary>
    public sealed class SignalRRealTimeNotificationService : IRealTimeNotificationService
    {
        #region Fields

        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<SignalRRealTimeNotificationService> _logger;

        #endregion

        #region Constructor

        /// <summary>Initializes a new instance of the <see cref="SignalRRealTimeNotificationService"/> class.</summary>
        /// <param name="hubContext">The authenticated SignalR hub context.</param>
        /// <param name="logger">Logs delivery attempts without payload or token data.</param>
        public SignalRRealTimeNotificationService(
            IHubContext<NotificationHub> hubContext,
            ILogger<SignalRRealTimeNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        #endregion

        #region Publishing

        /// <inheritdoc />
        public async Task SendToInstitutionAsync<T>(
            long institutionId,
            string eventName,
            T payload,
            CancellationToken cancellationToken = default)
        {
            var institutionGroup = SignalRGroupNameFactory.Institution(institutionId);

            // Security: Clients.All must never carry institution business data because it crosses institution boundaries.
            await SendAsync(_hubContext.Clients.Group(institutionGroup), eventName, payload, cancellationToken);
            _logger.LogDebug("Published SignalR event to a institution group. InstitutionId: {InstitutionId}", institutionId);
        }

        /// <inheritdoc />
        public async Task SendToUserAsync<T>(
            long institutionId,
            long userId,
            string eventName,
            T payload,
            CancellationToken cancellationToken = default)
        {
            var userIdentifier = SignalRGroupNameFactory.InstitutionUser(institutionId, userId);
            await SendAsync(_hubContext.Clients.User(userIdentifier), eventName, payload, cancellationToken);
            _logger.LogDebug("Published SignalR event to a institution user.");
        }

        /// <inheritdoc />
        public async Task SendToHostUserAsync<T>(
            long hostUserId,
            string eventName,
            T payload,
            CancellationToken cancellationToken = default)
        {
            var userIdentifier = SignalRGroupNameFactory.HostUser(hostUserId);
            await SendAsync(_hubContext.Clients.User(userIdentifier), eventName, payload, cancellationToken);
            _logger.LogDebug("Published SignalR event to a Host user.");
        }

        /// <inheritdoc />
        public async Task SendToInstitutionRoleAsync<T>(
            long institutionId,
            int roleId,
            string eventName,
            T payload,
            CancellationToken cancellationToken = default)
        {
            var roleGroup = SignalRGroupNameFactory.InstitutionRole(institutionId, roleId);
            await SendAsync(_hubContext.Clients.Group(roleGroup), eventName, payload, cancellationToken);
            _logger.LogDebug("Published SignalR event to a institution role group.");
        }

        /// <inheritdoc />
        public async Task SendToInstitutionRolesAsync<T>(
            long institutionId,
            IReadOnlyCollection<int> roleIds,
            string eventName,
            T payload,
            CancellationToken cancellationToken = default)
        {
            var roleGroups = CreateInstitutionRoleGroups(institutionId, roleIds);
            await SendAsync(_hubContext.Clients.Groups(roleGroups), eventName, payload, cancellationToken);
            _logger.LogDebug("Published SignalR event to {GroupCount} institution role groups.", roleGroups.Count);
        }

        /// <inheritdoc />
        public async Task SendToDepartmentAsync<T>(
            long institutionId,
            int departmentId,
            string eventName,
            T payload,
            CancellationToken cancellationToken = default)
        {
            var departmentGroup = SignalRGroupNameFactory.InstitutionDepartment(institutionId, departmentId);
            await SendAsync(_hubContext.Clients.Group(departmentGroup), eventName, payload, cancellationToken);
            _logger.LogDebug("Published SignalR event to a institution department group.");
        }

        /// <inheritdoc />
        public async Task SendToDepartmentRoleAsync<T>(
            long institutionId,
            int departmentId,
            int roleId,
            string eventName,
            T payload,
            CancellationToken cancellationToken = default)
        {
            var departmentRoleGroup = SignalRGroupNameFactory.InstitutionDepartmentRole(institutionId, departmentId, roleId);
            await SendAsync(_hubContext.Clients.Group(departmentRoleGroup), eventName, payload, cancellationToken);
            _logger.LogDebug("Published SignalR event to a institution department-role group.");
        }

        /// <inheritdoc />
        public async Task SendToDepartmentRolesAsync<T>(
            long institutionId,
            int departmentId,
            IReadOnlyCollection<int> roleIds,
            string eventName,
            T payload,
            CancellationToken cancellationToken = default)
        {
            var departmentRoleGroups = CreateDepartmentRoleGroups(institutionId, departmentId, roleIds);
            await SendAsync(_hubContext.Clients.Groups(departmentRoleGroups), eventName, payload, cancellationToken);
            _logger.LogDebug("Published SignalR event to {GroupCount} institution department-role groups.", departmentRoleGroups.Count);
        }

        /// <inheritdoc />
        public async Task SendToAllInstitutionsAsync<T>(
            string eventName,
            T payload,
            CancellationToken cancellationToken = default)
        {
            // Security: a future Host command/API must run ValidateInstitutionUserRequestAsync(), Host validation, and permission checks first.
            await SendAsync(
                _hubContext.Clients.Group(SignalRGroupNameFactory.AllInstitutions()),
                eventName,
                payload,
                cancellationToken);
            _logger.LogDebug("Published SignalR event to the all-institutions group.");
        }

        /// <inheritdoc />
        public async Task SendToCountryInstitutionsAsync<T>(
            int countryId,
            string eventName,
            T payload,
            CancellationToken cancellationToken = default)
        {
            // Security: a future Host command/API must run ValidateInstitutionUserRequestAsync(), Host validation, and permission checks first.
            var countryGroup = SignalRGroupNameFactory.CountryInstitutions(countryId);
            await SendAsync(_hubContext.Clients.Group(countryGroup), eventName, payload, cancellationToken);
            _logger.LogDebug("Published SignalR event to a country institution group.");
        }

        #endregion

        #region Private helpers

        /// <summary>Validates the event boundary and sends without broadcasting to unrelated users.</summary>
        private static Task SendAsync<T>(
            IClientProxy client,
            string eventName,
            T payload,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentException("An event name is required.", nameof(eventName));

            return client.SendAsync(eventName, payload, cancellationToken);
        }

        /// <summary>
        /// Creates distinct institution role groups from structurally valid authoritative role identifiers.
        /// </summary>
        private static IReadOnlyList<string> CreateInstitutionRoleGroups(long institutionId, IReadOnlyCollection<int> roleIds)
        {
            return ValidateAndDistinctRoleIds(roleIds)
                .Select(roleId => SignalRGroupNameFactory.InstitutionRole(institutionId, roleId))
                .ToArray();
        }

        /// <summary>
        /// Creates distinct institution department-role groups from structurally valid authoritative role identifiers.
        /// </summary>
        private static IReadOnlyList<string> CreateDepartmentRoleGroups(
            long institutionId,
            int departmentId,
            IReadOnlyCollection<int> roleIds)
        {
            return ValidateAndDistinctRoleIds(roleIds)
                .Select(roleId => SignalRGroupNameFactory.InstitutionDepartmentRole(institutionId, departmentId, roleId))
                .ToArray();
        }

        /// <summary>
        /// Rejects missing or non-positive role identifiers and removes duplicate selected roles.
        /// </summary>
        private static IReadOnlyCollection<int> ValidateAndDistinctRoleIds(IReadOnlyCollection<int> roleIds)
        {
            ArgumentNullException.ThrowIfNull(roleIds);

            var distinctRoleIds = roleIds.Distinct().ToArray();
            if (distinctRoleIds.Length == 0)
                throw new ArgumentException("At least one role identifier is required.", nameof(roleIds));

            if (distinctRoleIds.Any(roleId => roleId <= 0))
                throw new ArgumentOutOfRangeException(nameof(roleIds), "Role identifiers must be positive.");

            return distinctRoleIds;
        }

        #endregion
    }
}
