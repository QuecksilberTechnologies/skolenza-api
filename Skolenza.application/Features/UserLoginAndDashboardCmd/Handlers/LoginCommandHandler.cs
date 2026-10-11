// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Authenticates institution and Host users through centralized error handling.
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
using skolenza.application.DTOS.UserRoles;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Interfaces.IHashed;
using skolenza.application.Interfaces.IRepositories;
using skolenza.application.Interfaces.ITokenService;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;

//using skolenza.domain.Entity;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Authenticates Host users and Institution StaffMembers while preserving the existing Institution login flow.
// ============================================================================

namespace skolenza.application.Features.UserLoginAndDashboardCmd.Handlers
{

    #region Command

    /// <summary>
    /// Represents a request to authenticate either a Host user or a Institution StaffMember.
    /// </summary>
    public class LoginCommand : IRequest<ApiResponse<LoginResponseDTO>>
    {
        public LoginRequestDTO DTO { get; set; }


        public LoginCommand(LoginRequestDTO loginRequestDTO)
        {
            DTO = loginRequestDTO;
        }



    }

    /// <summary>
    /// Handles Host authentication first and delegates unchanged Institution StaffMember login behavior when no Host user matches.
    /// </summary>
        #endregion

    #region Handler

public class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse<LoginResponseDTO>>
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IStoreProcedureRepository _iCommonRepository;
        private readonly ILogger<LoginCommandHandler> _logger;
        private readonly IPasswordService _passwordService;
        private readonly IConfiguration _configuration;
        private readonly IEncryptionService _encryptionService;
        private readonly  IIdEncoderService _idEncoderService;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IFileStorageService _fileStorageService;

        public LoginCommandHandler(IMapper mapper, IUnitOfWork unitOfWork, ITokenService tokenService, IRefreshTokenRepository refreshTokenRepository, ILogger<LoginCommandHandler> logger, IStoreProcedureRepository iCommonRepository, IPasswordService passwordService, IFileStorageService fileStorageService,
            IConfiguration configuration, IEncryptionService encryptionService, IIdEncoderService idEncoderService, ICommonRequestService commonRequestService)
        {
            _logger = logger;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _refreshTokenRepository = refreshTokenRepository;
            _iCommonRepository = iCommonRepository;
            _passwordService = passwordService;
            _configuration = configuration;
            _encryptionService = encryptionService; //  same name use karo
            _idEncoderService = idEncoderService;
            _commonRequestService = commonRequestService;
       _fileStorageService = fileStorageService;
        }
        public async Task<ApiResponse<LoginResponseDTO>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
               // await _unitOfWork.BeginTransactionAsync();
              //
                if (string.IsNullOrEmpty(request.DTO.LoginId) && string.IsNullOrEmpty(request.DTO.Password))
                {
                    _logger.LogWarning(" LoginId and Password is not null or empty.");
                    throw new skolenza.application.Exceptions.ValidationErrorException(
                        skolenza.application.Constants.AppConstants.ErrorMessages.InvalidRequest);

                }
                string? savedFullPath = null;  //  File full path track karne ke liye

                if (string.IsNullOrEmpty(request.DTO.LoginId) && string.IsNullOrEmpty(request.DTO.Password))
                {
                    _logger.LogWarning(" LoginId and Password is not null or empty.");
                    throw new skolenza.application.Exceptions.ValidationErrorException(
                        skolenza.application.Constants.AppConstants.ErrorMessages.InvalidRequest);
                }


                #region Host Authentication

                var hostUser = await _unitOfWork.HostUserRepository
                    .GetByLoginIdAsync(request.DTO.LoginId);

                if (hostUser != null)
                {
                    return await HandleHostLoginAsync(
                        hostUser,
                        request.DTO,
                        cancellationToken);
                }

                #endregion


                //  Step 1: Validate if user exists
                long empId = await _unitOfWork.StoreProcedureRepository.ValidateActiveUserLoginOnlyAsync(request.DTO.LoginId);
                _logger.LogInformation("Validation result for LoginId {LoginId}: StaffMemberId = {empId}", request.DTO.LoginId, empId);

