using AutoMapper;
using skolenza.application.Interfaces.IEncryptionService;
using skolenza.application.Interfaces.IHashed;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace skolenza.persistance.Repositories
{
    public class StaffMemberManagerMappingRepository: IStaffMemberManagerMappingRepository
    {
        private readonly SkolenzaDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<StaffMemberManagerMappingRepository> _logger;
  
        public StaffMemberManagerMappingRepository(SkolenzaDbContext context, IMapper mapper, ILogger<StaffMemberManagerMappingRepository> logger)
        {
            this._context = context;
            this._mapper = mapper;
            this._logger = logger;        

        }

        public async Task<bool> AddAsync(StaffMemberManagerMapping entity)
        {
            await _context.StaffMemberManagerMappings.AddAsync(entity);
            return true;
        }
        public async Task<bool> ExistsPrimaryAsync(long staffMemberId, long institutionId)
        {
            return await _context.StaffMemberManagerMappings.AnyAsync(x =>
                    x.StaffMemberId == staffMemberId &&
                    x.InstitutionId == institutionId &&
                    x.ReportingTypeId == 1 &&
                    x.IsActive &&
                    (x.IsSoftDeleted != true));
        }

        /// <inheritdoc />
        public Task<bool> IsCurrentDirectReportAsync(
            long institutionId,
            long managerId,
            long staffMemberId,
            CancellationToken cancellationToken = default)
        {
            if (institutionId <= 0 || managerId <= 0 || staffMemberId <= 0)
            {
                return Task.FromResult(false);
            }

            var today = DateTime.UtcNow.Date;
            return _context.StaffMemberManagerMappings
                .AsNoTracking()
                .AnyAsync(mapping =>
                    mapping.InstitutionId == institutionId &&
                    mapping.ManagerId == managerId &&
                    mapping.StaffMemberId == staffMemberId &&
                    mapping.IsActive &&
                    mapping.IsSoftDeleted != true &&
                    mapping.EffectiveFrom <= today &&
                    (!mapping.EffectiveTo.HasValue || mapping.EffectiveTo.Value >= today),
                    cancellationToken);
        }
    }
}
