using ShopGuard.Core.Database;
using ShopGuard.E2ETests.Support;

namespace ShopGuard.E2ETests.Database;

/// <summary>
/// Scenario-scoped access to the database of the shop under test.
/// </summary>
public sealed class ShopDatabaseContext
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

    /// <summary>False when the target environment gives no database access (the public demo).</summary>
    public static bool IsConfigured
        => !string.IsNullOrWhiteSpace(TestConfiguration.Settings.ShopDbConnectionString);

    public ShopDatabase Shop { get; } = new(TestConfiguration.Settings.ShopDbConnectionString);

    /// <summary>
    /// Repeats a query until it returns a result or the timeout is over. The UI confirms an
    /// order as soon as the API has answered; this bridges the moment until the row is visible
    /// to another connection, without a fixed sleep.
    /// </summary>
    public static async Task<T> EventuallyAsync<T>(
        Func<Task<T>> query, Func<T, bool> isComplete, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        while (true)
        {
            var result = await query();
            if (isComplete(result) || DateTime.UtcNow >= deadline)
            {
                return result;
            }

            await Task.Delay(PollInterval);
        }
    }
}
