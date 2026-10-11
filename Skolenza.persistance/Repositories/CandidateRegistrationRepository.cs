using skolenza.application.DTOs.Registration;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace skolenza.persistance.Repositories
{
    public class CandidateRegistrationRepository : ICandidateRegistrationRepository
    {
        private SkolenzaDbContext _context;
        private ILogger _logger;

         
        public CandidateRegistrationRepository(SkolenzaDbContext? context, ILogger<CandidateRegistrationRepository>? logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<long> AddCandidateAsync(Candidate candidate)
        {
            try
            {
                if (candidate == null)
                    throw new ArgumentNullException(nameof(candidate), "Candidate data cannot be null.");

                await _context.Candidates.AddAsync(candidate);

                // Save changes and get auto-generated ID
                await _context.SaveChangesAsync();

                _logger?.LogInformation($"New candidate added successfully with ID: {candidate.Id}");

                return candidate.Id; // Return the generated Candidate ID
            }
            catch (ArgumentNullException ex)
            {
                _logger?.LogError(ex, "Null input encountered in AddCandidateAsync.");
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while adding a new candidate.");
                throw new ApplicationException("An unexpected error occurred. Please try again later.", ex);
            }
        }

      
        public Task<bool> DeleteCandidateAsync(int candidateId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<CandidateResponseDTO>> GetAllCandidatesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<CandidateResponseDTO> GetCandidateByIdAsync(int candidateId)
        {
            throw new NotImplementedException();
        }

        public async Task<long> GetCandidateIdAsync(CandidateRequestDTO request)
        {
            try
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request), "Request cannot be null.");

                // Query to find candidate ID by phone, email, PAN, or Aadhaar
                var candidate = await _context.Candidates
                    .Where(c =>
                        c.Email == request.Email ||
                        c.PhoneNumber == request.PhoneNumber ||
                        (c.Pan != null && c.Pan == request.Pan) ||
                        (c.Aadhaar != null && c.Aadhaar == request.Aadhaar))
                    .Select(c => c.Id)
                    .FirstOrDefaultAsync();

                return candidate; // Returns candidate ID or 0 if not found
            }
            catch (ArgumentNullException ex)
            {
                _logger?.LogError(ex, "Null input encountered in GetCandidateIdAsync.");
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while fetching candidate ID.");
                throw new ApplicationException("An unexpected error occurred. Please try again later.", ex);
            }
        }


        public async Task<bool> IsCandidateBlackListedAsync(CandidateRequestDTO request)
        {
            try
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request), "Request cannot be null.");

                // Check if the candidate is blacklisted based on Email, PhoneNumber, PAN, or Aadhaar
                var isBlacklisted = await _context.Candidates.AnyAsync(c =>
                    (c.Email == request.Email ||
                     c.PhoneNumber == request.PhoneNumber ||
                     (c.Pan != null && c.Pan == request.Pan) ||
                     (c.Aadhaar != null && c.Aadhaar == request.Aadhaar))
                    && c.IsBlacklisted);

                return isBlacklisted;
            }
            catch (ArgumentNullException ex)
            {
                _logger?.LogError(ex, "Null input encountered in IsCandidateBlackListedAsync.");
                throw; // Rethrow to notify the caller
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while checking if the candidate is blacklisted.");
                throw new ApplicationException("An unexpected error occurred. Please try again later.", ex);
            }
        }



        public async Task<bool> IsEmailPANAdharPhoneExistsAsync(CandidateRequestDTO request)
        {
            try
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request), "Request cannot be null.");

                // Duplicate check (Blacklist ko ignore karte hue)
                var isDuplicate = await _context.Candidates.AnyAsync(c =>
                    c.Email == request.Email ||
                    c.PhoneNumber == request.PhoneNumber ||
                    (c.Pan != null && c.Pan == request.Pan) ||
                    (c.Aadhaar != null && c.Aadhaar == request.Aadhaar)
                );

                if (isDuplicate)
                {
                    return true; // Duplicate record exists, blacklist check nahi karna
                }
                else
                    return false;


                // Blacklist check only if no duplicate found
                //var isBlacklisted = await _context.Candidates.AnyAsync(c =>
                //    (c.Email == request.Email ||
                //    c.PhoneNumber == request.PhoneNumber ||
                //    (c.Pan != null && c.Pan == request.Pan) ||
                //    (c.Aadhaar != null && c.Aadhaar == request.Aadhaar))
                //    && c.IsBlacklisted
                //);

                //return isBlacklisted; // Blacklisted record exists or not
            }
            catch (ArgumentNullException ex)
            {
                _logger?.LogError(ex, "Null input encountered in IsEmailPANAdharPhoneExistsAsync.");
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while checking for duplicate candidate data.");
                throw new ApplicationException("An unexpected error occurred. Please try again later.", ex);
            }
        }

        public Task<bool> UpdateCandidateAsync(CandidateResponseDTO candidate)
        {
            throw new NotImplementedException();
        }
    }
}
