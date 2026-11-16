using Microsoft.Extensions.Configuration;

namespace ShopGuard.Core.Configuration;

/// <summary>
/// Loads <see cref="TestSettings"/> in three layers. Each layer only replaces the values it
/// defines; the highest one wins:
/// <list type="number">
/// <item>section "TestSettings" of the JSON file (defaults)</item>
/// <item>section "Environments:{name}" of the JSON file (per target environment)</item>
/// <item>environment variables SHOPGUARD_{Property}, e.g. SHOPGUARD_ApiPassword (CI secrets)</item>
/// </list>
/// </summary>
public static class SettingsLoader
{
    public const string EnvVarPrefix = "SHOPGUARD_";

    public static TestSettings Load(string jsonPath, string environment = "local")
    {
        if (!File.Exists(jsonPath))
        {
            throw new FileNotFoundException($"Test configuration not found: {jsonPath}", jsonPath);
        }

        var file = new ConfigurationBuilder().AddJsonFile(jsonPath, optional: false).Build();
        var settings = file.GetSection("TestSettings").Get<TestSettings>()
            ?? throw new InvalidOperationException("Section 'TestSettings' is missing or invalid.");

        file.GetSection($"Environments:{environment}").Bind(settings);
        new ConfigurationBuilder().AddEnvironmentVariables(EnvVarPrefix).Build().Bind(settings);

        // HEADLESS=false is the conventional switch to watch the browser; it wins over everything.
        if (bool.TryParse(Environment.GetEnvironmentVariable("HEADLESS"), out var headless))
        {
            settings = settings with { Headless = headless };
        }

        EnsureComplete(settings);
        return settings;
    }

    // "required" is only checked by the compiler; a key missing in the JSON file would otherwise
    // surface much later as a NullReferenceException somewhere in a test.
    private static void EnsureComplete(TestSettings settings)
    {
        var missing = new (string Name, string? Value)[]
            {
                (nameof(TestSettings.ShopBaseUrl), settings.ShopBaseUrl),
                (nameof(TestSettings.ShopApiBaseUrl), settings.ShopApiBaseUrl),
                (nameof(TestSettings.ApiBaseUrl), settings.ApiBaseUrl),
                (nameof(TestSettings.ApiUsername), settings.ApiUsername),
                (nameof(TestSettings.ApiPassword), settings.ApiPassword),
            }
            .Where(entry => string.IsNullOrWhiteSpace(entry.Value))
            .Select(entry => entry.Name)
            .ToList();

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Test configuration is incomplete. Missing: {string.Join(", ", missing)}.");
        }
    }
}
