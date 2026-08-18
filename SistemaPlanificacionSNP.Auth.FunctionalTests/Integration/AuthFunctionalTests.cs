using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SistemaPlanificacionSNP.Auth.FunctionalTests.Infrastructure;
using SistemaPlanificacionSNP.Infrastructure.Common;
using SistemaPlanificacionSNP.Infrastructure.DTOs;

namespace SistemaPlanificacionSNP.Auth.FunctionalTests.Integration;

public sealed class AuthFunctionalTests : IClassFixture<AuthWebApplicationFactory>
{
    private readonly AuthWebApplicationFactory _factory;

    public AuthFunctionalTests(AuthWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Microservice", "Auth")]
    public async Task Login_ConCredencialesValidas_DebeRetornarTokenYPermitirConsultarMenuActual()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var loginRequest = new LoginDto
        {
            NombreUsuario = "admin.integration",
            Password = "Password123!"
        };

        // Act
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        loginBody.Should().NotBeNull();
        loginBody!.Success.Should().BeTrue();
        loginBody.Data.Should().NotBeNull();
        loginBody.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.Data.AccessToken);

        var menuResponse = await client.GetAsync("/api/usuarios/menu/actual");

        menuResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var menuBody = await menuResponse.Content.ReadFromJsonAsync<ApiResponse<List<MenuPermisoDto>>>();
        menuBody.Should().NotBeNull();
        menuBody!.Success.Should().BeTrue();
        menuBody.Data.Should().NotBeNull();
        menuBody.Data!.Should().HaveCountGreaterThan(0);
        menuBody.Data[0].Nombre.Should().Be("Seguridad");
    }

    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Microservice", "Auth")]
    public async Task Login_SinPassword_DebeRetornarBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var loginRequest = new LoginDto
        {
            NombreUsuario = "admin.integration",
            Password = string.Empty
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.Message.Should().Be("Usuario y contraseña requeridos");
    }
}