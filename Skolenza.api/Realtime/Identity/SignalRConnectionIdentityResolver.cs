// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Resolves authenticated institution and Host SignalR identities from
//               existing JWT claims without accepting client-selected groups.
// ============================================================================

using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.Constants;
using skolenza.application.Interfaces.IEncryptionService;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace skolenza.api.Realtime.Identity
{
    /// <summary>Resolves the server-owned SignalR identity and groups for an authenticated connection.</summary>
    public sealed class SignalRConnectionIdentityResolver
    {
        private const string InstitutionUserIdClaim = "UserId";
        private const string InstitutionStaffMemberIdClaim = "StaffMemberId";
        private const string InstitutionIdClaim = "InstitutionId";
        private const string InstitutionEncryptionKeyClaim = "InstitutionEncriptionKey";
        private const string InstitutionRoleIdClaim = "RoleId";
        private const string TokenPurposeClaim = "TokenPurpose";

        private readonly IIdEncoderService _idEncoderService;

        /// <summary>Initializes a new instance of the <see cref="SignalRConnectionIdentityResolver"/> class.</summary>
        /// <param name="idEncoderService">The established identifier encoder used by existing JWT claims.</param>
        public SignalRConnectionIdentityResolver(IIdEncoderService idEncoderService)
        {
            _idEncoderService = idEncoderService;
        }

        /// <summary>Resolves an authenticated connection into server-derived group and user identifiers.</summary>
        /// <param name="principal">The JWT-authenticated principal supplied by SignalR.</param>
        /// <param name="identity">The resolved identity when validation succeeds.</param>
        /// <returns><see langword="true"/> when the claims describe a valid access-token principal.</returns>
        internal bool TryResolve(
            ClaimsPrincipal? principal,
            [NotNullWhen(true)] out SignalRConnectionIdentity? identity)
        {
            identity = null;

            if (principal?.Identity?.IsAuthenticated != true)
                return false;

            var userType = principal.FindFirst(AppConstants.UserTypeClaim)?.Value;
            if (string.Equals(userType, AppConstants.HostUserType, StringComparison.Ordinal))
            {
                return TryResolveHostUser(principal, out identity);
            }

            // Empty is allowed only for a valid legacy Institution token issued before UserType was added.
            return string.IsNullOrWhiteSpace(userType) ||
                   string.Equals(userType, AppConstants.InstitutionUserType, StringComparison.Ordinal)
                ? TryResolveInstitutionUser(principal, out identity)
                : false;
        }

        #region Institution identity

        /// <summary>Resolves a institution staffMember from the established encrypted StaffMemberId and InstitutionId claims.</summary>
        private bool TryResolveInstitutionUser(
            ClaimsPrincipal principal,
            [NotNullWhen(true)] out SignalRConnectionIdentity? identity)
        {
            identity = null;

            var userId = principal.FindFirst(InstitutionUserIdClaim)?.Value;
            var encryptedStaffMemberId = principal.FindFirst(InstitutionStaffMemberIdClaim)?.Value;
            var encryptedInstitutionId = principal.FindFirst(InstitutionIdClaim)?.Value;
            var institutionEncryptionKey = principal.FindFirst(InstitutionEncryptionKeyClaim)?.Value;
            var roleId = principal.FindFirst(InstitutionRoleIdClaim)?.Value;
            var tokenPurpose = principal.FindFirst(TokenPurposeClaim)?.Value;

            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(encryptedStaffMemberId) ||
                string.IsNullOrWhiteSpace(encryptedInstitutionId) ||
                string.IsNullOrWhiteSpace(institutionEncryptionKey) ||
                !long.TryParse(roleId, out var parsedRoleId) ||
                parsedRoleId <= 0 ||
                !string.Equals(tokenPurpose, ConstantValues.Auth.ToString(), StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                var institutionId = RequestCommonHelper.DecodeUserAndInstitutionIds(
                    encryptedInstitutionId,
                    institutionEncryptionKey,
                    _idEncoderService);
                var staffMemberId = RequestCommonHelper.DecodeOnlyStaffMemberId(
                    encryptedStaffMemberId,
                    institutionEncryptionKey,
                    _idEncoderService);

                if (institutionId <= 0 || staffMemberId <= 0)
                    return false;

                var institutionGroup = SignalRGroupNameFactory.Institution(institutionId);
                var userIdentifier = SignalRGroupNameFactory.InstitutionUser(institutionId, staffMemberId);

                identity = new SignalRConnectionIdentity(
                    SignalRPrincipalType.InstitutionUser,
                    institutionGroup,
                    userIdentifier,
                    institutionId,
                    staffMemberId);
                return true;
            }
            catch (Exception)
            {
                // Security: malformed signed claims must not result in a partially assigned connection.
                return false;
            }
        }

        #endregion

        #region Host identity

        /// <summary>Resolves a Host user from the established Host token claims.</summary>
        private static bool TryResolveHostUser(
            ClaimsPrincipal principal,
            [NotNullWhen(true)] out SignalRConnectionIdentity? identity)
        {
            identity = null;

            var hostUserId = principal.FindFirst(AppConstants.HostUserIdClaim)?.Value;
            var hostRoleId = principal.FindFirst(AppConstants.HostRoleIdClaim)?.Value;
            var loginId = principal.FindFirst(AppConstants.LoginIdClaim)?.Value;
            var tokenPurpose = principal.FindFirst(TokenPurposeClaim)?.Value;

            if (!long.TryParse(hostUserId, out var parsedHostUserId) ||
                parsedHostUserId <= 0 ||
                !long.TryParse(hostRoleId, out var parsedHostRoleId) ||
                parsedHostRoleId <= 0 ||
                string.IsNullOrWhiteSpace(loginId) ||
                !string.Equals(tokenPurpose, AppConstants.AccessTokenPurpose, StringComparison.Ordinal))
            {
                return false;
            }

            var userIdentifier = SignalRGroupNameFactory.HostUser(parsedHostUserId);
            identity = new SignalRConnectionIdentity(
                SignalRPrincipalType.HostUser,
                SignalRGroupNameFactory.Host(),
                userIdentifier,
                InstitutionId: null,
                StaffMemberId: null);
            return true;
        }

        #endregion
    }

    /// <summary>Identifies the authenticated principal category assigned to a SignalR connection.</summary>
    internal enum SignalRPrincipalType
    {
        /// <summary>The connection belongs to a institution staffMember.</summary>
        InstitutionUser,

        /// <summary>The connection belongs to a Host user.</summary>
        HostUser
    }

    /// <summary>Represents the server-derived group and user identifier for one SignalR connection.</summary>
    internal sealed record SignalRConnectionIdentity(
        SignalRPrincipalType PrincipalType,
        string PrimaryGroup,
        string UserIdentifier,
        long? InstitutionId,
        long? StaffMemberId);

    #region SignalR user identity provider

    /// <summary>Supplies the same trusted user identifier that the hub assigns to each connection.</summary>
    public sealed class SkolenzaUserIdProvider : IUserIdProvider
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        /// <summary>Initializes a new instance of the <see cref="SkolenzaUserIdProvider"/> class.</summary>
        /// <param name="serviceScopeFactory">Creates the scoped claim resolver needed for each connection.</param>
        public SkolenzaUserIdProvider(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        /// <summary>Gets the server-derived unique identifier for a connected user.</summary>
        /// <param name="connection">The authenticated hub connection.</param>
        /// <returns>A institution-safe user identifier, or <see langword="null"/> for invalid claims.</returns>
        public string? GetUserId(HubConnectionContext connection)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var identityResolver = scope.ServiceProvider.GetRequiredService<SignalRConnectionIdentityResolver>();

            return identityResolver.TryResolve(connection.User, out var identity)
                ? identity.UserIdentifier
                : null;
        }
    }

    #endregion
}
