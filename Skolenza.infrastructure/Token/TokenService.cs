using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.DTOs.Staff;
using skolenza.application.DTOs.UserLogin;
using skolenza.application.DTOS.Token;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.ITokenService;
using skolenza.application.Wrappers;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;

// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Generates and validates Institution and Host access tokens using shared signing infrastructure.
// ============================================================================

namespace skolenza.infrastructure.Token
{
    /// <summary>
    /// Provides the existing Institution token operations and dedicated Host access-token generation.
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEncryptionService _encryptionService;

        public TokenService(
            IConfiguration configuration,
            ILogger<TokenService> logger,
            IUnitOfWork unitOfWork,
            IEncryptionService encryptionService)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        }

        #region Institution Access Token Generation

        /// <summary>
        /// Generates a Institution StaffMember access token while preserving the established Institution claim set.
        /// </summary>
        /// <param name="user">The Institution StaffMember token information.</param>
        /// <returns>The signed Institution access token, or <see langword="null"/> when generation fails.</returns>
        public async Task<string> GenerateInstitutionToken(GetTokenInfoDTO user)
        {
            try
            {
                if (user == null)
                    throw new ArgumentNullException(nameof(user), "User object cannot be null.");

                // 🧩 Add all custom claims safely
                var claims = new List<Claim>();
                try
                {
                    claims.Add(new Claim("UserId", user.UserId?.ToString() ?? string.Empty));
                    claims.Add(new Claim("StaffMemberId", user.StaffMemberId?.ToString() ?? string.Empty));
                    claims.Add(new Claim("RoleId", user.RoleId.ToString() ?? string.Empty));
                    claims.Add(new Claim("RoleTypeId", user.RoleTypeId.ToString() ?? string.Empty));
                    claims.Add(new Claim("StaffEmploymentTypeId", user.StaffEmploymentTypeId.ToString() ?? string.Empty));
                    claims.Add(new Claim("InstitutionId", user.InstitutionId?.ToString() ?? string.Empty));
                    claims.Add(new Claim(ClaimTypes.Email, user.Email ?? string.Empty));
                    claims.Add(new Claim("FullName", user.FullName ?? string.Empty));
                    claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
                    claims.Add(new Claim("InstitutionEncriptionKey", user.InstitutionEncriptionKey ?? string.Empty));
                    claims.Add(new Claim("GenderId", user.GenderId.ToString() ?? string.Empty));
                    claims.Add(new Claim("GenderName", user.GenderName ?? string.Empty));
                    claims.Add(new Claim("StaffEmploymentTypeName", user.StaffEmploymentTypeName ?? string.Empty));
                    claims.Add(new Claim("RoleTypeName", user.RoleTypeName ?? string.Empty));
                    claims.Add(new Claim(AppConstants.UserTypeClaim, AppConstants.InstitutionUserType));
                    claims.Add(new Claim("HasPermanent", user.HasPermanent.ToString()));
                    claims.Add(new Claim("TokenPurpose", user.TokenPurpose.ToString()));
                    claims.Add(new Claim("IssuedAt", user.HasPermanent.ToString()));
                    claims.Add(new Claim("ClientType", user.HasPermanent.ToString()));


                }
                catch (Exception innerEx)
                {
                    _logger.LogWarning(innerEx, "Error creating claims for user {UserId}", user.UserId);
                }

                var jwtToken = await GenerateSignedAccessTokenAsync(
                    claims,
                    includeNotBefore: false);

                _logger.LogInformation("Token generated successfully for UserId: {UserId}", user.UserId);

                return jwtToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Token generation failed for User: {UserId}", user?.UserId);
                return null;
            }
        }

        #endregion

        #region Host Access Token Generation

        /// <summary>
        /// Generates a Host access token without institution staffMember claims.
        /// </summary>
        /// <param name="hostUser">The authenticated Host principal information.</param>
        /// <returns>The signed Host access token, or an empty string when generation fails.</returns>
        public async Task<string> GenerateHostToken(HostTokenInfoDTO hostUser)
        {
            try
            {
                if (hostUser == null)
                {
                    throw new ArgumentNullException(nameof(hostUser), "Host user object cannot be null.");
                }

                // A Host session has no Institution dependency. Generate an ephemeral Host-scoped key using
                // the established key-generation service and retain it only in the signed access token.
                var hostInstitutionEncryptionKey = _encryptionService.GenerateKey();
                if (string.IsNullOrWhiteSpace(hostInstitutionEncryptionKey))
                {
                    throw new InvalidOperationException("The Host Institution identifier protection key could not be generated.");
                }

                var claims = new List<Claim>
                {
                    new Claim(JwtRegisteredClaimNames.Sub, hostUser.HostUserId.ToString()),
                    new Claim(AppConstants.HostUserIdClaim, hostUser.HostUserId.ToString()),
                    new Claim(AppConstants.HostRoleIdClaim, hostUser.HostRoleId.ToString()),
                    new Claim(AppConstants.LoginIdClaim, hostUser.LoginId),
                    new Claim("InstitutionEncriptionKey", hostInstitutionEncryptionKey),
                    new Claim("Name", hostUser.Name),
                    new Claim(AppConstants.UserTypeClaim, AppConstants.HostUserType),
                    new Claim("TokenPurpose", AppConstants.AccessTokenPurpose),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                if (!string.IsNullOrWhiteSpace(hostUser.Email))
                {
                    claims.Add(new Claim(ClaimTypes.Email, hostUser.Email));
                }

                var jwtToken = await GenerateSignedAccessTokenAsync(
                    claims,
                    includeNotBefore: true);

                _logger.LogInformation(
                    "Host token generated successfully for HostUserId: {HostUserId}",
                    hostUser.HostUserId);

                return jwtToken;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Host token generation failed for HostUserId: {HostUserId}",
                    hostUser?.HostUserId);
                return string.Empty;
            }
        }

        #endregion

        // ✅ 2️⃣ Validate Token
        public bool ValidateToken(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return false;

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["JWTSettings:Secret"]);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out _);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed.");
                return false;
            }
        }

        // ✅ 3️⃣ Extract Claims from Token
        public async Task<string> GetUserInfoFromToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return JsonConvert.SerializeObject(new { Error = "Token is missing.", IsExpired = true });

            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["JWTSettings:Secret"]);

            try
            {
                var principal = handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var userInfo = principal.Claims.ToDictionary(c => c.Type, c => c.Value);
                userInfo["Expiry"] = (validatedToken as JwtSecurityToken)?.ValidTo.ToString("o") ?? "";
                userInfo["IsExpired"] = ((validatedToken as JwtSecurityToken)?.ValidTo ?? DateTime.UtcNow) < DateTime.UtcNow ? "true" : "false";

                return JsonConvert.SerializeObject(userInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invalid or expired token detected.");
                return JsonConvert.SerializeObject(new { Error = "Invalid or tampered token.", IsExpired = true });
            }
        }

        // ✅ 4️⃣ Extract Expiry from Token
        public DateTime? GetExpiryFromToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                return jwtToken?.ValidTo;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not extract expiry from token.");
                return null;
            }
        }

        // ✅ 5️⃣ Generate Refresh Token
        public async Task<string> GenerateRefreshToken()
        {
            try
            {
                var randomNumber = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(randomNumber);
                    return Convert.ToBase64String(randomNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating refresh token.");
                return string.Empty;
            }
        }
        public TokenClaimsModel? ValidateAndExtractClaims(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["JWTSettings:Secret"]);

                var principal = handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = _configuration["JWTSettings:Issuer"],
                    ValidAudience = _configuration["JWTSettings:Audience"],
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwt = (JwtSecurityToken)validatedToken;

                return new TokenClaimsModel
                {
                    UserId = principal.FindFirst("UserId")?.Value,
                    InstitutionId = principal.FindFirst("InstitutionId")?.Value,
                    StaffMemberId = principal.FindFirst("StaffMemberId")?.Value,
                    RoleId = principal.FindFirst("RoleId")?.Value,
                    Email = principal.FindFirst(ClaimTypes.Email)?.Value,
                    Expiry = jwt.ValidTo,
                    IsExpired = jwt.ValidTo < DateTime.UtcNow,
                    TokenPurpose = principal.FindFirst("TokenPurpose")?.Value,
                    UserType = principal.FindFirst(AppConstants.UserTypeClaim)?.Value
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task<GetTokenInfoDTO?> GetUserInfoByLoginIdAsync(string loginId)
        {
            try
            {
                // ✅ Step 1: Validate active login and get StaffMemberId
                var empId = await _unitOfWork.StoreProcedureRepository.ValidateActiveUserLoginOnlyAsync(loginId);
                if (empId < 1)
                {
                    _logger.LogWarning($"No active user found for LoginId: {loginId}");
                    return null;
                }

                // ✅ Step 2: Get StaffMember details
                var userMin = await _unitOfWork.StaffMembers.GetSingleRecordAsync(empId, true);
                if (userMin == null)
                {
                    _logger.LogWarning($"StaffMember record not found for StaffMemberId: {empId}");
                    return null;
                }

                // ✅ Step 3: Get Institution encryption key
                var institutionKey = await _unitOfWork.InstitutionEncryptionKeyRepository.GetActiveKeyByInstitutionIdAsync(userMin.InstitutionId);
                if (institutionKey == null)
                {
                    _logger.LogWarning($"Institution encryption key not found for InstitutionId: {userMin.InstitutionId}");
                    return null;
                }

                // ✅ Step 4: Get user roles
                var roles = await _unitOfWork.UserRoleRepository.GetStaffMemberRolesWithDetailsByIdAsync(empId, userMin.InstitutionId);
                var roleInfo = roles?.FirstOrDefault(r => r.IsPrimaryRole == true);
                if (roleInfo == null)
                {
                    _logger.LogWarning($"Primary role not found for StaffMemberId: {empId}");
                    return null;
                }

                // ✅ Step 5: Build and return token info DTO
                var tokenInfo = new GetTokenInfoDTO
                {
                    InstitutionEncriptionKey = institutionKey.EncryptionKey,
                    InstitutionId = userMin.InstitutionId.ToString(),
                    UserId = loginId,
                    StaffMemberId = userMin.Id.ToString(),
                    RoleId = roleInfo.RoleId.ToString(),
                    RoleTypeId = roleInfo.Role.RoleType.ToString(),
                    RoleTypeName = roleInfo.Role.RoleName,
                    Email = userMin.OfficialEmail ?? string.Empty,
                    FullName = $"{userMin.FirstName} {userMin.LastName}",
                    Expiry = DateTime.UtcNow.AddMinutes(15)
                };

                return tokenInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching user info for LoginId: {loginId}");
                return null;
            }
        }

        #region Private Methods

        /// <summary>
        /// Signs a JWT with the configured issuer, audience, lifetime, and HMAC-SHA256 credentials.
        /// </summary>
        /// <param name="claims">The already-approved claims for the token type being generated.</param>
        /// <param name="includeNotBefore">Whether to include the standard not-before timestamp.</param>
        /// <returns>The serialized signed JWT.</returns>
        private Task<string> GenerateSignedAccessTokenAsync(
            IEnumerable<Claim> claims,
            bool includeNotBefore)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtKey = _configuration["JWTSettings:Secret"];

            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new ArgumentNullException("JWTSettings:Secret", "JWT key cannot be null or empty.");
            }

            var key = Encoding.UTF8.GetBytes(jwtKey);
            if (key.Length < 32)
            {
                throw new ArgumentException("JWT key must be at least 32 bytes long for HMAC SHA-256.");
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.Add(TimeSpan.Parse(_configuration["JWTSettings:TokenLifetime"])),
                Issuer = _configuration["JWTSettings:Issuer"],
                Audience = _configuration["JWTSettings:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            if (includeNotBefore)
            {
                tokenDescriptor.NotBefore = DateTime.UtcNow;
            }

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return Task.FromResult(tokenHandler.WriteToken(token));
        }

        #endregion
    }
}
   

   
//claims.Add(new Claim("UserId", user.UserId ?? ""));
//claims.Add(new Claim("StaffMemberId", user.StaffMemberId ?? ""));
//claims.Add(new Claim("RoleId", user.RoleId ?? ""));
//claims.Add(new Claim("RoleTypeId", user.RoleTypeId ?? ""));
//claims.Add(new Claim("StaffEmploymentTypeId", user.StaffEmploymentTypeId ?? ""));
//claims.Add(new Claim("InstitutionId", user.InstitutionId ?? ""));
//claims.Add(new Claim(ClaimTypes.Email, user.Email ?? ""));
//claims.Add(new Claim("FullName", user.FullName ?? ""));
//claims.Add(new Claim("TokenPurpose", user.TokenPurpose ?? "AUTH"));

//claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
//claims.Add(new Claim(JwtRegisteredClaimNames.Iat,
//    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
//    ClaimValueTypes.Integer64));


