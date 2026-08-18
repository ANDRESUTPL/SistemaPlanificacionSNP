using System.Net;
using FluentAssertions;
using SistemaPlanificacionSNP.Parametrizacion.FunctionalTests.Infrastructure;

namespace SistemaPlanificacionSNP.Parametrizacion.FunctionalTests.Integration;

public sealed class CatalogosFunctionalTests : IClassFixture<ParametrizacionWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CatalogosFunctionalTests(ParametrizacionWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Microservice", "Parametrizacion")]
    public async Task Get_Catalogos_WithAuthenticatedUser_ShouldReturnNonUnauthorizedResponse()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("X-Test-EntidadPublicaId", "101");

        // Act
        var response = await _client.GetAsync("/api/catalogos");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }
}
