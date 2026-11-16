using NUnit.Framework;
using Reqnroll;
using ShopGuard.E2ETests.Support;

namespace ShopGuard.E2ETests.Hooks;

/// <summary>
/// Lifecycle hooks for UI scenarios: starts an isolated browser context per scenario
/// and captures screenshot + Playwright trace when a scenario fails.
/// </summary>
[Binding]
public sealed class PlaywrightHooks(PlaywrightDriver driver, ScenarioContext scenarioContext)
{
    private static readonly string ArtifactsDirectory =
        Path.Combine(AppContext.BaseDirectory, "artifacts");

    [BeforeScenario("@ui", Order = 10)]
    public Task InitializeBrowserAsync() => driver.InitAsync();

    [AfterScenario("@ui")]
    public async Task CaptureArtifactsOnFailureAsync()
    {
        // An earlier hook may have ended the scenario before the browser started (a skipped
        // @db scenario, a failed browser launch). There is nothing to capture then, and
        // touching the page would hide the real reason behind a second error.
        if (!driver.IsInitialized)
        {
            return;
        }

        try
        {
            if (scenarioContext.TestError is not null)
            {
                Directory.CreateDirectory(ArtifactsDirectory);
                var baseName = Sanitize(scenarioContext.ScenarioInfo.Title);

                var screenshotPath = Path.Combine(ArtifactsDirectory, $"{baseName}.png");
                await driver.Page.ScreenshotAsync(new() { Path = screenshotPath, FullPage = true });

                var tracePath = Path.Combine(ArtifactsDirectory, $"{baseName}.trace.zip");
                await driver.SaveTraceAsync(tracePath);

                // Make both files visible in the NUnit/CI test report.
                TestContext.AddTestAttachment(screenshotPath, "Screenshot at failure");
                TestContext.AddTestAttachment(tracePath, "Playwright trace (open with 'playwright show-trace')");
            }
            else
            {
                await driver.DiscardTraceAsync();
            }
        }
        finally
        {
            await driver.DisposeAsync();
        }
    }

    [AfterTestRun]
    public static Task CloseBrowserAsync() => PlaywrightDriver.ShutdownAsync();

    private static string Sanitize(string scenarioTitle)
    {
        var invalid = Path.GetInvalidFileNameChars().Concat([' ', '"']).ToHashSet();
        var safe = new string(scenarioTitle.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return $"{safe}_{DateTime.UtcNow:HHmmss}";
    }
}
