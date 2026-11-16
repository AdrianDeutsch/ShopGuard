using Dapper;
using MySqlConnector;

namespace ShopGuard.IntegrationTests.Database;

/// <summary>
/// Writes rows into the test database. Only the columns that matter for a test are
/// parameters; everything else the schema requires gets a fixed value.
/// </summary>
internal sealed class ShopData(string connectionString)
{
    private int _sequence;

    // The shop uses 26-character ULIDs as keys; any unique 26-character string does here.
    private string NewId() => $"{Interlocked.Increment(ref _sequence):D26}";

    public async Task ClearAsync()
    {
        await using var connection = new MySqlConnection(connectionString);
        // Children first, so the foreign keys stay switched on.
        await connection.ExecuteAsync(
            """
            DELETE FROM payments;
            DELETE FROM invoice_items;
            DELETE FROM invoices;
            DELETE FROM products;
            DELETE FROM users;
            """);
    }

    public async Task<string> AddCustomerAsync(
        string email, string firstName = "Anna", string lastName = "Muster",
        string? street = "Hauptplatz", string? houseNumber = "5", string? city = "Wien")
    {
        var id = NewId();
        await using var connection = new MySqlConnection(connectionString);
        await connection.ExecuteAsync(
            """
            INSERT INTO users (id, first_name, last_name, street, house_number, city, state,
                               postal_code, country, email, role)
            VALUES (@id, @firstName, @lastName, @street, @houseNumber, @city, 'Wien', '1010', 'AT', @email, 'user')
            """,
            new { id, firstName, lastName, street, houseNumber, city, email });
        return id;
    }

    public async Task<string> AddProductAsync(string name, decimal price)
    {
        var id = NewId();
        await using var connection = new MySqlConnection(connectionString);
        await connection.ExecuteAsync(
            """
            INSERT INTO products (id, name, price, is_location_offer, is_rental, brand_id, category_id)
            VALUES (@id, @name, @price, 0, 0, 'brand', 'category')
            """,
            new { id, name, price });
        return id;
    }

    public async Task<string> AddInvoiceAsync(
        string invoiceNumber, string? customerId, decimal total,
        string status = "AWAITING_FULFILLMENT", DateTime? createdAt = null)
    {
        var id = NewId();
        var created = createdAt ?? new DateTime(2026, 1, 15, 10, 0, 0);
        await using var connection = new MySqlConnection(connectionString);
        await connection.ExecuteAsync(
            """
            INSERT INTO invoices (id, invoice_date, invoice_number, billing_street, billing_city,
                                  billing_country, subtotal, total, status, user_id, created_at)
            VALUES (@id, @created, @invoiceNumber, 'Hauptplatz 5', 'Wien', 'AT', @total, @total, @status,
                    @customerId, @created)
            """,
            new { id, created, invoiceNumber, total, status, customerId });
        return id;
    }

    public async Task AddInvoiceLineAsync(string invoiceId, string productId, decimal unitPrice, int quantity)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.ExecuteAsync(
            """
            INSERT INTO invoice_items (id, unit_price, quantity, invoice_id, product_id)
            VALUES (@id, @unitPrice, @quantity, @invoiceId, @productId)
            """,
            new { id = NewId(), unitPrice, quantity, invoiceId, productId });
    }

    public async Task AddPaymentAsync(string invoiceId, string paymentMethod, DateTime? createdAt = null)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.ExecuteAsync(
            """
            INSERT INTO payments (id, invoice_id, payment_method, created_at)
            VALUES (@id, @invoiceId, @paymentMethod, @created)
            """,
            new { id = NewId(), invoiceId, paymentMethod, created = createdAt ?? new DateTime(2026, 1, 15, 10, 0, 0) });
    }
}
