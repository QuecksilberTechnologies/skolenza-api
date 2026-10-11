using AutoMapper;
using skolenza.application.Constants;
using skolenza.application.DTOs.UserLogin;
using skolenza.application.Interfaces.IHashed;
using skolenza.application.Interfaces.IRepositories;

using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using skolenza.domain.Entity;

// ============================================================================
// Author      : Deepesh Gupta
// Company     : Quecksilber Technologies
// Role        : CEO
// Purpose     : Persists and retrieves Institution LoginCredential records.
// ============================================================================

namespace skolenza.persistance.Repositories
{
    public class UserLoginReopsitory : IUserLoginReopsitory
    {
        private readonly SkolenzaDbContext _context;
        private readonly ILogger<UserLoginReopsitory> _logger;
       
        private readonly IMapper _mapper;
        private readonly IPasswordService _passwordService;
        private readonly IConfiguration _configuration;

        public UserLoginReopsitory(SkolenzaDbContext context, ILogger<UserLoginReopsitory> logger, IMapper mapper,
             IPasswordService passwordService , IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
            
            _passwordService = passwordService;
            _configuration = configuration;
        }

        #region Authentication Queries

        public async Task<LoginCredential?> AuthenticateUser(string loginId)
        {
            try
            {
                

                _logger.LogInformation("🔐 Authenticating user with LoginId: {LoginId}", loginId);

                var user = await _context.LoginCredentials
                    .FirstOrDefaultAsync(u => u.LoginId == loginId && (u.IsActive==true && u.IsSoftDeleted!=true));

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Exception occurred while authenticating LoginId: {LoginId}", loginId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves an active, non-soft-deleted Institution login credential by its primary key.
        /// </summary>
        /// <param name="loginCredentialId">The immutable Institution credential identifier.</param>
        /// <returns>The matching credential, or <see langword="null"/> when it is inactive, soft deleted, or missing.</returns>
        public async Task<LoginCredential?> GetActiveByIdAsync(long loginCredentialId)
        {
            return await _context.LoginCredentials
                .AsNoTracking()
                .FirstOrDefaultAsync(credential =>
                    credential.Id == loginCredentialId &&
                    credential.IsActive &&
                    credential.IsSoftDeleted != true);
        }

        /// <summary>
        /// Retrieves the compact, validated Institution StaffMember data required to construct a NewLogin session.
        /// </summary>
        /// <param name="loginCredentialId">The active Institution login credential identifier.</param>
        /// <param name="cancellationToken">A token used to cancel the read-only projection query.</param>
        /// <returns>The minimal session-bootstrap projection, or <see langword="null"/> when required active records are unavailable.</returns>
        public async Task<NewLoginBootstrapReadModel?> GetNewLoginBootstrapAsync(
            long loginCredentialId,
            CancellationToken cancellationToken = default)
        {
            // Resolve only active staffMember and institution context; effective roles use the established role repository query.
            return await (
                from credential in _context.LoginCredentials.AsNoTracking()
                join staffMember in _context.StaffMembers.AsNoTracking()
                    on credential.StaffMemberId equals staffMember.Id
                join institution in _context.Institutions.AsNoTracking()
                    on staffMember.InstitutionId equals institution.Id
                join staffEmploymentType in _context.StaffEmploymentTypes.AsNoTracking()
                    on staffMember.StaffEmploymentTypeId equals staffEmploymentType.Id
                join department in _context.Departments.AsNoTracking()
                    on staffMember.DepartmentId equals department.Id into departmentJoin
                from department in departmentJoin.DefaultIfEmpty()
                join designation in _context.Designations.AsNoTracking()
                    on staffMember.DesignationId equals designation.Id into designationJoin
                from designation in designationJoin.DefaultIfEmpty()
                join gender in _context.Genders.AsNoTracking()
                    on staffMember.GenderId equals gender.Id into genderJoin
                from gender in genderJoin.DefaultIfEmpty()
                where credential.Id == loginCredentialId
                    && credential.IsActive
                    && credential.IsSoftDeleted != true
                    && credential.InstitutionId == staffMember.InstitutionId
                    && staffMember.IsActive
                    && !staffMember.IsSoftDeleted
                    && institution.IsActive
                    && institution.IsSoftDeleted != true
                    && staffEmploymentType.IsActive == true
                    && staffEmploymentType.IsSoftDeleted != true
                    && (department == null || (department.IsActive && !department.IsSoftDeleted))
                    && (designation == null || (designation.IsActive && !designation.IsSoftDeleted))
                select new NewLoginBootstrapReadModel
                {
                    StaffMemberId = staffMember.Id,
                    InstitutionId = institution.Id,
                    FirstName = staffMember.FirstName,
                    MiddleName = staffMember.MiddleName,
                    LastName = staffMember.LastName,
                    OfficialEmail = staffMember.OfficialEmail,
                    InstitutionName = institution.CompanyName,
                    StaffEmploymentTypeId = staffEmploymentType.Id,
                    StaffEmploymentTypeName = staffEmploymentType.TypeName,
                    DepartmentId = staffMember.DepartmentId,
                    DepartmentName = department != null ? department.DepartmentName : null,
                    DesignationId = staffMember.DesignationId,
                    DesignationName = designation != null ? designation.DesignationName : null,
                    GenderId = staffMember.GenderId ?? 0,
                    GenderName = gender != null ? gender.GenderName : null,
                    HasPermanent = staffMember.HasPermanent,
                    IsPasswordChangeRequired = credential.IsPasswordChangeRequired ?? false,
                    IsOnboard = credential.IsOnboard
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        #endregion

        #region Login Metadata

        /// <summary>
        /// Updates only permitted login metadata for an active Institution login credential without using the legacy SQL result DTO path.
        /// </summary>
        /// <param name="loginCredential">The credential carrying the server-approved login metadata values.</param>
        /// <param name="cancellationToken">A token used to cancel the persistence operation.</param>
        /// <returns><see langword="true"/> when the metadata was persisted; otherwise, <see langword="false"/>.</returns>
        public async Task<bool> UpdateLoginMetadataAsync(
            LoginCredential loginCredential,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(loginCredential);

            var persistedCredential = await _context.LoginCredentials
                .FirstOrDefaultAsync(
                    credential =>
                        credential.Id == loginCredential.Id &&
                        credential.IsActive &&
                        credential.IsSoftDeleted != true,
                    cancellationToken);

            if (persistedCredential == null)
            {
                return false;
            }

            // Persist only successful-login metadata and onboarding state; password
            // and credential ownership remain unchanged.
            persistedCredential.MacAddress = loginCredential.MacAddress;
            persistedCredential.IpAddressLocal = loginCredential.IpAddressLocal;
            persistedCredential.IpAddressPublic = loginCredential.IpAddressPublic;
            persistedCredential.Latitude = loginCredential.Latitude;
            persistedCredential.Longitude = loginCredential.Longitude;
            persistedCredential.LoginDevice = loginCredential.LoginDevice;
            persistedCredential.IsOnboard = true;
            persistedCredential.UpdatedById = loginCredential.UpdatedById;
            persistedCredential.UpdatedDateTime = loginCredential.UpdatedDateTime;

            return await _context.SaveChangesAsync(cancellationToken) > 0;
        }

        #endregion

        public async Task<long> CreateUser(LoginCredential loginRequest)
        {
            try
            {
                if (_context == null)
                {
                    _logger?.LogError("DbContext is null in CreateUser.");
                    throw new ArgumentNullException(nameof(_context), "DbContext is not initialized.");
                }               

                await _context.LoginCredentials.AddAsync(loginRequest); // Add LoginCredential
                await _context.SaveChangesAsync(); // Save changes

                _logger?.LogInformation("User created successfully with ID: {UserId}", loginRequest.Id);

                return loginRequest.Id; // Return auto-generated ID
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "An error occurred while creating user.");
                throw;
            }
        }

        public async Task<LoginCredential> GetStaffMemberIdByUserLogin(string userLogin)
        {
            var login = await _context.LoginCredentials.FirstOrDefaultAsync(x => x.LoginId == userLogin && x.IsSoftDeleted !=true);

            if (login == null)
                return null;


            return login;

        }
        public async Task<bool> UpdatePassword(long empId, string password, long updatedById)
        {
            var user = await _context.LoginCredentials
                .FirstOrDefaultAsync(x =>
                    x.StaffMemberId == empId &&
                    x.IsActive == true &&
                    x.IsSoftDeleted != true);

            if (user == null)
                return false;

            // 🔒 If same password, no update
            if (user.Password == password)
                return false;

            user.Password = password;
            user.UpdatedById = updatedById;
            user.UpdatedDateTime = DateTime.UtcNow;
            user.HasFirstLogin = false;
            user.IsPasswordChangeRequired = false;

            var rowsAffected = await _context.SaveChangesAsync();

            return rowsAffected > 0;
        }



        public async Task<bool> SetNewPassword(LoginCredential setRequest)
        {
            try
            {

                var user = await _context.LoginCredentials
                    .FirstOrDefaultAsync(x =>
                        x.LoginId == setRequest.LoginId &&
                        x.IsActive == true); // ✅ Only allow update if it's first login

                if (user == null)
                {
                    return false; // User not found or first login already done
                }

                user.Password = setRequest.Password;
                
                                            // user.UpdatedById = setRequest.UpdatedById;
                                            // user.UpdatedDateTime = DateTime.UtcNow;

                _context.LoginCredentials.Update(user);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating password for LoginId: {LoginId}", setRequest.LoginId);
                return false;
            }
        }



        private bool VerifyPassword(string providedPassword, string storedPassword)
        {
            // Secure hashing and comparison logic should be implemented here
            return providedPassword == storedPassword;
        }

 
    }

}
