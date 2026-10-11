// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Registers and maps the authenticated Skolenza SignalR
//               notification transport boundary.
// ============================================================================

using skolenza.api.Realtime.Hubs;
using skolenza.api.Realtime.Identity;
using skolenza.api.Realtime.Services;
using skolenza.application.Interfaces.IRealTimeNotification;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;

namespace skolenza.api.Realtime
{
    /// <summary>Registers and maps the API-owned SignalR notification services.</summary>
    public static class SignalRServiceExtensions
    {
        #region Registration

        /// <summary>Registers SignalR and its institution-safe application notification adapter.</summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The configured service collection.</returns>
        public static IServiceCollection AddSkolenzaSignalR(this IServiceCollection services)
        {
            services.AddSignalR();
            services.AddScoped<SignalRConnectionIdentityResolver>();
            services.AddSingleton<IUserIdProvider, SkolenzaUserIdProvider>();
            services.AddSingleton<IRealTimeNotificationService, SignalRRealTimeNotificationService>();

            return services;
        }

        #endregion

        #region Endpoint mapping

        /// <summary>Maps the authenticated notification hub to its fixed API route.</summary>
        /// <param name="endpoints">The endpoint route builder.</param>
        /// <returns>The configured endpoint route builder.</returns>
        public static IEndpointRouteBuilder MapSkolenzaSignalR(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHub<NotificationHub>(SignalRRouteConstants.NotificationHub);
            return endpoints;
        }

        #endregion
    }
}
