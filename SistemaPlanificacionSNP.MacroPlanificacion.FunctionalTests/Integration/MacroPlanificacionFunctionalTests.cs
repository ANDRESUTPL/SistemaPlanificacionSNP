using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SistemaPlanificacionSNP.Infrastructure.Common;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.MacroPlanificacion.FunctionalTests.Infrastructure;

namespace SistemaPlanificacionSNP.MacroPlanificacion.FunctionalTests.Integration;

public sealed class MacroPlanificacionFunctionalTests : IClassFixture<MacroPlanificacionWebApplicationFactory>
{
    private readonly MacroPlanificacionWebApplicationFactory _factory;

    public MacroPlanificacionFunctionalTests(MacroPlanificacionWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Microservice", "MacroPlanificacion")]
    public async Task CrearPlanNacionalYObjetivo_ConClaimsValidos_DebeRetornarCreated()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        client.DefaultRequestHeaders.Add("X-Test-UserId", "42");
        client.DefaultRequestHeaders.Add("X-Test-UserName", "macro.integration");

        var planRequest = new MacroPlanNacionalCreateDto
        {
            Nombre = "Plan Nacional Integracion",
            PeriodoInicio = 2025,
            PeriodoFin = 2030,
            Estado = "Activo"
        };

        // Act
        var planResponse = await client.PostAsJsonAsync("/api/planesNacionales/crear", planRequest);

        // Assert
        planResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

        var planBody = await planResponse.Content.ReadFromJsonAsync<ApiResponse<MacroPlanNacionalDto>>();
        planBody.Should().NotBeNull();
        planBody!.Success.Should().BeTrue();
        planBody.Data.Should().NotBeNull();
        planBody.Data!.PlanNacionalId.Should().BeGreaterThan(0);

        var objetivoRequest = new MacroObjetivoEstrategicoCreateDto
        {
            PlanNacionalId = planBody.Data.PlanNacionalId,
            Codigo = "OBJ-INT-001",
            Nombre = "Objetivo Integracion",
            Descripcion = "Creado desde pruebas funcionales"
        };

        // Act
        var objetivoResponse = await client.PostAsJsonAsync("/api/objetivosEstrategicos/crear", objetivoRequest);

        // Assert
        objetivoResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

        var objetivoBody = await objetivoResponse.Content.ReadFromJsonAsync<ApiResponse<MacroObjetivoEstrategicoDto>>();
        objetivoBody.Should().NotBeNull();
        objetivoBody!.Success.Should().BeTrue();
        objetivoBody.Data.Should().NotBeNull();
        objetivoBody.Data!.PlanNacionalId.Should().Be(planBody.Data.PlanNacionalId);
        objetivoBody.Data.Codigo.Should().Be("OBJ-INT-001");
    }

    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Microservice", "MacroPlanificacion")]
    public async Task CrearPlanNacional_ConPeriodoInvalido_DebeRetornarBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        client.DefaultRequestHeaders.Add("X-Test-UserId", "42");

        var request = new MacroPlanNacionalCreateDto
        {
            Nombre = "Plan Nacional Integracion",
            PeriodoInicio = 2030,
            PeriodoFin = 2025,
            Estado = "Activo"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/planesNacionales/crear", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<MacroPlanNacionalDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.Message.Should().Be("PeriodoInicio no puede ser mayor a PeriodoFin");
    }
}