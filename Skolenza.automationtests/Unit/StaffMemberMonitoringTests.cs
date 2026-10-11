using System.Reflection;
using skolenza.api.Controllers.Staff;
using skolenza.application.Common.Models;
using skolenza.application.DTOS.StaffMonitoring;
using skolenza.application.Exceptions;
using skolenza.application.Features.StaffMemberCmd.StaffMemberMonitoring;
using skolenza.infrastructure.FileStoringService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("StaffMemberMonitoring")]
public sealed class StaffMemberMonitoringTests
{
    [Test]
    public void Institution_administration_endpoints_require_bearer_authentication()
    {
        var names = new[]
        {
            nameof(StaffMonitoringController.UpsertPolicy),
            nameof(StaffMonitoringController.RegisterAgent),
            nameof(StaffMonitoringController.GetAgentStatus)
        };

        Assert.Multiple(() =>
        {
            foreach (var name in names)
            {
                var method = typeof(StaffMonitoringController).GetMethod(name);
                Assert.That(method, Is.Not.Null, name);
                Assert.That(method!.GetCustomAttribute<AuthorizeAttribute>(), Is.Not.Null, name);
                Assert.That(method.GetCustomAttribute<AllowAnonymousAttribute>(), Is.Null, name);
            }
        });
    }

    [Test]
    public void Agent_runtime_endpoints_use_agent_credential_boundary_and_rate_limit()
    {
        var names = new[]
        {
            nameof(StaffMonitoringController.GetRuntimeConfiguration),
            nameof(StaffMonitoringController.Heartbeat),
            nameof(StaffMonitoringController.UploadCapture)
        };

        Assert.Multiple(() =>
        {
            foreach (var name in names)
            {
                var method = typeof(StaffMonitoringController).GetMethod(name);
                Assert.That(method, Is.Not.Null, name);
                Assert.That(method!.GetCustomAttribute<AllowAnonymousAttribute>(), Is.Not.Null, name);
                Assert.That(method.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName,
                    Is.EqualTo("staffMember-monitoring-agent"), name);
                Assert.That(method.GetParameters().Any(parameter =>
                    parameter.GetCustomAttributes().Any(attribute =>
                        attribute.GetType().Name == "FromHeaderAttribute")), Is.True, name);
            }
        });
    }

    [TestCase(59, 600, 60, 180, 600, 3, 1024, 70)]
    [TestCase(600, 300, 60, 180, 600, 3, 1024, 70)]
    [TestCase(300, 600, 60, 60, 600, 3, 1024, 70)]
    [TestCase(300, 600, 60, 180, 180, 3, 1024, 70)]
    [TestCase(300, 600, 60, 180, 600, 0, 1024, 70)]
    [TestCase(300, 600, 60, 180, 600, 3, 0, 70)]
    [TestCase(300, 600, 60, 180, 600, 3, 1024, 101)]
    public void Monitoring_policy_rejects_unsafe_ranges(
        int minimum,
        int maximum,
        int heartbeat,
        int delayed,
        int unreachable,
        int retentionDays,
        long maximumBytes,
        int quality)
    {
        var handler = new UpsertStaffMemberMonitoringPolicyCommandHandler(null!, null!);
        var request = new UpsertStaffMemberMonitoringPolicyRequestDTO
        {
            MinimumCaptureIntervalSeconds = minimum,
            MaximumCaptureIntervalSeconds = maximum,
            HeartbeatIntervalSeconds = heartbeat,
            DelayedAfterSeconds = delayed,
            UnreachableAfterSeconds = unreachable,
            OfflineRetentionDays = retentionDays,
            MaximumOfflineBytes = maximumBytes,
            ImageQuality = quality
        };

        Assert.ThrowsAsync<ValidationErrorException>(async () =>
            await handler.Handle(new UpsertStaffMemberMonitoringPolicyCommand(request), CancellationToken.None));
    }

    [Test]
    public async Task File_storage_generates_institution_and_staffMember_isolated_object_key()
    {
        var root = Path.Combine(Path.GetTempPath(), "skolenza-monitoring-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new StaffMemberScreenshotFileStorage(Options.Create(new StaffMemberMonitoringStorageOptions
            {
                Provider = "FileSystem",
                RootPath = root,
                MaximumUploadBytes = 1024,
                AllowedContentTypes = ["image/jpeg"]
            }));
            var captureId = Guid.CreateVersion7();
            await using var content = new MemoryStream([1, 2, 3, 4]);

            var objectKey = await storage.SaveAsync(12, 34, captureId, "image/jpeg", content, CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(objectKey, Does.StartWith("institution/12/staffMember/34/"));
                Assert.That(objectKey, Does.EndWith(captureId.ToString("N") + ".jpg"));
                Assert.That(File.Exists(Path.Combine(root, objectKey.Replace('/', Path.DirectorySeparatorChar))), Is.True);
            });
            await storage.DeleteAsync(objectKey, CancellationToken.None);
            Assert.That(File.Exists(Path.Combine(root, objectKey.Replace('/', Path.DirectorySeparatorChar))), Is.False);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}

