// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Defines the fixed, authenticated SignalR endpoint routes.
// ============================================================================

namespace skolenza.api.Realtime
{
    /// <summary>Defines SignalR endpoint routes shared by authentication and endpoint mapping.</summary>
    public static class SignalRRouteConstants
    {
        #region Hub routes

        /// <summary>Gets the authenticated notifications hub route.</summary>
        public const string NotificationHub = "/hubs/notification";

        #endregion
    }
}
