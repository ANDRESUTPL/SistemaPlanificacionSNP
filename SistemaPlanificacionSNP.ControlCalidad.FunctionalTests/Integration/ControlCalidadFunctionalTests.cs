using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SistemaPlanificacionSNP.ControlCalidad.FunctionalTests.Infrastructure;
using SistemaPlanificacionSNP.Infrastructure.Common;
using SistemaPlanificacionSNP.Infrastructure.DTOs;

namespace SistemaPlanificacionSNP.ControlCalidad.FunctionalTests.Integration;

public sealed class ControlCalidadFunctionalTests : IClassFixture<ControlCalidadWebApplicationFactory>
{
    private readonly ControlCalidadWebApplicationFactory _factory;

    public ControlCalidadFunctionalTests(ControlCalidadWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Microservice", "ControlCalidad")]
    public async Task CrearRevisionYAuditoria_ConClaimsValidos_DebeRetornarCreated()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        client.DefaultRequestHeaders.Add("X-Test-UserId", "7");
        client.DefaultRequestHeaders.Add("X-Test-UserName", "ana.qa");
        client.DefaultRequestHeaders.Add("X-Test-Nombre", "Ana");
        client.DefaultRequestHeaders.Add("X-Test-Apellido", "QA");

        var revisionRequest = new RevisioneCreateDto
        {
            CodigoRevision = $"REV-INT-{Guid.NewGuid():N}"[..16],
            Modulo = "Planificacion",
            PlanEstrategicoId = 1,
            ProyectoInversionId = 1,
            Estado = "Pendiente",
            Observaciones = "Integracion funcional"
        };

        // Act
        var revisionResponse = await client.PostAsJsonAsync("/api/revisiones/crear", revisionRequest);

        // Assert
        revisionResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

        var revisionBody = await revisionResponse.Content.ReadFromJsonAsync<ApiResponse<RevisioneDto>>();
        revisionBody.Should().NotBeNull();
        revisionBody!.Success.Should().BeTrue();
        revisionBody.Data.Should().NotBeNull();
        revisionBody.Data!.RevisionId.Should().BeGreaterThan(0);

        var auditoriaRequest = new AuditoriaCreateDto
        {
            RevisionId = revisionBody.Data.RevisionId,
            Tipo = "Interna",
            Resultado = "Conforme"
        };

        // Act
        var auditoriaResponse = await client.PostAsJsonAsync("/api/auditorias/crear", auditoriaRequest);

        // Assert
        auditoriaResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

        var auditoriaBody = await auditoriaResponse.Content.ReadFromJsonAsync<ApiResponse<AuditoriaDto>>();
        auditoriaBody.Should().NotBeNull();
        auditoriaBody!.Success.Should().BeTrue();
        auditoriaBody.Data.Should().NotBeNull();
        auditoriaBody.Data!.RevisionId.Should().Be(revisionBody.Data.RevisionId);
        auditoriaBody.Data.Responsable.Should().Be("Ana QA");
    }

    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Microservice", "ControlCalidad")]
    public async Task CrearRevision_ConFechaFutura_DebeRetornarBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        client.DefaultRequestHeaders.Add("X-Test-UserId", "7");
        client.DefaultRequestHeaders.Add("X-Test-Nombre", "Ana");
        client.DefaultRequestHeaders.Add("X-Test-Apellido", "QA");

        var revisionRequest = new RevisioneCreateDto
        {
            CodigoRevision = "REV-FUT-001",
            Modulo = "Planificacion",
            PlanEstrategicoId = 1,
            ProyectoInversionId = 1,
            Estado = "Pendiente",
            FechaRevision = DateTime.UtcNow.AddDays(1),
            Observaciones = "Fallo esperado"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/revisiones/crear", revisionRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<RevisioneDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.Message.Should().Be("La fecha de revisión no puede estar en el futuro");
    }
}