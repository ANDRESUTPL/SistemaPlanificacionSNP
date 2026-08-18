using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using SistemaPlanificacionSNP.Infrastructure.Common;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.TestUtilities.Infrastructure;

namespace SistemaPlanificacionSNP.PlanificacionInstitucional.FunctionalTests.Integration;

public sealed class PlanEstrategicoInstitucionalTests
    : MsSqlWebApplicationFactoryBase<SistemaPlanificacionSNP.PlanificacionInstitucional.Api.Program, PlanificacionInstitucionalDbContext>
{
    [Fact]
    [Trait("Category", "Functional")]
    [Trait("Microservice", "PlanificacionInstitucional")]
    public async Task CrearPlanEstrategicoInstitucional_ConDatosValidos_DebeRetornarCreatedYPlanId()
    {
        // Arrange
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-EntidadPublicaId", "10");

        var request = new PlanesEstrategicoCreateDto
        {
            EntidadPublicaId = 10,
            Entidad = "Ministerio de Planificación e Inversión Pública",
            PeriodoPlanificacionId = 1,
            PeriodoInicio = 2025,
            PeriodoFin = 2030,
            Estado = "Borrador"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/PlanesEstrategicos/crear", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PlanesEstrategicoReadDto>>();
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Message.Should().Be("Plan creado exitosamente");
        body.Data.Should().NotBeNull();
        body.Data!.PlanEstrategicoId.Should().BeGreaterThan(0);
        body.Data.EntidadPublicaId.Should().Be(10);
        body.Data.Entidad.Should().Be("Ministerio de Planificación e Inversión Pública");
    }

	[Fact]
	[Trait("Category", "Functional")]
	[Trait("Microservice", "PlanificacionInstitucional")]
	public async Task CrearPlanEstrategicoInstitucional_ConPeriodoInvalido_DebeRetornarBadRequest()
	{
		// Arrange
		var client = CreateClient();
		client.DefaultRequestHeaders.Add("X-Test-EntidadPublicaId", "10");

		var request = new PlanesEstrategicoCreateDto
		{
			EntidadPublicaId = 10,
			Entidad = "Ministerio de Planificación e Inversión Pública",
			PeriodoPlanificacionId = 1,
			PeriodoInicio = 2030, // Inválido: Inicio > Fin
			PeriodoFin = 2025,
			Estado = "Borrador"
		};

		// Act
		var response = await client.PostAsJsonAsync("/api/PlanesEstrategicos/crear", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

		var errorDetails = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ValidationProblemDetails>();

		errorDetails.Should().NotBeNull();
		errorDetails!.Errors.Should().NotBeEmpty();

		// Actualizamos el texto esperado para que coincida con tu validador
		var rawJson = await response.Content.ReadAsStringAsync();
		rawJson.Should().Contain("PeriodoFin no puede ser menor a PeriodoInicio");
	}
}