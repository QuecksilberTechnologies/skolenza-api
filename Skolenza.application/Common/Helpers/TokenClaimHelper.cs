using skolenza.application.DTOS.Token;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims; using skolenza.domain.Entity; using MediatR;
using System.Text;

public static class TokenClaimHelper
{
    /// <summary>
    /// Extracts and validates JWT token claims
    /// Used for Login / Invite / SetPassword / ResetPassword flows
    /// </summary>
    public static GetTokenInfoDTO? ExtractClaims(string token, string? secretKey)
    {
        // 🔒 Secret key validation
        if (string.IsNullOrEmpty(secretKey))
            throw new ArgumentNullException(nameof(secretKey), "Secret key cannot be null or empty.");

        // 🔒 Token validation
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            // ✅ Detect Base64 OR plain text secret key automatically
            byte[] key;
            try
            {
                key = Convert.FromBase64String(secretKey);
            }
            catch
            {
                key = Encoding.UTF8.GetBytes(secretKey);
            }

            // 🔐 Validate token signature & expiry
            var principal = tokenHandler.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero // ⏱️ No extra grace time
                },
                out SecurityToken validatedToken
            );

            var jwtToken = (JwtSecurityToken)validatedToken;

            // ✅ Map token claims to DTO
            var dto = new GetTokenInfoDTO
            {
                // 🔹 User / StaffMember
                UserId = principal.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? string.Empty,
                StaffMemberId = principal.Claims.FirstOrDefault(c => c.Type == "StaffMemberId")?.Value ?? "0",

                // 🔹 Role info
                RoleId = principal.Claims.FirstOrDefault(c => c.Type == "RoleId")?.Value ?? "0",
                RoleTypeId = principal.Claims.FirstOrDefault(c => c.Type == "RoleTypeId")?.Value ?? "0",
                StaffEmploymentTypeId = principal.Claims.FirstOrDefault(c => c.Type == "StaffEmploymentTypeId")?.Value ?? "0",

                // 🔹 Institution info
                InstitutionId = principal.Claims.FirstOrDefault(c => c.Type == "InstitutionId")?.Value ?? "0",
                InstitutionEncriptionKey = principal.Claims
                    .FirstOrDefault(c => c.Type == "InstitutionEncriptionKey")?.Value ?? string.Empty,

                // 🔹 User info
                Email = principal.Claims
                    .FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? string.Empty,
                FullName = principal.Claims
                    .FirstOrDefault(c => c.Type == "FullName")?.Value ?? string.Empty,

                // 🔥 IMPORTANT: Token purpose (Login / SetPassword / ResetPassword)
                // 👉 Invite flow ke liye mandatory
                TokenPurpose = principal.Claims
                 .FirstOrDefault(c => c.Type == "TokenPurpose")?.Value ?? string.Empty,

                // ⏱️ Expiry handling
                Expiry = jwtToken.ValidTo,
                IsExpired = jwtToken.ValidTo < DateTime.UtcNow
            };

            return dto;
        }
        catch (Exception)
        {
            // ❌ Invalid token / signature / expired / tampered
            return null;
        }
    }
}
