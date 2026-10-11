using Skolenza.AutomationTests.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Skolenza.AutomationTests.UI;

[TestFixture]
[Category("UI")]
public sealed class LoginRouteTests : PlaywrightTest
{
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;

    [SetUp]
    public async Task StartBrowser()
    {
        if (TestSettings.WebBaseUrl is null)
        {
            Assert.Ignore(
                "UI test skipped. Set WebBaseUrl in automationsettings.json or SKOLENZA_TEST_WEB_BASE_URL after starting the frontend.");
        }

        _browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            // Use the Chromium revision that matches the installed Playwright package.
            // This avoids protocol crashes caused by an arbitrary machine Edge version.
            Headless = TestSettings.Headless
        });

        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = TestSettings.WebBaseUrl,
            ViewportSize = new ViewportSize
            {
                Width = 1440,
                Height = 900
            }
        });

        await _context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });

        _page = await _context.NewPageAsync();
    }

    [TearDown]
    public async Task SaveArtifactsAndCloseBrowser()
    {
        if (_context is not null)
        {
            var artifactsDirectory = Path.Combine(AppContext.BaseDirectory, "artifacts");
            Directory.CreateDirectory(artifactsDirectory);
            var artifactPrefix = TestContext.CurrentContext.Test.Name.Replace(' ', '-');

            if (TestContext.CurrentContext.Result.Outcome.Status == NUnit.Framework.Interfaces.TestStatus.Failed && _page is not null)
            {
                await _page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = Path.Combine(artifactsDirectory, $"{artifactPrefix}.png"),
                    FullPage = true
                });
            }

            await _context.Tracing.StopAsync(new TracingStopOptions
            {
                Path = Path.Combine(artifactsDirectory, $"{artifactPrefix}.zip")
            });

            await _context.CloseAsync();
        }

        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }
    }

    [Test]
    public async Task Login_route_loads_in_the_browser()
    {
        var response = await _page!.GotoAsync("/auth/login", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Ok, Is.True, "The frontend should return a successful response for /auth/login.");
        Assert.That(await _page.Locator("body").IsVisibleAsync(), Is.True);
    }
}
