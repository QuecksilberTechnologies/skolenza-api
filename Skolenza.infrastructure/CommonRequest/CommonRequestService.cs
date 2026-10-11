// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Provides shared institution and Host request validation.
// ================================================================

using skolenza.application.Common.Helpers.Converters;
using skolenza.application.Common.Helpers.EncryptionHelper;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Models.Security;
using skolenza.application.Constants;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace skolenza.infrastructure.CommonRequest
{
    public class CommonRequestService : ICommonRequestService
    {
        private readonly IHttpContextAccessor _context;
        private readonly IConfiguration _config;
        private readonly IUnitOfWork _uow;
        private readonly IIdEncoderService _encoder;
        private readonly ILogger<CommonRequestService> _logger;

        public CommonRequestService(
            IHttpContextAccessor ctx,
            IConfiguration cfg,
            IUnitOfWork uow,
            IIdEncoderService enc,
            ILogger<CommonRequestService> logger)
        {
            _context = ctx;
            _config = cfg;
            _uow = uow;
            _encoder = enc;
            _logger = logger;
        }

        #region Institution Request Validation

        /// <summary>
        /// Resolves only the code for an active Module. Permission decisions remain with the
        /// calling StaffMember authorization flow and the existing permission stored procedure.
        /// </summary>
        public async Task<string?> GetActiveModuleCodeAsync(int moduleId)
        {
            if (moduleId <= 0)
            {
                return null;
            }

            var module = await _uow.ModuleRepository.GetModuleByIdAsync(moduleId);
            return string.IsNullOrWhiteSpace(module?.ModuleCode)
                ? null
                : module.ModuleCode.Trim();
        }

        /// <summary>
        /// Resolves a master Module code without applying the master activity filter. Existing
        /// institution entitlements remain valid after a Host retires a master module; the caller's
        /// institution permission check remains responsible for access approval.
        /// </summary>
        public async Task<string?> GetModuleCodeAsync(int moduleId)
        {
            if (moduleId <= 0)
            {
                return null;
            }

            var module = await _uow.ModuleRepository.GetModuleByIdIncludingInactiveAsync(moduleId);
            return string.IsNullOrWhiteSpace(module?.ModuleCode)
                ? null
                : module.ModuleCode.Trim();
        }

        /// <summary>Resolves the canonical operation name used to bind HTTP actions to permissions.</summary>
        public async Task<string?> GetActiveOperationNameAsync(int operationId)
        {
            if (operationId <= 0)
            {
                return null;
            }

            var operation = await _uow.OperationRepository.GetOperationByIdAsync(operationId);
            return operation?.IsActive == true && !string.IsNullOrWhiteSpace(operation.OperationName)
                ? operation.OperationName.Trim()
                : null;
        }

        /// <summary>
        /// Validates the current authenticated institution request and resolves the trusted institution, staffMember, and role context.
        /// </summary>
        /// <returns>The validated institution request context.</returns>
        public async Task<CommonDecodedResult> ValidateInstitutionUserRequestAsync()
        {
            try
            {
                var token = RequestCommonHelper.ExtractBearerToken(_context.HttpContext);
                if (string.IsNullOrEmpty(token))
                    return new CommonDecodedResult { Success = false, ErrorMessage = "Token missing." };

                var claims = RequestCommonHelper.ValidateAndExtractClaims(token, _config);
                if (claims == null)
                    return new CommonDecodedResult { Success = false, ErrorMessage = "Token expired or invalid." };

                // Institution access tokens now carry an explicit principal type. Continue to accept
                // an empty value only for sessions issued before that claim was introduced.
                if (!string.IsNullOrWhiteSpace(claims.UserType) &&
                    !string.Equals(
                        claims.UserType,
                        AppConstants.InstitutionUserType,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return new CommonDecodedResult { Success = false, ErrorMessage = AppConstants.ErrorMessages.Unauthorized };
                }

                long loggedInId = await _uow.StoreProcedureRepository.ValidateActiveUserLoginOnlyAsync(claims.UserId);
                if (loggedInId < 1)
                    return new CommonDecodedResult { Success = false, ErrorMessage = "Inactive user." };

                var institutionId = RequestCommonHelper.DecodeUserAndInstitutionIds(claims.InstitutionId!,claims.InstitutionEncriptionKey!, _encoder );

                var decoded = (UserEmpId: loggedInId, InstitutionId: institutionId);

                if (decoded.UserEmpId != loggedInId)
                    return new CommonDecodedResult { Success = false, ErrorMessage = "User mismatch." };

                if (decoded.InstitutionId <= 0 )
                {
                    _logger.LogWarning("❌ Institution information missing .");
                    return new CommonDecodedResult { Success = false, ErrorMessage = "InstitutionId not correct" };
                }

                return new CommonDecodedResult
                {
                    Success = true,
                    LoggedInStaffMemberId = loggedInId,
                    UserStaffMemberId = decoded.UserEmpId,
                    InstitutionId = decoded.InstitutionId,
                    RoleId = SafeParser.TryParseInt(claims.RoleId),
                    RoleTypeId = SafeParser.TryParseInt(claims.RoleTypeId),
                    Claims = claims
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CommonRequestService Error");
                return new CommonDecodedResult { Success = false, ErrorMessage = "Internal validation error." };
            }
        }

        /// <summary>
        /// Evaluates staffMember-owned data access from the already validated request context.
        /// Permission to invoke the endpoint is handled by the existing stored procedure; this
        /// method separately protects which staffMember's records that permitted endpoint may read or modify.
        /// </summary>
        public async Task<bool> CanAccessStaffMemberDataAsync(
            CommonDecodedResult requestContext,
            long targetStaffMemberId,
            StaffMemberDataAccessRequirement requirement,
            CancellationToken cancellationToken = default)
        {
            if (!requestContext.Success ||
                requestContext.InstitutionId <= 0 ||
                requestContext.LoggedInStaffMemberId <= 0 ||
                targetStaffMemberId <= 0)
            {
                return false;
            }

            var accessCacheKey = $"StaffMemberDataAccess:{requestContext.InstitutionId}:{requestContext.LoggedInStaffMemberId}:{requestContext.RoleTypeId}:{targetStaffMemberId}:{(int)requirement}";
            if (_context.HttpContext?.Items.TryGetValue(accessCacheKey, out var cachedAccess) == true &&
                cachedAccess is bool allowed)
            {
                return allowed;
            }

            // Prove that the decoded target belongs to the requester's institution before evaluating
            // role visibility. This prevents a valid encrypted identifier from another institution
            // being used as an authorization target.
            var targetStaffMember = await _uow.StaffMembers.GetByIdAsync(
                targetStaffMemberId,
                requestContext.InstitutionId,
                track: false);
            if (targetStaffMember is null)
            {
                return CacheStaffMemberDataAccess(accessCacheKey, false);
            }

            if (requestContext.RoleTypeId == ConstantValues.RoleTypeAdmin ||
                requestContext.LoggedInStaffMemberId == targetStaffMemberId)
            {
                return CacheStaffMemberDataAccess(accessCacheKey, true);
            }

            if (requestContext.RoleTypeId != ConstantValues.RoleTypeManager)
            {
                // An StaffMember can use the directory only; personal and work-location
                // information of another staffMember remains private.
                return CacheStaffMemberDataAccess(
                    accessCacheKey,
                    requirement == StaffMemberDataAccessRequirement.DirectoryBasic);
            }

            // A Manager may see only current direct reports, never the entire institution. The
            // mapping lookup is set-based and indexed by institution/manager/staffMember in the database.
            if (requirement is not (StaffMemberDataAccessRequirement.DirectoryBasic or StaffMemberDataAccessRequirement.WorkLocation))
            {
                return CacheStaffMemberDataAccess(accessCacheKey, false);
            }

            var isDirectReport = await _uow.StaffMemberManagerMappingRepository.IsCurrentDirectReportAsync(
                requestContext.InstitutionId,
                requestContext.LoggedInStaffMemberId,
                targetStaffMemberId,
                cancellationToken);
            return CacheStaffMemberDataAccess(accessCacheKey, isDirectReport);
        }

        /// <summary>Caches a request-local authorization decision; it never crosses HTTP requests.</summary>
        private bool CacheStaffMemberDataAccess(string cacheKey, bool allowed)
        {
            if (_context.HttpContext is not null)
            {
                _context.HttpContext.Items[cacheKey] = allowed;
            }

            return allowed;
        }

        #endregion

        #region Unified Authentication Validation

        /// <summary>
        /// Validates the current authenticated Host or Institution request by reusing the established principal-specific validation path.
        /// </summary>
        /// <returns>The trusted authenticated principal context.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when the JWT, token purpose, or represented principal is invalid.</exception>
        public async Task<AuthenticatedRequestContext> ValidateAuthenticatedRequestAsync()
        {
            var principal = _context.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
            }

            // Host tokens carry the explicit principal-type claim established by GenerateHostToken.
            var userType = principal.FindFirst(AppConstants.UserTypeClaim)?.Value;
            if (string.Equals(userType, AppConstants.HostUserType, StringComparison.OrdinalIgnoreCase))
            {
                var hostUserId = await ValidateHostUserRequestAsync();
                return new AuthenticatedRequestContext
                {
                    UserType = LoginUserType.Host,
                    AuthenticatedUserId = hostUserId
                };
            }

            // Institution tokens carry their explicit principal type. Empty remains supported only for
            // access tokens issued before the Institution UserType claim was added.
            if (!string.IsNullOrWhiteSpace(userType) &&
                !string.Equals(userType, AppConstants.InstitutionUserType, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
            }

            var institutionValidation = await ValidateInstitutionUserRequestAsync();
            if (!institutionValidation.Success ||
                institutionValidation.Claims == null ||
                !string.Equals(
                    institutionValidation.Claims.TokenPurpose,
                    ConstantValues.Auth.ToString(),
                    StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
            }

            return new AuthenticatedRequestContext
            {
                UserType = LoginUserType.InstitutionStaffMember,
                AuthenticatedUserId = institutionValidation.LoggedInStaffMemberId,
                InstitutionId = institutionValidation.InstitutionId,
                RoleId = institutionValidation.RoleId
            };
        }

        #endregion

        #region HostUser Token Validation

        /// <summary>
        /// Validates that the current authenticated JWT represents an active Host user with an active Host role.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Thrown when the current token, claims, Host user, or Host role are invalid.</exception>
        public async Task<long> ValidateHostUserRequestAsync()
        {
            var hostContext = await ValidateHostUserContextAsync(
                enforceTokenRoleMatch: true,
                requireInstitutionEncryptionKey: false);

            return hostContext.HostUserId;
        }

        /// <summary>
        /// Validates the authenticated Host token and requires the current active Host role to be the verified Super Admin role.
        /// </summary>
        /// <returns>The trusted Host JWT and current database context.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when the principal is not a Host user or no longer holds the Super Admin role.</exception>
        public async Task<HostUserRequestContext> ValidateHostSuperAdminRequestAsync()
        {
            var hostContext = await ValidateHostUserContextAsync(
                enforceTokenRoleMatch: true,
                requireInstitutionEncryptionKey: false);

            if (!string.Equals(hostContext.UserType, AppConstants.HostUserType, StringComparison.Ordinal) ||
                hostContext.CurrentHostRoleId != AppConstants.SuperAdminHostRoleId)
            {
                throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
            }

            return hostContext;
        }

        /// <summary>
        /// Validates a Host request for runtime permission enforcement without treating a changed database role as a valid session.
        /// </summary>
        /// <returns>The trusted Host context used by Host runtime permission and Institution identifier protection logic.</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when the current Host token or current Host principal is invalid.</exception>
        public Task<HostUserRequestContext> ValidateHostUserPermissionRequestAsync() =>
            ValidateHostUserContextAsync(
                enforceTokenRoleMatch: false,
                requireInstitutionEncryptionKey: true);

        /// <summary>
        /// Resolves a current Host principal from signed claims while preserving stale-role detection for the database permission function.
        /// </summary>
        /// <param name="enforceTokenRoleMatch">Whether callers require immediate role-snapshot equality.</param>
        /// <param name="requireInstitutionEncryptionKey">Whether the Host-facing Institution identifier key claim is required.</param>
        /// <returns>The trusted Host context.</returns>
        private async Task<HostUserRequestContext> ValidateHostUserContextAsync(
            bool enforceTokenRoleMatch,
            bool requireInstitutionEncryptionKey)
        {
            const string unauthorizedMessage = "A valid Host user token is required.";

            var principal = _context.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                throw new UnauthorizedAccessException(unauthorizedMessage);
            }

            var userType = principal.FindFirst(AppConstants.UserTypeClaim)?.Value;
            if (!string.Equals(userType, AppConstants.HostUserType, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException(unauthorizedMessage);
            }

            var tokenPurpose = principal.FindFirst("TokenPurpose")?.Value;
            if (!string.Equals(tokenPurpose, AppConstants.AccessTokenPurpose, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException(unauthorizedMessage);
            }

            var hostUserIdValue = principal.FindFirst(AppConstants.HostUserIdClaim)?.Value;
            var hostRoleIdValue = principal.FindFirst(AppConstants.HostRoleIdClaim)?.Value;
            var loginId = principal.FindFirst(AppConstants.LoginIdClaim)?.Value;
            var institutionEncryptionKey = principal.FindFirst("InstitutionEncriptionKey")?.Value;

            if (!long.TryParse(hostUserIdValue, out var hostUserId) ||
                !long.TryParse(hostRoleIdValue, out var hostRoleId) ||
                hostUserId <= 0 ||
                hostRoleId <= 0 ||
                string.IsNullOrWhiteSpace(loginId) ||
                (requireInstitutionEncryptionKey && string.IsNullOrWhiteSpace(institutionEncryptionKey)))
            {
                throw new UnauthorizedAccessException(unauthorizedMessage);
            }

            var hostUser = await _uow.HostUserRepository.GetByIdAsync(hostUserId);
            if (hostUser == null ||
                !hostUser.IsActive ||
                hostUser.IsSoftDeleted ||
                (enforceTokenRoleMatch && hostUser.HostRoleId != hostRoleId) ||
                !string.Equals(hostUser.LoginId, loginId, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException(unauthorizedMessage);
            }

            var hostRole = await _uow.HostRoleRepository.GetByIdAsync(hostUser.HostRoleId);
            if (hostRole == null || !hostRole.IsActive || hostRole.IsSoftDeleted)
            {
                throw new UnauthorizedAccessException(unauthorizedMessage);
            }

            // Permission checks intentionally defer role-snapshot comparison to the database function so a
            // changed Host role returns the explicit stale-context result instead of silently authorizing.
            return new HostUserRequestContext
            {
                HostUserId = hostUser.Id,
                TokenHostRoleId = hostRoleId,
                CurrentHostRoleId = hostUser.HostRoleId,
                UserType = userType,
                InstitutionEncryptionKey = requireInstitutionEncryptionKey
                    ? EncryptionSanitizer.SuperSanitize(institutionEncryptionKey)
                    : string.Empty
            };
        }

        #endregion
    
    }

}
