// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Defines common refresh-token persistence operations for Institution StaffMembers and Host users.
// ============================================================================

using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.ITokenService
{
    /// <summary>
    /// Defines common refresh-token lookup, insertion, rotation, and revocation operations.
    /// </summary>
    public interface IRefreshTokenRepository
    {
        #region Refresh Token Operations

        /// <summary>
        /// Retrieves a refresh token by its SHA-256 hash, including its declared owner navigations,
        /// without pre-filtering revocation or expiration.
        /// </summary>
        /// <param name="hashedToken">The SHA-256 hash of the opaque submitted token.</param>
        /// <returns>The matching token, or <see langword="null"/> when no token exists.</returns>
        Task<RefreshToken?> GetByHashedTokenAsync(string hashedToken);

        /// <summary>
        /// Persists a new common refresh token for the supplied principal type.
        /// </summary>
        /// <param name="token">The refresh-token entity with a hashed token value and owner type.</param>
        /// <returns><see langword="true"/> when persistence succeeds; otherwise, <see langword="false"/>.</returns>
        Task<bool> InsertAsync(RefreshToken token);

        /// <summary>
        /// Revokes a refresh token as part of token rotation.
        /// </summary>
        /// <param name="refreshTokenId">The refresh-token identifier.</param>
        /// <param name="revokedByIp">The client IP associated with revocation.</param>
        Task RevokeAsync(long refreshTokenId, string? revokedByIp);

        /// <summary>
        /// Records the hash of the token that replaced an older token during rotation.
        /// </summary>
        /// <param name="refreshTokenId">The replaced refresh-token identifier.</param>
        /// <param name="replacedByHashedToken">The hash of the replacement token.</param>
        Task UpdateReplacedByTokenAsync(long refreshTokenId, string replacedByHashedToken);

        /// <summary>V2 only: atomically claims an unrevoked, unexpired token inside the caller's replacement transaction.</summary>
        Task<bool> TryClaimForRotationAsync(long refreshTokenId, string replacementHash, string? ipAddress,
            DateTime now, CancellationToken cancellationToken = default);

        #endregion
    }
}
