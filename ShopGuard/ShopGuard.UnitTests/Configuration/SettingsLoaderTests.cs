using AwesomeAssertions;
using ShopGuard.Core.Configuration;

namespace ShopGuard.UnitTests.Configuration;

[TestFixture]
[NonParallelizable] // Tests mutate process-wide environment variables.
public sealed class SettingsLoaderTests
{
    private static readonly string[] EnvironmentVariables =
        ["HEADLESS", "SHOPGUARD_ShopBaseUrl", "SHOPGUARD_ApiPassword", "SHOPGUARD_DefaultTimeoutMs"];

    private string _jsonPath = null!;

    [SetUp]
    public void WriteSettingsFile()
    {
        _jsonPath = Path.Combine(Path.GetTempPath(), $"shopguard-settings-{Guid.NewGuid():N}.json");
        File.WriteAllText(_jsonPath, """
            {
              "TestSettings": {
                "ShopBaseUrl": "https://shop.example.com",
                "ShopApiBaseUrl": "https://api.shop.example.com",
                "ApiBaseUrl": "https://booking.example.com",
                "ApiUsername": "admin",
                "ApiPassword": "from-file",
                "Headless": true,
                "DefaultTimeoutMs": 30000
              },
              "Environments": {
                "staging": {
                  "ShopBaseUrl": "https://staging.example.com",
                  "ShopDbConnectionString": "Server=staging-db;Database=shop",
                  "Headless": "false"
                }
              }
            }
            """);
        ClearEnvironmentVariables();
    }

    [TearDown]
    public void Cleanup()
    {
        File.Delete(_jsonPath);
        ClearEnvironmentVariables();
    }

    private static void ClearEnvironmentVariables()
    {
        foreach (var name in EnvironmentVariables)
        {
            Environment.SetEnvironmentVariable(name, null);
        }
    }

    [Test]
    public void Load_ReadsRootSection()
    {
        var settings = SettingsLoader.Load(_jsonPath);

        settings.ShopBaseUrl.Should().Be("https://shop.example.com");
        settings.ShopApiBaseUrl.Should().Be("https://api.shop.example.com");
        settings.ApiUsername.Should().Be("admin");
        settings.Headless.Should().BeTrue();
        settings.DefaultTimeoutMs.Should().Be(30000);
    }

    [Test]
    public void Load_WithoutDatabaseConnectionString_LeavesItEmpty()
    {
        // Not every target gives database access; the E2E layer skips @db scenarios then.
        SettingsLoader.Load(_jsonPath).ShopDbConnectionString.Should().BeEmpty();
    }

    [Test]
    public void Load_AppliesEnvironmentSectionOverrides()
    {
        var settings = SettingsLoader.Load(_jsonPath, environment: "staging");

        settings.ShopBaseUrl.Should().Be("https://staging.example.com");
        settings.ShopDbConnectionString.Should().Be("Server=staging-db;Database=shop");
        settings.Headless.Should().BeFalse();
        // Values not overridden fall back to the root section.
        settings.ApiBaseUrl.Should().Be("https://booking.example.com");
    }

    [Test]
    public void Load_UnknownEnvironment_FallsBackToRootSection()
    {
        var settings = SettingsLoader.Load(_jsonPath, environment: "does-not-exist");

        settings.ShopBaseUrl.Should().Be("https://shop.example.com");
    }

    [Test]
    public void Load_EnvironmentVariable_WinsOverRootSection()
    {
        // This is how a real secret reaches the tests in CI without being committed.
        Environment.SetEnvironmentVariable("SHOPGUARD_ApiPassword", "from-ci-secret");

        SettingsLoader.Load(_jsonPath).ApiPassword.Should().Be("from-ci-secret");
    }

    [Test]
    public void Load_EnvironmentVariable_WinsOverEnvironmentSection()
    {
        Environment.SetEnvironmentVariable("SHOPGUARD_ShopBaseUrl", "http://localhost:4200");

        var settings = SettingsLoader.Load(_jsonPath, environment: "staging");

        settings.ShopBaseUrl.Should().Be("http://localhost:4200");
    }

    [Test]
    public void Load_EnvironmentVariable_IsConvertedToThePropertyType()
    {
        Environment.SetEnvironmentVariable("SHOPGUARD_DefaultTimeoutMs", "120000");

        SettingsLoader.Load(_jsonPath).DefaultTimeoutMs.Should().Be(120_000);
    }

    [Test]
    public void Load_HeadlessEnvVariable_WinsOverFile()
    {
        Environment.SetEnvironmentVariable("HEADLESS", "false");

        var settings = SettingsLoader.Load(_jsonPath);

        settings.Headless.Should().BeFalse();
    }

    [Test]
    public void Load_MissingFile_ThrowsFileNotFound()
    {
        var act = () => SettingsLoader.Load("/nonexistent/appsettings.test.json");

        act.Should().Throw<FileNotFoundException>();
    }

    [Test]
    public void Load_MissingSection_Throws()
    {
        File.WriteAllText(_jsonPath, """{ "SomethingElse": {} }""");

        var act = () => SettingsLoader.Load(_jsonPath);

        act.Should().Throw<InvalidOperationException>().WithMessage("*TestSettings*");
    }

    [Test]
    public void Load_MissingRequiredValues_NamesEveryMissingKey()
    {
        File.WriteAllText(_jsonPath, """
            {
              "TestSettings": {
                "ShopBaseUrl": "https://shop.example.com",
                "ApiBaseUrl": "https://booking.example.com",
                "ApiUsername": "admin"
              }
            }
            """);

        var act = () => SettingsLoader.Load(_jsonPath);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ShopApiBaseUrl*")
            .WithMessage("*ApiPassword*");
    }
}
