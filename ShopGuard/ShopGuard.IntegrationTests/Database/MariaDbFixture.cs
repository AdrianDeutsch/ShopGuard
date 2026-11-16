using Dapper;
using MySqlConnector;
using Testcontainers.MariaDb;

namespace ShopGuard.IntegrationTests.Database;

/// <summary>
/// Starts one throw-away MariaDB for all tests of this namespace and creates the Toolshop
/// tables in it. The image is the same major version the shop itself runs on, so the SQL of
/// <c>ShopDatabase</c> is checked against the real dialect, not against a stand-in engine.
/// </summary>
[SetUpFixture]
public sealed class MariaDbFixture
{
    private static MariaDbContainer? _container;

    public static string ConnectionString
        => _container?.GetConnectionString()
           ?? throw new InvalidOperationException("The MariaDB container has not been started.");

    [OneTimeSetUp]
    public async Task StartDatabaseAsync()
    {
        _container = new MariaDbBuilder("mariadb:10.6").Build();
        await _container.StartAsync();

        var schemaPath = Path.Combine(AppContext.BaseDirectory, "Database", "toolshop-schema.sql");
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.ExecuteAsync(await File.ReadAllTextAsync(schemaPath));
    }

    [OneTimeTearDown]
    public async Task StopDatabaseAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
