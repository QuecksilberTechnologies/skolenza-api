// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Resends the existing Institution onboarding welcome email for an unverified Institution after Host validation.
// ================================================================

using skolenza.application.Constants;
using skolenza.application.Common.Helpers;
using skolenza.application.DTOs.BaseDTO;
using skolenza.application.DTOS.Token;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEmail;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.ITokenService;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Net;
using skolenza.domain.Entity;

namespace skolenza.application.Features.InstitutionManagementCmd.Commands;

#region Command

/// <summary>
/// Represents the Host request to resend onboarding verification for one Institution.
/// </summary>
public sealed class ResendInstitutionVerificationCommand : IRequest<ApiResponse<bool>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResendInstitutionVerificationCommand"/> class.
    /// </summary>
    /// <param name="encryptedInstitutionId">The encrypted Institution identifier from the route.</param>
    /// <param name="permissionRequest">The query-bound Host module-operation permission request.</param>
    public ResendInstitutionVerificationCommand(
        string encryptedInstitutionId,
        PermissionRequestDTO? permissionRequest)
    {
        EncryptedInstitutionId = encryptedInstitutionId;
        ModuleId = permissionRequest?.ModuleId ?? 0;
        OperationId = permissionRequest?.OperationId ?? 0;
    }

    /// <summary>
    /// Gets the encrypted Institution identifier from the route.
    /// </summary>
    public string EncryptedInstitutionId { get; }

    /// <summary>
    /// Gets the requested Host module identifier.
    /// </summary>
    public int ModuleId { get; }

    /// <summary>
    /// Gets the requested Host operation identifier.
    /// </summary>
    public int OperationId { get; }
}

#endregion

#region Handler

