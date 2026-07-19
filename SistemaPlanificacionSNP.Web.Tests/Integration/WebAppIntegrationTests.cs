using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SistemaPlanificacionSNP.Web.Services;
using SistemaPlanificacionSNP.Web.Tests.Common;

namespace SistemaPlanificacionSNP.Web.Tests.Integration;

public class WebAppIntegrationTests : IClassFixture<WebAppTestFactory>
{
    private readonly WebAppTestFactory _factory;

    public WebAppIntegrationTests(WebAppTestFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/macroplanificacion/planes")]
    [InlineData("/controlcalidad/revisiones")]
    [InlineData("/evaluacion/dashboard")]
    public async Task MainRoutes_WithAuthenticatedUser_ShouldRenderHtml(string route)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(route);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");

        var html = await response.Content.ReadAsStringAsync();
        html.Should().NotBeNullOrWhiteSpace();
        html.Should().Contain("Sistema de Planificación");
    }

    [Fact]
    public async Task MacroPlanificacionRoute_ShouldRenderExpectedContent()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/macroplanificacion/planes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync();
        html.Should().Contain("Macro Planificaci");
        html.Should().Contain("Total Planes Nacionales");
    }
}

public sealed class WebAppTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiGateway:BaseUrl"] = "https://gateway.test"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IApiClient>();
            services.AddSingleton<IApiClient, FakeApiClient>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }
}

internal sealed class FakeApiClient : IApiClient
{
    public Task<T?> GetAsync<T>(string endpoint)
    {
        return Task.FromResult(default(T));
    }

    public Task<T?> PostAsync<T>(string endpoint, object? data = null)
    {
        return Task.FromResult(default(T));
    }

    public Task<T?> PutAsync<T>(string endpoint, object? data = null)
    {
        return Task.FromResult(default(T));
    }

    public Task<bool> DeleteAsync(string endpoint)
    {
        return Task.FromResult(true);
    }

    public Task<string?> GetStringAsync(string endpoint)
    {
        return Task.FromResult<string?>(null);
    }

    public Task<HttpResponseMessage?> SendAsync(HttpMethod method, string endpoint, object? data = null)
    {
        var response = endpoint switch
        {
            "/api/planesnacionales" => WebTestData.JsonResponse(WebTestData.ApiPaginatedResponse(new[]
            {
                new { planNacionalId = 1, nombre = "Plan Nacional Integración", periodoInicio = 2025, periodoFin = 2030, estado = "Activo", fechaCreacion = DateTime.UtcNow }
            })),
            "/api/planesnacionales/resumen" => WebTestData.JsonResponse(WebTestData.ApiResponse(new { totalPlanes = 1, totalObjetivos = 0 })),
            var revisiones when revisiones.StartsWith("/api/revisiones?") => WebTestData.JsonResponse(WebTestData.ApiPaginatedResponse(new[]
            {
                new { revisionId = 1, codigoRevision = "REV-INT-001", modulo = "MacroPlanificacion", estado = "Pendiente", fechaRevision = DateTime.UtcNow, observaciones = "Integración" }
            })),
            "/api/revisiones/dashboard" => WebTestData.JsonResponse(WebTestData.ApiResponse(new
            {
                totalRevisiones = 1,
                revisionesPendientes = 1,
                revisionesAprobadas = 0,
                revisionesRechazadas = 0,
                totalAuditorias = 0,
                auditoriasConformes = 0,
                auditoriasNoConformes = 0
            })),
            "/api/proyectosinversion?pageSize=1000" => WebTestData.JsonResponse(WebTestData.ApiPaginatedResponse(new[]
            {
                new { proyectoInversionId = 1, codigoProyecto = "PRY-001", nombre = "Proyecto Integración", estado = "Activo", monto = 1000m, planEstrategicoId = 1 }
            })),
            _ => WebTestData.JsonResponse(new { message = "Fake endpoint not configured" }, HttpStatusCode.NotFound)
        };

        return Task.FromResult<HttpResponseMessage?>(response);
    }
}