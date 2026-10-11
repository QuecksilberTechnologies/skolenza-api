using skolenza.application.Common.Contexts;
using skolenza.application.Constants;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Interfaces.ITokenService;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace skolenza.api.Middlewares
{
    public class InstitutionContextMiddleware
    {
        private const string InstitutionContextKey = "InstitutionContext";
        private readonly RequestDelegate _next;
        private readonly ILogger<InstitutionContextMiddleware> _logger;

        public InstitutionContextMiddleware(
            RequestDelegate next,
            ILogger<InstitutionContextMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            ITokenService tokenService,
            IInstitutionKeyResolver institutionKeyResolver,
            IIdEncoderService encoderService)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(authHeader) ||
                !authHeader.StartsWith("Bearer "))
            {
                await _next(context);
                return;
            }

            var token = authHeader.Replace("Bearer ", "").Trim();
            var claims = tokenService.ValidateAndExtractClaims(token);

            if (claims == null || claims.IsExpired)
            {
                await _next(context);
                return;
            }

            var encodedInstitutionId = claims.InstitutionId;
            if (string.IsNullOrWhiteSpace(encodedInstitutionId))
            {
                if (string.Equals(
                        claims.UserType,
                        AppConstants.HostUserType,
                        StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(
                        claims.TokenPurpose,
                        AppConstants.AccessTokenPurpose,
                        StringComparison.Ordinal))
                {
                    await _next(context);
                    return;
                }

                _logger.LogWarning("InstitutionId missing in a non-Host access token.");
                throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
            }

            var institutionId = encoderService.DecodeId_long(encodedInstitutionId, null);
            if (institutionId <= 0)
            {
                _logger.LogWarning("Invalid InstitutionId after decoding.");
                throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
            }

            var encryptionKey = await institutionKeyResolver.ResolveAsync(institutionId);

            context.Items[InstitutionContextKey] = new InstitutionContext
            {
                InstitutionId = institutionId,
                InstitutionEncryptionKey = encryptionKey
            };

            await _next(context);
        }
    }
}