                if (empId < 1)
                {
                    _logger.LogWarning("User validation failed for LoginId: {LoginId}", request.DTO.LoginId);
                   // await _unitOfWork.RollbackTransactionAsync();
                    throw new UnauthorizedAccessException(
                        skolenza.application.Constants.AppConstants.ErrorMessages.Unauthorized);
                }



                var loginRequest = new LoginRequestDTO
                {
                    LoginId = request.DTO.LoginId,
                    Password = request.DTO.Password,
                    IpAddressLocal = request.DTO.IpAddressLocal,
                    IpAddressPublic = request.DTO.IpAddressPublic,
                    LoginDevice = request.DTO.LoginDevice,
                    MacAddress = request.DTO.MacAddress,
                    Latitude = request.DTO.Latitude,
                    Longitude = request.DTO.Longitude,
                };

                //  Authenticate user
                var user = await _unitOfWork.UserLoginRepository.AuthenticateUser(loginRequest.LoginId);

                if (user == null || string.IsNullOrWhiteSpace(user.Password))
                {
                    throw new UnauthorizedAccessException(ConstantValues.invalidCredential);
                }

                //  Verify password
                if (!_passwordService.VerifyPassword(user.Password, loginRequest.Password))
                {
                    throw new UnauthorizedAccessException(ConstantValues.invalidCredential);
                }

                //if (!passwordMatch)
                //{
                //    _logger.LogWarning(" Login failed: Incorrect password for LoginId: {LoginId}", loginRequest.LoginId);
                //    return null;
                //}



                _logger.LogInformation("✅ User authenticated successfully for LoginId: {LoginId}", loginRequest.LoginId);

                bool? IsPasswordChange = user.IsPasswordChangeRequired;


                // ‍ Step 5: Fetch StaffMember Info
                // var staffMember = await _unitOfWork.StaffMembers.GetStaffMemberInfoForLoginByIdAsync(empId);
                GetMinimalStaffMemberResponseDTO empMinimalResponse = await _unitOfWork.StaffMembers.GetSingleRecordAsync(empId,true);
                InstitutionSubscriptionPlanRequestDTO dto = new InstitutionSubscriptionPlanRequestDTO();



                if (empMinimalResponse == null)
                {
                  //  await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogWarning("StaffMember may not active or deleted, please contact admin. LoginId: {LoginId}", loginRequest.LoginId);

                    return new ApiResponse<LoginResponseDTO>
                    {
                        IsSucceeded = false,
                        Message = "StaffMember not active. Please contact admin."
                    };
                }

                // Null check with fallback to 0
                dto.InstitutionId = empMinimalResponse.InstitutionId;
                var subscriptionInfo = await _unitOfWork.InstitutionSubscriptionRepository
                     .GetValidateInstitutionPlan(dto);

                //  Check if subscription info exists and EndDate > today
                if (subscriptionInfo == null ||
                             !subscriptionInfo.SubscriptionEndDate.HasValue ||
                             subscriptionInfo.SubscriptionEndDate.Value.Date < DateTime.Today)
                             {
                           //    await _unitOfWork.RollbackTransactionAsync();

                                  _logger.LogWarning("Subscription expired or missing for institution {InstitutionId}", dto.InstitutionId);

                            return new ApiResponse<LoginResponseDTO>
                              {
                                 IsSucceeded = false,
                                  Message = "Your subscription has expired. Please contact admin to renew the plan."
                         };
                }
                // Get Active true and Isdeleted false staffMember

                //  Step 6: Fetch all roles
                var userRoles = await _unitOfWork.UserRoleRepository.GetStaffMemberRolesWithDetailsByIdAsync(empId, empMinimalResponse.InstitutionId);
                var roleInfo = userRoles.FirstOrDefault(x => x.IsPrimaryRole == true);


                //bool isAdmin = userRoles.Any(ur => ur.Role.RoleType == ConstantValues.InstitutionAdminRoleType &&   ur.Role.IsSystemDefault == ConstantValues.IsByDefaultFalse &&
                // ur.Role.RoleCode.Equals(ConstantValues.InstitutionAdminRoleCode, StringComparison.OrdinalIgnoreCase) &&
                // ur.Role.IsActive == true &&
                // ur.Role.IsSoftDeleted == false);

