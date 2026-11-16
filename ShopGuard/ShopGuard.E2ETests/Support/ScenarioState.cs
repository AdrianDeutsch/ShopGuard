using ShopGuard.Core.Database;
using ShopGuard.Core.Helpers;
using ShopGuard.Core.Models;

namespace ShopGuard.E2ETests.Support;

/// <summary>
/// Scenario-scoped state bag shared between step definition classes
/// (injected by Reqnroll context injection instead of stringly-typed ScenarioContext keys).
/// </summary>
public sealed class ScenarioState
{
    // Shop (UI + database) state
    public CustomerAccount? Customer { get; set; }
    public SubmittedRegistration? Registration { get; set; }
    public List<CartItem> CartItems { get; } = [];
    public string? InvoiceNumber { get; set; }
    public Invoice? PersistedInvoice { get; set; }

    // Booking API state
    public Booking? RequestBooking { get; set; }
    public BookingCreatedResponse? CreatedBooking { get; set; }
    public Booking? FetchedBooking { get; set; }
    public string? AuthToken { get; set; }
    public int LastStatusCode { get; set; }
}
