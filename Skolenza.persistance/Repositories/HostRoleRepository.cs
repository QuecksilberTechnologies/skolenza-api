// ================================================================
// Author  : Deepesh Gupta
// Company : Quecksilber Technologies
// Role    : CEO
// Purpose : Persists and retrieves host roles.
// ================================================================

using AutoMapper;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IHashed;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Repositories
{
    /// <summary>
    /// Provides persistence operations for host roles.
    /// </summary>
    public class HostRoleRepository : IHostRoleRepository
    {
        #region Fields

        private readonly SkolenzaDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<HostRoleRepository> _logger;

        private readonly IPasswordService _passwordService;
        private readonly IEncryptionService _encryptionService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="HostRoleRepository"/> class.
        /// </summary>
        /// <param name="context">The database context used for persistence.</param>
        /// <param name="mapper">The mapper supplied to the repository.</param>
        /// <param name="logger">The logger supplied to the repository.</param>
        /// <param name="passwordService">The password service supplied to the repository.</param>
        /// <param name="encryptionService">The encryption service supplied to the repository.</param>
        public HostRoleRepository(
            SkolenzaDbContext context,
            IMapper mapper,
            ILogger<HostRoleRepository> logger,
            IPasswordService passwordService,
            IEncryptionService encryptionService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _passwordService = passwordService;
            _encryptionService = encryptionService;
        }

        #endregion

        #region Host Role Methods

        /// <summary>
        /// Retrieves a non-soft-deleted host role by identifier.
        /// </summary>
        /// <param name="id">The host-role identifier.</param>
        /// <returns>The matching host role, or <see langword="null"/> when it is not found.</returns>
        public async Task<HostRole?> GetByIdAsync(long id)
        {
            return await _context.HostRoles
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    !x.IsSoftDeleted);
        }

        /// <summary>
        /// Retrieves all non-soft-deleted host roles in descending identifier order.
        /// </summary>
        /// <returns>A list of host roles, which is empty when no matching roles exist.</returns>
        public async Task<List<HostRole>> GetAllAsync()
        {
            return await _context.HostRoles
                .AsNoTracking()
                .Where(x => !x.IsSoftDeleted)
                .OrderByDescending(x => x.Id)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves a non-soft-deleted host role by name.
        /// </summary>
        /// <param name="roleName">The host-role name to search for.</param>
        /// <returns>The matching host role, or <see langword="null"/> when it is not found.</returns>
        public async Task<HostRole?> GetByRoleNameAsync(string roleName)
        {
            return await _context.HostRoles
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Name == roleName &&
                    !x.IsSoftDeleted);
        }

        /// <summary>
        /// Adds a host role and persists it using the repository's existing convention.
        /// </summary>
        /// <param name="entity">The host-role entity to add.</param>
        /// <returns>The persisted host-role entity.</returns>
        public async Task<HostRole> AddAsync(HostRole entity)
        {
            await _context.HostRoles.AddAsync(entity);

            await _context.SaveChangesAsync();

            return entity;
        }

        /// <summary>
        /// Updates a host role and persists the changes using the repository's existing convention.
        /// </summary>
        /// <param name="entity">The host-role entity to update.</param>
        /// <returns>The persisted host-role entity.</returns>
        public async Task<HostRole> UpdateAsync(HostRole entity)
        {
            _context.HostRoles.Update(entity);

            await _context.SaveChangesAsync();

            return entity;
        }

        /// <summary>
        /// Persists a host role's prepared soft-delete state.
        /// </summary>
        /// <param name="entity">The host-role entity marked for soft deletion.</param>
        /// <returns><see langword="true"/> when at least one record is persisted; otherwise, <see langword="false"/>.</returns>
        public async Task<bool> DeleteAsync(HostRole entity)
        {
            _context.HostRoles.Update(entity);

            return await _context.SaveChangesAsync() > 0;
        }

        #endregion
    }
}