                //bool isAdmin = true;
                //if (isAdmin)
                //  {
                //      UpdateInstitutionEnabledOperationFromModuleOperationRequestDTO updateInstitutionEnabledOperationFromModuleOperationRequestDTO = new UpdateInstitutionEnabledOperationFromModuleOperationRequestDTO();
                //      updateInstitutionEnabledOperationFromModuleOperationRequestDTO.InstitutionId = empMinimalResponse.InstitutionId;
                //      var updatedDone = _unitOfWork.StoreProcedureRepository.UpdateInstitutionEnabledOperationFromModuleOperationRequestDTO(updateInstitutionEnabledOperationFromModuleOperationRequestDTO);
                //  }
                List<UserRoleDTO>? userRoleDTOs = null;
                string? allRoleIdsCsv = null;
                UserRoleDTO? primaryRole = null;
                if (userRoles.Count !=0)
                {
                     allRoleIdsCsv = userRoles?
                    .Where(r => r.RoleId != null)
                    .Select(r => r.RoleId.ToString())
                    .Aggregate((a, b) => $"{a},{b}");

                    if (!string.IsNullOrEmpty(allRoleIdsCsv))
                        _logger.LogInformation("Fetched Role IDs for LoginId {LoginId}: {Roles}", loginRequest.LoginId, allRoleIdsCsv);
                    else
                        _logger.LogWarning("No roles found for LoginId {LoginId}", loginRequest.LoginId);


                    //  Step 7: Map roles to DTOs
                      userRoleDTOs = _mapper.Map<List<UserRoleDTO>>(userRoles);
                    //  Find & separate primary role
                        primaryRole = userRoleDTOs.FirstOrDefault(ur => ur.IsPrimaryRole && ur.IsActive);

                    if (primaryRole != null)
                        userRoleDTOs.Remove(primaryRole); // Remove primary from list

                    //  Extract secondary role IDs
                    List<int> secondaryRoleIds = userRoleDTOs?.Where(ur => ur.RoleId > 0).Select(ur => ur.RoleId).Distinct().ToList()?? new List<int>();


                    string secondaryRolesCsv = secondaryRoleIds.Any()
                        ? string.Join(",", secondaryRoleIds)
                        : "NULL";
                    //  Step 8: Map StaffMember Info

                }


              //  staffMemberInfo.StaffEmploymentTypeId = empInfo.StaffEmploymentTypeId;

 


                //  Step 9: Load Common Itskolenza
                //  var commonItskolenza = await _unitOfWork.CommonRepository.GetCommonItemAsync();

                //  Step 1: Get CommonMenuParent first (wait until it completes)
                var parent = await _unitOfWork.ModuleRepository.GetCommonMenuParentAsync();
                if (parent == null) return null;

                //  Step 2: ONLY AFTER FIRST IS DONE, call second method
                List<ModuleDTO> CommonItems = await _unitOfWork.ModuleRepository.GetCommonMenuTreeAsync(parent.Id);

                //  Step 3: THEN call RolePermissions API
                var requestDto = new GetActiveRoleModuleOperationsRequestDTO
                {
                    RoleIds = allRoleIdsCsv,
                    InstitutionId = empMinimalResponse.InstitutionId
                };





                var rolePermissions = await _unitOfWork.StoreProcedureRepository.GetActiveRoleModuleOperationsAsync(requestDto);




