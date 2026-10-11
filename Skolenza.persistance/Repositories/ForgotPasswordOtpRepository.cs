using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace skolenza.persistance.Repositories
{
    class ForgotPasswordOtpRepository : IForgotPasswordOtpRepository
    {
        private readonly SkolenzaDbContext _context;
        private ILogger<ForgotPasswordOtpRepository> _logger;
        public ForgotPasswordOtpRepository(SkolenzaDbContext context, ILogger<ForgotPasswordOtpRepository> logger)
        {
            this._context = context;
            this._logger = logger;
        }

        public async Task<ForgotPasswordOtpdetail?> GetOtpValidateTrueAndUsedFalseByStaffMemberIdAsync(long userId, long? institutionId)
        {
            try
            {
                var otpDetail = await _context.ForgotPasswordOtpdetails
                    .Where(x => x.UserId == userId
                                && x.InstitutionId == institutionId
                                && x.IsUsed == false
                                && x.IsValidate == true
                                && x.OtpexpireDateTime > DateTime.Now)
                    .OrderByDescending(x => x.CreatedDateTime)
                    .FirstOrDefaultAsync();

                if (otpDetail == null)
                {
                    _logger.LogInformation("No valid OTP found for UserId: {UserId}, InstitutionId: {InstitutionId}", userId, institutionId);
                }
                else
                {
                    _logger.LogInformation("Valid OTP retrieved for UserId: {UserId}, OTP ID: {OtpId}", userId, otpDetail.Id);
                }

                return otpDetail;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching OTP for UserId: {UserId}, InstitutionId: {InstitutionId}", userId, institutionId);
                return null;
            }
        }

        public async Task<ForgotPasswordOtpdetail?> GetValidOtpByStaffMemberIdAsync(long userId, long? institutionId)
        {
            try
            {
                var otpDetail = await _context.ForgotPasswordOtpdetails
                    .Where(x => x.UserId == userId
                                && x.InstitutionId == institutionId
                                && !x.IsUsed
                                && x.IsValidate == false
                                && x.OtpexpireDateTime > DateTime.Now)
                    .OrderByDescending(x => x.CreatedDateTime)
                    .FirstOrDefaultAsync();

                if (otpDetail == null)
                {
                    _logger.LogInformation("No valid OTP found for UserId: {UserId}, InstitutionId: {InstitutionId}", userId, institutionId);
                }
                else
                {
                    _logger.LogInformation("Valid OTP retrieved for UserId: {UserId}, OTP ID: {OtpId}", userId, otpDetail.Id);
                }

                return otpDetail;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching OTP for UserId: {UserId}, InstitutionId: {InstitutionId}", userId, institutionId);
                return null;
            }
        }

        public async Task<bool> UpdateOTPAsync(ForgotPasswordOtpdetail otp)
        {
            try
            {

                _context.ForgotPasswordOtpdetails.Update(otp);
                await _context.SaveChangesAsync();
                _logger.LogInformation("OTP updated successfully for StaffMemberId {EmpId}", otp.StaffMemberId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating OTP for StaffMemberId {EmpId}", otp.StaffMemberId);
                return false;
            }
        }

        public async Task<long> AddAsync(ForgotPasswordOtpdetail otp)
        {
            try
            {
                await _context.ForgotPasswordOtpdetails.AddAsync(otp);
                await _context.SaveChangesAsync();

                // Return the inserted OTP record's Id (assuming Id is auto-incremented primary key)
                return otp.Id;
            }
            catch (Exception ex)
            {
                // Log the exception with context (adjust logger as per your project structure)
                _logger.LogError(ex, "Error occurred while adding ForgotPasswordOTPDetail for StaffMemberId: {StaffMemberId}", otp.StaffMemberId);

                // Re-throw or handle gracefully
                throw;
            }
        }

        public async Task DeleteAsync(ForgotPasswordOtpdetail otp)
        {
            _context.ForgotPasswordOtpdetails.Remove(otp);
        }

        public async Task<ForgotPasswordOtpdetail?> GetByOtpAndStaffMemberIdAsync(string otp, long staffMemberId)
        {
            return await _context.ForgotPasswordOtpdetails
                .FirstOrDefaultAsync(x => x.Otp == otp && x.StaffMemberId == staffMemberId && !x.IsUsed);
        }


    }
}