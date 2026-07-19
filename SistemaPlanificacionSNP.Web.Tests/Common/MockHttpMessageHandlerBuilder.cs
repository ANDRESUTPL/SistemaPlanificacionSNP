using Moq;
using Moq.Protected;

namespace SistemaPlanificacionSNP.Web.Tests.Common;

public sealed class MockHttpMessageHandlerBuilder
{
    private readonly Queue<HttpResponseMessage> _responses = new();

    public List<CapturedHttpRequest> Requests { get; } = new();

    public MockHttpMessageHandlerBuilder RespondWith(HttpResponseMessage response)
    {
        _responses.Enqueue(response);
        return this;
    }

    public HttpMessageHandler Build()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                Requests.Add(CapturedHttpRequest.From(request));
                return _responses.Count > 0
                    ? _responses.Dequeue()
                    : new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
            });

        return handlerMock.Object;
    }
}

public sealed record CapturedHttpRequest(
    HttpMethod Method,
    Uri? RequestUri,
    string? AuthorizationScheme,
    string? AuthorizationParameter,
    string? Content)
{
    public static CapturedHttpRequest From(HttpRequestMessage request)
    {
        var content = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();

        return new CapturedHttpRequest(
            request.Method,
            request.RequestUri,
            request.Headers.Authorization?.Scheme,
            request.Headers.Authorization?.Parameter,
            content);
    }
}