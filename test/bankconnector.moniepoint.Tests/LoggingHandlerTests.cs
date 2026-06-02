using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using zone.bankconnector.moniepoint.Https.Handlers;

namespace zone.bankconnector.moniepoint.Tests;

public class LoggingHandlerTests
{

    private static (HttpClient client, LoggingHandler handler) Build(
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string responseBody = "{}")
    {
        var logger  = NullLogger<LoggingHandler>.Instance;
        var handler = new LoggingHandler(logger)
        {
            InnerHandler = new StubHttpHandler(statusCode, responseBody)
        };
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        return (client, handler);
    }


    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new LoggingHandler(null!));
    }


    [Fact]
    public async Task SendAsync_GetRequest_ReturnsResponseFromInner()
    {
        var (client, _) = Build(HttpStatusCode.OK, "{\"status\":\"ok\"}");
        var response = await client.GetAsync("http://localhost/test");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_GetRequest_SuccessStatus_DoesNotThrow()
    {
        var (client, _) = Build(HttpStatusCode.OK);
        var exception = await Record.ExceptionAsync(() => client.GetAsync("http://localhost/test"));
        Assert.Null(exception);
    }


    [Fact]
    public async Task SendAsync_PostRequest_SuccessStatus_ReturnsResponse()
    {
        var (client, _) = Build(HttpStatusCode.OK, "{\"responseCode\":\"00\"}");
        var content  = new StringContent("{\"request\":\"HEXDATA\"}", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("http://localhost/ne", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SendAsync_PostRequest_NonSuccess_StillReturnsResponse()
    {
        var (client, _) = Build(HttpStatusCode.BadRequest, "{\"statusMessage\":\"Bad request\"}");
        var content  = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("http://localhost/ne", content);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }


    [Fact]
    public async Task SendAsync_SensitiveJsonBody_RequestReachesInnerHandlerUnmodified()
    {
        const string body = "{\"request\":\"HEXHEXHEX\",\"bvn\":\"12345678901\"}";
        string? receivedBody = null;

        var logger  = NullLogger<LoggingHandler>.Instance;
        var inner   = new CapturingHttpHandler(req =>
        {
            receivedBody = req.Content!.ReadAsStringAsync().Result;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
        });
        var handler = new LoggingHandler(logger) { InnerHandler = inner };
        var client  = new HttpClient(handler);

        await client.PostAsync("http://localhost/ne",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(body, receivedBody);
    }


    [Fact]
    public async Task SendAsync_InnerHandlerThrows_ExceptionPropagates()
    {
        var logger  = NullLogger<LoggingHandler>.Instance;
        var inner   = new ThrowingHttpHandler();
        var handler = new LoggingHandler(logger) { InnerHandler = inner };
        var client  = new HttpClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetAsync("http://localhost/test"));
    }

    private sealed class StubHttpHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string         _body;

        public StubHttpHandler(HttpStatusCode statusCode, string body)
        {
            _statusCode = statusCode;
            _body       = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var resp = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            };
            return Task.FromResult(resp);
        }
    }

    private sealed class CapturingHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
        public CapturingHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            => _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_handler(request));
    }

    private sealed class ThrowingHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("Simulated network failure");
    }
}
