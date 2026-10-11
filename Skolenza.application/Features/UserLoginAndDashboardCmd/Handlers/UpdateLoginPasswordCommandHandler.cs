// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Handles UpdateLoginPasswordCommandHandler requests using authenticated institution context.
// ================================================================

using AutoMapper;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.Constants;
using skolenza.application.DTOs.UserLogin;
using skolenza.application.DTOS.UserLogin;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.ICommonRequest;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IFileStorage;
using skolenza.application.Interfaces.IHashed;
using skolenza.application.Interfaces.IPermission;
using skolenza.application.Interfaces.ITokenService;
using skolenza.application.Wrappers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.UserLoginAndDashboardCmd.Handlers
{

    #region Command

    /// <summary>
    /// Represents the UpdateLoginPasswordCommand application component.
    /// </summary>
    public class UpdateLoginPasswordCommand : IRequest<ApiResponse<UpdatePasswordResponseDTO>>
    {
        public UpdatePasswordRequestDTO? DTO { get; set; }


        public UpdateLoginPasswordCommand(UpdatePasswordRequestDTO? setLoginPasswordRequest)
        {
            this.DTO = setLoginPasswordRequest;
        }



    }
    /// <summary>
    /// Handles authenticated institution requests for this feature.
    /// </summary>
        #endregion

    #region Handler

public class UpdateLoginPasswordCommandHandler : IRequestHandler<UpdateLoginPasswordCommand, ApiResponse<UpdatePasswordResponseDTO>>
{
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UpdateLoginPasswordCommandHandler> _logger;
        private readonly ITokenService _tokenService;
        private readonly IPermissionService _permissionService;
        private readonly IConfiguration _config;
        private readonly IEncryptionService _encryptionService;
        private readonly IIdEncoderService _idEncoderService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ICommonRequestService _commonRequestService;
        private readonly IPasswordService _passwordService;

        public UpdateLoginPasswordCommandHandler(
           IUnitOfWork unitOfWork,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            ILogger<UpdateLoginPasswordCommandHandler> logger,
            ITokenService tokenService,
            IPermissionService permissionService,
            IConfiguration config,
            IEncryptionService encryptionService, IIdEncoderService idEncoderService, ICommonRequestService commonRequestService,
            IPasswordService passwordService, IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _tokenService = tokenService;
            _permissionService = permissionService;
            _config = config;
            _encryptionService = encryptionService;
            _idEncoderService = idEncoderService;
            _commonRequestService = commonRequestService;
            _passwordService = passwordService;
            _fileStorageService = fileStorageService;
        }

        public async Task<ApiResponse<UpdatePasswordResponseDTO>> Handle(UpdateLoginPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
                await _unitOfWork.BeginTransactionAsync();

                // 1️ COMMON VALIDATION (Mandatory)
                #region Institution Request Validation
                var validation = await _commonRequestService.ValidateInstitutionUserRequestAsync();
                #endregion

                if (!validation.Success)
                    throw new UnauthorizedAccessException(validation.ErrorMessage);

                var targetStaffMemberId = RequestCommonHelper.DecodeOnlyStaffMemberId(
                    request.DTO.StaffMemberId,
                    validation.Claims.InstitutionEncriptionKey,
                    _idEncoderService);

                //  Authenticate user
                var user = await _unitOfWork.UserLoginRepository.AuthenticateUser(request.DTO.LoginId);

                if (user == null || string.IsNullOrWhiteSpace(user.Password))
                {
                    throw new UnauthorizedAccessException(ConstantValues.invalidCredential);
                }

                //  Verify password
                if (!_passwordService.VerifyPassword(user.Password, request.DTO.OldPassword))
                {
                    throw new UnauthorizedAccessException(ConstantValues.invalidCredential);
                }

                // Inside Handle() method
                string? hashedPassword = _passwordService.HashPassword(request.DTO.NewPassword);
                if(string.IsNullOrEmpty(hashedPassword))
                {
                    _logger.LogWarning("Password hashing failed for LoginId: {LoginId}", request.DTO.LoginId);
                    return new ApiResponse<UpdatePasswordResponseDTO>
                    {
                        IsSucceeded = false,
                        Message = "An error occurred while processing your request. Please try again.",
                        Data = null
                    };
                }
                hashedPassword = hashedPassword.Trim();



                // Just in case StaffMemberId not mapped from request, ensure it is set


            bool isUpdated = await _unitOfWork.UserLoginRepository.UpdatePassword(
                targetStaffMemberId,
                hashedPassword,
                validation.LoggedInStaffMemberId);

                if (!isUpdated)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw new InvalidOperationException("The password update did not complete.");
                }

                //  COMMIT HERE
                await _unitOfWork.CommitTransactionAsync();

                return ApiResponse<UpdatePasswordResponseDTO>.Success(
                    new UpdatePasswordResponseDTO { Success = true },
                    "Password has been set successfully."
                );



        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in SetLoginPasswordCommandHandler.Handle.");

            await _unitOfWork.RollbackTransactionAsync();

            return new ApiResponse<UpdatePasswordResponseDTO>
            {
                IsSucceeded = false,
                Message = "An error occurred while setting the password. Please try again later.",
                Data = null
            };
        }
    }
}

    #endregion
}
