using AwesomeAssertions;
using ShopGuard.Core.Database;

namespace ShopGuard.IntegrationTests.Database;

/// <summary>
/// Checks the queries of <see cref="ShopDatabase"/> against a real MariaDB with the shop's
/// own table definitions. The end-to-end scenarios use the same class against the running
/// shop; these tests cover the cases a healthy shop does not produce on demand
/// (no payment or two payments, duplicate invoice number, order without account).
/// </summary>
[TestFixture]
public sealed class ShopDatabaseTests
{
    private ShopData _data = null!;
    private ShopDatabase _shop = null!;

    [SetUp]
    public async Task ResetDatabaseAsync()
    {
        _data = new ShopData(MariaDbFixture.ConnectionString);
        _shop = new ShopDatabase(MariaDbFixture.ConnectionString);
        await _data.ClearAsync();
    }

    // ---- Invoices -------------------------------------------------------------------------

    [Test]
    public async Task FindInvoices_ReturnsTheInvoiceWithItsCustomer()
    {
        var customerId = await _data.AddCustomerAsync("anna@example.com");
        var invoiceId = await _data.AddInvoiceAsync("INV-2026000001", customerId, total: 49.90m);

        var invoices = await _shop.FindInvoicesAsync("INV-2026000001");

        var invoice = invoices.Should().ContainSingle().Which;
        invoice.Id.Should().Be(invoiceId);
        invoice.InvoiceNumber.Should().Be("INV-2026000001");
        invoice.CustomerEmail.Should().Be("anna@example.com");
        invoice.Subtotal.Should().Be(49.90m);
        invoice.Total.Should().Be(49.90m);
        invoice.Status.Should().Be("AWAITING_FULFILLMENT");
        invoice.BillingStreet.Should().Be("Hauptplatz 5");
        invoice.BillingCity.Should().Be("Wien");
    }

    [Test]
    public async Task FindInvoices_UnknownNumber_ReturnsNothing()
    {
        await _data.AddInvoiceAsync("INV-2026000001", customerId: null, total: 10m);

        (await _shop.FindInvoicesAsync("INV-DOES-NOT-EXIST")).Should().BeEmpty();
    }

    [Test]
    public async Task FindInvoices_InvoiceWithTwoPayments_IsReturnedOnce()
    {
        // A join to payments would return this invoice twice and make it look like a duplicate.
        var invoiceId = await _data.AddInvoiceAsync("INV-2026000002", customerId: null, total: 10m);
        await _data.AddPaymentAsync(invoiceId, "cash-on-delivery");
        await _data.AddPaymentAsync(invoiceId, "bank-transfer");

        (await _shop.FindInvoicesAsync("INV-2026000002")).Should().ContainSingle();
    }

    [Test]
    public async Task FindInvoices_InvoiceWithoutCustomerAccount_IsStillFound()
    {
        await _data.AddInvoiceAsync("INV-2026000003", customerId: null, total: 10m);

        var invoice = (await _shop.FindInvoicesAsync("INV-2026000003")).Should().ContainSingle().Which;

        invoice.CustomerEmail.Should().BeNull();
    }

    [Test]
    public async Task FindInvoices_NumberUsedTwice_ReturnsBothOldestFirst()
    {
        // The schema has no unique key on invoice_number, so this state is possible. The
        // end-to-end scenario asserts "exactly one" and would catch it.
        var first = await _data.AddInvoiceAsync("INV-DUPLICATE", null, 10m, createdAt: new DateTime(2026, 1, 1));
        var second = await _data.AddInvoiceAsync("INV-DUPLICATE", null, 20m, createdAt: new DateTime(2026, 1, 2));

        var invoices = await _shop.FindInvoicesAsync("INV-DUPLICATE");

        invoices.Select(invoice => invoice.Id).Should().Equal(first, second);
    }

    [Test]
    public async Task FindInvoices_TreatsTheNumberAsDataNotAsSql()
    {
        await _data.AddInvoiceAsync("INV-2026000004", customerId: null, total: 10m);

        (await _shop.FindInvoicesAsync("' OR '1'='1")).Should().BeEmpty();
    }

    [TestCase("ON_HOLD")]
    [TestCase("SHIPPED")]
    [TestCase("COMPLETED")]
    public async Task FindInvoices_ReturnsTheStatusAsText(string status)
    {
        await _data.AddInvoiceAsync("INV-STATUS", customerId: null, total: 10m, status: status);

        (await _shop.FindInvoicesAsync("INV-STATUS")).Single().Status.Should().Be(status);
    }

    // ---- Invoice lines --------------------------------------------------------------------

