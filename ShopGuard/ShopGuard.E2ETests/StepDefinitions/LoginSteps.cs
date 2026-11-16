using AwesomeAssertions;
using Reqnroll;
using ShopGuard.Core.Helpers;
using ShopGuard.E2ETests.Pages;
using ShopGuard.E2ETests.Support;

namespace ShopGuard.E2ETests.StepDefinitions;

[Binding]
public sealed class LoginSteps(PlaywrightDriver driver, ShopApiContext shopApi, ScenarioState state)
{
    private LoginPage LoginPage => new(driver.Page);

    [Given("ich besitze ein registriertes Kundenkonto")]
    public async Task GivenRegisteredCustomerAccount()
    {
        // Created through the API, not the registration form: every scenario gets its own
        // account in milliseconds, and a broken form only fails the registration scenario.
        state.Customer = TestDataGenerator.NewCustomer("login");
        await shopApi.Client.RegisterCustomerAsync(state.Customer);
    }

    [Given("ich bin auf der Login-Seite")]
    public Task GivenOnLoginPage() => LoginPage.OpenAsync();

    [Given("ich bin als Kunde angemeldet")]
    public async Task GivenLoggedInCustomer()
    {
        await LoginPage.OpenAsync();
        await LoginPage.LoginAsync(state.Customer!.Email, state.Customer.Password);
        await driver.Page.WaitForURLAsync("**/account");
    }

    [When("ich mich mit gültigen Zugangsdaten anmelde")]
    public async Task WhenLoginWithValidCredentials()
    {
        await LoginPage.LoginAsync(state.Customer!.Email, state.Customer.Password);
        await driver.Page.WaitForURLAsync("**/account");
    }

    [When("ich mich mit dem falschen Passwort {string} anmelde")]
    public Task WhenLoginWithWrongPassword(string wrongPassword)
        => LoginPage.LoginAsync(state.Customer!.Email, wrongPassword);

    [When("ich mich mit der E-Mail {string} und dem Passwort {string} anmelde")]
    public Task WhenLoginWithCredentials(string email, string password)
        => LoginPage.LoginAsync(email, password);

    [Then("bin ich als Kunde angemeldet")]
    public async Task ThenLoggedIn()
    {
        await LoginPage.WaitForLoggedInAsync();
        (await LoginPage.IsLoggedInAsync()).Should().BeTrue();
    }

    [Then("bin ich nicht angemeldet")]
    public async Task ThenNotLoggedIn()
        => (await LoginPage.IsLoggedInAsync()).Should().BeFalse();

    [Then("sehe ich die Fehlermeldung {string}")]
    public async Task ThenErrorMessageShown(string expectedMessage)
    {
        // Wrong password and unknown e-mail must produce the same text, otherwise the login
        // would reveal which e-mail addresses have an account. Both scenarios expect the same
        // message, so a difference between the two cases fails one of them.
        (await LoginPage.GetErrorMessageAsync()).Should().Be(expectedMessage);
    }

    [Then("sehe ich einen Validierungsfehler am E-Mail-Feld")]
    public async Task ThenEmailValidationErrorShown()
        => (await LoginPage.GetEmailValidationErrorAsync()).Should().NotBeEmpty();
}
