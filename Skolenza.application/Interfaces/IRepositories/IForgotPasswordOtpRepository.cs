using System.Threading.Tasks; using skolenza.domain.Entity; using MediatR;
 // ← jahan ForgotPasswordOtp entity rakhi hai

namespace skolenza.application.Interfaces.IRepositories
{
    public interface IForgotPasswordOtpRepository
    {
        Task<ForgotPasswordOtpdetail?> GetValidOtpByStaffMemberIdAsync(long staffMemberId, long? InstitutionId);
        Task<ForgotPasswordOtpdetail?> GetOtpValidateTrueAndUsedFalseByStaffMemberIdAsync(long staffMemberId, long? InstitutionId);


        Task<long> AddAsync(ForgotPasswordOtpdetail otp);
        Task DeleteAsync(ForgotPasswordOtpdetail otp);

        Task<ForgotPasswordOtpdetail?> GetByOtpAndStaffMemberIdAsync(string otp, long staffMemberId);

        Task<bool> UpdateOTPAsync(ForgotPasswordOtpdetail otp); // ✅ New method
    }
}
