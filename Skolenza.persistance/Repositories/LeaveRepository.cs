using skolenza.application.Constants;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace skolenza.persistance.Repositories;

/// <summary>
/// Persists institution leave-type reference data. Policy behavior is stored by the
/// generic policy framework and is deliberately not duplicated here.
/// </summary>
public class LeaveRepository : ILeaveRepository
{
    private readonly SkolenzaDbContext _context;
    private readonly ILogger<LeaveRepository> _logger;

    public LeaveRepository(
        SkolenzaDbContext context,
        ILogger<LeaveRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<LeaveType>> GetAllLeaveAsync(bool? isActive, long? institutionId)
    {
        IQueryable<LeaveType> query = _context.LeaveTypes
            .AsNoTracking()
            .Where(leaveType =>
                leaveType.InstitutionId == institutionId &&
                (leaveType.IsSoftDeleted == false || leaveType.IsSoftDeleted == null));

        query = isActive.HasValue
            ? query.Where(leaveType => leaveType.IsActive == isActive.Value)
            : query.Where(leaveType => leaveType.IsActive == false);

        return await query.ToListAsync();
    }

    public async Task<List<LeaveType>> CreateLeaveTypeAsync(LeaveType leaveType)
    {
        ArgumentNullException.ThrowIfNull(leaveType);

        await _context.LeaveTypes.AddAsync(leaveType);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Leave type {LeaveTypeId} was created for institution {InstitutionId}.",
            leaveType.Id,
            leaveType.InstitutionId);

        return await GetAllLeaveAsync(leaveType.IsActive, leaveType.InstitutionId);
    }

    public Task<LeaveType?> GetLeaveByIdAsync(int leaveId)
    {
        return _context.LeaveTypes.FirstOrDefaultAsync(leaveType => leaveType.Id == leaveId);
    }

    public async Task<bool> UpdateLeavTypeAsync(LeaveType leaveType, long userId)
    {
        ArgumentNullException.ThrowIfNull(leaveType);

        var existingLeave = await _context.LeaveTypes.FirstOrDefaultAsync(candidate =>
            candidate.Id == leaveType.Id &&
            (candidate.IsSoftDeleted == false || candidate.IsSoftDeleted == null));

        if (existingLeave == null || string.IsNullOrWhiteSpace(leaveType.LeaveName))
        {
            return false;
        }

        existingLeave.LeaveName = leaveType.LeaveName.Trim();
        existingLeave.Description = leaveType.Description?.Trim();
        existingLeave.IsActive = leaveType.IsActive;
        existingLeave.UpdateById = userId;
        existingLeave.UpdateDateTime = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteLeaveAsync(LeaveType leaveType)
    {
        ArgumentNullException.ThrowIfNull(leaveType);

        var existingLeave = await _context.LeaveTypes.FirstOrDefaultAsync(candidate =>
            candidate.Id == leaveType.Id);

        if (existingLeave == null || existingLeave.IsSoftDeleted == true)
        {
            return false;
        }

        existingLeave.IsSoftDeleted = ConstantValues.IsByDefaultTrue;
        existingLeave.SoftDeletedDateTime = DateTime.UtcNow;
        existingLeave.SoftDeletedBy = leaveType.SoftDeletedBy;

        await _context.SaveChangesAsync();
        return true;
    }
}
