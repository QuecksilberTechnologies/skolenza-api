using AutoMapper;
using skolenza.application.DTOs.UserLogin;
using skolenza.application.Features.UserLoginAndDashboardCmd.Commands;
using skolenza.application.Interfaces;
using skolenza.application.Interfaces.IEmail;
using skolenza.application.Wrappers;
using skolenza.domain.Entity;
using MediatR;
using Microsoft.Extensions.Logging;

namespace skolenza.application.Features.UserLoginAndDashboardCmd.Handlers
{
    //public class ResetLoginPasswordByAdminCommandHandler : IRequestHandler<ForgotPasswordCommand, ApiResponse<UpdatePasswordResponseDTO>>
    //{
    //    private readonly IMapper _mapper;
    //    private readonly IUnitOfWork _unitOfWork;
    //    private readonly IEmailService _emailService;
    //    private readonly ILogger<ResetLoginPasswordByAdminCommandHandler> _logger;

    //    public ResetLoginPasswordByAdminCommandHandler(
    //        IMapper mapper,
    //        IUnitOfWork unitOfWork,

    //        ILogger<ResetLoginPasswordByAdminCommandHandler> logger, IEmailService emailService)
    //    {
    //        _logger = logger;
    //        _mapper = mapper;
    //        _unitOfWork = unitOfWork;
    //        _emailService = emailService;

    //    }

        //public async Task<ApiResponse<UpdatePasswordResponseDTO>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
        //{
        //    try
        //    {
        //        var loginId = request.dto.LoginId;

        //        long empId = await _unitOfWork.CommonRepository.ValidateActiveUserLoginOnlyAsync(loginId);
        //        _logger.LogInformation("Validation result for LoginId {LoginId}: {EmpId}", loginId, empId);

        //        if (empId < 1)
        //        {
        //            _logger.LogWarning("User not found or not authorized.");
        //            await _unitOfWork.RollbackTransactionAsync();
        //            return ApiResponse<UpdatePasswordResponseDTO>.Fail("User is not authenticated or authorized to perform this action.");
        //        }

        //        long userId = await _unitOfWork.CommonRepository.ValidateActiveUserCredentialOnlyAsync(loginId);
        //        var empInfo = await _unitOfWork.StaffMembers.GetSingleRecordAsync(empId, true); ;

        //        // 🔍 Step 2: Get OTP entry (validated and unused)
        //        var existingOtpEntry = await _unitOfWork.ForgotPasswordOtpRepository
        //            .GetOtpValidateTrueAndUsedFalseByStaffMemberIdAsync(userId, empInfo.InstitutionId);

        //        if (existingOtpEntry == null)
        //        {
        //            return ApiResponse<UpdatePasswordResponseDTO>.Fail("No OTP found or it has expired.");
        //        }

        //        // 🔄 Step 3: Match OTP
        //        if (existingOtpEntry.Otp != request.dto.Otp)
        //        {
        //            return ApiResponse<UpdatePasswordResponseDTO>.Fail("Invalid OTP entered.");
        //        }

        //        // 🔄 Step 4: Check expiry
        //        if (existingOtpEntry.OtpexpireDateTime <= DateTime.Now)
        //        {
        //            return ApiResponse<UpdatePasswordResponseDTO>.Fail("OTP has expired. Please request a new one.");
        //        }

        //        // 🔐 Step 5: Confirm OTP is validated and not used
        //        if (!existingOtpEntry.IsValidate || existingOtpEntry.IsUsed)
        //        {
        //            return ApiResponse<UpdatePasswordResponseDTO>.Fail("OTP is either not validated or already used.");
        //        }

        //        // ✅ Step 6: Set new password
        //        var loginCredential = new LoginCredential
        //        {
        //            StaffMemberId = empId,
        //            InstitutionId = empInfo.InstitutionId,
        //            LoginId = request.dto.LoginId,
        //            Password = request.dto.Password
        //        };

        //        bool isUpdated = await _unitOfWork.UserLoginRepository.SetNewPassword(loginCredential);

        //        if (!isUpdated)
        //        {
        //            _logger.LogWarning("Password update failed for LoginId: {LoginId}", loginId);
        //            await _unitOfWork.RollbackTransactionAsync();

        //            return ApiResponse<UpdatePasswordResponseDTO>.Fail("Password could not be updated. Please try again.");
        //        }

        //        // 🔄 Step 7: Mark OTP as used
        //        existingOtpEntry.IsUsed = true;
        //        existingOtpEntry.UsedDateTime = DateTime.Now;
        //        await _unitOfWork.ForgotPasswordOtpRepository.UpdateOTPAsync(existingOtpEntry);


        //        var response = new UpdatePasswordResponseDTO
        //        {
        //            Success = true
        //        };

        //        return ApiResponse<UpdatePasswordResponseDTO>.Success(response);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "An error occurred in SetNewLoginPasswordCommandHandler.Handle.");
        //        await _unitOfWork.RollbackTransactionAsync();

        //        return ApiResponse<UpdatePasswordResponseDTO>.Fail("An error occurred while setting the password. Please try again later.");
        //    }
        //}

   // }

}