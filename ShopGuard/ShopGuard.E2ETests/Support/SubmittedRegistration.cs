namespace ShopGuard.E2ETests.Support;

/// <summary>
/// What the registration form did when "Register" was clicked.
/// </summary>
/// <param name="SentAddress">The address in the request the form sent to the API.</param>
/// <param name="HouseNumberShown">The content of the house number field at that moment.</param>
public sealed record SubmittedRegistration(SentAddress SentAddress, string HouseNumberShown);

/// <summary>
/// Address part of the body of POST /users/register. Every field is optional here, because
/// the point of reading the request is to see which fields the form really sent.
/// </summary>
public sealed record SentAddress(
    string? Street,
    string? HouseNumber,
    string? City,
    string? State,
    string? Country,
    string? PostalCode);