                var grouped = rolePermissions
             .GroupBy(m => new { m.MainModuleId, m.MainModuleName }) //  Group by Main Module
             .Select(main => new MainModuleDto
             {
                 MainModuleId = main.Key.MainModuleId,
                 MainModuleName = main.Key.MainModuleName,

                 SubModules = main
                     .GroupBy(sm => new { sm.ParentModuleId, sm.SubModuleName }) //  Sub Module Group
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
                                                       //  Newly Added
                                 mod.URLPath,                  //  From ModuleOperationMapping
                                                 //  From ModuleOperationMapping
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
                                 SubModuleURL = mod.Key.URLPath,              //
                                                       //

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
                var InstitutionEnabledModulesWithOperationData = await _unitOfWork.UserRolesPermissionOnModuleRepository.GetAllInstitutionEnabledModulesWithOperationsAsync(empMinimalResponse.InstitutionId);
                //  Step 1: Safe InstitutionId extraction

                long tempInstitutionId = empMinimalResponse?.InstitutionId ?? 0;
                long tempStaffMemberId = empMinimalResponse?.Id ?? 0;
                string convertedInstitutionId = tempInstitutionId.ToString();

                //  Step 2: Get encryption key
                var institutionEncryptionKey = await _unitOfWork.InstitutionEncryptionKeyRepository
                    .GetActiveKeyByInstitutionIdAsync(tempInstitutionId);


                if (institutionEncryptionKey == null || string.IsNullOrEmpty(institutionEncryptionKey.EncryptionKey))
                {
                    throw new Exception("Institution encryption key not found or invalid.");

                }

                string finalKey = EncryptionSanitizer.SuperSanitize(institutionEncryptionKey.EncryptionKey);
                string encriptedStaffMemberId = _idEncoderService.EncodeId_long(tempStaffMemberId, finalKey);
                string encriptedInstitutionId = _idEncoderService.EncodeId_long(tempInstitutionId, finalKey);
                //  Decrypt same encrypted string

               // string? ProfileImagePath = $"{_configuration["FileSettings:BaseUrl"] ?? string.Empty}{await _unitOfWork.StaffMembers.ProfileImage(empId) ?? null}";
                string? profileKey = await _unitOfWork.StaffMembers.ProfileImage(empId);

                string? ProfileImagePath = null;

                if (!string.IsNullOrWhiteSpace(profileKey))
                {
                    ProfileImagePath = _fileStorageService.GetFileUrl(profileKey);
                }
                //string Decrut = (_encryptionService.Decrypt(tempStaffMemberId.ToString(), institutionEncryptionKey.EncryptionKey));

                GetStaffMemberLoginInfoResponseDTO? staffMemberInfo = _mapper.Map<GetStaffMemberLoginInfoResponseDTO>(empMinimalResponse);
                staffMemberInfo.IsPasswordChangeRequired = IsPasswordChange;
                staffMemberInfo.UserPrimaryRole = primaryRole;
                staffMemberInfo.RoleTypeId = roleInfo.Role.RoleType;
                staffMemberInfo.RoleTypeName = roleInfo.Role.RoleName;
                staffMemberInfo.StaffMemberId = encriptedStaffMemberId.Trim();
                staffMemberInfo.UserSecondryRoles = userRoleDTOs;
                staffMemberInfo.ProfileImageLink = ProfileImagePath;
                staffMemberInfo.IsOnboard=user.IsOnboard;
                var institution = await _unitOfWork.InstitutionRepository.GetByIdAsync(dto.InstitutionId, true );
                GetRoleRequestDTO getRoleRequestDTO = new GetRoleRequestDTO
                {
                    Id = staffMemberInfo.UserPrimaryRole?.RoleId ?? 0,
                    RoleType = roleInfo.Role.RoleType,
                    IsActive = true
                };

                //  Get role list (filtered by roleId)
                var roleTypeList = await _unitOfWork.RoleRepository.GetAsync(
                    dto.InstitutionId,
                    getRoleRequestDTO);

                //  Select specific role
                var roleType = roleTypeList.Data.FirstOrDefault(r => r.Id == staffMemberInfo.UserPrimaryRole.RoleId && r.IsActive == true);

                //  Get institution info (await lagana mat bhoolna)

                //  Assign institution name

                if(dto.InstitutionId==0)
                {
                   // await _unitOfWork.RollbackTransactionAsync();
                    return new ApiResponse<LoginResponseDTO>
                    {
                        IsSucceeded = false,
                        Message = "Institution information is invalid. Please contact admin."
                    };
                }




                //  Step 4: Prepare staffMember info
                staffMemberInfo.InstitutionName = institution?.CompanyName ?? string.Empty;


                //  Step 5: Prepare token info DTO
                GetTokenInfoDTO getTokenInfoDTO = new GetTokenInfoDTO()
                {
                     InstitutionEncriptionKey = finalKey,
                    InstitutionId = encriptedInstitutionId,
                    UserId = request.DTO.LoginId,
                    StaffMemberId = encriptedStaffMemberId.Trim().ToString(), // long
                    RoleId = staffMemberInfo.UserPrimaryRole.RoleId.ToString(), // long
                    RoleTypeId = staffMemberInfo.RoleTypeId.ToString() ?? "0",
                    RoleTypeName = staffMemberInfo.RoleTypeName ?? "",
                    StaffEmploymentTypeId =  staffMemberInfo.StaffEmploymentTypeId.ToString()??"0",
                    GenderId = empMinimalResponse.GenderId.ToString(),
                    GenderName = empMinimalResponse.GenderName,
                    Email = request.DTO.LoginId ,
                    FullName = empMinimalResponse.FirstName + ("-" + empMinimalResponse.LastName) ?? " Unknown",
                    Expiry = DateTime.UtcNow.AddMinutes(15),
                    TokenPurpose = ConstantValues.Auth.ToString(),


                };

                #region 🎫 Token Generation

                //  Step 3: Generate tokens
                var token = await _tokenService.GenerateInstitutionToken(getTokenInfoDTO);


                var refreshToken = await _tokenService.GenerateRefreshToken(); // PLAIN
                var hashedRefreshToken = HashHelper.Sha256(refreshToken);      // HASHED

                bool isInserted = await _unitOfWork.RefreshTokenRepository.InsertAsync(new RefreshToken
                {
                    LoginId = request.DTO.LoginId,
                    UserType = (short)LoginUserType.InstitutionStaffMember,
                    LoginCredentialId = user.Id,
                    HostUserId = null,
                    Token = hashedRefreshToken,
                    ExpiryDate = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow,
                    CreatedByIp = ConstantValues.IP,
                    IsRevoked = false,

                    // OPTIONAL (future multi-device)
                    // DeviceId = request.DTO.DeviceId,
                    // DeviceType = request.DTO.DeviceType
                });

                #endregion

                //  Step 4: Update login audit
                if (isInserted!= false)
                    {
                    _logger.LogInformation("RefreshToken inserted successfully for LoginId: {LoginId}", loginRequest.LoginId);
                   }
                else
                {
                    _logger.LogWarning("Failed to insert RefreshToken for LoginId: {LoginId}", loginRequest.LoginId);

                   // await _unitOfWork.RollbackTransactionAsync();

                }
                bool updated = await _unitOfWork.StoreProcedureRepository.UpdateLoginCredential(loginRequest);
                if (updated)
                {

                    _logger.LogInformation("LoginCredential updated successfully for LoginId: {LoginId}", loginRequest.LoginId);
                   // await _unitOfWork.CommitTransactionAsync();
                }
                else
                    _logger.LogWarning("Failed to update LoginCredential for LoginId: {LoginId}", loginRequest.LoginId);

                // A successfully authenticated Institution user has completed the
                // onboarding sign-in. Persist the state with the same audit
                // values received for this login; Host users never reach this path.
                user.MacAddress = loginRequest.MacAddress;
                user.IpAddressLocal = loginRequest.IpAddressLocal;
                user.IpAddressPublic = loginRequest.IpAddressPublic;
                user.Latitude = loginRequest.Latitude;
                user.Longitude = loginRequest.Longitude;
                user.LoginDevice = loginRequest.LoginDevice;
                user.IsOnboard = true;
                user.UpdatedById = empId;
                user.UpdatedDateTime = DateTime.UtcNow;

                if (!await _unitOfWork.UserLoginRepository.UpdateLoginMetadataAsync(user, cancellationToken))
                {
                    _logger.LogError(
                        "Could not persist onboarding completion for LoginCredentialId: {LoginCredentialId}",
                        user.Id);
                    throw new skolenza.application.Exceptions.ApiException(
                        AppConstants.ErrorMessages.InternalServerError,
                        (int)System.Net.HttpStatusCode.InternalServerError);
                }

                staffMemberInfo.IsOnboard = true;

                //  Step 10: Fetch Permissions

                //  var permissionList = new List<List<RoleModulePermission>> { rolePermissions };

                //  Step 11: Final Response Object
                var loginResponse = new LoginResponseDTO
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    Success = ConstantValues.isSucceeded,
                    StaffMemberInfo = staffMemberInfo,
                    CommonItems = CommonItems,
                    OperationalMenus = grouped,
                    Allroles = allRoleIdsCsv?.Trim()
                };




                //  Final Return
                return ApiResponse<LoginResponseDTO>.Success(loginResponse, "Login successful.");



              //  return new ApiResponse<LoginResponseDTO>(loginResponse, ConstantValues.successMessage, ConstantValues.isSucceeded);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in LoginCommandHandler.Handle method.");

               // await _unitOfWork.RollbackTransactionAsync();  //  Rollback Transaction in case of error

                return new ApiResponse<LoginResponseDTO>
                {
                    IsSucceeded = false,
                    Message = "An error occurred while processing the login request. Please try again later.",
                    Data = null
                };
            }
        }

        #region Host Authentication

        /// <summary>
        /// Completes Host authentication and issues a Host access token with a common refresh token.
        /// </summary>
        /// <param name="hostUser">The Host user found by the Host-user repository.</param>
        /// <param name="loginRequest">The submitted login request.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>A Host login response or a failure response when validation cannot complete.</returns>
        private async Task<ApiResponse<LoginResponseDTO>> HandleHostLoginAsync(
            HostUser hostUser,
            LoginRequestDTO loginRequest,
            CancellationToken cancellationToken)
        {
            if (!hostUser.IsActive || hostUser.IsSoftDeleted)
            {
                throw new UnauthorizedAccessException(
                    skolenza.application.Constants.AppConstants.ErrorMessages.Unauthorized);
            }

            if (!_passwordService.VerifyPassword(hostUser.PasswordHash, loginRequest.Password))
            {
                throw new UnauthorizedAccessException(ConstantValues.invalidCredential);
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

            var refreshToken = await _tokenService.GenerateRefreshToken();
            var isRefreshTokenStored = await _refreshTokenRepository.InsertAsync(
                new RefreshToken
                {
                    LoginId = hostUser.LoginId,
                    UserType = (short)LoginUserType.Host,
                    LoginCredentialId = null,
                    HostUserId = hostUser.Id,
                    Token = HashHelper.Sha256(refreshToken),
                    ExpiryDate = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow,
                    CreatedByIp = ConstantValues.IP,
                    IsRevoked = false
                });

            if (!isRefreshTokenStored)
            {
                throw new InvalidOperationException("The Host refresh token could not be issued.");
            }

            var commonParent = await _unitOfWork.ModuleRepository.GetCommonMenuParentAsync();
            List<ModuleDTO> commonItems = new();

            if (commonParent != null)
            {
                commonItems = await _unitOfWork.ModuleRepository
                    .GetCommonMenuTreeAsync(commonParent.Id);
            }

            _logger.LogInformation(
                "Host user authenticated successfully for LoginId: {LoginId}",
                hostUser.LoginId);

            return ApiResponse<LoginResponseDTO>.Success(
                CreateHostLoginResponse(hostUser, hostRole, permissions, accessToken, refreshToken, commonItems),
                "Login successful.");
        }

        /// <summary>
        /// Creates the additive login response for an authenticated Host principal.
        /// </summary>
        /// <param name="hostUser">The authenticated Host user.</param>
        /// <param name="hostRole">The active Host role assigned to the user.</param>
        /// <param name="permissions">The effective permissions sourced from HostRoleModuleAndPermission.</param>
        /// <param name="accessToken">The signed Host access token.</param>
        /// <param name="refreshToken">The opaque Host refresh token returned to the client.</param>
        /// <param name="commonItems">The shared common-menu tree, or an empty list when no Common parent exists.</param>
        /// <returns>The Host login response.</returns>
        private LoginResponseDTO CreateHostLoginResponse(
            HostUser hostUser,
            HostRole hostRole,
            List<HostUserPermissionResponseDTO> permissions,
            string accessToken,
            string refreshToken,
            List<ModuleDTO> commonItems)
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
                HostPermissions = permissions,
                CommonItems = commonItems
            };
        }

        #endregion



    }

    #endregion
}
