using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using skolenza.application.Common.Models;
using skolenza.infrastructure.BackgroundJob;

namespace skolenza.automationtests.Unit;

[TestFixture]
[Category("BulkImport")]
public sealed class BulkImportWorkerTests
{
    [Test]
    public async Task Host_shutdown_while_waiting_for_next_poll_completes_without_an_exception()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var worker = new BulkImportWorker(
            services.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new BulkImportOptions
            {
                WorkerEnabled = true,
                PollIntervalSeconds = 30,
                BatchTimeoutSeconds = 30
            }),
            NullLogger<BulkImportWorker>.Instance);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var executeAsync = typeof(BulkImportWorker).GetMethod(
            "ExecuteAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(executeAsync, Is.Not.Null);

        var execution = (Task)executeAsync!.Invoke(worker, [cancellation.Token])!;

        Assert.DoesNotThrowAsync(async () => await execution);
    }
}
