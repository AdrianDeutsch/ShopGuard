namespace ShopGuard.Core.Database;

// Rows of the Toolshop database, reduced to the columns the tests assert on.

/// <summary>An order as the shop stored it (table invoices, with the e-mail of its customer).</summary>
public sealed record Invoice
{
    public required string Id { get; init; }

    public required string InvoiceNumber { get; init; }

    /// <summary>E-mail of the customer who placed the order; null for an order without account.</summary>
    public string? CustomerEmail { get; init; }

    public decimal Subtotal { get; init; }

    public decimal Total { get; init; }

    public required string Status { get; init; }

    public required string BillingStreet { get; init; }

    public required string BillingCity { get; init; }
}

/// <summary>One position of an order (table invoice_items, with the product name).</summary>
public sealed record InvoiceLine
{
    public required string ProductName { get; init; }

    public decimal UnitPrice { get; init; }

    public int Quantity { get; init; }
}

/// <summary>A customer account (table users).</summary>
public sealed record Customer
{
    public required string Id { get; init; }

    public required string Email { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public string? Street { get; init; }

    public string? HouseNumber { get; init; }

    public string? City { get; init; }

    public string? State { get; init; }

    public string? PostalCode { get; init; }

    public string? Country { get; init; }
}
