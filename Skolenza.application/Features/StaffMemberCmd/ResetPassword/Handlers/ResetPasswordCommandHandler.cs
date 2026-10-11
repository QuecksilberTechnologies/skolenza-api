// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Resets a selected Institution StaffMember password through the existing
//           credential repository and password-hashing service.
// ================================================================

using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.Constants;
using skolenza.application.DTOS.Staff.ResetPassword;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IHashed;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.StaffMemberCmd.ResetPassword.Handlers;

/// <summary>
/// Represents the authorized request to reset a selected Institution StaffMember
/// password.
/// </summary>
public sealed class ResetPasswordCommand
    : IRequest<ApiResponse<bool>>
{
    /// <summary>
    /// Gets the administrator-supplied password reset details.
    /// </summary>
    public ResetStaffMemberPasswordRequestDTO DTO { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResetPasswordCommand"/> class.
    /// </summary>
    /// <param name="dto">The password-reset details.</param>
    public ResetPasswordCommand(ResetStaffMemberPasswordRequestDTO dto)
    {
        DTO = dto;
    }
}

/// <summary>
/// Handles authorized Institution StaffMember password resets using the existing
/// <see cref="IPasswordService"/> and credential repository flow.
/// </summary>
public sealed class ResetPasswordCommandHandler
    : IRequestHandler<ResetPasswordCommand, ApiResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommonRequestService _commonRequestService;
    private readonly IIdEncoderService _idEncoderService;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResetPasswordCommandHandler"/> class.
    /// </summary>
    public ResetPasswordCommandHandler(
        IUnitOfWork unitOfWork,
        ICommonRequestService commonRequestService,
        IIdEncoderService idEncoderService,
        IPasswordService passwordService,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _commonRequestService = commonRequestService;
        _idEncoderService = idEncoderService;
        _passwordService = passwordService;
        _logger = logger;
    }

    /// <summary>
    /// Resets the active credential password for an StaffMember who belongs to
    /// the authenticated Institution.
    /// </summary>
    public async Task<ApiResponse<bool>> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        if (request?.DTO is null ||
            string.IsNullOrWhiteSpace(request.DTO.StaffMemberId) ||
            string.IsNullOrWhiteSpace(request.DTO.NewPassword) ||
            string.IsNullOrWhiteSpace(request.DTO.ConfirmPassword))
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.RequiredDataMissing);
        }

        if (!string.Equals(
                request.DTO.NewPassword,
                request.DTO.ConfirmPassword,
                StringComparison.Ordinal))
        {
            throw new ValidationErrorException("New password and confirm password do not match.");
        }

        // The MediatR behavior performs the module-code and stored-procedure
        // permission check. This validation supplies the trusted Institution key
        // required to decode the target StaffMember identifier.
        var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
        if (!validation.Success)
        {
            throw new UnauthorizedAccessException(
                validation.ErrorMessage ?? AppConstants.ErrorMessages.Unauthorized);
        }

        var staffMemberId = RequestCommonHelper.DecodeOnlyStaffMemberId(
            request.DTO.StaffMemberId,
            validation.Claims.InstitutionEncriptionKey,
            _idEncoderService);

        if (staffMemberId <= 0)
        {
            throw new ValidationErrorException(AppConstants.ErrorMessages.InvalidIdentifier);
        }

        // Verify Institution ownership before modifying the existing active
        // LoginCredential selected by the repository.
        var staffMember = await _unitOfWork.StaffMembers.GetByIdAsync(
            staffMemberId,
            validation.InstitutionId,
            true);
        if (staffMember is null)
        {
            throw new ApiException(
                AppConstants.ErrorMessages.ResourceNotFound,
                StatusCodes.Status404NotFound);
        }

        var passwordHash = _passwordService.HashPassword(request.DTO.NewPassword);
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            _logger.LogError(
                "Password hashing failed while resetting the password for StaffMemberId {StaffMemberId}.",
                staffMemberId);
            throw new ApiException(
                AppConstants.ErrorMessages.InternalServerError,
                StatusCodes.Status500InternalServerError);
        }

        var isUpdated = await _unitOfWork.UserLoginRepository.UpdatePassword(
            staffMemberId,
            passwordHash.Trim(),
            validation.LoggedInStaffMemberId);
        if (!isUpdated)
        {
            throw new ApiException(
                AppConstants.ErrorMessages.ResourceNotFound,
                StatusCodes.Status404NotFound);
        }

        _logger.LogInformation(
            "Reset password completed for StaffMemberId {StaffMemberId} by StaffMemberId {ActorStaffMemberId}.",
            staffMemberId,
            validation.LoggedInStaffMemberId);

        return ApiResponse<bool>.Success(
            true,
            "StaffMember password reset successfully.");
    }
}
