using skolenza.application.Common.Helpers;
using skolenza.application.DTOS.Institution;
using skolenza.application.Exceptions;
using skolenza.domain.Entity;
using Microsoft.EntityFrameworkCore;

namespace skolenza.persistance.Repositories;

public partial class InstitutionStaffCodePatternRepository
{
    #region Pattern Preview And Confirmation

    /// <inheritdoc />
    public async Task<SaveStaffMemberCodePatternResponseDTO> SaveWithStaffMemberCodesAsync(
        long institutionId,
        long actorId,
        StaffMemberCodePattern proposed,
        bool create,
        bool confirm,
        string? previewHash,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        if (confirm)
        {
            await LockInstitutionCodeAllocationAsync(institutionId, cancellationToken);
        }

        var patternQuery = confirm
            ? _context.StaffMemberCodePatterns.FromSqlInterpolated(
                $"SELECT * FROM skolenza.\"StaffMemberCodePattern\" WHERE \"InstitutionId\" = {institutionId} AND \"IsActive\" = TRUE FOR UPDATE")
            : _context.StaffMemberCodePatterns;
        var patterns = await patternQuery.AsNoTracking()
            .Where(pattern => pattern.InstitutionId == institutionId && pattern.IsActive)
            .ToListAsync(cancellationToken);
        if (patterns.Count > 1 || (create && patterns.Count != 0) || (!create && patterns.Count != 1))
        {
            throw new ConflictException(create
                ? "An active pattern already exists. Use update."
                : "Exactly one active pattern is required for update.");
        }

        var current = patterns.SingleOrDefault();
        var staffMemberQuery = confirm
            ? _context.StaffMembers.FromSqlInterpolated(
                $"SELECT * FROM skolenza.\"StaffMember\" WHERE \"InstitutionId\" = {institutionId} ORDER BY \"Id\" FOR UPDATE")
            : _context.StaffMembers;
        var staffMembers = await staffMemberQuery.AsNoTracking()
            .Where(staffMember => staffMember.InstitutionId == institutionId)
            .OrderBy(staffMember => staffMember.Id)
            .ToListAsync(cancellationToken);
        var result = StaffMemberCodePatternFormatter.PreviewChanges(institutionId, current, proposed,
            staffMembers.Where(staffMember => !staffMember.IsSoftDeleted).ToList());
        var reservedCodes = staffMembers.Where(staffMember => staffMember.IsSoftDeleted && staffMember.EmployementCode is not null)
            .Select(staffMember => staffMember.EmployementCode!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var row in result.StaffMembers.Where(row => row.ProposedCode is not null && reservedCodes.Contains(row.ProposedCode)))
        {
            row.Errors.Add("Proposed code is reserved by an archived staffMember.");
            result.Errors.Add("Archived staffMember codes cannot be reused.");
        }
        if (!confirm)
        {
            return result;
        }

        if (!result.CanCommit)
        {
            throw new ValidationErrorException("StaffMember-code preview contains errors.");
        }

        if (string.IsNullOrEmpty(previewHash) ||
            !string.Equals(previewHash, result.PreviewHash, StringComparison.Ordinal))
        {
            throw new ConflictException("Pattern or staffMembers changed. Review a fresh preview before confirming.");
        }

        var now = DateTime.UtcNow;
        proposed.InstitutionId = institutionId;
        proposed.LastUsedNumber = result.LastUsedNumber;
        proposed.IsActive = true;
        if (current is null)
        {
            proposed.AddedById = actorId;
            proposed.AddedDateTime = now;
            _context.StaffMemberCodePatterns.Add(proposed);
        }
        else
        {
            proposed.Id = current.Id;
            proposed.AddedById = current.AddedById;
            proposed.AddedDateTime = current.AddedDateTime;
            proposed.UpdatedById = actorId;
            proposed.UpdatedDateTime = now;
            _context.StaffMemberCodePatterns.Update(proposed);
        }

        foreach (var row in result.StaffMembers.Where(row => row.CurrentCode != row.ProposedCode))
        {
            await _context.StaffMembers.Where(staffMember => staffMember.Id == row.StaffMemberId && staffMember.InstitutionId == institutionId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(staffMember => staffMember.EmployementCode, row.ProposedCode)
                    .SetProperty(staffMember => staffMember.UpdatedById, actorId)
                    .SetProperty(staffMember => staffMember.UpdatedDateTime, now), cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        result.Applied = true;
        return result;
    }

    /// <summary>Serializes this institution's allocation and pattern changes without blocking other institutions.</summary>
    private Task LockInstitutionCodeAllocationAsync(long institutionId, CancellationToken cancellationToken)
    {
        var lockKey = $"{skolenza.application.Constants.BulkImportConstants.StaffMemberCodeLockPrefix}{institutionId}";
        return _context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", cancellationToken);
    }

    #endregion
}
