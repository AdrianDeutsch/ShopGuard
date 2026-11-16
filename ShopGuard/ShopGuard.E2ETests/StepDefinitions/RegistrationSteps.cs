using AwesomeAssertions;
using Reqnroll;
using ShopGuard.Core.Helpers;
using ShopGuard.E2ETests.Database;
using ShopGuard.E2ETests.Pages;
using ShopGuard.E2ETests.Support;

namespace ShopGuard.E2ETests.StepDefinitions;

[Binding]
public sealed class RegistrationSteps(
    PlaywrightDriver driver, ShopApiContext shopApi, ShopDatabaseContext database, ScenarioState state)
{
    private RegisterPage RegisterPage => new(driver.Page);

    [When("ich mich über das Registrierungsformular als neuer Kunde registriere")]
    public async Task WhenRegisterThroughTheForm()
    {
        state.Customer = TestDataGenerator.NewCustomer("register");

        await RegisterPage.OpenAsync();
        // RegisterAsync returns once the shop has redirected to the login page.
        state.Registration = await RegisterPage.RegisterAsync(state.Customer);
    }

    [Then("ist das Kundenkonto mit den abgeschickten Daten in der Shop-Datenbank gespeichert")]
    public async Task ThenCustomerIsPersisted()
    {
        var account = state.Customer!;
        var sent = state.Registration!.SentAddress;

        var customer = await database.Shop.FindCustomerAsync(account.Email);

        customer.Should().NotBeNull("the registration was confirmed in the UI");
        customer.FirstName.Should().Be(account.FirstName);
        customer.LastName.Should().Be(account.LastName);
        customer.Street.Should().Be(sent.Street);
        customer.City.Should().Be(sent.City);
        customer.State.Should().Be(sent.State);
        customer.PostalCode.Should().Be(sent.PostalCode);
        customer.Country.Should().Be(sent.Country);
    }

    [Then("ist die im Formular angezeigte Hausnummer in der Shop-Datenbank gespeichert")]
    public async Task ThenHouseNumberIsPersisted()
    {
        var shown = state.Registration!.HouseNumberShown;
        shown.Should().NotBeEmpty("the form must still show the house number that was typed in");

        var customer = await database.Shop.FindCustomerAsync(state.Customer!.Email);

        customer.Should().NotBeNull();
        customer.HouseNumber.Should().Be(shown);
    }

    [Then("kann sich der neue Kunde über die Shop-API anmelden")]
    public async Task ThenCustomerCanLogInThroughTheApi()
    {
        var token = await shopApi.Client.LoginAsync(state.Customer!.Email, state.Customer.Password);

        token.Should().NotBeNullOrWhiteSpace("the account created through the form must be usable");
    }
}
