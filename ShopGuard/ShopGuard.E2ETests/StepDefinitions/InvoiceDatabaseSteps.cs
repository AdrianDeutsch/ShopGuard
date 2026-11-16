using AwesomeAssertions;
using Reqnroll;
using ShopGuard.Core.Helpers;
using ShopGuard.E2ETests.Database;
using ShopGuard.E2ETests.Support;

namespace ShopGuard.E2ETests.StepDefinitions;

/// <summary>
/// Compares what the customer saw in the browser with what the shop wrote to its database.
/// </summary>
[Binding]
public sealed class InvoiceDatabaseSteps(ShopDatabaseContext database, ScenarioState state)
{
    [Then("ist zur Rechnungsnummer genau eine Rechnung in der Shop-Datenbank gespeichert")]
    public async Task ThenExactlyOneInvoiceIsPersisted()
    {
        var invoices = await ShopDatabaseContext.EventuallyAsync(
            () => database.Shop.FindInvoicesAsync(state.InvoiceNumber!),
            found => found.Count > 0);

        invoices.Should().ContainSingle($"invoice number {state.InvoiceNumber} was shown to the customer");
        state.PersistedInvoice = invoices[0];
    }

    [Then("gehört die Rechnung zum angemeldeten Kunden")]
    public void ThenInvoiceBelongsToCustomer()
        => state.PersistedInvoice!.CustomerEmail.Should().Be(state.Customer!.Email);

    [Then("entspricht der gespeicherte Rechnungsbetrag der Summe der bestellten Produkte")]
    public void ThenInvoiceTotalMatchesCart()
    {
        var expected = PriceCalculator.CalculateCartTotal(state.CartItems);

        state.PersistedInvoice!.Subtotal.Should().Be(expected);
        state.PersistedInvoice.Total.Should().Be(expected);
    }

    [Then("enthält die Rechnung genau die bestellten Produkte mit Menge und Preis")]
    public async Task ThenInvoiceLinesMatchCart()
    {
        var lines = await database.Shop.GetInvoiceLinesAsync(state.PersistedInvoice!.Id);

        var expected = state.CartItems
            .OrderBy(item => item.ProductName, StringComparer.Ordinal)
            .Select(item => new { item.ProductName, item.UnitPrice, item.Quantity });
        lines.OrderBy(line => line.ProductName, StringComparer.Ordinal)
            .Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
    }

    [Then("hat die Rechnung in der Datenbank den Status {string}")]
    public void ThenInvoiceHasStatus(string expectedStatus)
        => state.PersistedInvoice!.Status.Should().Be(expectedStatus);

    [Then("ist als Zahlungsart {string} gespeichert")]
    public async Task ThenPaymentMethodIs(string expectedPaymentMethod)
    {
        var paymentMethods = await database.Shop.GetPaymentMethodsAsync(state.PersistedInvoice!.Id);

        // Exactly one payment: an order that was paid once must not be recorded twice.
        paymentMethods.Should().Equal(expectedPaymentMethod);
    }
}