/// <summary>
/// Reuses the established Institution onboarding token and welcome-email infrastructure for an unverified Institution.
/// </summary>
public sealed class ResendInstitutionVerificationCommandHandler
    : IRequestHandler<ResendInstitutionVerificationCommand, ApiResponse<bool>>
{
    #region Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;
    private readonly ITokenService _tokenService;
    private readonly IIdEncoderService _idEncoderService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ResendInstitutionVerificationCommandHandler"/> class.
    /// </summary>
    /// <param name="unitOfWork">Provides Institution and onboarding credential queries.</param>
    /// <param name="commonRequestService">Validates the current Host principal.</param>
    /// <param name="tokenService">Generates the established onboarding token format.</param>
    /// <param name="idEncoderService">Encodes identifiers used by the established Institution onboarding token flow.</param>
    /// <param name="encryptionService">Decrypts the Host-facing Institution identifier before onboarding work begins.</param>
    /// <param name="emailService">Sends the established Institution welcome template.</param>
    /// <param name="configuration">Provides the configured frontend base URL.</param>
    public ResendInstitutionVerificationCommandHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService,
        ITokenService tokenService,
        IIdEncoderService idEncoderService,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
        _tokenService = tokenService;
        _idEncoderService = idEncoderService;
        _emailService = emailService;
        _configuration = configuration;
    }

    #endregion

    #region Handle

    /// <summary>
    /// Resends the existing onboarding welcome email only when the Institution is not verified.
    /// </summary>
    /// <param name="request">The resend-verification command.</param>
    /// <param name="cancellationToken">The token used to observe cancellation.</param>
    /// <returns>A successful response when the welcome email was sent.</returns>
    /// <exception cref="ValidationErrorException">Thrown when the route identifier is invalid.</exception>
    /// <exception cref="NotFoundException">Thrown when the Institution or its legitimate onboarding credential is unavailable.</exception>
    /// <exception cref="ConflictException">Thrown when the Institution is already verified.</exception>
    public async Task<ApiResponse<bool>> Handle(
        ResendInstitutionVerificationCommand request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        var hostContext = await _commonRequestService.ValidateHostUserPermissionRequestAsync();
        var institutionId = HostInstitutionIdentifierProtector.Decrypt(
            request.EncryptedInstitutionId,
            hostContext.InstitutionEncryptionKey,
            _idEncoderService);

        var institution = await _unitOfWork.InstitutionRepository
            .GetHostManagedInstitutionByIdAsync(institutionId, cancellationToken);

        if (institution is null)
        {
            throw new NotFoundException(AppConstants.ErrorMessages.ResourceNotFound);
        }

        var institutionLoginCredential = InstitutionOnboardingVerification.FindCredential(institution);

        // The Host list and resend action must use the exact same definition.
        // Older records can have IsOnboard persisted while Institution.IsVerified was
        // not updated by the legacy verification handler.
        if (InstitutionOnboardingVerification.IsVerified(institution, institutionLoginCredential))
        {
            throw new ConflictException(AppConstants.ErrorMessages.InstitutionAlreadyVerified);
        }

        if (institutionLoginCredential is null)
        {
            throw new NotFoundException(AppConstants.ErrorMessages.InstitutionOnboardingCredentialNotFound);
        }

        var onboardingStaffMember = institution.StaffMember
            .FirstOrDefault(staffMember => staffMember.Id == institutionLoginCredential.StaffMemberId);

        if (onboardingStaffMember is null)
        {
            throw new NotFoundException(AppConstants.ErrorMessages.InstitutionOnboardingStaffMemberNotFound);
        }

        // Reuse the initial Institution set-password token format, expiration, URL, and welcome template.
        var tokenInfo = new GetTokenInfoDTO
        {
            StaffMemberId = _idEncoderService.EncodeId_long(institutionLoginCredential.StaffMemberId, null),
            InstitutionId = _idEncoderService.EncodeId_long(institution.Id, null),
            Email = institution.InstitutionEmail,
            FullName = onboardingStaffMember.FirstName ?? institution.ContactPersonName ?? string.Empty,
            TokenPurpose = _idEncoderService.EncodeId_int(ConstantValues.SetPassword, string.Empty),
            IssuedAt = DateTime.UtcNow,
            Expiry = DateTime.UtcNow.AddMinutes(30),
            IsFirstLogin = true,
            ClientType = "Web"
        };

        var token = await _tokenService.GenerateInstitutionToken(tokenInfo);
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ApiException(
                AppConstants.ErrorMessages.InternalServerError,
                (int)HttpStatusCode.InternalServerError);
        }

        var baseUrl = _configuration["FrontEndWebURL:BaseUrl"] ?? string.Empty;
        var emailSent = await _emailService.SendTemplatedEmailUsingHostConfigAsync(
            ConstantValues.WelcomeEmail,
            institution.InstitutionEmail,
            institution.Id,
            new Dictionary<string, string>
            {
                ["UserName"] = tokenInfo.FullName,
                ["InstitutionName"] = institution.CompanyName ?? string.Empty,
                ["SupportEmail"] = institution.InstitutionEmail,
                ["VerificationUrl"] = $"{baseUrl}/auth/set-password?token={token}",
                ["LinkExpiryMinutes"] = "30"
            });

        if (!emailSent)
        {
            throw new ApiException(
                AppConstants.ErrorMessages.InternalServerError,
                (int)HttpStatusCode.InternalServerError);
        }

        // A Institution which has not verified must remain in the pending-onboarding state.
        // This also repairs legacy records that were created with the opposite value.
        if (institutionLoginCredential.IsOnboard)
        {
            institutionLoginCredential.IsOnboard = false;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return ApiResponse<bool>.Success(
            true,
            AppConstants.SuccessMessages.InstitutionVerificationResentSuccessfully);
    }

    #endregion
}

#endregion

/// <summary>
/// Defines the single Host-visible verification state for a Institution onboarding
/// account. Legacy data may have only the onboarding-credential flag, whereas
/// new verification writes also persist <see cref="Institution.IsVerified"/>.
/// </summary>
public static class InstitutionOnboardingVerification
{
    /// <summary>Finds the non-deleted onboarding credential that belongs to the Institution email address.</summary>
    public static LoginCredential? FindCredential(Institution institution)
    {
        ArgumentNullException.ThrowIfNull(institution);

        return institution.StaffMember
            .Where(staffMember => staffMember.InstitutionId == institution.Id && !staffMember.IsSoftDeleted)
            .SelectMany(staffMember => staffMember.LoginCredential)
            .Where(credential =>
                credential.InstitutionId == institution.Id &&
                string.Equals(credential.LoginId, institution.InstitutionEmail, StringComparison.OrdinalIgnoreCase) &&
                credential.IsSoftDeleted != true)
            .OrderBy(credential => credential.Id)
            .FirstOrDefault();
    }

    /// <summary>
    /// Treats either the canonical Institution flag or a completed legacy onboarding
    /// credential as verified. This prevents a verified Institution from receiving a
    /// second verification email while legacy data is being normalized.
    /// </summary>
    public static bool IsVerified(Institution institution, LoginCredential? credential)
    {
        ArgumentNullException.ThrowIfNull(institution);
        return institution.IsVerified || credential?.IsOnboard == true;
    }
}
