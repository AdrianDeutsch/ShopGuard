using AwesomeAssertions;
using Reqnroll;
using ShopGuard.E2ETests.Pages;
using ShopGuard.E2ETests.Support;

namespace ShopGuard.E2ETests.StepDefinitions;

[Binding]
public sealed class SearchSteps(PlaywrightDriver driver)
{
    private SearchResultsPage SearchResultsPage => new(driver.Page);

    [Given("ich bin auf der Startseite des Shops")]
    public Task GivenOnShopHomePage()
        => driver.Page.GotoAsync(TestConfiguration.Settings.ShopBaseUrl);

    [When("ich nach {string} suche")]
    public Task WhenSearchFor(string searchTerm)
        => SearchResultsPage.SearchAsync(searchTerm);

    [Then("sehe ich mindestens {int} Suchergebnis(se)")]
    public async Task ThenAtLeastResults(int minimumCount)
        => (await SearchResultsPage.GetProductTitlesAsync()).Should().HaveCountGreaterThanOrEqualTo(minimumCount);

    [Then("enthält ein Suchergebnis den Text {string}")]
    public async Task ThenResultContains(string expectedText)
    {
        var titles = await SearchResultsPage.GetProductTitlesAsync();
        titles.Should().Contain(title => title.Contains(expectedText, StringComparison.OrdinalIgnoreCase));
    }

    [Then("sehe ich den Hinweis, dass keine Produkte gefunden wurden")]
    public async Task ThenNoProductsFoundShown()
        => (await SearchResultsPage.HasNoResultsAsync()).Should().BeTrue();
}
