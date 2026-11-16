using NUnit.Framework;
using Reqnroll;
using ShopGuard.E2ETests.Database;

namespace ShopGuard.E2ETests.Hooks;

[Binding]
public sealed class DatabaseHooks
{
    /// <summary>
    /// Database checks need direct access to the shop's MariaDB, which only the self-hosted
    /// shop offers. Against any other target the scenario is reported as skipped, with the
    /// reason, instead of failing on a connection error.
    /// </summary>
    [BeforeScenario("@db", Order = 0)]
    public static void SkipWithoutDatabaseAccess()
    {
        if (!ShopDatabaseContext.IsConfigured)
        {
            Assert.Ignore(
                "No shop database configured for this environment. " +
                "Run with SHOPGUARD_ENV=selfhosted against the Toolshop from docker/toolshop.compose.yml.");
        }
    }
}
