namespace ShopGuard.Core.Configuration;

/// <summary>
/// Strongly typed test configuration, loaded by <see cref="SettingsLoader"/> from
/// appsettings.test.json, an environment section and SHOPGUARD_* environment variables.
/// </summary>
public sealed record TestSettings
{
    /// <summary>Web shop under test (Toolshop UI).</summary>
    public required string ShopBaseUrl { get; init; }

    /// <summary>REST API of the web shop; used to prepare test data such as customer accounts.</summary>
    public required string ShopApiBaseUrl { get; init; }

    /// <summary>
    /// MariaDB of the web shop. Empty when the target gives no database access (the public
    /// demo); scenarios tagged @db are skipped then.
    /// </summary>
    public string ShopDbConnectionString { get; init; } = string.Empty;

    /// <summary>Booking API under test (restful-booker).</summary>
    public required string ApiBaseUrl { get; init; }

    public required string ApiUsername { get; init; }

    public required string ApiPassword { get; init; }

    public bool Headless { get; init; } = true;

    public int DefaultTimeoutMs { get; init; } = 30_000;
}
