using skolenza.application.Common.Models;
using skolenza.application.Interfaces;
using Microsoft.Extensions.Options;

namespace skolenza.infrastructure.FileStoringService;

/// <summary>Stores screenshots behind a provider abstraction for local development.</summary>
public sealed class StaffMemberScreenshotFileStorage(IOptions<StaffMemberMonitoringStorageOptions> options)
    : IStaffMemberScreenshotStorage
{
    private readonly StaffMemberMonitoringStorageOptions _options = options.Value;

    public async Task<string> SaveAsync(
        long institutionId,
        long staffMemberId,
        Guid captureId,
        string contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        var extension = contentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase) ? ".webp" : ".jpg";
        var relativePath = Path.Combine(
            "institution",
            institutionId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "staffMember",
            staffMemberId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            DateTime.UtcNow.ToString("yyyy", System.Globalization.CultureInfo.InvariantCulture),
            DateTime.UtcNow.ToString("MM", System.Globalization.CultureInfo.InvariantCulture),
            captureId.ToString("N") + extension);
        var fullPath = Path.GetFullPath(Path.Combine(_options.RootPath, relativePath));
        var rootPath = Path.GetFullPath(_options.RootPath);

        if (!fullPath.StartsWith(rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The generated screenshot storage path is invalid.");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var destination = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await content.CopyToAsync(destination, cancellationToken);
        return relativePath.Replace(Path.DirectorySeparatorChar, '/');
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_options.RootPath, objectKey.Replace('/', Path.DirectorySeparatorChar)));
        var rootPath = Path.GetFullPath(_options.RootPath);
        if (fullPath.StartsWith(rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
