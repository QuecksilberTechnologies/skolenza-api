// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Defines and handles the request to Forgot Password.
// ================================================================

using skolenza.application.DTOs.UserLogin;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using skolenza.application.Common.Helpers;
using skolenza.application.Constants;
using skolenza.application.Features.UserLoginAndDashboardCmd.Commands;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IEmail;
using skolenza.application.Interfaces.ITokenService;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.UserLoginAndDashboardCmd.Commands
{
    #region Command

    /// <summary>
    /// Represents the request to Forgot Password.
    /// </summary>
public class ForgotPasswordCommand : IRequest<ApiResponse<ForgotPasswordResponseDTO>>
    {
        public ForgotPasswordUserIdRequestDTO dTO { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="ForgotPasswordCommand"/> class.
        /// </summary>


        public ForgotPasswordCommand(ForgotPasswordUserIdRequestDTO dto)
        {
            dTO = dto;
        }



    }

    #endregion
}

namespace skolenza.application.Features.UserLoginAndDashboardCmd.Handlers
{
    /// <summary>
    /// Handles the request to Forgot Password.
    /// </summary>
public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ApiResponse<ForgotPasswordResponseDTO>>
    {
        #region Fields

        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ILogger<ForgotPasswordCommandHandler> _logger;
        private readonly IEmailService _emailService;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="ForgotPasswordCommandHandler"/> class.
        /// </summary>

        public ForgotPasswordCommandHandler(IMapper mapper, IUnitOfWork unitOfWork, ITokenService tokenService, IRefreshTokenRepository refreshTokenRepository, ILogger<ForgotPasswordCommandHandler> logger, IEmailService emailService)
        {
            _logger = logger;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _tokenService = tokenService;
            _refreshTokenRepository = refreshTokenRepository;
            _emailService = emailService;
            
        }
        #endregion

        #region Handler
        /// <summary>
        /// Processes the supplied ForgotPasswordCommand.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The response produced for the request.</returns>

      
        public async Task<ApiResponse<ForgotPasswordResponseDTO>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 🔐 Step 1: Validate if user exists
                long empId = await _unitOfWork.StoreProcedureRepository.ValidateActiveUserLoginOnlyAsync(request.dTO.LoginId);
                _logger.LogInformation("Validation result for LoginId {LoginId}: StaffMemberId = {empId}", request.dTO.LoginId, empId);

                if (empId < 1)
                {
                    _logger.LogWarning("User validation failed for LoginId: {LoginId}", request.dTO.LoginId);
                    await _unitOfWork.RollbackTransactionAsync();
                    throw new UnauthorizedAccessException(
                        skolenza.application.Constants.AppConstants.ErrorMessages.Unauthorized);
                }
                long userId = await _unitOfWork.StoreProcedureRepository.ValidateActiveUserCredentialOnlyAsync(request.dTO.LoginId);                

                var empInfo = await _unitOfWork.StaffMembers.GetSingleRecordAsync(empId,true);;



                // 🔍 Check Existing OTP

                var existingOtpEntry = await _unitOfWork.ForgotPasswordOtpRepository.GetValidOtpByStaffMemberIdAsync(userId, empInfo.InstitutionId);
                 
                var now = DateTime.Now;

                if (existingOtpEntry != null)
                {
                    var resendWaitTime = existingOtpEntry.CreatedDateTime.AddMinutes(2);

                    // OTP already sent within 2 minutes
                    if (existingOtpEntry.CreatedDateTime.AddMinutes(2) > now)
                    {
                        var responseApi = new ForgotPasswordResponseDTO
                        {
                            EmailResendAfterMinute = 2,
                            Message = "OTP already sent. Please wait 2 minutes without refreshing page."
                        };


                        return ApiResponse<ForgotPasswordResponseDTO>.Success(responseApi);
                    }

                    // ✅ 2. Delete expired OTP if 2 minutes have passed
                    if (existingOtpEntry.OtpexpireDateTime <= now || resendWaitTime <= now)
                    {
                        await _unitOfWork.ForgotPasswordOtpRepository.DeleteAsync(existingOtpEntry);
                    }
                }

                // ✅ Get StaffMember Info
                if (empInfo == null)
                {
                    _logger.LogWarning("StaffMember info not found for StaffMemberId: {empId}", empId);
                    await _unitOfWork.RollbackTransactionAsync();
                    throw new skolenza.application.Exceptions.NotFoundException(
                        skolenza.application.Constants.AppConstants.ErrorMessages.ResourceNotFound);
                }

                string Otp = OtpHelper.Generate6CharAlphanumericOtp();
                // 🔐 OTP Generate
                //string otp = "A11111";

                // 🧍‍♂️ Full Name build
                string UserName = $"{empInfo.FirstName} {(empInfo.MiddleName ?? "")} {empInfo.LastName}".Trim();
                long? InstitutionId = empInfo.InstitutionId;

                // 📩 Get Template from DB
                var emailTemplate = await _unitOfWork.EmailTemplateRepository.GetTemplateByCodeAsync(ConstantValues.ForgotPasswordEmail);
                if (emailTemplate == null)
                {
                    _logger.LogError("Email template 'FORGOT_PASSWORD' not found.");
                    throw new InvalidOperationException("The OTP email template is not configured.");
                }

                // 🧩 Placeholder Dictionary
                var placeholders = new Dictionary<string, string>
                  {
                   { "UserName", UserName },
                      { "Otp", Otp } // use 'Otp' with capital 'O' if template has {{Otp}}
                   };

                // 🛠️ Render subject and body from template
                string subject = EmailTemplateRenderer.RenderBody(emailTemplate.Subject ?? "Verification Email", placeholders);
                string body = EmailTemplateRenderer.RenderBody(emailTemplate.Body ?? "", placeholders);

                // 📬 Send Email
                bool isSent = await _emailService.SendOtpEmailAsync(
                    toEmail: request.dTO.LoginId,
                    subject: subject,
                    body: body,
                    otp: Otp, // If your service needs it, else remove
                    InstitutionId: InstitutionId
                );

                if (!isSent)
                {
                    _logger.LogWarning("OTP email send failed to {Email}", request.dTO.LoginId);
                    throw new InvalidOperationException("The OTP email could not be sent.");
                }
                //   string body = EmailTemplateRenderer.RenderBody(emailTemplate.Body ?? "", placeholders);
                //   string subject = EmailTemplateRenderer.RenderBody(emailTemplate.Subject ?? "", placeholders);

                  
                var newOtpEntry = new ForgotPasswordOtpdetail
                {
                    StaffMemberId = empId,
                    Otp = Otp,
                    OtpexpireDateTime = DateTime.Now.AddMinutes(5),
                    InstitutionId = (long)InstitutionId,
                    CreatedDateTime = DateTime.Now,
                    IsUsed = false,
                    UserId = userId,

                };

               

                _logger.LogInformation("OTP sent successfully to {Email}", request.dTO.LoginId);


             
                

                // 📨 Step 2: Save to database and get inserted Id
                long otpId = await _unitOfWork.ForgotPasswordOtpRepository.AddAsync(newOtpEntry);



                var response = new ForgotPasswordResponseDTO
                {
                    Message = "OTP sent successfully.",
                    EmailResendAfterMinute = 0,
                   
                };

                return ApiResponse<ForgotPasswordResponseDTO>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in ForgotPassword Handler.");
                await _unitOfWork.RollbackTransactionAsync();

                throw;
            }
        }

      
    
        #endregion
}
}
