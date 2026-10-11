namespace skolenza.application.Interfaces;

public interface IStaffMemberScreenshotStorage
{
    Task<string> SaveAsync(
        long institutionId,
        long staffMemberId,
        Guid captureId,
        string contentType,
        Stream content,
        CancellationToken cancellationToken);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}
