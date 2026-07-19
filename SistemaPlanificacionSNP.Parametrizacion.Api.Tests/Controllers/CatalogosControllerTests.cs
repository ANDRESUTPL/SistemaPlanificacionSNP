using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SistemaPlanificacionSNP.Infrastructure.Common;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Parametrizacion.Api.Controllers;
using SistemaPlanificacionSNP.Parametrizacion.Api.Services;

namespace SistemaPlanificacionSNP.Parametrizacion.Api.Tests.Controllers;

public class CatalogosControllerTests
{
    [Fact]
    public async Task GetAll_ShouldReturnOkWithCatalogos()
    {
        var catalogos = new List<CatalogoDto>
        {
            new() { CatalogoId = 1, Codigo = "TIPO_ENTIDAD", Nombre = "Tipos de entidad", Activo = true }
        };

        var serviceMock = new Mock<IParametrizacionService>();
        serviceMock.Setup(s => s.GetCatalogosAsync()).ReturnsAsync(catalogos);

        var controller = BuildController(serviceMock);

        var result = await controller.GetAll();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<List<CatalogoDto>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().ContainSingle(x => x.Codigo == "TIPO_ENTIDAD");
        response.Message.Should().Be("Catálogos obtenidos exitosamente.");
    }

    [Fact]
    public async Task GetByCodigo_WhenCatalogoDoesNotExist_ShouldReturnNotFound()
    {
        var serviceMock = new Mock<IParametrizacionService>();
        serviceMock.Setup(s => s.GetCatalogoByCodigoAsync("NO_EXISTE")).ReturnsAsync((CatalogoDto?)null);

        var controller = BuildController(serviceMock);

        var result = await controller.GetByCodigo("NO_EXISTE");

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFound.Value.Should().BeOfType<ApiResponse<CatalogoDto>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Contain("no encontrado");
    }

    [Fact]
    public async Task Create_WhenServiceThrowsValidation_ShouldReturnBadRequest()
    {
        var dto = new CatalogoCreateDto { Codigo = "DUP", Nombre = "Duplicado" };
        var serviceMock = new Mock<IParametrizacionService>();
        serviceMock.Setup(s => s.CreateCatalogoAsync(dto))
            .ThrowsAsync(new InvalidOperationException("Ya existe un catalogo con este codigo."));

        var controller = BuildController(serviceMock);

        var result = await controller.Create(dto);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequest.Value.Should().BeOfType<ApiResponse<CatalogoDto>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Ya existe un catalogo con este codigo.");
    }

    [Fact]
    public async Task CreateItem_WhenServiceReturnsItem_ShouldReturnOk()
    {
        var dto = new ItemCatalogoCreateDto
        {
            CatalogoId = 1,
            Codigo = "MIN",
            Nombre = "Ministerio",
            Orden = 1
        };
        var created = new ItemCatalogoDto
        {
            ItemCatalogoId = 12,
            CatalogoId = 1,
            Codigo = "MIN",
            Nombre = "Ministerio",
            Activo = true
        };

        var serviceMock = new Mock<IParametrizacionService>();
        serviceMock.Setup(s => s.CreateItemCatalogoAsync(dto)).ReturnsAsync(created);

        var controller = BuildController(serviceMock);

        var result = await controller.CreateItem(dto);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<ItemCatalogoDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.ItemCatalogoId.Should().Be(12);
    }

    [Fact]
    public async Task GetAll_WhenServiceThrows_ShouldReturnInternalServerError()
    {
        var serviceMock = new Mock<IParametrizacionService>();
        serviceMock.Setup(s => s.GetCatalogosAsync()).ThrowsAsync(new Exception("boom"));

        var controller = BuildController(serviceMock);

        var result = await controller.GetAll();

        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
        var response = objectResult.Value.Should().BeOfType<ApiResponse<List<CatalogoDto>>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Error interno del servidor");
    }

    private static CatalogosController BuildController(Mock<IParametrizacionService> serviceMock)
    {
        return new CatalogosController(
            serviceMock.Object,
            new Mock<ILogger<CatalogosController>>().Object);
    }
}