using skolenza.application.Interfaces.IHashed;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;


namespace skolenza.infrastructure.Security.HashedService
{
    public class PasswordService : IPasswordService
    {
        private readonly PasswordHasher<string> _hasher = new();
        private readonly ILogger<PasswordService> _logger;

        public PasswordService(ILogger<PasswordService> logger)
        {
            _logger = logger;
        }

        public string HashPassword(string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    _logger.LogWarning("⚠️ Attempted to hash an empty or null password.");
                    throw new ArgumentException("Password cannot be empty.");
                }

                var hashed = _hasher.HashPassword(null, password);
                _logger.LogInformation("✅ Password hashed successfully.");

             bool test=   VerifyPassword(hashed, password);
            

                _logger.LogInformation(test
                    ? "✅ Password verified successfully."
                    : "❌ Password verification failed.");

                return hashed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error occurred while hashing password.");
                throw;
            }
        }


        public bool VerifyPassword(string hashedPassword, string enteredPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(hashedPassword) || string.IsNullOrWhiteSpace(enteredPassword))
                {
                    _logger.LogWarning("⚠️ Either hashedPassword or enteredPassword is null/empty during verification.");
                    return false;
                }

                // Non-standard format check
                if (!hashedPassword.StartsWith("AQAAAA"))
                {
                    _logger.LogWarning("⚠️ Non-standard password format detected. Using plain comparison fallback.");
                    return hashedPassword == enteredPassword;
                }

                var result = _hasher.VerifyHashedPassword(null, hashedPassword, enteredPassword);
                bool isValid = result == PasswordVerificationResult.Success;

                _logger.LogInformation(isValid
                    ? "✅ Password verified successfully."
                    : "❌ Password verification failed.");

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unexpected error occurred while verifying password.");
                return false;
            }
        }


    }
}