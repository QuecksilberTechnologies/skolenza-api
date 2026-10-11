// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Refreshes institution and Host tokens through centralized error handling.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers.EncryptionHelper;
using skolenza.application.Common.Helpers.Hash;
using skolenza.application.Constants;
using skolenza.application.DTOs.Staff;
using skolenza.application.DTOs.Role;
using skolenza.application.DTOs.RoleModulePermission;
using skolenza.application.DTOs.Institution;
using skolenza.application.DTOs.UserLogin;
using skolenza.application.DTOS.Staff.BaseStaffMember;
using skolenza.application.DTOS.Host;
using skolenza.application.DTOS.Token;
using skolenza.application.DTOS.Token.ems.application.DTOs.UserLogin;
using skolenza.application.DTOS.UserRoles;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Interfaces.ITokenService;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Rotates common refresh tokens for Host users and Institution StaffMembers.
// ============================================================================

namespace skolenza.application.Features.UserLoginAndDashboardCmd.Handlers
{

    #region Command

    /// <summary>
    /// Represents a request to rotate a Host or Institution refresh token.
    /// </summary>
    public class RefreshTokenCommand : IRequest<ApiResponse<LoginResponseDTO>>
    {
        public RefreshTokenRequestDTO DTO { get; }

        public RefreshTokenCommand(RefreshTokenRequestDTO request)
        {
            DTO = request;
        }
    }

    /// <summary>
    /// Handles owner-specific validation and access-token generation before shared refresh-token rotation.
    /// </summary>
        #endregion

    #region Handler

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ApiResponse<LoginResponseDTO>>
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ILogger<RefreshTokenCommandHandler> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEncryptionService _encryptionService;
        private readonly IIdEncoderService _idEncoderService;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IFileStorageService _fileStorageService;

        public RefreshTokenCommandHandler(
            IMapper mapper,
            IUnitOfWork unitOfWork,
            ITokenService tokenService,
            IRefreshTokenRepository refreshTokenRepository,
            ILogger<RefreshTokenCommandHandler> logger,
            IConfiguration configuration,
            IEncryptionService encryptionService,
            IIdEncoderService idEncoderService,
            ICommonRequestService commonRequestService, IFileStorageService fileStorageService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _refreshTokenRepository = refreshTokenRepository;
            _logger = logger;
            _configuration = configuration;
            _encryptionService = encryptionService;
            _idEncoderService = idEncoderService;
            _commonRequestService = commonRequestService;
            _fileStorageService = fileStorageService;
        }

