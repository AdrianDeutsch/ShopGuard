using System.Text.Json.Serialization;

namespace ShopGuard.Core.Models;

/// <summary>
/// Registration payload of the Toolshop API (POST /users/register).
/// </summary>
public sealed record CustomerAccount
{
    [JsonPropertyName("first_name")]
    public required string FirstName { get; init; }

    [JsonPropertyName("last_name")]
    public required string LastName { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("password")]
    public required string Password { get; init; }

    [JsonPropertyName("dob")]
    public required DateOnly DateOfBirth { get; init; }

    [JsonPropertyName("phone")]
    public required string Phone { get; init; }

    [JsonPropertyName("address")]
    public required CustomerAddress Address { get; init; }
}

public sealed record CustomerAddress
{
    [JsonPropertyName("street")]
    public required string Street { get; init; }

    [JsonPropertyName("house_number")]
    public required string HouseNumber { get; init; }

    [JsonPropertyName("city")]
    public required string City { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("country")]
    public required string Country { get; init; }

    [JsonPropertyName("postal_code")]
    public required string PostalCode { get; init; }
}
