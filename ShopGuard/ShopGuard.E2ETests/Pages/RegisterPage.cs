using System.Text.Json;
using Microsoft.Playwright;
using ShopGuard.Core.Models;
using ShopGuard.E2ETests.Support;

namespace ShopGuard.E2ETests.Pages;

/// <summary>
/// Page object for /auth/register.
/// </summary>
public sealed class RegisterPage(IPage page) : BasePage(page)
{
    private static readonly JsonSerializerOptions SnakeCase =
        new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    private ILocator Field(string dataTest) => Page.Locator($"[data-test='{dataTest}']");

    public Task OpenAsync() => NavigateAsync("auth/register");

    /// <summary>
    /// Fills in and submits the registration form. The shop redirects to the login page on success.
    /// </summary>
    /// <returns>
    /// The address the form sent. It can differ from the typed one: once country, postal code
    /// and house number are known, the form looks the address up (GET /postcode-lookup) and
    /// replaces street, city and state with the result, whenever that answer arrives. The
    /// request the form finally sends is the one reliable record of what was submitted.
    /// </returns>
    public async Task<SubmittedRegistration> RegisterAsync(CustomerAccount account)
    {
        await Field("first-name").FillAsync(account.FirstName);
        await Field("last-name").FillAsync(account.LastName);
        await Field("dob").FillAsync(account.DateOfBirth.ToString("yyyy-MM-dd"));
        await Field("phone").FillAsync(account.Phone);
        await Field("email").FillAsync(account.Email);
        await Field("password").FillAsync(account.Password);

        await Field("street").FillAsync(account.Address.Street);
        await Field("house_number").FillAsync(account.Address.HouseNumber);
        await Field("postal_code").FillAsync(account.Address.PostalCode);
        await Field("city").FillAsync(account.Address.City);
        await Field("state").FillAsync(account.Address.State);
        // The option values are ISO country codes, the same ones the API expects.
        await Field("country").SelectOptionAsync(new SelectOptionValue { Value = account.Address.Country });

        // The lookup never touches the house number, so the field can be read before the click.
        var houseNumberShown = await Field("house_number").InputValueAsync();
        var request = await Page.RunAndWaitForRequestAsync(
            () => Field("register-submit").ClickAsync(),
            candidate => candidate.Method == "POST" && candidate.Url.EndsWith("/users/register"));
        await Page.WaitForURLAsync("**/auth/login");

        var body = JsonSerializer.Deserialize<RegistrationRequest>(request.PostData ?? "{}", SnakeCase);
        return new SubmittedRegistration(
            body?.Address ?? throw new InvalidOperationException("The registration request contained no address."),
            houseNumberShown);
    }

    private sealed record RegistrationRequest(SentAddress? Address);
}
