using Microsoft.EntityFrameworkCore;
using skolenza.application.Common.Enums;
using skolenza.application.Common.Helpers.RequestHelper;
using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Student;
using skolenza.application.Exceptions;
using skolenza.application.Interfaces.IRepositories;
using skolenza.domain.Entity;
using skolenza.persistance.Data.Context;

namespace skolenza.persistance.Repositories;

public sealed class InstitutionPortalDomainMasterRepository(SkolenzaDbContext context)
    : IInstitutionPortalDomainMasterRepository
{
    public async Task<PagedResponseDTO<InstitutionPortalDomainResponseDTO>> GetPageAsync(
        long institutionId,
        InstitutionPortalDomainFilterDTO filter,
        CancellationToken cancellationToken)
    {
        var query = context.InstitutionPortalDomainMasters.AsNoTracking()
            .Where(item => item.InstitutionId == institutionId && !item.IsSoftDeleted);

        if (filter.ActiveOnly)
        {
            query = query.Where(item => item.IsActive);
        }

        var page = await query
            .OrderByDescending(item => item.AddedDateTime)
            .ThenBy(item => item.Id)
            .Select(item => new InstitutionPortalDomainResponseDTO
            {
                Id = item.Id,
                DomainName = item.DomainName,
                VerificationStatus = (StudentPortalDomainVerificationStatus)item.VerificationStatus,
                ChallengeExpiresAt = item.ChallengeExpiresAt,
                VerifiedAt = item.VerifiedAt,
                IsActive = item.IsActive,
                AddedDateTime = item.AddedDateTime,
                UpdatedDateTime = item.UpdatedDateTime
            })
            .ToPageAsync(filter.PageNumber, filter.PageSize, cancellationToken);

        return new PagedResponseDTO<InstitutionPortalDomainResponseDTO>(
            page.Data, page.TotalCount, page.PageNumber, page.PageSize)
        {
            TotalPages = page.TotalPages
        };
    }

    public async Task<InstitutionPortalDomainResponseDTO> CreateAsync(
        long institutionId,
        long actorStaffMemberId,
        string domainName,
        string challengeValueHash,
        DateTime challengeExpiresAt,
        CancellationToken cancellationToken)
    {
        if (await context.InstitutionPortalDomainMasters.AnyAsync(item =>
                item.InstitutionId == institutionId && !item.IsSoftDeleted, cancellationToken))
        {
            throw new ConflictException("This institution already has a portal domain entry. Deactivate it before replacing the hostname.");
        }

        if (await context.InstitutionPortalDomainMasters.AnyAsync(item =>
                item.DomainName == domainName && !item.IsSoftDeleted, cancellationToken))
        {
            throw new ConflictException("This portal domain is already registered to an institution.");
        }

        var entity = new InstitutionPortalDomainMaster
        {
            InstitutionId = institutionId,
            DomainName = domainName,
            ChallengeValueHash = challengeValueHash,
            ChallengeExpiresAt = challengeExpiresAt,
            VerificationStatus = (short)StudentPortalDomainVerificationStatus.Pending,
            IsActive = false,
            AddedById = actorStaffMemberId,
            AddedDateTime = DateTime.UtcNow
        };

        context.InstitutionPortalDomainMasters.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return await GetResponseAsync(entity.Id, institutionId, cancellationToken);
    }

    public async Task<InstitutionPortalDomainVerificationData> GetVerificationDataAsync(
        long id,
        long institutionId,
        CancellationToken cancellationToken) =>
        await context.InstitutionPortalDomainMasters.AsNoTracking()
            .Where(item => item.Id == id && item.InstitutionId == institutionId && !item.IsSoftDeleted)
            .Select(item => new InstitutionPortalDomainVerificationData(
                item.DomainName,
                item.ChallengeValueHash,
                item.ChallengeExpiresAt,
                item.VerificationStatus))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new NotFoundException("Institution portal domain was not found.");

    public async Task<bool> MarkVerifiedAsync(
        long id,
        long institutionId,
        long actorStaffMemberId,
        string expectedChallengeValueHash,
        DateTime verifiedAt,
        CancellationToken cancellationToken)
    {
        var updated = await context.InstitutionPortalDomainMasters
            .Where(item => item.Id == id && item.InstitutionId == institutionId && !item.IsSoftDeleted &&
                !item.IsActive && item.VerificationStatus == (short)StudentPortalDomainVerificationStatus.Pending &&
                item.ChallengeValueHash == expectedChallengeValueHash && item.ChallengeExpiresAt > verifiedAt)
            .ExecuteUpdateAsync(update => update
                .SetProperty(item => item.VerificationStatus, (short)StudentPortalDomainVerificationStatus.Verified)
                .SetProperty(item => item.VerifiedAt, verifiedAt)
                .SetProperty(item => item.IsActive, true)
                .SetProperty(item => item.UpdatedById, actorStaffMemberId)
                .SetProperty(item => item.UpdatedDateTime, verifiedAt), cancellationToken);

        return updated == 1;
    }

    public async Task<InstitutionPortalDomainChallengeResponseDTO> RenewChallengeAsync(
        long id,
        long institutionId,
        long actorStaffMemberId,
        string challengeValueHash,
        DateTime challengeExpiresAt,
        string challengeValue,
        CancellationToken cancellationToken)
    {
        var entity = await context.InstitutionPortalDomainMasters.SingleOrDefaultAsync(item =>
            item.Id == id && item.InstitutionId == institutionId && !item.IsSoftDeleted,
            cancellationToken) ?? throw new NotFoundException("Institution portal domain was not found.");

        if (entity.VerificationStatus == (short)StudentPortalDomainVerificationStatus.Verified || entity.IsActive)
        {
            throw new ConflictException("A verified portal domain cannot be challenged again. Deactivate it before replacing the hostname.");
        }

        entity.ChallengeValueHash = challengeValueHash;
        entity.ChallengeExpiresAt = challengeExpiresAt;
        entity.UpdatedById = actorStaffMemberId;
        entity.UpdatedDateTime = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        var response = await GetResponseAsync(id, institutionId, cancellationToken);
        return new InstitutionPortalDomainChallengeResponseDTO
        {
            Domain = response,
            DnsTxtRecordName = skolenza.application.Common.Helpers.InstitutionPortalDomainChallenge.CreateRecordName(entity.DomainName),
            DnsTxtRecordValue = challengeValue
        };
    }

    public async Task DeleteAsync(
        long id,
        long institutionId,
        long actorStaffMemberId,
        CancellationToken cancellationToken)
    {
        var entity = await context.InstitutionPortalDomainMasters.SingleOrDefaultAsync(item =>
            item.Id == id && item.InstitutionId == institutionId && !item.IsSoftDeleted,
            cancellationToken) ?? throw new NotFoundException("Institution portal domain was not found.");

        entity.IsActive = false;
        entity.IsSoftDeleted = true;
        entity.SoftDeletedById = actorStaffMemberId;
        entity.DeletedDateTime = DateTime.UtcNow;
        entity.UpdatedById = actorStaffMemberId;
        entity.UpdatedDateTime = entity.DeletedDateTime;
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<InstitutionPortalDomainResponseDTO> GetResponseAsync(
        long id,
        long institutionId,
        CancellationToken cancellationToken) =>
        await context.InstitutionPortalDomainMasters.AsNoTracking()
            .Where(item => item.Id == id && item.InstitutionId == institutionId && !item.IsSoftDeleted)
            .Select(item => new InstitutionPortalDomainResponseDTO
            {
                Id = item.Id,
                DomainName = item.DomainName,
                VerificationStatus = (StudentPortalDomainVerificationStatus)item.VerificationStatus,
                ChallengeExpiresAt = item.ChallengeExpiresAt,
                VerifiedAt = item.VerifiedAt,
                IsActive = item.IsActive,
                AddedDateTime = item.AddedDateTime,
                UpdatedDateTime = item.UpdatedDateTime
            })
            .SingleAsync(cancellationToken);
}
