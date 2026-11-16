using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ShopGuard.Core.Models;

namespace ShopGuard.Core.Api;

/// <summary>
/// Client for the parts of the Toolshop REST API the tests need to prepare their data.
/// UI scenarios create their customer account here instead of clicking through the
/// registration form each time: the setup is faster and a broken form only fails the
/// scenario that is about the form.
/// </summary>
public sealed class ShopApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient httpClient;

    public ShopApiClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
        if (!httpClient.DefaultRequestHeaders.Accept.Any())
        {
            // Without it Laravel answers validation errors with an HTML redirect instead of JSON.
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
    }

    /// <summary>Registers a customer account via POST /users/register.</summary>
    /// <returns>The id the shop assigned to the new customer.</returns>
    public async Task<string> RegisterCustomerAsync(CustomerAccount account, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("users/register", account, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);

        var created = await response.Content.ReadFromJsonAsync<RegisteredCustomer>(JsonOptions, ct);
        return created?.Id ?? throw new ApiException(
            response.StatusCode, null, "The registration response did not contain a customer id.");
    }

    /// <summary>Signs in via POST /users/login.</summary>
    /// <returns>The access token, or null when the shop rejected the credentials (HTTP 401).</returns>
    public async Task<string?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync("users/login", new { email, password }, JsonOptions, ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        await EnsureSuccessAsync(response, ct);
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions, ct);
        return login?.AccessToken;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        throw new ApiException(
            response.StatusCode,
            body,
            $"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri} failed with {(int)response.StatusCode} {response.StatusCode}: {body}");
    }

    private sealed record RegisteredCustomer(string? Id);

    private sealed record LoginResponse([property: JsonPropertyName("access_token")] string? AccessToken);
}
