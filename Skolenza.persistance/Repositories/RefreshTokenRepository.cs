// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Persists common refresh tokens for Institution StaffMembers and Host users.
// ============================================================================

using skolenza.application.Interfaces.ITokenService;
using skolenza.application.Common.Enums;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace skolenza.persistance.Repositories
{
    /// <summary>
    /// Provides common hashed refresh-token persistence and rotation operations.
    /// </summary>
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        #region Fields

        private readonly SkolenzaDbContext _context;
        private readonly ILogger<RefreshTokenRepository> _logger;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="RefreshTokenRepository"/> class.
        /// </summary>
        /// <param name="context">The database context used to persist refresh tokens.</param>
        /// <param name="logger">The logger used to record persistence failures.</param>
        public RefreshTokenRepository(
            SkolenzaDbContext context,
            ILogger<RefreshTokenRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        #endregion

        #region Refresh Token Operations

        /// <inheritdoc />
        public Task<RefreshToken?> GetByHashedTokenAsync(string hashedToken)
        {
            // Revoked and expired rows are intentionally returned so the handler can
            // distinguish invalid input from reuse and expiration attempts.
            return _context.RefreshTokens
                .AsNoTracking()
                .Include(token => token.LoginCredential)
                .Include(token => token.HostUser)
                .FirstOrDefaultAsync(token => token.Token == hashedToken);
        }

        /// <inheritdoc />
        public async Task<bool> InsertAsync(RefreshToken token)
        {
            if (!HasValidOwner(token))
            {
                _logger.LogError(
                    "Rejected refresh token with invalid owner invariant. LoginId={LoginId}, UserType={UserType}, LoginCredentialId={LoginCredentialId}, HostUserId={HostUserId}",
                    token.LoginId,
                    token.UserType,
                    token.LoginCredentialId,
                    token.HostUserId);
                return false;
            }

            try
            {
                await _context.RefreshTokens.AddAsync(token);
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to insert refresh token for LoginId {LoginId} and UserType {UserType}.",
                    token.LoginId,
                    token.UserType);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task RevokeAsync(long refreshTokenId, string? revokedByIp)
        {
            var token = await _context.RefreshTokens.FindAsync(refreshTokenId);
            if (token == null)
            {
                return;
            }

            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = revokedByIp;

            await _context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task UpdateReplacedByTokenAsync(
            long refreshTokenId,
            string replacedByHashedToken)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(item => item.Id == refreshTokenId);

            if (token == null)
            {
                _logger.LogWarning(
                    "Refresh token was not found while recording its replacement. TokenId={TokenId}",
                    refreshTokenId);
                return;
            }

            token.ReplacedByToken = replacedByHashedToken;
            await _context.SaveChangesAsync();
        }

        #endregion

        #region V2 Atomic Rotation Claim

        /// <inheritdoc />
        public async Task<bool> TryClaimForRotationAsync(
            long refreshTokenId, string replacementHash, string? ipAddress,
            DateTime now, CancellationToken cancellationToken = default)
        {
            if (_context.Database.CurrentTransaction == null)
            {
                throw new InvalidOperationException("Refresh rotation requires an active transaction.");
            }

            var affected = await _context.RefreshTokens
                .Where(token => token.Id == refreshTokenId && token.IsRevoked != true && token.ExpiryDate > now)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(token => token.IsRevoked, true)
                    .SetProperty(token => token.RevokedAt, now)
                    .SetProperty(token => token.RevokedByIp, ipAddress)
                    .SetProperty(token => token.ReplacedByToken, replacementHash), cancellationToken);
            return affected == 1;
        }

        #endregion

        #region Owner Validation

        /// <summary>
        /// Determines whether a refresh token has exactly one valid owner foreign key for its declared user type.
        /// </summary>
        /// <param name="token">The refresh token to validate before persistence.</param>
        /// <returns><see langword="true"/> when the owner fields satisfy the ownership invariant; otherwise, <see langword="false"/>.</returns>
        private static bool HasValidOwner(RefreshToken token)
        {
            return token.UserType switch
            {
                (short)LoginUserType.InstitutionStaffMember =>
                    token.LoginCredentialId.HasValue && !token.HostUserId.HasValue,
                (short)LoginUserType.Host =>
                    token.HostUserId.HasValue && !token.LoginCredentialId.HasValue,
                _ => false
            };
        }

        #endregion
    }
}
