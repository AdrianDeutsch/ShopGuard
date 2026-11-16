using ShopGuard.Core.Api;

namespace ShopGuard.E2ETests.Support;

/// <summary>
/// Scenario-scoped access to the REST API of the shop under test (not the booking API).
/// </summary>
public sealed class ShopApiContext : IDisposable
{
    private readonly Lazy<(HttpClient Http, ShopApiClient Client)> _lazy = new(() =>
    {
        var http = new HttpClient
        {
            // Trailing slash is required so relative paths like "users/register" resolve correctly.
            BaseAddress = new Uri(TestConfiguration.Settings.ShopApiBaseUrl.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(60),
        };
        return (http, new ShopApiClient(http));
    });

    public ShopApiClient Client => _lazy.Value.Client;

    public void Dispose()
    {
        if (_lazy.IsValueCreated)
        {
            _lazy.Value.Http.Dispose();
        }
    }
}
