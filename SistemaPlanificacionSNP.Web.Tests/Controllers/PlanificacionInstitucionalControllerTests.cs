using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SistemaPlanificacionSNP.Web.Controllers;
using SistemaPlanificacionSNP.Web.Models;
using SistemaPlanificacionSNP.Web.Services;
using SistemaPlanificacionSNP.Web.Tests.Common;

namespace SistemaPlanificacionSNP.Web.Tests.Controllers;

public class PlanificacionInstitucionalControllerTests : ControllerTestBase
{
    [Fact]
    public async Task CrearPlan_DebeDerivarPeriodoDelPlanNacional_IgnorandoElPeriodoEnviado()
    {
        var apiClientMock = new Mock<IApiClient>();
        object? payloadEnviado = null;

        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Get, "/api/instituciones/periodos", null))
            .ReturnsAsync(WebTestData.JsonResponse(WebTestData.ApiResponse(new[]
            {
                new { periodoPlanificacionId = 1, codigo = "P-2025", nombre = "2025-2030", fechaInicio = new DateTime(2025, 1, 1), fechaFin = new DateTime(2030, 12, 31), activo = true },
                new { periodoPlanificacionId = 2, codigo = "P-2031", nombre = "2031-2036", fechaInicio = new DateTime(2031, 1, 1), fechaFin = new DateTime(2036, 12, 31), activo = true }
            })));
        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Get, "/api/planesnacionales", null))
            .ReturnsAsync(WebTestData.JsonResponse(WebTestData.ApiPaginatedResponse(new[]
            {
                new { planNacionalId = 10, nombre = "Plan Nacional", periodoPlanificacionId = 1, periodoInicio = 2025, periodoFin = 2030, estado = "Activo" }
            })));
        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Get, "/api/instituciones/entidades", null))
            .ReturnsAsync(WebTestData.JsonResponse(WebTestData.ApiResponse(new[]
            {
                new { entidadPublicaId = 5, nombre = "Ministerio", sigla = "MIN" }
            })));
        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Post, "/api/planesestrategicos/crear", It.IsAny<object>()))
            .Callback<HttpMethod, string, object?>((_, _, payload) => payloadEnviado = payload)
            .ReturnsAsync(WebTestData.JsonResponse(WebTestData.ApiResponse(new { planEstrategicoId = 1 })));

        var controller = new PlanificacionInstitucionalController(
            apiClientMock.Object,
            new Mock<ILogger<PlanificacionInstitucionalController>>().Object);
        ConfigureController(controller);
        var model = new PlanEstrategicoCreateViewModel
        {
            EntidadPublicaId = 5,
            PlanNacionalId = 10,
            PeriodoPlanificacionId = 2
        };

        var result = await controller.CrearPlan(model);

        result.Should().BeOfType<RedirectToActionResult>();
        payloadEnviado.Should().NotBeNull();
        payloadEnviado!.GetType().GetProperty("PeriodoPlanificacionId")!.GetValue(payloadEnviado).Should().Be(1);
        payloadEnviado.GetType().GetProperty("PeriodoInicio")!.GetValue(payloadEnviado).Should().Be(2025);
        payloadEnviado.GetType().GetProperty("PeriodoFin")!.GetValue(payloadEnviado).Should().Be(2030);
    }
}