// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Defines transport-agnostic, institution-safe real-time notification
//               publishing for application handlers.
// ============================================================================

namespace skolenza.application.Interfaces.IRealTimeNotification
{
    /// <summary>
    /// Publishes real-time notifications without coupling application code to a transport implementation.
    /// Live delivery is not a substitute for permanent notification storage or audit records.
    /// </summary>
    public interface IRealTimeNotificationService
    {
        #region Publishing

        /// <summary>Sends an event to authenticated connections that belong to one institution.</summary>
        /// <typeparam name="T">The event payload type.</typeparam>
        /// <param name="institutionId">The trusted institution identifier.</param>
        /// <param name="eventName">The client event name.</param>
        /// <param name="payload">The event payload.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the transport accepts the event.</returns>
        Task SendToInstitutionAsync<T>(long institutionId, string eventName, T payload, CancellationToken cancellationToken = default);

        /// <summary>Sends an event to one authenticated institution staffMember.</summary>
        /// <typeparam name="T">The event payload type.</typeparam>
        /// <param name="institutionId">The trusted institution identifier.</param>
        /// <param name="userId">The trusted staffMember identifier represented by the access-token StaffMemberId claim.</param>
        /// <param name="eventName">The client event name.</param>
        /// <param name="payload">The event payload.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the transport accepts the event.</returns>
        Task SendToUserAsync<T>(long institutionId, long userId, string eventName, T payload, CancellationToken cancellationToken = default);

        /// <summary>Sends an event to one authenticated Host user.</summary>
        /// <typeparam name="T">The event payload type.</typeparam>
        /// <param name="hostUserId">The trusted Host-user identifier.</param>
        /// <param name="eventName">The client event name.</param>
        /// <param name="payload">The event payload.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the transport accepts the event.</returns>
        Task SendToHostUserAsync<T>(long hostUserId, string eventName, T payload, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends an event to institution users with one authoritative role membership.
        /// </summary>
        /// <typeparam name="T">The event payload type.</typeparam>
        /// <param name="institutionId">The trusted institution identifier.</param>
        /// <param name="roleId">The authoritative institution role identifier.</param>
        /// <param name="eventName">The client event name.</param>
        /// <param name="payload">The event payload.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the transport accepts the event.</returns>
        Task SendToInstitutionRoleAsync<T>(
            long institutionId,
            int roleId,
            string eventName,
            T payload,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends an event to institution users with any selected authoritative role membership.
        /// </summary>
        /// <typeparam name="T">The event payload type.</typeparam>
        /// <param name="institutionId">The trusted institution identifier.</param>
        /// <param name="roleIds">The selected authoritative institution role identifiers.</param>
        /// <param name="eventName">The client event name.</param>
        /// <param name="payload">The event payload.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the transport accepts the event.</returns>
        Task SendToInstitutionRolesAsync<T>(
            long institutionId,
            IReadOnlyCollection<int> roleIds,
            string eventName,
            T payload,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends an event to institution users in one authoritative department.
        /// </summary>
        /// <typeparam name="T">The event payload type.</typeparam>
        /// <param name="institutionId">The trusted institution identifier.</param>
        /// <param name="departmentId">The authoritative institution department identifier.</param>
        /// <param name="eventName">The client event name.</param>
        /// <param name="payload">The event payload.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the transport accepts the event.</returns>
        Task SendToDepartmentAsync<T>(
            long institutionId,
            int departmentId,
            string eventName,
            T payload,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends an event to institution users with one authoritative department-role membership.
        /// </summary>
        /// <typeparam name="T">The event payload type.</typeparam>
        /// <param name="institutionId">The trusted institution identifier.</param>
        /// <param name="departmentId">The authoritative institution department identifier.</param>
        /// <param name="roleId">The authoritative institution role identifier.</param>
        /// <param name="eventName">The client event name.</param>
        /// <param name="payload">The event payload.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the transport accepts the event.</returns>
        Task SendToDepartmentRoleAsync<T>(
            long institutionId,
            int departmentId,
            int roleId,
            string eventName,
            T payload,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends an event to institution users with any selected authoritative role membership in one department.
        /// </summary>
        /// <typeparam name="T">The event payload type.</typeparam>
        /// <param name="institutionId">The trusted institution identifier.</param>
        /// <param name="departmentId">The authoritative institution department identifier.</param>
        /// <param name="roleIds">The selected authoritative institution role identifiers.</param>
        /// <param name="eventName">The client event name.</param>
        /// <param name="payload">The event payload.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the transport accepts the event.</returns>
        Task SendToDepartmentRolesAsync<T>(
            long institutionId,
            int departmentId,
            IReadOnlyCollection<int> roleIds,
            string eventName,
            T payload,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends an event to all active institution connections.
        /// Future Host command/API callers must run existing ValidateInstitutionUserRequestAsync(), applicable Host validation, and Host permission checks before invoking this capability.
        /// </summary>
        /// <typeparam name="T">The event payload type.</typeparam>
        /// <param name="eventName">The client event name.</param>
        /// <param name="payload">The event payload.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the transport accepts the event.</returns>
        Task SendToAllInstitutionsAsync<T>(
            string eventName,
            T payload,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends an event to active institution connections in one authoritative country.
        /// Country/state targeting is deferred until Institution has an authoritative State relationship.
        /// Future Host command/API callers must run existing ValidateInstitutionUserRequestAsync(), applicable Host validation, and Host permission checks before invoking this capability.
        /// </summary>
        /// <typeparam name="T">The event payload type.</typeparam>
        /// <param name="countryId">The authoritative country identifier.</param>
        /// <param name="eventName">The client event name.</param>
        /// <param name="payload">The event payload.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when the transport accepts the event.</returns>
        Task SendToCountryInstitutionsAsync<T>(
            int countryId,
            string eventName,
            T payload,
            CancellationToken cancellationToken = default);

        #endregion
    }
}
