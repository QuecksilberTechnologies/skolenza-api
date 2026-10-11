// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Authenticates Host users and Institution staffMembers and creates minimal secure session bootstrap responses.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers.EncryptionHelper;
using skolenza.application.Common.Helpers.Hash;
using skolenza.application.Constants;
using skolenza.application.DTOs.Institution;
using skolenza.application.DTOs.UserLogin;
using skolenza.application.DTOS.Token;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Interfaces.IHashed;
using skolenza.application.Interfaces.ITokenService;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Net;

namespace skolenza.application.Features.UserLoginAndDashboardCmd.Handlers;

#region Command

/// <summary>
/// Represents a request to authenticate a Host user or Institution StaffMember using the existing login request contract.
/// </summary>
public sealed class NewLoginCommand : IRequest<ApiResponse<NewLoginResponseDTO>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NewLoginCommand"/> class.
    /// </summary>
    /// <param name="request">The existing client login request.</param>
    public NewLoginCommand(LoginRequestDTO request)
    {
        Request = request;
    }

    /// <summary>
    /// Gets the existing client login request.
    /// </summary>
    public LoginRequestDTO Request { get; }
}

#endregion

#region Handler

/// <summary>
/// Authenticates Host users first, then Institution StaffMembers, and returns only compact session bootstrap data.
/// </summary>
public sealed class NewLoginCommandHandler : IRequestHandler<NewLoginCommand, ApiResponse<NewLoginResponseDTO>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly IIdEncoderService _idEncoderService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IMapper _mapper;
    private readonly ILogger<NewLoginCommandHandler> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="NewLoginCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides the existing Host and Institution authentication repositories.</param>
    /// <param name="passwordService">Verifies the existing credential password hashes.</param>
    /// <param name="tokenService">Generates the existing Institution access and refresh tokens.</param>
    /// <param name="idEncoderService">Encodes client-facing Institution and StaffMember identifiers.</param>
    /// <param name="fileStorageService">Resolves profile-image URLs from stored file keys.</param>
    /// <param name="mapper">Maps the login read model to the compact public user context.</param>
    /// <param name="logger">Records non-sensitive authentication lifecycle information.</param>
    public NewLoginCommandHandler(
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        ITokenService tokenService,
        IIdEncoderService idEncoderService,
        IFileStorageService fileStorageService,
        IMapper mapper,
        ILogger<NewLoginCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _idEncoderService = idEncoderService;
        _fileStorageService = fileStorageService;
        _mapper = mapper;
        _logger = logger;
    }

    #endregion

    #region Handle

    /// <summary>
    /// Authenticates an active Host user or Institution StaffMember and creates a compact secure session bootstrap response.
    /// </summary>
    /// <param name="command">The requested Host-user or Institution StaffMember login.</param>
    /// <param name="cancellationToken">A token used to cancel authentication processing.</param>
    /// <returns>A successful compact session bootstrap response.</returns>
    /// <exception cref="ValidationErrorException">Thrown when the login request is incomplete.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the credential, staffMember, institution, subscription, or primary role is invalid.</exception>
    /// <exception cref="ApiException">Thrown when secure token issuance or metadata persistence cannot complete.</exception>
    public async Task<ApiResponse<NewLoginResponseDTO>> Handle(
        NewLoginCommand command,
        CancellationToken cancellationToken)
    {
        #region Authentication

        var request = command?.Request;
        if (request == null ||
            string.IsNullOrWhiteSpace(request.LoginId) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidRequest);
        }

        #region Principal Resolution

        // Preserve legacy precedence: a valid Host login identifier is never evaluated as a Institution credential.
        var hostUser = await _unitOfWork.HostUserRepository
            .GetByLoginIdAsync(request.LoginId);

        if (hostUser != null)
        {
            return await HandleHostLoginAsync(hostUser, request, cancellationToken);
        }

        #endregion

        // Preserve the legacy stored-function validation before loading the credential required for password verification.
        var validatedStaffMemberId = await _unitOfWork.StoreProcedureRepository
            .ValidateActiveUserLoginOnlyAsync(request.LoginId);

        if (validatedStaffMemberId < 1)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        // Validate the active institution login credential before password verification.
        var loginCredential = await _unitOfWork.UserLoginRepository
            .AuthenticateUser(request.LoginId);

        if (loginCredential == null ||
            string.IsNullOrWhiteSpace(loginCredential.Password) ||
            !_passwordService.VerifyPassword(loginCredential.Password, request.Password))
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        // Resolve the minimal staffMember context required to bootstrap the authenticated session.
        var bootstrap = await _unitOfWork.UserLoginRepository
            .GetNewLoginBootstrapAsync(loginCredential.Id, cancellationToken);

        if (bootstrap == null)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        // The stored-function result and credential-owned staffMember must identify the same authenticated staffMember.
        if (bootstrap.StaffMemberId != validatedStaffMemberId)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        var subscription = await _unitOfWork.InstitutionSubscriptionRepository
            .GetValidateInstitutionPlan(new InstitutionSubscriptionPlanRequestDTO { InstitutionId = bootstrap.InstitutionId });

        if (subscription == null ||
            !subscription.SubscriptionEndDate.HasValue ||
            subscription.SubscriptionEndDate.Value.Date < DateTime.Today)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        #endregion

        #region Role Resolution

        // Resolve all effective institution-scoped roles with the same query and filters used by legacy Auth login.
        var effectiveRoleAssignments = await _unitOfWork.UserRoleRepository
            .GetStaffMemberRolesWithDetailsByIdAsync(bootstrap.StaffMemberId, bootstrap.InstitutionId);

        var primaryRoleAssignments = effectiveRoleAssignments
            .Where(assignment =>
                assignment.IsPrimaryRole == true &&
                assignment.RoleId.HasValue &&
                assignment.Role != null)
            .ToList();

        // A session must have exactly one trusted primary role; no arbitrary assignment may be selected.
        if (primaryRoleAssignments.Count != 1)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        var primaryRoleAssignment = primaryRoleAssignments[0];
        var primaryRole = _mapper.Map<NewLoginRoleDTO>(primaryRoleAssignment.Role!);

        // Return each valid secondary role once, excluding the selected primary role.
        var secondaryRoles = effectiveRoleAssignments
            .Where(assignment =>
                assignment.RoleId.HasValue &&
                assignment.Role != null &&
                assignment.RoleId != primaryRoleAssignment.RoleId)
            .GroupBy(assignment => assignment.RoleId!.Value)
            .Select(assignments => _mapper.Map<NewLoginRoleDTO>(assignments.First().Role!))
            .ToArray();

        #endregion

        #region Session Composition

        var institutionEncryptionKey = await _unitOfWork.InstitutionEncryptionKeyRepository
            .GetActiveKeyByInstitutionIdAsync(bootstrap.InstitutionId, cancellationToken);

        if (institutionEncryptionKey == null || string.IsNullOrWhiteSpace(institutionEncryptionKey.EncryptionKey))
        {
            throw new ApiException(
                AppConstants.ErrorMessages.InternalServerError,
                (int)HttpStatusCode.InternalServerError);
        }

        var trustedInstitutionKey = EncryptionSanitizer.SuperSanitize(institutionEncryptionKey.EncryptionKey);
        var accessToken = await _tokenService.GenerateInstitutionToken(
            CreateTokenInfo(request.LoginId, bootstrap, primaryRole, trustedInstitutionKey));

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ApiException(
                AppConstants.ErrorMessages.InternalServerError,
                (int)HttpStatusCode.InternalServerError);
        }

        var accessTokenExpiresAtUtc = _tokenService.GetExpiryFromToken(accessToken);
        if (!accessTokenExpiresAtUtc.HasValue)
        {
            throw new ApiException(
                AppConstants.ErrorMessages.InternalServerError,
                (int)HttpStatusCode.InternalServerError);
        }

        var refreshToken = await _tokenService.GenerateRefreshToken();
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ApiException(
                AppConstants.ErrorMessages.InternalServerError,
                (int)HttpStatusCode.InternalServerError);
        }

        // Persist the same refresh-token expiry value that is returned to the client.
        var refreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(7);
        var refreshTokenStored = await _unitOfWork.RefreshTokenRepository.InsertAsync(
            new RefreshToken
            {
                LoginId = loginCredential.LoginId,
                UserType = (short)LoginUserType.InstitutionStaffMember,
                LoginCredentialId = loginCredential.Id,
                HostUserId = null,
                Token = HashHelper.Sha256(refreshToken),
                ExpiryDate = refreshTokenExpiresAtUtc,
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = request.IpAddressPublic,
                IsRevoked = false
            });

        if (!refreshTokenStored)
        {
            throw new ApiException(
                AppConstants.ErrorMessages.InternalServerError,
                (int)HttpStatusCode.InternalServerError);
        }

        // Persist only the authenticated request's permitted device and network metadata.
        loginCredential.MacAddress = request.MacAddress;
        loginCredential.IpAddressLocal = request.IpAddressLocal;
        loginCredential.IpAddressPublic = request.IpAddressPublic;
        loginCredential.Latitude = request.Latitude;
        loginCredential.Longitude = request.Longitude;
        loginCredential.LoginDevice = request.LoginDevice;
        loginCredential.IsOnboard = true;
        loginCredential.UpdatedById = bootstrap.StaffMemberId;
        loginCredential.UpdatedDateTime = DateTime.UtcNow;

        if (!await _unitOfWork.UserLoginRepository.UpdateLoginMetadataAsync(loginCredential, cancellationToken))
        {
            throw new ApiException(
                AppConstants.ErrorMessages.InternalServerError,
                (int)HttpStatusCode.InternalServerError);
        }

        // Build the compact login response without loading navigation or permission data.
        var user = _mapper.Map<NewLoginUserContextDTO>(bootstrap);
        user.IsOnboard = true;
        user.StaffMemberId = _idEncoderService.EncodeId_long(bootstrap.StaffMemberId, trustedInstitutionKey);
        user.InstitutionId = _idEncoderService.EncodeId_long(bootstrap.InstitutionId, trustedInstitutionKey);
        user.FullName = BuildFullName(bootstrap);
        user.PrimaryRole = primaryRole;
        user.SecondaryRoles = secondaryRoles;

        // Residential geography is intentionally deferred; never infer it from Institution registration data.
        user.CountryId = null;
        user.CountryName = null;
        user.StateId = null;
        user.StateName = null;
        user.CityId = null;
        user.CityName = null;

        var profileImageKey = await _unitOfWork.StaffMembers.ProfileImage(bootstrap.StaffMemberId);
        if (!string.IsNullOrWhiteSpace(profileImageKey))
        {
            user.ProfileImageUrl = _fileStorageService.GetFileUrl(profileImageKey);
        }

        _logger.LogInformation(
            "Issued a compact Institution StaffMember session for InstitutionId {InstitutionId} and StaffMemberId {StaffMemberId}.",
            bootstrap.InstitutionId,
            bootstrap.StaffMemberId);

        return ApiResponse<NewLoginResponseDTO>.Success(
            new NewLoginResponseDTO
            {
                UserType = LoginUserType.InstitutionStaffMember.ToString(),
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc.Value.ToUniversalTime(),
                RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc,
                User = user
            },
            AppConstants.SuccessMessages.LoginSuccessful);

        #endregion
    }

    #endregion

    #region Host Session

    /// <summary>
    /// Authenticates a resolved Host user and creates the compact Host session response.
    /// </summary>
    /// <param name="hostUser">The active Host user selected by the legacy Host-first login precedence.</param>
    /// <param name="request">The existing login request containing the submitted password.</param>
    /// <param name="cancellationToken">A token used to cancel session creation.</param>
    /// <returns>The compact Host session response.</returns>
    private async Task<ApiResponse<NewLoginResponseDTO>> HandleHostLoginAsync(
        HostUser hostUser,
        LoginRequestDTO request,
        CancellationToken cancellationToken)
    {
        #region Host Authentication

        // Recheck the Host state before verifying the existing Host password hash.
        if (!hostUser.IsActive || hostUser.IsSoftDeleted ||
            !_passwordService.VerifyPassword(hostUser.PasswordHash, request.Password))
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        var hostRole = await _unitOfWork.HostRoleRepository.GetByIdAsync(hostUser.HostRoleId);
        if (hostRole == null || !hostRole.IsActive || hostRole.IsSoftDeleted)
        {
            throw new UnauthorizedAccessException(AppConstants.ErrorMessages.Unauthorized);
        }

        #endregion

        #region Host Session Composition

        // Generate Host claims exclusively from the trusted Host principal and Host role.
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
            throw new ApiException(
                AppConstants.ErrorMessages.InternalServerError,
                (int)HttpStatusCode.InternalServerError);
        }

        var accessTokenExpiresAtUtc = _tokenService.GetExpiryFromToken(accessToken);
        if (!accessTokenExpiresAtUtc.HasValue)
        {
            throw new ApiException(
                AppConstants.ErrorMessages.InternalServerError,
                (int)HttpStatusCode.InternalServerError);
        }

        var refreshToken = await _tokenService.GenerateRefreshToken();
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new ApiException(
                AppConstants.ErrorMessages.InternalServerError,
                (int)HttpStatusCode.InternalServerError);
        }

        // Preserve the Host refresh-token ownership invariant: HostUserId only, never LoginCredentialId.
        var refreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(7);
        var refreshTokenStored = await _unitOfWork.RefreshTokenRepository.InsertAsync(
            new RefreshToken
            {
                LoginId = hostUser.LoginId,
                UserType = (short)LoginUserType.Host,
                LoginCredentialId = null,
                HostUserId = hostUser.Id,
                Token = HashHelper.Sha256(refreshToken),
                ExpiryDate = refreshTokenExpiresAtUtc,
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = ConstantValues.IP,
                IsRevoked = false
            });

        if (!refreshTokenStored)
        {
            throw new ApiException(
                AppConstants.ErrorMessages.InternalServerError,
                (int)HttpStatusCode.InternalServerError);
        }

        // Keep authorization and navigation payloads outside the authentication response boundary.
        return ApiResponse<NewLoginResponseDTO>.Success(
            new NewLoginResponseDTO
            {
                UserType = LoginUserType.Host.ToString(),
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAtUtc = accessTokenExpiresAtUtc.Value.ToUniversalTime(),
                RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc,
                HostUser = new NewLoginHostUserContextDTO
                {
                    HostUserId = hostUser.Id,
                    Name = hostUser.Name,
                    LoginId = hostUser.LoginId,
                    Email = hostUser.Email
                },
                HostRole = new NewLoginHostRoleDTO
                {
                    HostRoleId = hostRole.Id,
                    Name = hostRole.Name
                }
            },
            AppConstants.SuccessMessages.LoginSuccessful);

        #endregion
    }

    #endregion

    #region Session Bootstrap

    /// <summary>
    /// Builds the existing Institution access-token claim input from trusted server-side login data.
    /// </summary>
    /// <param name="loginId">The authenticated login identifier.</param>
    /// <param name="bootstrap">The validated minimal Institution StaffMember bootstrap data.</param>
    /// <param name="primaryRole">The single effective primary role used by the established access-token claim set.</param>
    /// <param name="institutionEncryptionKey">The active institution key used by the established token and ID-encoding convention.</param>
    /// <returns>The existing Institution token-service input.</returns>
    private GetTokenInfoDTO CreateTokenInfo(
        string loginId,
        NewLoginBootstrapReadModel bootstrap,
        NewLoginRoleDTO primaryRole,
        string institutionEncryptionKey)
    {
        return new GetTokenInfoDTO
        {
            InstitutionEncriptionKey = institutionEncryptionKey,
            InstitutionId = _idEncoderService.EncodeId_long(bootstrap.InstitutionId, institutionEncryptionKey),
            UserId = loginId,
            StaffMemberId = _idEncoderService.EncodeId_long(bootstrap.StaffMemberId, institutionEncryptionKey),
            RoleId = primaryRole.RoleId.ToString(),
            RoleTypeId = primaryRole.RoleTypeId.ToString(),
            // Preserve the established token claim source, which uses the primary role's display name.
            RoleTypeName = primaryRole.RoleName,
            StaffEmploymentTypeId = bootstrap.StaffEmploymentTypeId.ToString(),
            StaffEmploymentTypeName = bootstrap.StaffEmploymentTypeName ?? string.Empty,
            GenderId = bootstrap.GenderId.ToString(),
            GenderName = bootstrap.GenderName ?? string.Empty,
            Email = bootstrap.OfficialEmail ?? string.Empty,
            FullName = BuildFullName(bootstrap),
            HasPermanent = bootstrap.HasPermanent,
            TokenPurpose = ConstantValues.Auth.ToString(),
            IssuedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Builds a whitespace-safe staffMember display name from the minimal bootstrap projection.
    /// </summary>
    /// <param name="bootstrap">The validated minimal Institution StaffMember bootstrap data.</param>
    /// <returns>The normalized staffMember display name.</returns>
    private static string BuildFullName(NewLoginBootstrapReadModel bootstrap)
    {
        return string.Join(
            " ",
            new[] { bootstrap.FirstName, bootstrap.MiddleName, bootstrap.LastName }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    #endregion
}

#endregion
