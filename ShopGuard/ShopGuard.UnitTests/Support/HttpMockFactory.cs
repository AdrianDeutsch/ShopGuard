using System.Net;
using System.Text;
using Moq;
using Moq.Protected;
using ShopGuard.Core.Api;

namespace ShopGuard.UnitTests.Support;

/// <summary>
/// Builds the API clients on top of a Moq-mocked HttpMessageHandler, so unit tests can
/// assert URLs, headers and payloads without any network access.
/// </summary>
public sealed class HttpMockFactory
{
    public const string BaseAddress = "https://api.test/";

    private readonly Queue<(HttpStatusCode Status, string Body)> _responses = new();

    /// <summary>All requests the client has sent, in order.</summary>
    public List<CapturedRequest> Requests { get; } = [];

    /// <summary>Enqueues the response for the next request (FIFO; the last one repeats).</summary>
    public HttpMockFactory Respond(HttpStatusCode status, string body = "")
    {
        _responses.Enqueue((status, body));
        return this;
    }

    /// <summary>Creates a booking API client wired to the mocked handler.</summary>
    public BookingApiClient CreateClient() => new(CreateHttpClient());

    /// <summary>Creates a shop API client wired to the mocked handler.</summary>
    public ShopApiClient CreateShopClient() => new(CreateHttpClient());

    private HttpClient CreateHttpClient()
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (request, _) =>
            {
                // Capture before HttpClient disposes the request content.
                var body = request.Content is null
                    ? null
                    : await request.Content.ReadAsStringAsync();
                var headers = request.Headers
                    .ToDictionary(h => h.Key, h => string.Join(";", h.Value));
                Requests.Add(new CapturedRequest(request.Method, request.RequestUri, body, headers));

                var (status, responseBody) = _responses.Count > 1 ? _responses.Dequeue() : _responses.Peek();
                return new HttpResponseMessage(status)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
                    RequestMessage = request,
                };
            });

        return new HttpClient(handler.Object) { BaseAddress = new Uri(BaseAddress) };
    }
}
