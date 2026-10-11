// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Provides Institution persistence operations and Host-managed Institution lifecycle staging.
// ================================================================

using skolenza.application.Interfaces.IRepositories;
using skolenza.application.DTOs.Institution;
using skolenza.application.DTOS.Pagination;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace skolenza.persistance.Repositories
{
    public class InstitutionRepository : IInstitutionRepository
    {
        private readonly SkolenzaDbContext _context;
        private readonly ILogger<InstitutionRepository> _logger;

        public InstitutionRepository(
            SkolenzaDbContext context,
            ILogger<InstitutionRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<Institution>> GetAllInstitutionBySubscriptionIdAsync(
            Institution institution,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (institution == null)
                {
                    _logger.LogWarning("Institution filter is null while fetching institutions.");
                    return new List<Institution>();
                }

                IQueryable<Institution> query = _context.Institutions
                    .AsNoTracking()
                    .Where(x => x.IsActive == true && x.IsSoftDeleted != true);

                if (!string.IsNullOrWhiteSpace(institution.CompanyName))
                    query = query.Where(x => x.CompanyName.Contains(institution.CompanyName));

                if (!string.IsNullOrWhiteSpace(institution.CompanyEmailDomain))
                    query = query.Where(x => x.CompanyEmailDomain.Contains(institution.CompanyEmailDomain));

                if (!string.IsNullOrWhiteSpace(institution.InstitutionEmail))
                    query = query.Where(x => x.InstitutionEmail.Contains(institution.InstitutionEmail));

                if (!string.IsNullOrWhiteSpace(institution.ContactPersonName))
                    query = query.Where(x => x.ContactPersonName.Contains(institution.ContactPersonName));

                if (!string.IsNullOrWhiteSpace(institution.ContactNumber))
                    query = query.Where(x => x.ContactNumber.Contains(institution.ContactNumber));

                if (!string.IsNullOrWhiteSpace(institution.InstitutionCode))
                    query = query.Where(x => x.InstitutionCode.Contains(institution.InstitutionCode));

                if (institution.CountryId > 0)
                    query = query.Where(x => x.CountryId == institution.CountryId);

                var result = await query.ToListAsync(cancellationToken);

                _logger.LogInformation("Fetched {Count} institutions matching the criteria.", result.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching institution list.");
                throw;
            }
        }

        public async Task AddInstitutionAsync(Institution institution, CancellationToken cancellationToken = default)
        {
            try
            {
                if (institution == null)
                {
                    _logger.LogWarning("Institution entity is null in AddInstitutionAsync.");
                    throw new ArgumentNullException(nameof(institution));
                }

                institution.IsActive = true;
                institution.AddedDateTime = DateTime.UtcNow;

                // IMPORTANT:
                // Do not set AddedById = institution.Id here because Id is not generated yet.
                // Set AddedById in handler if a valid creator id exists.

                await _context.Institutions.AddAsync(institution, cancellationToken);

                _logger.LogInformation("Institution added to DbContext successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding institution.");
                throw;
            }
        }

        public async Task AddInstitutionProfileAsync(InstitutionProfile institutionProfile, CancellationToken cancellationToken = default)
        {
            try
            {
                if (institutionProfile == null)
                {
                    _logger.LogWarning("InstitutionProfile entity is null in AddInstitutionProfileAsync.");
                    throw new ArgumentNullException(nameof(institutionProfile));
                }

                await _context.InstitutionProfiles.AddAsync(institutionProfile, cancellationToken);

                _logger.LogInformation("InstitutionProfile added to DbContext successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while adding institution profile.");
                throw;
            }
        }

        public async Task<bool> CheckInstitutionByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return false;
                }

                return await _context.Institutions
                    .AsNoTracking()
                    .AnyAsync(t => t.InstitutionEmail == email, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking institution email existence.");
                throw;
            }
        }

        public async Task DeleteInstitutionAsync(Institution institution, CancellationToken cancellationToken = default)
        {
            try
            {
                if (institution == null)
                {
                    throw new ArgumentNullException(nameof(institution));
                }

                _context.Institutions.Remove(institution);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting institution.");
                throw;
            }
        }

        public async Task<Institution?> GetByCodeAsync(string institutionCode, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(institutionCode))
                {
                    return null;
                }

                return await _context.Institutions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.InstitutionCode == institutionCode &&
                             x.IsActive == true &&
                             x.IsSoftDeleted != true,
                        cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting institution by code.");
                throw;
            }
        }

        public async Task<Institution?> GetByIdAsync(long? id, bool isActive)
        {
            try
            {
                if (!id.HasValue || id.Value <= 0)
                {
                    return null;
                }

                return await _context.Institutions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t =>
                        t.Id == id.Value &&
                        t.IsActive == isActive &&
                        t.IsSoftDeleted != true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting institution by id.");
                throw;
            }
        }

        public async Task<Institution?> UpdateInstitutionAsync(Institution institution, CancellationToken cancellationToken = default)
        {
            try
            {
                if (institution == null)
                {
                    throw new ArgumentNullException(nameof(institution));
                }

                var existingInstitution = await _context.Institutions
                    .FirstOrDefaultAsync(x => x.Id == institution.Id, cancellationToken);

                if (existingInstitution == null)
                {
                    _logger.LogWarning("Institution not found for update. InstitutionId: {InstitutionId}", institution.Id);
                    return null;
                }

                existingInstitution.IsActive = institution.IsActive;
                existingInstitution.IsVerified = institution.IsVerified;
                existingInstitution.UpdatedDateTime = DateTime.UtcNow;
                existingInstitution.UpdatedById = institution.UpdatedById;

                _context.Institutions.Update(existingInstitution);

                _logger.LogInformation("Institution updated in DbContext successfully. InstitutionId: {InstitutionId}", institution.Id);

                return existingInstitution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating institution. InstitutionId: {InstitutionId}", institution?.Id);
                throw;
            }
        }

        #region Host Management

        /// <summary>
        /// Retrieves a filtered, paged, non-soft-deleted Institution collection for Host-side management.
        /// </summary>
        /// <param name="request">The Host Institution list filters and paging values.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The requested page of Institution entities.</returns>
        public async Task<PagedResponseDTO<Institution>> GetHostManagedInstitutionsAsync(
            GetAllInstitutionsRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var pageNumber = request.PageNumber > 0 ? request.PageNumber : 1;
            var pageSize = request.PageSize is > 0 and <= 100 ? request.PageSize : 10;
            var query = _context.Institutions
                .AsNoTracking()
                .Include(institution => institution.StaffMember.Where(staffMember => !staffMember.IsSoftDeleted))
                    // Filtered Includes must translate independently of the outer staffMember lambda.
                    .ThenInclude(staffMember => staffMember.LoginCredential.Where(credential =>
                        credential.InstitutionId == credential.StaffMember.InstitutionId &&
                        credential.IsSoftDeleted != true))
                .Where(institution => institution.IsSoftDeleted != true);

            if (request.IsActive.HasValue)
            {
                query = query.Where(institution => institution.IsActive == request.IsActive.Value);
            }

            if (request.IsVerified.HasValue)
            {
                query = request.IsVerified.Value
                    ? query.Where(institution => institution.IsVerified || institution.StaffMember.Any(staffMember =>
                        !staffMember.IsSoftDeleted &&
                        staffMember.LoginCredential.Any(credential =>
                            credential.InstitutionId == institution.Id &&
                            credential.LoginId == institution.InstitutionEmail &&
                            credential.IsOnboard &&
                            credential.IsSoftDeleted != true)))
                    : query.Where(institution => !institution.IsVerified && !institution.StaffMember.Any(staffMember =>
                        !staffMember.IsSoftDeleted &&
                        staffMember.LoginCredential.Any(credential =>
                            credential.InstitutionId == institution.Id &&
                            credential.LoginId == institution.InstitutionEmail &&
                            credential.IsOnboard &&
                            credential.IsSoftDeleted != true)));
            }

            if (!string.IsNullOrWhiteSpace(request.SearchKeyword))
            {
                var searchPattern = $"%{request.SearchKeyword.Trim()}%";
                query = query.Where(institution =>
                    EF.Functions.ILike(institution.CompanyName, searchPattern) ||
                    (institution.InstitutionCode != null && EF.Functions.ILike(institution.InstitutionCode, searchPattern)) ||
                    EF.Functions.ILike(institution.CompanyEmailDomain, searchPattern) ||
                    EF.Functions.ILike(institution.InstitutionEmail, searchPattern) ||
                    (institution.ContactPersonName != null && EF.Functions.ILike(institution.ContactPersonName, searchPattern)) ||
                    (institution.ContactNumber != null && EF.Functions.ILike(institution.ContactNumber, searchPattern)));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var data = await query
                .OrderByDescending(institution => institution.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResponseDTO<Institution>(data, totalCount, pageNumber, pageSize)
            {
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        /// <summary>
        /// Retrieves a tracked, non-soft-deleted Institution for a Host-managed operation.
        /// </summary>
        /// <param name="institutionId">The authoritative Institution identifier.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>The Institution entity, or <see langword="null"/> when unavailable.</returns>
        public Task<Institution?> GetHostManagedInstitutionByIdAsync(long institutionId, CancellationToken cancellationToken = default)
        {
            return _context.Institutions
                .Include(institution => institution.StaffMember.Where(staffMember =>
                    staffMember.InstitutionId == institutionId &&
                    !staffMember.IsSoftDeleted))
                .ThenInclude(staffMember => staffMember.LoginCredential.Where(credential =>
                    credential.InstitutionId == institutionId &&
                    credential.IsSoftDeleted != true))
                .FirstOrDefaultAsync(
                    institution => institution.Id == institutionId && institution.IsSoftDeleted != true,
                    cancellationToken);
        }

        /// <summary>
        /// Retrieves a safe, Host-visible Institution detail projection for an active or inactive, non-soft-deleted Institution.
        /// Each related collection is independently limited to its live records; no credential secret, password, token,
        /// or encryption key is projected.
        /// </summary>
        public async Task<HostInstitutionDetailResponseDTO?> GetHostManagedInstitutionDetailAsync(
            long institutionId,
            CancellationToken cancellationToken = default)
        {
            var detail = await _context.Institutions
                .AsNoTracking()
                // Host users must be able to open an inactive Institution to inspect or reactivate it.
                // Soft-deleted Institutions remain unavailable everywhere in the Host management flow.
                .Where(institution => institution.Id == institutionId && institution.IsSoftDeleted != true)
                .Select(institution => new HostInstitutionDetailResponseDTO
                {
                    InstitutionTypeId = institution.InstitutionTypeId,
                    InstitutionTypeName = institution.InstitutionType.IsActive && institution.InstitutionType.IsSoftDeted != true
                        ? institution.InstitutionType.InstitutionTypeName
                        : null,
                    CompanyName = institution.CompanyName,
                    InstitutionCode = institution.InstitutionCode,
                    CompanyEmailDomain = institution.CompanyEmailDomain,
                    InstitutionEmail = institution.InstitutionEmail,
                    ContactPersonName = institution.ContactPersonName,
                    GenderId = institution.GenderId,
                    ContactNumber = institution.ContactNumber,
                    CountryId = institution.CountryId,
                    DefaultCurrency = institution.DefaultCurrency,
                    IsVerified = institution.IsVerified,
                    IsActive = institution.IsActive
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (detail is null)
            {
                return null;
            }

            detail.Profiles = await _context.InstitutionProfiles
                .AsNoTracking()
                .Where(profile => profile.InstitutionId == institutionId)
                .OrderBy(profile => profile.Id)
                .Select(profile => new HostInstitutionProfileDetailDTO
                {
                    Id = profile.Id,
                    LogoUrl = profile.LogoUrl,
                    ThemeColor = profile.ThemeColor,
                    BusinessType = profile.BusinessType,
                    Industry = profile.Industry,
                    TotalStaffMembers = profile.TotalStaffMembers,
                    TotalCampuses = profile.TotalCampuses,
                    FoundedYear = profile.FoundedYear,
                    WebsiteUrl = profile.WebsiteUrl
                })
                .ToListAsync(cancellationToken);

            detail.Campuses = await _context.Campuss
                .AsNoTracking()
                .Where(location =>
                    location.InstitutionId == institutionId &&
                    location.IsActive &&
                    !location.IsSoftDeleted)
                .OrderBy(location => location.LocationName)
                .ThenBy(location => location.Id)
                .Select(location => new HostCampusDetailDTO
                {
                    Id = location.Id,
                    LocationCode = location.LocationCode,
                    LocationName = location.LocationName,
                    LocationType = location.LocationType,
                    CountryId = location.CountryId,
                    StateId = location.StateId,
                    LocalityId = location.LocalityId,
                    DistrictId = location.DistrictId,
                    Address = location.Address,
                    Landmark = location.Landmark,
                    PostalCode = location.PostalCode,
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    GeoFenceRadiusMeters = location.GeoFenceRadiusMeters,
                    TimeZoneId = location.TimeZoneId,
                    IsHeadOffice = location.IsHeadOffice,
                    IsGeoFenceEnabled = location.IsGeoFenceEnabled,
                    IsAttendanceAllowed = location.IsAttendanceAllowed,
                    IsBiometricEnabled = location.IsBiometricEnabled
                })
                .ToListAsync(cancellationToken);

            detail.Subscriptions = await _context.InstitutionSubscriptions
                .AsNoTracking()
                .Where(subscription =>
                    subscription.InstitutionId == institutionId &&
                    subscription.IsActive)
                .OrderByDescending(subscription => subscription.SubscriptionStartDate)
                .ThenByDescending(subscription => subscription.Id)
                .Select(subscription => new HostInstitutionSubscriptionDetailDTO
                {
                    Id = subscription.Id,
                    SubscriptionPlanId = subscription.SubscriptionPlanId,
                    SubscriptionPlanName = subscription.SubscriptionPlan.PlanName,
                    SubscriptionStartDate = subscription.SubscriptionStartDate,
                    SubscriptionEndDate = subscription.SubscriptionEndDate,
                    IsTrial = subscription.IsTrial,
                    PaymentTxnId = subscription.PaymentTxnId,
                    PaymentMode = subscription.PaymentMode
                })
                .ToListAsync(cancellationToken);

            detail.Departments = await _context.Departments
                .AsNoTracking()
                .Where(department =>
                    department.InstitutionId == institutionId &&
                    department.IsActive &&
                    !department.IsSoftDeleted)
                .OrderBy(department => department.DepartmentName)
                .ThenBy(department => department.Id)
                .Select(department => new HostInstitutionDepartmentDetailDTO
                {
                    Id = department.Id,
                    DepartmentName = department.DepartmentName,
                    Description = department.Description,
                    Remark = department.Remark,
                    IsExecutiveOffice = department.IsExecutiveOffice
                })
                .ToListAsync(cancellationToken);

            detail.StaffMemberCodePatterns = await _context.StaffMemberCodePatterns
                .AsNoTracking()
                .Where(pattern => pattern.InstitutionId == institutionId && pattern.IsActive)
                .OrderBy(pattern => pattern.Id)
                .Select(pattern => new HostInstitutionStaffMemberCodePatternDetailDTO
                {
                    Id = pattern.Id,
                    Prefix = pattern.Prefix,
                    IncludeYear = pattern.IncludeYear,
                    IncludeMonth = pattern.IncludeMonth,
                    IncludeDepartment = pattern.IncludeDepartment,
                    Separator = pattern.Separator,
                    RunningNumberLength = pattern.RunningNumberLength,
                    LastUsedNumber = pattern.LastUsedNumber
                })
                .ToListAsync(cancellationToken);

            detail.StaffMembers = await _context.StaffMembers
                .AsNoTracking()
                .Where(staffMember =>
                    staffMember.InstitutionId == institutionId &&
                    staffMember.IsActive &&
                    !staffMember.IsSoftDeleted)
                .OrderBy(staffMember => staffMember.FirstName)
                .ThenBy(staffMember => staffMember.Id)
                .Select(staffMember => new HostInstitutionStaffMemberDetailDTO
                {
                    Id = staffMember.Id,
                    EmployementCode = staffMember.EmployementCode,
                    FirstName = staffMember.FirstName,
                    MiddleName = staffMember.MiddleName,
                    LastName = staffMember.LastName,
                    GenderId = staffMember.GenderId,                  
                    StaffEmploymentTypeId = staffMember.StaffEmploymentTypeId,
                    OfficialEmail = staffMember.OfficialEmail,
                    DateOfOnBoarding = staffMember.DateOfOnBoarding,
                    CountryId = staffMember.CountryId
                })
                .ToListAsync(cancellationToken);

            var staffMemberIds = detail.StaffMembers.Select(staffMember => staffMember.Id).ToList();
            detail.LoginCredentials = await _context.LoginCredentials
                .AsNoTracking()
                .Where(credential =>
                    credential.InstitutionId == institutionId &&
                    staffMemberIds.Contains(credential.StaffMemberId) &&
                    credential.IsActive &&
                    credential.IsSoftDeleted != true)
                .OrderBy(credential => credential.LoginId)
                .ThenBy(credential => credential.Id)
                .Select(credential => new HostInstitutionLoginCredentialDetailDTO
                {
                    Id = credential.Id,
                    StaffMemberId = credential.StaffMemberId,
                    LoginId = credential.LoginId,
                    HasFirstLogin = credential.HasFirstLogin,
                    IsPasswordChangeRequired = credential.IsPasswordChangeRequired,
                    IsOnboard = credential.IsOnboard,
                    Remark = credential.Remark
                })
                .ToListAsync(cancellationToken);

            detail.EmailConfigurations = await _context.InstitutionEmailConfigs
                .AsNoTracking()
                .Where(configuration => configuration.InstitutionId == institutionId && configuration.IsActive)
                .OrderBy(configuration => configuration.Id)
                .Select(configuration => new HostInstitutionEmailConfigurationDetailDTO
                {
                    Id = configuration.Id,
                    SmtpHost = configuration.SmtpHost,
                    SmtpPort = configuration.SmtpPort,
                    SmtpUsername = configuration.SmtpUsername,
                    FromEmail = configuration.FromEmail,
                    FromName = configuration.FromName
                })
                .ToListAsync(cancellationToken);

            return detail;
        }

        /// <summary>
        /// Retrieves the first persisted profile for a Institution as a tracked entity for the Host-managed nested update flow.
        /// Institution onboarding creates one profile record per Institution.
        /// </summary>
        public Task<InstitutionProfile?> GetInstitutionProfileForUpdateAsync(long institutionId, CancellationToken cancellationToken = default)
        {
            return _context.InstitutionProfiles
                .Where(profile => profile.InstitutionId == institutionId)
                .OrderBy(profile => profile.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Determines whether another non-soft-deleted Institution uses the supplied email address.
        /// </summary>
        /// <param name="institutionEmail">The email address to check.</param>
        /// <param name="excludedInstitutionId">The current Institution excluded from the check.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns><see langword="true"/> when another Institution uses the email.</returns>
        public Task<bool> IsInstitutionEmailInUseAsync(
            string institutionEmail,
            long excludedInstitutionId,
            CancellationToken cancellationToken = default)
        {
            return _context.Institutions
                .AsNoTracking()
                .AnyAsync(
                    institution =>
                        institution.Id != excludedInstitutionId &&
                        institution.IsSoftDeleted != true &&
                        institution.InstitutionEmail == institutionEmail,
                    cancellationToken);
        }

        /// <summary>
        /// Determines whether another non-soft-deleted Institution uses the supplied Institution code.
        /// </summary>
        /// <param name="institutionCode">The Institution code to check.</param>
        /// <param name="excludedInstitutionId">The current Institution excluded from the check.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns><see langword="true"/> when another Institution uses the code.</returns>
        public Task<bool> IsInstitutionCodeInUseAsync(
            string institutionCode,
            long excludedInstitutionId,
            CancellationToken cancellationToken = default)
        {
            return _context.Institutions
                .AsNoTracking()
                .AnyAsync(
                    institution =>
                        institution.Id != excludedInstitutionId &&
                        institution.IsSoftDeleted != true &&
                        institution.InstitutionCode == institutionCode,
                    cancellationToken);
        }

        /// <summary>
        /// Stages a Host-managed Institution update prepared by the handler.
        /// </summary>
        /// <param name="institution">The tracked Institution entity with validated editable changes.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>A task representing the staging operation.</returns>
        public Task StageHostManagedUpdateAsync(Institution institution, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(institution);
            _context.Institutions.Update(institution);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Stages a Institution status transition and synchronizes valid, non-soft-deleted credentials.
        /// </summary>
        /// <param name="institution">The tracked Institution whose active state was prepared by the handler.</param>
        /// <param name="hostUserId">The validated Host user used for credential audit fields.</param>
        /// <param name="utcNow">The single UTC audit timestamp captured for the Host request.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>A task representing the staging operation.</returns>
        public async Task SynchronizeInstitutionStatusAsync(
            Institution institution,
            long hostUserId,
            DateTime utcNow,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(institution);

            _context.Institutions.Update(institution);

            var credentials = await _context.LoginCredentials
                .Where(credential =>
                    credential.InstitutionId == institution.Id &&
                    credential.IsSoftDeleted != true)
                .ToListAsync(cancellationToken);

            foreach (var credential in credentials)
            {
                credential.IsActive = institution.IsActive;
                credential.UpdatedById = hostUserId;
                credential.UpdatedDateTime = utcNow;
            }
        }

        /// <summary>
        /// Stages Institution soft deletion and deactivates all related login credentials without deleting historical records.
        /// </summary>
        /// <param name="institution">The tracked Institution prepared for soft deletion.</param>
        /// <param name="hostUserId">The validated Host user used for credential audit fields.</param>
        /// <param name="utcNow">The single UTC audit timestamp captured for the Host request.</param>
        /// <param name="cancellationToken">The token used to observe cancellation.</param>
        /// <returns>A task representing the staging operation.</returns>
        public async Task SoftDeleteInstitutionAndDeactivateCredentialsAsync(
            Institution institution,
            long hostUserId,
            DateTime utcNow,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(institution);

            _context.Institutions.Update(institution);

            var credentials = await _context.LoginCredentials
                .Where(credential => credential.InstitutionId == institution.Id)
                .ToListAsync(cancellationToken);

            foreach (var credential in credentials)
            {
                // Preserve each credential's existing soft-delete state while deactivating access.
                credential.IsActive = false;
                credential.UpdatedById = hostUserId;
                credential.UpdatedDateTime = utcNow;
            }
        }

        #endregion
    }
}
