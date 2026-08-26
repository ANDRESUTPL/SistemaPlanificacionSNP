using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SistemaPlanificacionSNP.Web.Controllers;
using SistemaPlanificacionSNP.Web.Services;
using SistemaPlanificacionSNP.Web.Tests.Common;

namespace SistemaPlanificacionSNP.Web.Tests.Controllers;

public class DashboardControllerTests : ControllerTestBase
{
    [Fact]
    public async Task Index_ShouldReturnViewWithUserDataInViewBag()
    {
        var controller = BuildController(new Mock<IApiClient>());

        var result = await controller.Index();

        result.Should().BeOfType<ViewResult>();
        ((string)controller.ViewBag.NombreUsuario).Should().Be("Administrador");
        ((string)controller.ViewBag.UserId).Should().Be("test-user-id");
    }

    [Fact]
    public async Task GetDashboardData_WhenApisReturnSuccess_ShouldReturnJsonWithSuccessTrue()
    {
        var apiClientMock = new Mock<IApiClient>();
        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Get, "/api/planesestrategicos/dashboard", null))
            .ReturnsAsync(WebTestData.JsonResponse(new { success = true, data = new { } }));
        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Get, "/api/planesestrategicos?pageNumber=1&pageSize=1000", null))
            .ReturnsAsync(WebTestData.JsonResponse(WebTestData.ApiPaginatedResponse(Array.Empty<object>())));
        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Get, "/api/proyectosinversion?pageNumber=1&pageSize=1000", null))
            .ReturnsAsync(WebTestData.JsonResponse(WebTestData.ApiPaginatedResponse(Array.Empty<object>())));
        var controller = BuildController(apiClientMock);

        var result = await controller.GetDashboardData();

        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var successProperty = jsonResult.Value!.GetType().GetProperty("success");
        ((bool)successProperty!.GetValue(jsonResult.Value)!).Should().BeTrue();
    }

    [Fact]
    public async Task GetDashboardData_WhenApiClientReturnsNull_ShouldStillReturnJsonWithSuccessTrue()
    {
        var apiClientMock = new Mock<IApiClient>();
        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Get, It.IsAny<string>(), null))
            .ReturnsAsync((HttpResponseMessage?)null);
        var controller = BuildController(apiClientMock);

        var result = await controller.GetDashboardData();

        var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
        var successProperty = jsonResult.Value!.GetType().GetProperty("success");
        ((bool)successProperty!.GetValue(jsonResult.Value)!).Should().BeTrue();
    }

    [Fact]
    public async Task GetDashboardData_WhenApiThrows_ShouldReturnInternalServerError()
    {
        var apiClientMock = new Mock<IApiClient>();
        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Get, It.IsAny<string>(), null))
            .ThrowsAsync(new InvalidOperationException("Fallo de conexión"));
        var controller = BuildController(apiClientMock);

        var result = await controller.GetDashboardData();

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetMenuActual_WhenApiReturnsSuccess_ShouldReturnJsonContent()
    {
        var apiClientMock = new Mock<IApiClient>();
        apiClientMock.Setup(x => x.SendAsync(HttpMethod.Get, "/api/usuarios/menu/actual", null))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":[]}")
            });
        var controller = BuildController(apiClientMock);

        var result = await controller.GetMenuActual();

        var content = result.Should().BeOfType<ContentResult>().Subject;
        content.ContentType.Should().Be("application/json");
        content.Content.Should().Be("{\"data\":[]}");
    }

    private static DashboardController BuildController(Mock<IApiClient> apiClientMock)
    {
        var controller = new DashboardController(
            apiClientMock.Object,
            new Mock<ILogger<DashboardController>>().Object);
        ConfigureController(controller);
        return controller;
    }
}