using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using ShopGuard.Core.Api;
using ShopGuard.Core.Helpers;
using ShopGuard.UnitTests.Support;

namespace ShopGuard.UnitTests.Api;

[TestFixture]
public sealed class ShopApiClientTests
{
    private const string RegisteredResponse = """{"id":"01HXYZ","email":"anna@example.com"}""";

    [Test]
    public async Task RegisterCustomerAsync_PostsToRegisterEndpoint_AndReturnsTheNewId()
    {
        var factory = new HttpMockFactory().Respond(HttpStatusCode.Created, RegisteredResponse);
        var client = factory.CreateShopClient();

        var id = await client.RegisterCustomerAsync(TestDataGenerator.NewCustomer());

        id.Should().Be("01HXYZ");
        var request = factory.Requests.Single();
        request.Method.Should().Be(HttpMethod.Post);
        request.Uri.Should().Be($"{HttpMockFactory.BaseAddress}users/register");
    }

    [Test]
    public async Task RegisterCustomerAsync_SendsThePayloadInTheShapeTheApiExpects()
    {
        var factory = new HttpMockFactory().Respond(HttpStatusCode.Created, RegisteredResponse);
        var account = TestDataGenerator.NewCustomer("payload");

        await factory.CreateShopClient().RegisterCustomerAsync(account);

        // snake_case keys, ISO date, and the address as a nested object including the house number.
        using var payload = JsonDocument.Parse(factory.Requests.Single().Body!);
        var root = payload.RootElement;
        root.GetProperty("first_name").GetString().Should().Be(account.FirstName);
        root.GetProperty("last_name").GetString().Should().Be(account.LastName);
        root.GetProperty("email").GetString().Should().Be(account.Email);
        root.GetProperty("dob").GetString().Should().Be("1990-01-15");
        var address = root.GetProperty("address");
        address.GetProperty("street").GetString().Should().Be(account.Address.Street);
        address.GetProperty("house_number").GetString().Should().Be(account.Address.HouseNumber);
        address.GetProperty("postal_code").GetString().Should().Be(account.Address.PostalCode);
        address.GetProperty("country").GetString().Should().Be("DE");
    }

    [Test]
    public async Task RegisterCustomerAsync_AsksForJson()
    {
        var factory = new HttpMockFactory().Respond(HttpStatusCode.Created, RegisteredResponse);

        await factory.CreateShopClient().RegisterCustomerAsync(TestDataGenerator.NewCustomer());

        factory.Requests.Single().Headers["Accept"].Should().Contain("application/json");
    }

    [Test]
    public async Task RegisterCustomerAsync_OnValidationError_ThrowsApiExceptionWithTheReason()
    {
        const string validationError = """{"email":["A customer with this email address already exists."]}""";
        var client = new HttpMockFactory()
            .Respond(HttpStatusCode.UnprocessableEntity, validationError)
            .CreateShopClient();

        var act = () => client.RegisterCustomerAsync(TestDataGenerator.NewCustomer());

        var exception = (await act.Should().ThrowAsync<ApiException>()).Which;
        exception.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        exception.ResponseBody.Should().Be(validationError);
        // The reason is part of the message, so a failed test setup explains itself in the report.
        exception.Message.Should().Contain("already exists");
    }

    [Test]
    public async Task RegisterCustomerAsync_ResponseWithoutId_ThrowsApiException()
    {
        var client = new HttpMockFactory().Respond(HttpStatusCode.Created, "{}").CreateShopClient();

        var act = () => client.RegisterCustomerAsync(TestDataGenerator.NewCustomer());

        await act.Should().ThrowAsync<ApiException>().WithMessage("*customer id*");
    }

    [Test]
    public async Task LoginAsync_PostsCredentials_AndReturnsTheAccessToken()
    {
        var factory = new HttpMockFactory()
            .Respond(HttpStatusCode.OK, """{"access_token":"jwt-123","token_type":"bearer","expires_in":300}""");

        var token = await factory.CreateShopClient().LoginAsync("anna@example.com", "Secret!1");

        token.Should().Be("jwt-123");
        var request = factory.Requests.Single();
        request.Uri.Should().Be($"{HttpMockFactory.BaseAddress}users/login");
        request.Body.Should().Contain("\"email\":\"anna@example.com\"").And.Contain("\"password\":\"Secret!1\"");
    }

    [Test]
    public async Task LoginAsync_RejectedCredentials_ReturnsNull()
    {
        var client = new HttpMockFactory()
            .Respond(HttpStatusCode.Unauthorized, """{"error":"Unauthorized"}""")
            .CreateShopClient();

        (await client.LoginAsync("anna@example.com", "wrong")).Should().BeNull();
    }

    [Test]
    public async Task LoginAsync_ServerError_ThrowsApiException()
    {
        var client = new HttpMockFactory()
            .Respond(HttpStatusCode.InternalServerError, "boom")
            .CreateShopClient();

        var act = () => client.LoginAsync("anna@example.com", "Secret!1");

        (await act.Should().ThrowAsync<ApiException>()).Which.StatusCode
            .Should().Be(HttpStatusCode.InternalServerError);
    }
}
