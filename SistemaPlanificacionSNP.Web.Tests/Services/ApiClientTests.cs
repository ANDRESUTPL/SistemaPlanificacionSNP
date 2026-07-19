using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SistemaPlanificacionSNP.Web.Services;
using SistemaPlanificacionSNP.Web.Tests.Common;

namespace SistemaPlanificacionSNP.Web.Tests.Services;

public class ApiClientTests
{
    [Fact]
    public async Task SendAsync_WhenTokenExists_ShouldAttachBearerToken()
    {
        var handlerBuilder = new MockHttpMessageHandlerBuilder()
            .RespondWith(WebTestData.JsonResponse(new { success = true }));
        var client = CreateApiClient(handlerBuilder, accessToken: "jwt-token");

        var response = await client.SendAsync(HttpMethod.Get, "/api/test");

        response.Should().NotBeNull();
        handlerBuilder.Requests.Should().ContainSingle();
        var request = handlerBuilder.Requests[0];
        request.Method.Should().Be(HttpMethod.Get);
        request.RequestUri.Should().Be(new Uri("https://gateway.test/api/test"));
        request.AuthorizationScheme.Should().Be("Bearer");
        request.AuthorizationParameter.Should().Be("jwt-token");
    }

    [Fact]
    public async Task GetAsync_WhenResponseIsOk_ShouldDeserializeCaseInsensitiveJson()
    {
        var handlerBuilder = new MockHttpMessageHandlerBuilder()
            .RespondWith(WebTestData.JsonResponse(new { ID = 15, NAME = "Plan Nacional" }));
        var client = CreateApiClient(handlerBuilder);

        var result = await client.GetAsync<TestDto>("/api/test/15");

        result.Should().NotBeNull();
        result!.Id.Should().Be(15);
        result.Name.Should().Be("Plan Nacional");
    }

    [Fact]
    public async Task PostAsync_ShouldSerializePayloadAsJson()
    {
        var handlerBuilder = new MockHttpMessageHandlerBuilder()
            .RespondWith(WebTestData.JsonResponse(new { id = 22, name = "Creado" }));
        var client = CreateApiClient(handlerBuilder);
        var payload = new { Codigo = "PND", Nombre = "Plan" };

        var result = await client.PostAsync<TestDto>("/api/test", payload);

        result.Should().NotBeNull();
        result!.Id.Should().Be(22);
        handlerBuilder.Requests.Should().ContainSingle();
        handlerBuilder.Requests[0].Method.Should().Be(HttpMethod.Post);
        handlerBuilder.Requests[0].Content.Should().Contain("\"Codigo\":\"PND\"");
        handlerBuilder.Requests[0].Content.Should().Contain("\"Nombre\":\"Plan\"");
    }

    [Fact]
    public async Task GetAsync_WhenResponseIsError_ShouldReturnDefault()
    {
        var handlerBuilder = new MockHttpMessageHandlerBuilder()
            .RespondWith(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = CreateApiClient(handlerBuilder);

        var result = await client.GetAsync<TestDto>("/api/fails");

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenUnauthorized()
    {
        var handlerBuilder = new MockHttpMessageHandlerBuilder()
            .RespondWith(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateApiClient(handlerBuilder);

        var result = await client.DeleteAsync("/api/protected");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetStringAsync_WhenResponseIsOk_ShouldReturnContent()
    {
        var handlerBuilder = new MockHttpMessageHandlerBuilder()
            .RespondWith(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\":\"ok\"}")
            });
        var client = CreateApiClient(handlerBuilder);

        var result = await client.GetStringAsync("/api/raw");

        result.Should().Be("{\"status\":\"ok\"}");
    }

    private static ApiClient CreateApiClient(MockHttpMessageHandlerBuilder handlerBuilder, string? accessToken = null)
    {
        var httpClient = new HttpClient(handlerBuilder.Build())
        {
            BaseAddress = new Uri("https://gateway.test")
        };

        var authServiceMock = new Mock<IAuthService>();
        authServiceMock.Setup(x => x.GetAccessToken()).Returns(accessToken);

        return new ApiClient(httpClient, authServiceMock.Object, NullLogger<ApiClient>.Instance);
    }

    private sealed class TestDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}