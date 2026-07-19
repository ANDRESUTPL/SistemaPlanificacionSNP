using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SistemaPlanificacionSNP.Infrastructure.Common;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Parametrizacion.Api.Controllers;
using SistemaPlanificacionSNP.Parametrizacion.Api.Services;

namespace SistemaPlanificacionSNP.Parametrizacion.Api.Tests.Controllers;

public class InstitucionesControllerTests
{
    [Fact]
    public async Task GetPeriodos_ShouldReturnOkWithPeriodos()
    {
        var periodos = new List<PeriodoPlanificacionDto>
        {
            new()
            {
                PeriodoPlanificacionId = 1,
                Codigo = "2025-2028",
                Nombre = "Periodo 2025-2028",
                FechaInicio = new DateTime(2025, 1, 1),
                FechaFin = new DateTime(2028, 12, 31),
                Activo = true
            }
        };

        var serviceMock = new Mock<IParametrizacionService>();
        serviceMock.Setup(s => s.GetPeriodosAsync()).ReturnsAsync(periodos);

        var controller = BuildController(serviceMock);

        var result = await controller.GetPeriodos();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<List<PeriodoPlanificacionDto>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().ContainSingle(x => x.Codigo == "2025-2028");
    }

    [Fact]
    public async Task CreatePeriodo_WhenServiceReturnsPeriodo_ShouldReturnOk()
    {
        var dto = new PeriodoPlanificacionCreateUpdateDto
        {
            Codigo = "2029-2032",
            Nombre = "Periodo 2029-2032",
            FechaInicio = new DateTime(2029, 1, 1),
            FechaFin = new DateTime(2032, 12, 31),
            Activo = true
        };
        var created = new PeriodoPlanificacionDto
        {
            PeriodoPlanificacionId = 2,
            Codigo = "2029-2032",
            Nombre = "Periodo 2029-2032",
            FechaInicio = dto.FechaInicio,
            FechaFin = dto.FechaFin,
            Activo = true
        };

        var serviceMock = new Mock<IParametrizacionService>();
        serviceMock.Setup(s => s.CreatePeriodoAsync(dto)).ReturnsAsync(created);

        var controller = BuildController(serviceMock);

        var result = await controller.CreatePeriodo(dto);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<PeriodoPlanificacionDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.PeriodoPlanificacionId.Should().Be(2);
    }

    [Fact]
    public async Task GetEntidades_ShouldReturnOkWithEntidades()
    {
        var entidades = new List<EntidadPublicaDto>
        {
            new()
            {
                EntidadPublicaId = 5,
                Nombre = "Ministerio de Planificacion",
                Sigla = "MP",
                Tipo = "Ministerio",
                NivelGobierno = "Nacional",
                Activa = true
            }
        };

        var serviceMock = new Mock<IParametrizacionService>();
        serviceMock.Setup(s => s.GetEntidadesAsync()).ReturnsAsync(entidades);

        var controller = BuildController(serviceMock);

        var result = await controller.GetEntidades();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<List<EntidadPublicaDto>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().ContainSingle(x => x.Sigla == "MP");
    }

    [Fact]
    public async Task CreateEntidad_WhenServiceReturnsEntidad_ShouldReturnOk()
    {
        var dto = new EntidadPublicaCreateUpdateDto
        {
            Nombre = "Ministerio de Planificacion",
            Sigla = "MP",
            Tipo = "Ministerio",
            NivelGobierno = "Nacional"
        };
        var created = new EntidadPublicaDto
        {
            EntidadPublicaId = 6,
            Nombre = dto.Nombre,
            Sigla = dto.Sigla,
            Tipo = dto.Tipo,
            NivelGobierno = dto.NivelGobierno,
            Activa = true
        };

        var serviceMock = new Mock<IParametrizacionService>();
        serviceMock.Setup(s => s.CreateEntidadAsync(dto)).ReturnsAsync(created);

        var controller = BuildController(serviceMock);

        var result = await controller.CreateEntidad(dto);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<EntidadPublicaDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.EntidadPublicaId.Should().Be(6);
    }

    [Fact]
    public async Task GetEntidades_WhenServiceThrows_ShouldReturnInternalServerError()
    {
        var serviceMock = new Mock<IParametrizacionService>();
        serviceMock.Setup(s => s.GetEntidadesAsync()).ThrowsAsync(new Exception("boom"));

        var controller = BuildController(serviceMock);

        var result = await controller.GetEntidades();

        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
        var response = objectResult.Value.Should().BeOfType<ApiResponse<List<EntidadPublicaDto>>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Error interno");
    }

    private static InstitucionesController BuildController(Mock<IParametrizacionService> serviceMock)
    {
        return new InstitucionesController(
            serviceMock.Object,
            new Mock<ILogger<InstitucionesController>>().Object);
    }
}