        public async Task<ApiResponse<LoginResponseDTO>> Handle(
            RefreshTokenCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                if (request?.DTO == null || string.IsNullOrWhiteSpace(request.DTO.RefreshToken))
                {
                    throw new skolenza.application.Exceptions.ValidationErrorException(
                        skolenza.application.Constants.AppConstants.ErrorMessages.InvalidRequest);
                }

                // =====================================================
                // STEP 1: Validate incoming refresh token
                // =====================================================
                var incomingHashedToken = HashHelper.Sha256(request.DTO.RefreshToken);

                var oldToken = await _refreshTokenRepository.GetByHashedTokenAsync(incomingHashedToken);

                if (oldToken == null)
                {
                    _logger.LogWarning("Invalid refresh token attempt. IP={IP}", request.DTO.IpAddress);
                    throw new UnauthorizedAccessException("Invalid refresh token.");
                }

                if (oldToken.IsRevoked == true)
                {
                    _logger.LogWarning("Refresh token reuse detected. LoginId={LoginId}, IP={IP}",
                        oldToken.LoginId, request.DTO.IpAddress);
                    throw new UnauthorizedAccessException("Refresh token revoked.");
                }

                if (oldToken.ExpiryDate < DateTime.UtcNow)
                {
                    _logger.LogInformation("Expired refresh token used. LoginId={LoginId}, IP={IP}",
                        oldToken.LoginId, request.DTO.IpAddress);
                    throw new UnauthorizedAccessException("Refresh token expired.");
                }

                if (!HasValidRefreshTokenOwner(oldToken))
                {
                    _logger.LogWarning(
                        "Refresh token owner invariant failed. TokenId={TokenId}, UserType={UserType}, LoginCredentialId={LoginCredentialId}, HostUserId={HostUserId}",
                        oldToken.Id,
                        oldToken.UserType,
                        oldToken.LoginCredentialId,
                        oldToken.HostUserId);
                    throw new UnauthorizedAccessException("Invalid refresh token owner.");
                }

                if (oldToken.UserType == (short)LoginUserType.Host)
                {
                    return await CreateHostRefreshResponseAsync(
                        oldToken,
                        request.DTO.IpAddress,
                        cancellationToken);
                }

                if (oldToken.UserType != (short)LoginUserType.InstitutionStaffMember)
                {
                    _logger.LogWarning(
                        "Refresh token has an unsupported UserType. TokenId={TokenId}, UserType={UserType}",
                        oldToken.Id,
                        oldToken.UserType);
                    throw new UnauthorizedAccessException("Invalid refresh token owner.");
                }
                // =====================================================
                // STEP 2: Resolve the Institution owner from the immutable foreign key.
                // =====================================================
                var institutionLoginCredential = await _unitOfWork.UserLoginRepository
                    .GetActiveByIdAsync(oldToken.LoginCredentialId!.Value);

                if (institutionLoginCredential == null ||
                    string.IsNullOrWhiteSpace(institutionLoginCredential.LoginId) ||
                    !string.Equals(institutionLoginCredential.LoginId, oldToken.LoginId, StringComparison.Ordinal))
                {
                    _logger.LogWarning(
                        "Refresh token Institution owner mismatch. TokenId={TokenId}, LoginCredentialId={LoginCredentialId}",
                        oldToken.Id,
                        oldToken.LoginCredentialId);
                    throw new UnauthorizedAccessException("Invalid refresh token owner.");
                }

                string loginId = institutionLoginCredential.LoginId;

                // =====================================================
                // STEP 3: Validate active user fresh from DB
                // =====================================================
                long empId = await _unitOfWork.StoreProcedureRepository.ValidateActiveUserLoginOnlyAsync(loginId);

                _logger.LogInformation("Refresh validation for LoginId {LoginId}: StaffMemberId = {empId}", loginId, empId);

                if (empId < 1)
                {
                    _logger.LogWarning("User validation failed during refresh for LoginId: {LoginId}", loginId);
                    throw new UnauthorizedAccessException(
                        skolenza.application.Constants.AppConstants.ErrorMessages.Unauthorized);
                }

                // =====================================================
                // STEP 4: Fresh staffMember info
                // =====================================================
                GetMinimalStaffMemberResponseDTO empMinimalResponse =
                    await _unitOfWork.StaffMembers.GetSingleRecordAsync(empId, true);

                InstitutionSubscriptionPlanRequestDTO dto = new InstitutionSubscriptionPlanRequestDTO();

                if (empMinimalResponse == null)
                {
                    _logger.LogWarning("StaffMember may not active or deleted during refresh. LoginId: {LoginId}", loginId);

                    return new ApiResponse<LoginResponseDTO>
                    {
                        IsSucceeded = false,
                        Message = "StaffMember not active. Please contact admin."
                    };
                }

                // =====================================================
                // STEP 5: Fresh subscription validation
                // =====================================================
                dto.InstitutionId = empMinimalResponse.InstitutionId;

                var subscriptionInfo = await _unitOfWork.InstitutionSubscriptionRepository.GetValidateInstitutionPlan(dto);

                if (subscriptionInfo == null ||
                    !subscriptionInfo.SubscriptionEndDate.HasValue ||
                    subscriptionInfo.SubscriptionEndDate.Value.Date < DateTime.Today)
                {
                    _logger.LogWarning("Subscription expired or missing for institution {InstitutionId} during refresh", dto.InstitutionId);

                    return new ApiResponse<LoginResponseDTO>
                    {
                        IsSucceeded = false,
                        Message = "Your subscription has expired. Please contact admin to renew the plan."
                    };
                }

                // =====================================================
                // STEP 6: Fresh roles
                // =====================================================
                var userRoles = await _unitOfWork.UserRoleRepository
                    .GetStaffMemberRolesWithDetailsByIdAsync(empId, empMinimalResponse.InstitutionId);

                if (userRoles == null || userRoles.Count == 0)
                {
                    _logger.LogWarning("No roles found during refresh for LoginId: {LoginId}", loginId);
                    throw new skolenza.application.Exceptions.NotFoundException(
                        skolenza.application.Constants.AppConstants.ErrorMessages.ResourceNotFound);
                }

                var roleInfo = userRoles.FirstOrDefault(x => x.IsPrimaryRole == true);

                if (roleInfo == null || roleInfo.Role == null)
                {
                    _logger.LogWarning("Primary role missing during refresh for LoginId: {LoginId}", loginId);
                    throw new skolenza.application.Exceptions.NotFoundException(
                        skolenza.application.Constants.AppConstants.ErrorMessages.ResourceNotFound);
                }

                List<UserRoleDTO>? userRoleDTOs = null;
                string? allRoleIdsCsv = null;
                UserRoleDTO? primaryRole = null;

                allRoleIdsCsv = userRoles
                    .Where(r => r.RoleId != null)
                    .Select(r => r.RoleId.ToString())
                    .Aggregate((a, b) => $"{a},{b}");

                if (!string.IsNullOrEmpty(allRoleIdsCsv))
                    _logger.LogInformation("Fetched Role IDs during refresh for LoginId {LoginId}: {Roles}", loginId, allRoleIdsCsv);
                else
                    _logger.LogWarning("No roles CSV formed during refresh for LoginId {LoginId}", loginId);

                userRoleDTOs = _mapper.Map<List<UserRoleDTO>>(userRoles);

                primaryRole = userRoleDTOs.FirstOrDefault(ur => ur.IsPrimaryRole && ur.IsActive);

                if (primaryRole != null)
                    userRoleDTOs.Remove(primaryRole);

                // =====================================================
                // STEP 7: Fresh common items
                // =====================================================
                var parent = await _unitOfWork.ModuleRepository.GetCommonMenuParentAsync();
                if (parent == null)
                {
                    throw new skolenza.application.Exceptions.NotFoundException(
                        skolenza.application.Constants.AppConstants.ErrorMessages.ResourceNotFound);
                }

                List<ModuleDTO> CommonItems = await _unitOfWork.ModuleRepository.GetCommonMenuTreeAsync(parent.Id);

                // =====================================================
                // STEP 8: Fresh operational menus / permissions
                // =====================================================
                var requestDto = new GetActiveRoleModuleOperationsRequestDTO
                {
                    RoleIds = allRoleIdsCsv,
                    InstitutionId = empMinimalResponse.InstitutionId
                };

                var rolePermissions = await _unitOfWork.StoreProcedureRepository.GetActiveRoleModuleOperationsAsync(requestDto);

                var grouped = rolePermissions
                    .GroupBy(m => new { m.MainModuleId, m.MainModuleName })
                    .Select(main => new MainModuleDto
                    {
                        MainModuleId = main.Key.MainModuleId,
                        MainModuleName = main.Key.MainModuleName,

                        SubModules = main
                            .GroupBy(sm => new { sm.ParentModuleId, sm.SubModuleName })
                            .Select(sub => new SubModuleDto
                            {
                                SubModuleId = sub.Key.ParentModuleId,
                                SubModuleName = sub.Key.SubModuleName,

                                Modules = sub
                                    .GroupBy(mod => new
                                    {
                                        mod.ModuleId,
                                        mod.ModuleName,
                                        mod.DisplayName,
                                        mod.ImageIconWeb,
                                        mod.ImageIconMobile,
                                        mod.URLPath,
                                        mod.DataViewStructureId,
                                        mod.DisplayOn
                                    })
                                    .Select(mod => new ModuleDto
                                    {
                                        ModuleId = mod.Key.ModuleId,
                                        ModuleName = mod.Key.ModuleName,
                                        DisplayName = mod.Key.DisplayName,
                                        ImageIconWeb = mod.Key.ImageIconWeb,
                                        ImageIconMobile = mod.Key.ImageIconMobile,
                                        SubModuleURL = mod.Key.URLPath,
                                        DataViewStructureId = mod.Key.DataViewStructureId,
                                        DisplayOn = mod.Key.DisplayOn,

                                        Operations = mod
                                            .Select(op => new OperationDto
                                            {
                                                OperationId = op.OperationId,
                                                OperationName = op.OperationName
                                            }).ToList()
                                    }).ToList()
                            }).ToList()
                    }).ToList();

                var InstitutionEnabledModulesWithOperationData =
                    await _unitOfWork.UserRolesPermissionOnModuleRepository.GetAllInstitutionEnabledModulesWithOperationsAsync(empMinimalResponse.InstitutionId);

                // =====================================================
                // STEP 9: Fresh encryption key
                // =====================================================
                long tempInstitutionId = empMinimalResponse?.InstitutionId ?? 0;
                long tempStaffMemberId = empMinimalResponse?.Id ?? 0;

                var institutionEncryptionKey = await _unitOfWork.InstitutionEncryptionKeyRepository
                    .GetActiveKeyByInstitutionIdAsync(tempInstitutionId);

                if (institutionEncryptionKey == null || string.IsNullOrEmpty(institutionEncryptionKey.EncryptionKey))
                {
                    throw new Exception("Institution encryption key not found or invalid.");
                }

                string finalKey = EncryptionSanitizer.SuperSanitize(institutionEncryptionKey.EncryptionKey);
                string encriptedStaffMemberId = _idEncoderService.EncodeId_long(tempStaffMemberId, finalKey);
                string encriptedInstitutionId = _idEncoderService.EncodeId_long(tempInstitutionId, finalKey);

                // =====================================================
                // STEP 10: Fresh profile image + staffMember response
                // =====================================================
                                string? profileKey = await _unitOfWork.StaffMembers.ProfileImage(empId);

                string? ProfileImagePath = null;

                if (!string.IsNullOrWhiteSpace(profileKey))
                {
                    ProfileImagePath = _fileStorageService.GetFileUrl(profileKey);
                }
                bool? isPasswordChange = null;

                var user = await _unitOfWork.UserLoginRepository.AuthenticateUser(loginId);
                if (user != null)
                {
                    isPasswordChange = user.IsPasswordChangeRequired;
                }

                GetStaffMemberLoginInfoResponseDTO? staffMemberInfo =
                    _mapper.Map<GetStaffMemberLoginInfoResponseDTO>(empMinimalResponse);

                staffMemberInfo.IsPasswordChangeRequired = isPasswordChange;
                staffMemberInfo.UserPrimaryRole = primaryRole;
                staffMemberInfo.RoleTypeId = roleInfo.Role.RoleType;
                staffMemberInfo.RoleTypeName = roleInfo.Role.RoleName;
                staffMemberInfo.StaffMemberId = encriptedStaffMemberId.Trim();
                staffMemberInfo.UserSecondryRoles = userRoleDTOs;
                staffMemberInfo.ProfileImageLink = ProfileImagePath;
                staffMemberInfo.IsOnboard = user.IsOnboard ;

                var institution = await _unitOfWork.InstitutionRepository.GetByIdAsync(dto.InstitutionId, true);

                GetRoleRequestDTO getRoleRequestDTO = new GetRoleRequestDTO
                {
                    Id = staffMemberInfo.UserPrimaryRole?.RoleId ?? 0,
                    RoleType = roleInfo.Role.RoleType,
                    IsActive = true
                };

                var roleTypeList = await _unitOfWork.RoleRepository.GetAsync(
                    dto.InstitutionId,
                    getRoleRequestDTO);
                var roleType = roleTypeList.Data.FirstOrDefault(r =>
                    r.Id == staffMemberInfo.UserPrimaryRole.RoleId && r.IsActive == true);

                if (dto.InstitutionId == 0)
                {
                    return new ApiResponse<LoginResponseDTO>
                    {
                        IsSucceeded = false,
                        Message = "Institution information is invalid. Please contact admin."
                    };
                }

                staffMemberInfo.InstitutionName = institution?.CompanyName ?? string.Empty;

                // =====================================================
                // STEP 11: Fresh token info DTO
                // =====================================================
                GetTokenInfoDTO getTokenInfoDTO = new GetTokenInfoDTO()
                {
                    InstitutionEncriptionKey = finalKey,
                    InstitutionId = encriptedInstitutionId,
                    UserId = loginId,
                    StaffMemberId = encriptedStaffMemberId.Trim(),
                    RoleId = staffMemberInfo.UserPrimaryRole.RoleId.ToString(),
                    RoleTypeId = staffMemberInfo.RoleTypeId.ToString() ?? "0",
                    RoleTypeName = staffMemberInfo.RoleTypeName ?? "",
                    StaffEmploymentTypeId = staffMemberInfo.StaffEmploymentTypeId.ToString() ?? "0",
                    GenderId = empMinimalResponse.GenderId.ToString(),
                    GenderName = empMinimalResponse.GenderName,
                    Email = loginId,
                    FullName = ((empMinimalResponse.FirstName ?? "") + "-" + (empMinimalResponse.LastName ?? "")).Trim('-'),
                    Expiry = DateTime.UtcNow.AddMinutes(15),
                    TokenPurpose = ConstantValues.Auth.ToString(),
                };

                // =====================================================
                // STEP 12: Generate new token pair
                // =====================================================
                var token = await _tokenService.GenerateInstitutionToken(getTokenInfoDTO);

                var newRefreshToken = await RotateRefreshTokenAsync(
                    oldToken,
                    request.DTO.IpAddress,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(newRefreshToken))
                {
                    throw new InvalidOperationException("The refresh token could not be issued.");
                }

                // =====================================================
                // STEP 14: Full login-style response
                // =====================================================
                var loginResponse = new LoginResponseDTO
                {
                    Token = token,
                    RefreshToken = newRefreshToken,
                    Success = ConstantValues.isSucceeded,
                    StaffMemberInfo = staffMemberInfo,
                    CommonItems = CommonItems,
                    OperationalMenus = grouped,
                    Allroles = allRoleIdsCsv?.Trim()
                };

                return ApiResponse<LoginResponseDTO>.Success(loginResponse, "Token refreshed successfully.");
            }
            catch (Exception ex)
            {
                try
                {
                    await _unitOfWork.RollbackTransactionAsync();
                }
                catch
                {
                }

                _logger.LogError(ex, "Error while refreshing token.");
                throw;
            }
        }

        #region Refresh Token Owner Validation

        /// <summary>
        /// Validates the mutually exclusive refresh-token owner foreign keys for the declared user type.
        /// </summary>
        /// <param name="token">The refresh token retrieved by its submitted-token hash.</param>
        /// <returns><see langword="true"/> when the token has exactly one matching owner foreign key; otherwise, <see langword="false"/>.</returns>
        private static bool HasValidRefreshTokenOwner(RefreshToken token)
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

        #region Host Owner Validation

        /// <summary>
        /// Validates a Host refresh-token owner and creates a Host response before using the shared rotation block.
        /// </summary>
        /// <param name="oldToken">The common refresh-token row whose user type is Host.</param>
        /// <param name="requestIpAddress">The client IP address associated with the request.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>A refreshed Host login response.</returns>
        private async Task<ApiResponse<LoginResponseDTO>> CreateHostRefreshResponseAsync(
            RefreshToken oldToken,
            string? requestIpAddress,
            CancellationToken cancellationToken)
        {
            var hostUser = await _unitOfWork.HostUserRepository.GetByIdAsync(oldToken.HostUserId!.Value);
            if (hostUser == null ||
                !hostUser.IsActive ||
                hostUser.IsSoftDeleted ||
                !string.Equals(hostUser.LoginId, oldToken.LoginId, StringComparison.Ordinal))
            {
                throw new UnauthorizedAccessException(
                    skolenza.application.Constants.AppConstants.ErrorMessages.Unauthorized);
            }

            var hostRole = await _unitOfWork.HostRoleRepository.GetByIdAsync(hostUser.HostRoleId);
            if (hostRole == null || !hostRole.IsActive || hostRole.IsSoftDeleted)
            {
                throw new UnauthorizedAccessException(
                    skolenza.application.Constants.AppConstants.ErrorMessages.Unauthorized);
            }

            var permissions = await _unitOfWork.HostRolePermissionRepository
                .GetHostUserPermissionsAsync(hostUser.HostRoleId, cancellationToken);

            var accessToken = await _tokenService.GenerateHostToken(new HostTokenInfoDTO
            {
                HostUserId = hostUser.Id,
                HostRoleId = hostUser.HostRoleId,
                LoginId = hostUser.LoginId,
                Name = hostUser.Name,
                Email = hostUser.Email
            });

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new InvalidOperationException("The Host access token could not be issued.");
            }

            var refreshToken = await RotateRefreshTokenAsync(
                oldToken,
                requestIpAddress,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new InvalidOperationException("The refresh token could not be issued.");
            }

            return ApiResponse<LoginResponseDTO>.Success(
                CreateHostLoginResponse(hostUser, hostRole, permissions, accessToken, refreshToken),
                "Token refreshed successfully.");
        }

        #endregion

        #region Common Refresh Token Rotation

        /// <summary>
        /// Performs the single shared refresh-token rotation algorithm after the owner and access token have been validated.
        /// </summary>
        /// <param name="oldToken">The common refresh-token row to replace.</param>
        /// <param name="requestIpAddress">The client IP address associated with token revocation and creation.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The new opaque refresh token, or <see langword="null"/> when the replacement cannot be persisted.</returns>
        private async Task<string?> RotateRefreshTokenAsync(
            RefreshToken oldToken,
            string? requestIpAddress,
            CancellationToken cancellationToken)
        {
            var refreshToken = await _tokenService.GenerateRefreshToken();
            var hashedRefreshToken = HashHelper.Sha256(refreshToken);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            await _refreshTokenRepository.UpdateReplacedByTokenAsync(oldToken.Id, hashedRefreshToken);
            await _refreshTokenRepository.RevokeAsync(oldToken.Id, requestIpAddress);

            var isInserted = await _refreshTokenRepository.InsertAsync(new RefreshToken
            {
                LoginId = oldToken.LoginId,
                UserType = oldToken.UserType,
                LoginCredentialId = oldToken.LoginCredentialId,
                HostUserId = oldToken.HostUserId,
                Token = hashedRefreshToken,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = requestIpAddress,
                IsRevoked = false
            });

            if (!isInserted)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return null;
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return refreshToken;
        }

        #endregion

        #region Host Login Response

        /// <summary>
        /// Creates the additive login response returned when a Host token is issued or refreshed.
        /// </summary>
        /// <param name="hostUser">The active Host user.</param>
        /// <param name="hostRole">The active Host role.</param>
        /// <param name="permissions">The effective Host permissions.</param>
        /// <param name="accessToken">The signed Host access token.</param>
        /// <param name="refreshToken">The opaque Host refresh token returned to the client.</param>
        /// <returns>The Host login response.</returns>
        private LoginResponseDTO CreateHostLoginResponse(
            HostUser hostUser,
            HostRole hostRole,
            List<HostUserPermissionResponseDTO> permissions,
            string accessToken,
            string refreshToken)
        {
            return new LoginResponseDTO
            {
                Success = ConstantValues.isSucceeded,
                Token = accessToken,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenExpiry = _tokenService.GetExpiryFromToken(accessToken),
                UserType = AppConstants.HostUserType,
                HostUser = new GetHostUserResponseDTO
                {
                    Id = hostUser.Id,
                    HostRoleId = hostUser.HostRoleId,
                    HostRoleName = hostRole.Name,
                    Name = hostUser.Name,
                    LoginId = hostUser.LoginId,
                    Email = hostUser.Email,
                    MobileNumber = hostUser.MobileNumber,
                    IsActive = hostUser.IsActive,
                    AddedDateTime = hostUser.AddedDateTime,
                    UpdatedDateTime = hostUser.UpdatedDateTime
                },
                HostRole = new GetHostRoleResponseDTO
                {
                    Id = hostRole.Id,
                    Name = hostRole.Name,
                    Description = hostRole.Description,
                    IsActive = hostRole.IsActive,
                    AddedDateTime = hostRole.AddedDateTime,
                    UpdatedDateTime = hostRole.UpdatedDateTime
                },
                HostPermissions = permissions
            };
        }

        #endregion
    }
    #endregion
}
