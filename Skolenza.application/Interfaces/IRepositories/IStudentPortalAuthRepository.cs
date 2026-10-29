using skolenza.application.DTOS.Student;
using skolenza.domain.Entity;

namespace skolenza.application.Interfaces.IRepositories;

public interface IStudentPortalAuthRepository
{
    Task<long?> ResolveVerifiedInstitutionAsync(string hostname, CancellationToken cancellationToken);

    Task<StudentPortalAccount?> GetAccountForLoginAsync(
        long institutionId,
        string normalizedEmail,
        CancellationToken cancellationToken);

    Task<StudentPortalAccount?> GetAccountByIdAsync(
        long institutionId,
        long accountId,
        CancellationToken cancellationToken);

    Task<bool> HasActivePersonaAsync(long institutionId, long accountId, CancellationToken cancellationToken);

    Task<StudentPortalProfileResponseDTO?> GetProfileAsync(
        long institutionId,
        long accountId,
        CancellationToken cancellationToken);

    Task QueueGuardianInvitationAsync(
        long institutionId,
        long guardianId,
        DateTime issuedAtUtc,
        CancellationToken cancellationToken);

    Task<StudentPortalAccount> AcceptGuardianInvitationAsync(
        long institutionId,
        string tokenHash,
        string passwordHash,
        long? authenticatedAccountId,
        DateTime acceptedAtUtc,
        CancellationToken cancellationToken);

    Task QueuePasswordResetAsync(
        long institutionId,
        string normalizedEmail,
        DateTime issuedAtUtc,
        CancellationToken cancellationToken);

    Task ResetPasswordAsync(
        long institutionId,
        string tokenHash,
        string passwordHash,
        DateTime resetAtUtc,
        CancellationToken cancellationToken);

    Task RegisterFailedLoginAsync(
        long institutionId,
        long accountId,
        short failureLimit,
        DateTime lockoutUntilUtc,
        CancellationToken cancellationToken);

    Task ResetLoginFailuresAsync(long institutionId, long accountId, DateTime loginUtc, CancellationToken cancellationToken);

    Task AddRefreshTokenAsync(StudentPortalRefreshToken token, CancellationToken cancellationToken);

    Task<StudentPortalRefreshToken?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken);

    Task<bool> RotateRefreshTokenAsync(
        StudentPortalRefreshToken current,
        StudentPortalRefreshToken replacement,
        CancellationToken cancellationToken);

    Task RevokeRefreshTokenAsync(StudentPortalRefreshToken token, CancellationToken cancellationToken);
}
