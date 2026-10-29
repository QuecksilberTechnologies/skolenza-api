using skolenza.application.DTOS.Pagination;
using skolenza.application.DTOS.Student;

namespace skolenza.application.Interfaces.IRepositories;

public sealed record InstitutionPortalDomainVerificationData(
    string DomainName,
    string ChallengeValueHash,
    DateTime ChallengeExpiresAt,
    short VerificationStatus);

public interface IInstitutionPortalDomainMasterRepository
{
    Task<PagedResponseDTO<InstitutionPortalDomainResponseDTO>> GetPageAsync(
        long institutionId,
        InstitutionPortalDomainFilterDTO filter,
        CancellationToken cancellationToken);

    Task<InstitutionPortalDomainResponseDTO> CreateAsync(
        long institutionId,
        long actorStaffMemberId,
        string domainName,
        string challengeValueHash,
        DateTime challengeExpiresAt,
        CancellationToken cancellationToken);

    Task<InstitutionPortalDomainVerificationData> GetVerificationDataAsync(
        long id,
        long institutionId,
        CancellationToken cancellationToken);

    Task<bool> MarkVerifiedAsync(
        long id,
        long institutionId,
        long actorStaffMemberId,
        string expectedChallengeValueHash,
        DateTime verifiedAt,
        CancellationToken cancellationToken);

    Task<InstitutionPortalDomainChallengeResponseDTO> RenewChallengeAsync(
        long id,
        long institutionId,
        long actorStaffMemberId,
        string challengeValueHash,
        DateTime challengeExpiresAt,
        string challengeValue,
        CancellationToken cancellationToken);

    Task DeleteAsync(long id, long institutionId, long actorStaffMemberId, CancellationToken cancellationToken);
}
