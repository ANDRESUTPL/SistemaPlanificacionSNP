using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SistemaPlanificacionSNP.Web.Controllers;
using SistemaPlanificacionSNP.Web.Models;
using SistemaPlanificacionSNP.Web.Services;
using SistemaPlanificacionSNP.Web.Tests.Common;

namespace SistemaPlanificacionSNP.Web.Tests.Controllers;

public class MacroPlanificacionControllerTests : ControllerTestBase
{
    [Fact]
    public async Task Index_ShouldReturnViewWithPlansAndResumen()
    {
        var apiClientMock = new Mock<IApiClient>();
        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Get, "/api/planesnacionales", null))
            .ReturnsAsync(WebTestData.JsonResponse(WebTestData.ApiPaginatedResponse(new[]
            {
                new { planNacionalId = 1, nombre = "Plan Nacional Salud", periodoInicio = 2025, periodoFin = 2030, estado = "Activo" }
            })));
        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Get, "/api/planesnacionales/resumen", null))
            .ReturnsAsync(WebTestData.JsonResponse(WebTestData.ApiResponse(new { totalPlanes = 1, totalObjetivos = 3 })));

        var controller = BuildController(apiClientMock);

        var result = await controller.Index(null);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<MacroPlanificacionIndexViewModel>().Subject;
        model.PlanesNacionales.Should().ContainSingle(x => x.Nombre == "Plan Nacional Salud");
        model.Resumen.TotalPlanes.Should().Be(1);
        model.Resumen.TotalObjetivos.Should().Be(3);
    }

    [Fact]
    public async Task Index_WithBuscar_ShouldFilterPlansInMemory()
    {
        var apiClientMock = new Mock<IApiClient>();
        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Get, "/api/planesnacionales", null))
            .ReturnsAsync(WebTestData.JsonResponse(WebTestData.ApiPaginatedResponse(new[]
            {
                new { planNacionalId = 1, nombre = "Plan Nacional Salud", periodoInicio = 2025, periodoFin = 2030, estado = "Activo" },
                new { planNacionalId = 2, nombre = "Plan Nacional Educación", periodoInicio = 2025, periodoFin = 2030, estado = "Activo" }
            }, totalItems: 2)));
        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Get, "/api/planesnacionales/resumen", null))
            .ReturnsAsync(WebTestData.JsonResponse(WebTestData.ApiResponse(new { totalPlanes = 2, totalObjetivos = 0 })));

        var controller = BuildController(apiClientMock);

        var result = await controller.Index("salud");

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<MacroPlanificacionIndexViewModel>().Subject;
        model.PlanesNacionales.Should().ContainSingle();
        model.PlanesNacionales[0].Nombre.Should().Be("Plan Nacional Salud");
    }

    [Fact]
    public async Task Index_WhenPlansUnauthorized_ShouldRedirectToLogin()
    {
        var apiClientMock = new Mock<IApiClient>();
        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Get, "/api/planesnacionales", null))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Get, "/api/planesnacionales/resumen", null))
            .ReturnsAsync(WebTestData.JsonResponse(WebTestData.ApiResponse(new { totalPlanes = 0, totalObjetivos = 0 })));

        var controller = BuildController(apiClientMock);

        var result = await controller.Index(null);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be("Login");
        redirect.ControllerName.Should().Be("Account");
    }

    [Fact]
    public async Task CrearPlan_WhenYearsAreInvalid_ShouldReturnViewAndNotCallApi()
    {
        var apiClientMock = new Mock<IApiClient>();
        var controller = BuildController(apiClientMock);
        var model = new PlanNacionalCreateViewModel
        {
            Nombre = "Plan inválido",
            PeriodoInicio = 2030,
            PeriodoFin = 2025
        };

        var result = await controller.CrearPlan(model);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeSameAs(model);
        controller.ModelState.Should().ContainKey(nameof(model.PeriodoFin));
        apiClientMock.Verify(x => x.SendAsync(It.IsAny<HttpMethod>(), It.IsAny<string>(), It.IsAny<object?>()), Times.Never);
    }

    [Fact]
    public async Task CrearPlan_WhenApiSucceeds_ShouldSetSuccessAndRedirectToIndex()
    {
        var apiClientMock = new Mock<IApiClient>();
        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Post, "/api/planesnacionales/crear", It.IsAny<object>()))
            .ReturnsAsync(WebTestData.JsonResponse(WebTestData.ApiResponse(new { planNacionalId = 10 })));
        var controller = BuildController(apiClientMock);
        var model = new PlanNacionalCreateViewModel
        {
            Nombre = "Plan Nacional",
            PeriodoInicio = 2025,
            PeriodoFin = 2030
        };

        var result = await controller.CrearPlan(model);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be(nameof(MacroPlanificacionController.Index));
        controller.TempData["Success"].Should().Be("Plan Nacional de Desarrollo creado exitosamente.");
    }

    [Fact]
    public async Task CrearPlan_WhenApiRejects_ShouldAddModelErrorAndReturnView()
    {
        var apiClientMock = new Mock<IApiClient>();
        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Post, "/api/planesnacionales/crear", It.IsAny<object>()))
            .ReturnsAsync(WebTestData.JsonResponse(new { message = "El plan ya existe" }, HttpStatusCode.BadRequest));
        var controller = BuildController(apiClientMock);
        var model = new PlanNacionalCreateViewModel
        {
            Nombre = "Plan Nacional",
            PeriodoInicio = 2025,
            PeriodoFin = 2030
        };

        var result = await controller.CrearPlan(model);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeSameAs(model);
        controller.ModelState[string.Empty]!.Errors.Should().ContainSingle(x => x.ErrorMessage == "El plan ya existe");
    }

    private static MacroPlanificacionController BuildController(Mock<IApiClient> apiClientMock)
    {
        var controller = new MacroPlanificacionController(
            apiClientMock.Object,
            new Mock<ILogger<MacroPlanificacionController>>().Object);
        ConfigureController(controller);
        return controller;
    }
}