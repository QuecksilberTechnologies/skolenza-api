// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Purpose     : Opt-in token-only refresh; retain the legacy endpoint until UI acceptance.
// Reference   : docs/AUTH_REFRESH_TOKEN_V2.md
// ============================================================================

using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers.EncryptionHelper;
using skolenza.application.Common.Helpers.Hash;
using skolenza.application.Constants;
using skolenza.application.DTOs.Institution;
using skolenza.application.DTOS.Host;
using skolenza.application.DTOS.Token;
using skolenza.application.DTOS.Token.ems.application.DTOs.UserLogin;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.ITokenService;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.UserLoginAndDashboardCmd.Handlers
{
    #region Command

    /// <summary>Requests token-only refresh for an existing Host or Institution session.</summary>
    public class RefreshTokenV2Command : IRequest<ApiResponse<RefreshTokenV2ResponseDTO>>
    {
        public RefreshTokenRequestDTO DTO { get; }

        public RefreshTokenV2Command(RefreshTokenRequestDTO request)
        {
            DTO = request;
        }
    }

    #endregion

    /// <summary>
    /// Validates current session eligibility without fetching presentation data or menu permissions.
    /// Uses the existing token service, repositories and centralized exception pipeline.
    /// </summary>
    public class RefreshTokenV2CommandHandler
        : IRequestHandler<RefreshTokenV2Command, ApiResponse<RefreshTokenV2ResponseDTO>>
    {
        #region Dependencies

        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IIdEncoderService _idEncoderService;
        private readonly ILogger<RefreshTokenV2CommandHandler> _logger;

        public RefreshTokenV2CommandHandler(
            IUnitOfWork unitOfWork,
            ITokenService tokenService,
            IRefreshTokenRepository refreshTokenRepository,
            IIdEncoderService idEncoderService,
            ILogger<RefreshTokenV2CommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _refreshTokenRepository = refreshTokenRepository;
            _idEncoderService = idEncoderService;
            _logger = logger;
        }

        #endregion

        #region Refresh

        public async Task<ApiResponse<RefreshTokenV2ResponseDTO>> Handle(
            RefreshTokenV2Command request,
            CancellationToken cancellationToken)
        {
            if (request?.DTO == null || string.IsNullOrWhiteSpace(request.DTO.RefreshToken))
            {
                throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
            }

            if (request.DTO.IpAddress?.Length > 50)
            {
                throw new ValidationErrorException("IpAddress must not exceed 50 characters.");
            }

            var oldToken = await _refreshTokenRepository.GetByHashedTokenAsync(
                HashHelper.Sha256(request.DTO.RefreshToken));
            if (oldToken == null || oldToken.IsRevoked == true || oldToken.ExpiryDate <= DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Invalid, expired or revoked refresh token.");
            }

            var validOwner = oldToken.UserType switch
            {
                (short)LoginUserType.Host => oldToken.HostUserId.HasValue && !oldToken.LoginCredentialId.HasValue,
                (short)LoginUserType.InstitutionStaffMember => oldToken.LoginCredentialId.HasValue && !oldToken.HostUserId.HasValue,
                _ => false
            };
            if (!validOwner)
            {
                throw new UnauthorizedAccessException("Invalid refresh token owner.");
            }

            string accessToken;
            if (oldToken.UserType == (short)LoginUserType.Host)
            {
                accessToken = await GenerateHostAccessTokenAsync(oldToken);
            }
            else
            {
                // Preserve legacy business-failure envelopes for inactive staffMembers/subscriptions.
                var institutionResult = await GenerateInstitutionAccessTokenAsync(oldToken, cancellationToken);
                if (!institutionResult.IsSucceeded)
                {
                    return new ApiResponse<RefreshTokenV2ResponseDTO>
                    {
                        IsSucceeded = false,
                        Message = institutionResult.Message
                    };
                }
                accessToken = institutionResult.Data!;
            }

            var expiry = _tokenService.GetExpiryFromToken(accessToken);
            if (string.IsNullOrWhiteSpace(accessToken) || !expiry.HasValue)
            {
                throw new InvalidOperationException("The access token could not be issued.");
            }

            var response = await RotateAsync(oldToken, accessToken, expiry.Value, request.DTO.IpAddress, cancellationToken);
            return ApiResponse<RefreshTokenV2ResponseDTO>.Success(response, "Token refreshed successfully.");
        }

        #endregion

        #region Current Owner Validation

        private async Task<string> GenerateHostAccessTokenAsync(RefreshToken oldToken)
        {
            var hostUser = await _unitOfWork.HostUserRepository.GetByIdAsync(oldToken.HostUserId!.Value);
            if (hostUser == null || !hostUser.IsActive || hostUser.IsSoftDeleted ||
                !string.Equals(hostUser.LoginId, oldToken.LoginId, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
            }

            var role = await _unitOfWork.HostRoleRepository.GetByIdAsync(hostUser.HostRoleId);
            if (role == null || !role.IsActive || role.IsSoftDeleted)
            {
                throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
            }

            return await _tokenService.GenerateHostToken(new HostTokenInfoDTO
            {
                HostUserId = hostUser.Id,
                HostRoleId = hostUser.HostRoleId,
                LoginId = hostUser.LoginId,
                Name = hostUser.Name,
                Email = hostUser.Email
            });
        }

        private async Task<ApiResponse<string>> GenerateInstitutionAccessTokenAsync(
            RefreshToken oldToken,
            CancellationToken cancellationToken)
        {
            var credential = await _unitOfWork.UserLoginRepository.GetActiveByIdAsync(oldToken.LoginCredentialId!.Value);
            if (credential == null || string.IsNullOrWhiteSpace(credential.LoginId) ||
                !string.Equals(credential.LoginId, oldToken.LoginId, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException("Invalid refresh token owner.");
            }

            var staffMemberId = await _unitOfWork.StoreProcedureRepository.ValidateActiveUserLoginOnlyAsync(credential.LoginId);
            if (staffMemberId < 1)
            {
                throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
            }

            var staffMember = await _unitOfWork.StaffMembers.GetSingleRecordAsync(staffMemberId, true);
            if (staffMember == null)
            {
                return new ApiResponse<string>
                {
                    IsSucceeded = false,
                    Message = "StaffMember not active. Please contact admin."
                };
            }

            var subscription = await _unitOfWork.InstitutionSubscriptionRepository.GetValidateInstitutionPlan(
                new InstitutionSubscriptionPlanRequestDTO { InstitutionId = staffMember.InstitutionId });
            if (subscription == null || !subscription.SubscriptionEndDate.HasValue ||
                subscription.SubscriptionEndDate.Value.Date < DateTime.Today)
            {
                return new ApiResponse<string>
                {
                    IsSucceeded = false,
                    Message = "Your subscription has expired. Please contact admin to renew the plan."
                };
            }

            var roles = await _unitOfWork.UserRoleRepository.GetStaffMemberRolesWithDetailsByIdAsync(
                staffMemberId, staffMember.InstitutionId, cancellationToken);
            var primaryRole = roles?.FirstOrDefault(x => x.IsPrimaryRole == true && x.IsActive);
            if (primaryRole?.Role == null || !primaryRole.RoleId.HasValue)
            {
                throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);
            }

            var key = await _unitOfWork.InstitutionEncryptionKeyRepository.GetActiveKeyByInstitutionIdAsync(staffMember.InstitutionId);
            if (key == null || string.IsNullOrWhiteSpace(key.EncryptionKey))
            {
                throw new InvalidOperationException("Institution encryption key not found or invalid.");
            }

            var finalKey = EncryptionSanitizer.SuperSanitize(key.EncryptionKey);
            // Keep existing JWT claims and signing policy; only the response/presentation reads are reduced.
            var token = await _tokenService.GenerateInstitutionToken(new GetTokenInfoDTO
            {
                InstitutionEncriptionKey = finalKey,
                InstitutionId = _idEncoderService.EncodeId_long(staffMember.InstitutionId, finalKey),
                StaffMemberId = _idEncoderService.EncodeId_long(staffMemberId, finalKey).Trim(),
                UserId = credential.LoginId,
                RoleId = primaryRole.RoleId.Value.ToString(),
                RoleTypeId = primaryRole.Role.RoleType.ToString(),
                RoleTypeName = primaryRole.Role.RoleName ?? string.Empty,
                StaffEmploymentTypeId = staffMember.StaffEmploymentTypeId.ToString(),
                GenderId = staffMember.GenderId.ToString(),
                GenderName = staffMember.GenderName,
                Email = credential.LoginId,
                FullName = ((staffMember.FirstName ?? "") + "-" + (staffMember.LastName ?? "")).Trim('-'),
                Expiry = DateTime.UtcNow.AddMinutes(15),
                TokenPurpose = ConstantValues.Auth.ToString()
            });
            return ApiResponse<string>.Success(token, "Token generated.");
        }

        #endregion

        #region Existing Refresh Rotation Policy

        private async Task<RefreshTokenV2ResponseDTO> RotateAsync(
            RefreshToken oldToken,
            string accessToken,
            DateTime accessExpiry,
            string? ipAddress,
            CancellationToken cancellationToken)
        {
            var raw = await _tokenService.GenerateRefreshToken();
            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new InvalidOperationException("The refresh token could not be issued.");
            }
            var hash = HashHelper.Sha256(raw);
            var createdAt = DateTime.UtcNow;
            var refreshExpiry = createdAt.AddDays(7);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var claimed = await _refreshTokenRepository.TryClaimForRotationAsync(
                    oldToken.Id, hash, ipAddress, DateTime.UtcNow, cancellationToken);
                if (!claimed)
                {
                    throw new UnauthorizedAccessException("Refresh token expired or already consumed.");
                }
                var inserted = await _refreshTokenRepository.InsertAsync(new RefreshToken
                {
                    LoginId = oldToken.LoginId,
                    UserType = oldToken.UserType,
                    LoginCredentialId = oldToken.LoginCredentialId,
                    HostUserId = oldToken.HostUserId,
                    Token = hash,
                    CreatedAt = createdAt,
                    ExpiryDate = refreshExpiry,
                    CreatedByIp = ipAddress,
                    IsRevoked = false
                });
                if (!inserted)
                {
                    throw new InvalidOperationException("The refresh token could not be persisted.");
                }
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            } 
            catch (Exception exception)
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
                _logger.LogError(exception, "Token-only refresh rotation failed.");
                throw;
            }

            return new RefreshTokenV2ResponseDTO
            {
                Token = accessToken,
                RefreshToken = raw,
                TokenExpiry = accessExpiry,
                RefreshTokenExpiresAtUtc = refreshExpiry
            };
        }

        #endregion
    }
}
