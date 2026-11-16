using Dapper;
using MySqlConnector;

namespace ShopGuard.Core.Database;

/// <summary>
/// Read-only access to the MariaDB of the Toolshop. The tests use it to check what the shop
/// really stored after an action in the UI. It never writes: test data is created through the
/// shop itself, so the database only ever contains states the shop can produce.
/// </summary>
public sealed class ShopDatabase(string connectionString)
{
    /// <summary>
    /// Returns every invoice with the given number. A list, not a single row: the column has
    /// no unique constraint, so "exactly one" is something a test has to assert.
    /// </summary>
    public async Task<IReadOnlyList<Invoice>> FindInvoicesAsync(string invoiceNumber)
    {
        await using var connection = new MySqlConnection(connectionString);
        var invoices = await connection.QueryAsync<Invoice>(
            """
            SELECT i.id             AS Id,
                   i.invoice_number AS InvoiceNumber,
                   u.email          AS CustomerEmail,
                   i.subtotal       AS Subtotal,
                   i.total          AS Total,
                   i.status         AS Status,
                   i.billing_street AS BillingStreet,
                   i.billing_city   AS BillingCity
            FROM invoices i
            LEFT JOIN users u ON u.id = i.user_id
            WHERE i.invoice_number = @invoiceNumber
            ORDER BY i.created_at
            """,
            new { invoiceNumber });
        return invoices.ToList();
    }

    /// <summary>Returns the positions of an invoice, sorted by product name.</summary>
    public async Task<IReadOnlyList<InvoiceLine>> GetInvoiceLinesAsync(string invoiceId)
    {
        await using var connection = new MySqlConnection(connectionString);
        var lines = await connection.QueryAsync<InvoiceLine>(
            """
            SELECT pr.name       AS ProductName,
                   ii.unit_price AS UnitPrice,
                   ii.quantity   AS Quantity
            FROM invoice_items ii
            JOIN products pr ON pr.id = ii.product_id
            WHERE ii.invoice_id = @invoiceId
            ORDER BY pr.name
            """,
            new { invoiceId });
        return lines.ToList();
    }

    /// <summary>
    /// Returns the payment methods recorded for an invoice, oldest first. Payments are a
    /// query of their own: joined to the invoice, a second payment would make one invoice look
    /// like two.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetPaymentMethodsAsync(string invoiceId)
    {
        await using var connection = new MySqlConnection(connectionString);
        var methods = await connection.QueryAsync<string>(
            "SELECT payment_method FROM payments WHERE invoice_id = @invoiceId ORDER BY created_at, id",
            new { invoiceId });
        return methods.ToList();
    }

    /// <summary>Returns the customer account with the given e-mail address, or null.</summary>
    public async Task<Customer?> FindCustomerAsync(string email)
    {
        await using var connection = new MySqlConnection(connectionString);
        return await connection.QuerySingleOrDefaultAsync<Customer>(
            """
            SELECT id           AS Id,
                   email        AS Email,
                   first_name   AS FirstName,
                   last_name    AS LastName,
                   street       AS Street,
                   house_number AS HouseNumber,
                   city         AS City,
                   state        AS State,
                   postal_code  AS PostalCode,
                   country      AS Country
            FROM users
            WHERE email = @email
            """,
            new { email });
    }
}
