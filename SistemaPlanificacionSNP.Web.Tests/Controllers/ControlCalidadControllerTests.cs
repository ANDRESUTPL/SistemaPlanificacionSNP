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

public class ControlCalidadControllerTests : ControllerTestBase
{
    [Fact]
    public async Task Index_ShouldReturnViewWithRevisionesAndDashboard()
    {
        var apiClientMock = new Mock<IApiClient>();
        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Get, It.Is<string>(endpoint => endpoint.StartsWith("/api/revisiones?")), null))
            .ReturnsAsync(WebTestData.JsonResponse(WebTestData.ApiPaginatedResponse(new[]
            {
                new { revisionId = 7, codigoRevision = "REV-001", modulo = "Macro", estado = "Pendiente", fechaRevision = DateTime.UtcNow }
            }, totalItems: 1)));
        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Get, "/api/revisiones/dashboard", null))
            .ReturnsAsync(WebTestData.JsonResponse(WebTestData.ApiResponse(new
            {
                totalRevisiones = 1,
                revisionesPendientes = 1,
                revisionesAprobadas = 0,
                revisionesRechazadas = 0,
                totalAuditorias = 0,
                auditoriasConformes = 0,
                auditoriasNoConformes = 0
            })));

        var controller = BuildController(apiClientMock);

        var result = await controller.Index(null);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        var model = view.Model.Should().BeOfType<ControlCalidadIndexViewModel>().Subject;
        model.Revisiones.Should().ContainSingle(x => x.CodigoRevision == "REV-001");
        model.Dashboard.TotalRevisiones.Should().Be(1);
        model.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task Index_WithBuscar_ShouldSendCodigoRevisionQuery()
    {
        var apiClientMock = new Mock<IApiClient>();
        string? capturedEndpoint = null;
        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Get, It.Is<string>(endpoint => endpoint.StartsWith("/api/revisiones?")), null))
            .Callback<HttpMethod, string, object?>((_, endpoint, _) => capturedEndpoint = endpoint)
            .ReturnsAsync(WebTestData.JsonResponse(WebTestData.ApiPaginatedResponse(Array.Empty<object>())));
        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Get, "/api/revisiones/dashboard", null))
            .ReturnsAsync(WebTestData.JsonResponse(WebTestData.ApiResponse(new { })));

        var controller = BuildController(apiClientMock);

        await controller.Index("REV-001", page: 2);

        capturedEndpoint.Should().Be("/api/revisiones?pageNumber=2&pageSize=10&codigoRevision=REV-001");
    }

    [Fact]
    public async Task CrearRevision_WhenModelStateIsInvalid_ShouldReturnViewAndNotCallCreateApi()
    {
        var apiClientMock = new Mock<IApiClient>();
        var controller = BuildController(apiClientMock);
        var model = new RevisionCreateViewModel();
        controller.ModelState.AddModelError(nameof(model.CodigoRevision), "El código es obligatorio");

        var result = await controller.CrearRevision(model);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeSameAs(model);
        apiClientMock.Verify(x => x.SendAsync(HttpMethod.Post, "/api/revisiones/crear", It.IsAny<object?>()), Times.Never);
    }

    [Fact]
    public async Task CrearRevision_WhenApiSucceeds_ShouldSetSuccessAndRedirectToIndex()
    {
        var apiClientMock = new Mock<IApiClient>();
        SetupCascada(apiClientMock);
        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Post, "/api/revisiones/crear", It.IsAny<object>()))
            .ReturnsAsync(WebTestData.JsonResponse(WebTestData.ApiResponse(new { revisionId = 15 })));
        var controller = BuildController(apiClientMock);
        var model = new RevisionCreateViewModel
        {
            CodigoRevision = "REV-001",
            EntidadPublicaId = 5,
            PlanEstrategicoId = 1,
            ProyectoInversionId = 10,
            Estado = "Pendiente"
        };

        var result = await controller.CrearRevision(model);

        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be(nameof(ControlCalidadController.Index));
        controller.TempData["Success"].Should().Be("Revisión técnica aperturada exitosamente.");
    }

    [Fact]
    public async Task CrearRevision_WhenProyectoDoesNotBelongToPlan_ShouldAddModelErrorAndNotCallCreateApi()
    {
        var apiClientMock = new Mock<IApiClient>();
        SetupCascada(apiClientMock);
        var controller = BuildController(apiClientMock);
        var model = new RevisionCreateViewModel
        {
            CodigoRevision = "REV-001",
            EntidadPublicaId = 5,
            PlanEstrategicoId = 1,
            ProyectoInversionId = 999,
            Estado = "Pendiente"
        };

        var result = await controller.CrearRevision(model);

        result.Should().BeOfType<ViewResult>();
        controller.ModelState[nameof(model.ProyectoInversionId)]!.Errors
            .Should().ContainSingle(x => x.ErrorMessage == "El proyecto seleccionado no pertenece al PEI indicado.");
        apiClientMock.Verify(x => x.SendAsync(HttpMethod.Post, "/api/revisiones/crear", It.IsAny<object?>()), Times.Never);
    }

    [Fact]
    public async Task CrearRevision_WhenApiRejects_ShouldAddModelErrorAndReturnView()
    {
        var apiClientMock = new Mock<IApiClient>();
        SetupCascada(apiClientMock);
        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Post, "/api/revisiones/crear", It.IsAny<object>()))
            .ReturnsAsync(WebTestData.JsonResponse(new { message = "Código duplicado" }, HttpStatusCode.BadRequest));
        var controller = BuildController(apiClientMock);
        var model = new RevisionCreateViewModel
        {
            CodigoRevision = "REV-001",
            EntidadPublicaId = 5,
            PlanEstrategicoId = 1,
            ProyectoInversionId = 10,
            Estado = "Pendiente"
        };

        var result = await controller.CrearRevision(model);

        var view = result.Should().BeOfType<ViewResult>().Subject;
        view.Model.Should().BeSameAs(model);
        controller.ModelState[string.Empty]!.Errors.Should().ContainSingle(x => x.ErrorMessage == "Código duplicado");
    }

    [Fact]
    public async Task PlanesPorEntidad_ShouldReturnOnlyPlansOfRequestedEntidad()
    {
        var apiClientMock = new Mock<IApiClient>();
        SetupCascada(apiClientMock);
        var controller = BuildController(apiClientMock);

        var result = await controller.PlanesPorEntidad(5);

        var json = result.Should().BeOfType<JsonResult>().Subject;
        var payload = json.Value.Should().BeAssignableTo<System.Collections.IEnumerable>().Subject
            .Cast<object>()
            .ToList();

        payload.Should().HaveCount(1);
    }

    [Fact]
    public async Task ProyectosPorPlan_ShouldReturnProjectsOfRequestedPlan()
    {
        var apiClientMock = new Mock<IApiClient>();
        SetupCascada(apiClientMock);
        var controller = BuildController(apiClientMock);

        var result = await controller.ProyectosPorPlan(1);

        var json = result.Should().BeOfType<JsonResult>().Subject;
        var payload = json.Value.Should().BeAssignableTo<System.Collections.IEnumerable>().Subject
            .Cast<object>()
            .ToList();

        payload.Should().HaveCount(1);
    }

    private static void SetupCascada(Mock<IApiClient> apiClientMock)
    {
        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Get, "/api/planesestrategicos?pageNumber=1&pageSize=1000", null))
            .ReturnsAsync(WebTestData.JsonResponse(WebTestData.ApiPaginatedResponse(new[]
            {
                new { planEstrategicoId = 1, entidad = "Ministerio de Salud", entidadPublicaId = 5, periodoInicio = 2026, periodoFin = 2030, estado = "Aprobado" },
                new { planEstrategicoId = 2, entidad = "Ministerio de Educación", entidadPublicaId = 6, periodoInicio = 2026, periodoFin = 2030, estado = "Borrador" }
            }, totalItems: 2)));

        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Get, "/api/proyectosinversion?planEstrategicoId=1&pageNumber=1&pageSize=1000", null))
            .ReturnsAsync(WebTestData.JsonResponse(WebTestData.ApiPaginatedResponse(new[]
            {
                new { proyectoInversionId = 10, planEstrategicoId = 1, codigoProyecto = "CUP-001", nombre = "Hospital General", monto = 100m, estado = "Ejecucion" }
            })));

        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Get, "/api/proyectosinversion?pageNumber=1&pageSize=1000", null))
            .ReturnsAsync(WebTestData.JsonResponse(WebTestData.ApiPaginatedResponse(new[]
            {
                new { proyectoInversionId = 10, planEstrategicoId = 1, codigoProyecto = "CUP-001", nombre = "Hospital General", monto = 100m, estado = "Ejecucion" }
            })));
    }

    private static ControlCalidadController BuildController(Mock<IApiClient> apiClientMock)
    {
        var controller = new ControlCalidadController(
            apiClientMock.Object,
            new Mock<ILogger<ControlCalidadController>>().Object);
        ConfigureController(controller);
        return controller;
    }
}