    [Test]
    public async Task GetInvoiceLines_ReturnsProductNamePriceAndQuantitySortedByName()
    {
        var pliers = await _data.AddProductAsync("Combination Pliers", 14.15m);
        var hammer = await _data.AddProductAsync("Claw Hammer", 11.48m);
        var invoiceId = await _data.AddInvoiceAsync("INV-2026000005", null, 39.78m);
        await _data.AddInvoiceLineAsync(invoiceId, pliers, unitPrice: 14.15m, quantity: 2);
        await _data.AddInvoiceLineAsync(invoiceId, hammer, unitPrice: 11.48m, quantity: 1);

        var lines = await _shop.GetInvoiceLinesAsync(invoiceId);

        lines.Should().BeEquivalentTo(
            [
                new InvoiceLine { ProductName = "Claw Hammer", UnitPrice = 11.48m, Quantity = 1 },
                new InvoiceLine { ProductName = "Combination Pliers", UnitPrice = 14.15m, Quantity = 2 },
            ],
            options => options.WithStrictOrdering());
    }

    [Test]
    public async Task GetInvoiceLines_ReturnsOnlyTheLinesOfThatInvoice()
    {
        var pliers = await _data.AddProductAsync("Combination Pliers", 14.15m);
        var mine = await _data.AddInvoiceAsync("INV-MINE", null, 14.15m);
        var other = await _data.AddInvoiceAsync("INV-OTHER", null, 28.30m);
        await _data.AddInvoiceLineAsync(mine, pliers, 14.15m, 1);
        await _data.AddInvoiceLineAsync(other, pliers, 14.15m, 2);

        var lines = await _shop.GetInvoiceLinesAsync(mine);

        lines.Should().ContainSingle().Which.Quantity.Should().Be(1);
    }

    [Test]
    public async Task GetInvoiceLines_UsesThePriceAtTheTimeOfTheOrder()
    {
        // The product became more expensive after the order; the invoice keeps the old price.
        var pliers = await _data.AddProductAsync("Combination Pliers", price: 19.99m);
        var invoiceId = await _data.AddInvoiceAsync("INV-2026000006", null, 14.15m);
        await _data.AddInvoiceLineAsync(invoiceId, pliers, unitPrice: 14.15m, quantity: 1);

        (await _shop.GetInvoiceLinesAsync(invoiceId)).Single().UnitPrice.Should().Be(14.15m);
    }

    [Test]
    public async Task GetInvoiceLines_UnknownInvoice_ReturnsNothing()
        => (await _shop.GetInvoiceLinesAsync("no-such-invoice")).Should().BeEmpty();

    // ---- Payments -------------------------------------------------------------------------

    [Test]
    public async Task GetPaymentMethods_InvoiceWithoutPayment_ReturnsNothing()
    {
        var invoiceId = await _data.AddInvoiceAsync("INV-2026000007", customerId: null, total: 10m);

        (await _shop.GetPaymentMethodsAsync(invoiceId)).Should().BeEmpty();
    }

    [Test]
    public async Task GetPaymentMethods_ReturnsEveryPaymentOfThatInvoiceOldestFirst()
    {
        var mine = await _data.AddInvoiceAsync("INV-MINE", customerId: null, total: 10m);
        var other = await _data.AddInvoiceAsync("INV-OTHER", customerId: null, total: 10m);
        await _data.AddPaymentAsync(mine, "bank-transfer", createdAt: new DateTime(2026, 1, 16));
        await _data.AddPaymentAsync(mine, "cash-on-delivery", createdAt: new DateTime(2026, 1, 15));
        await _data.AddPaymentAsync(other, "credit-card");

        var methods = await _shop.GetPaymentMethodsAsync(mine);

        methods.Should().Equal("cash-on-delivery", "bank-transfer");
    }

    // ---- Customers ------------------------------------------------------------------------

    [Test]
    public async Task FindCustomer_ReturnsNameAndAddress()
    {
        var id = await _data.AddCustomerAsync(
            "anna@example.com", "Anna", "Muster", street: "Hauptplatz", houseNumber: "5", city: "Wien");

        var customer = await _shop.FindCustomerAsync("anna@example.com");

        customer.Should().BeEquivalentTo(new Customer
        {
            Id = id,
            Email = "anna@example.com",
            FirstName = "Anna",
            LastName = "Muster",
            Street = "Hauptplatz",
            HouseNumber = "5",
            City = "Wien",
            State = "Wien",
            PostalCode = "1010",
            Country = "AT",
        });
    }

    [Test]
    public async Task FindCustomer_AccountWithoutHouseNumber_ReturnsNullForIt()
    {
        // The state DEFECT-003 leaves behind: registered through the form, house number lost.
        await _data.AddCustomerAsync("ben@example.com", houseNumber: null);

        var customer = await _shop.FindCustomerAsync("ben@example.com");

        customer.Should().NotBeNull();
        customer.HouseNumber.Should().BeNull();
    }

    [Test]
    public async Task FindCustomer_UnknownEmail_ReturnsNull()
        => (await _shop.FindCustomerAsync("nobody@example.com")).Should().BeNull();
}
