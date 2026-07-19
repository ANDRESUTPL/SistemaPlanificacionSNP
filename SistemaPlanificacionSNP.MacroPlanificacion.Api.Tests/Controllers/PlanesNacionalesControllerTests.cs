using System.Security.Claims;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SistemaPlanificacionSNP.Domain.Entities.MacroPlanificacion;
using SistemaPlanificacionSNP.Infrastructure.Common;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.Services;
using SistemaPlanificacionSNP.MacroPlanificacion.Api.Controllers;

namespace SistemaPlanificacionSNP.MacroPlanificacion.Api.Tests.Controllers;

public class PlanesNacionalesControllerTests
{
    [Fact]
    public async Task GetAll_ShouldReturnOkWithPaginatedResponse()
    {
        var query = new MacroPlanNacionalQueryDto { PageNumber = 1, PageSize = 10 };
        var entities = new List<PlanesNacionalesDesarrollo>
        {
            BuildPlan(1, "Plan Nacional", 2025, 2030, "Activo")
        };

        var serviceMock = new Mock<IMacroPlanNacionalService>();
        serviceMock.Setup(s => s.GetPagedAsync(query)).ReturnsAsync((entities, 1));

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<List<MacroPlanNacionalDto>>(entities)).Returns(new List<MacroPlanNacionalDto>
        {
            new() { PlanNacionalId = 1, Nombre = "Plan Nacional", PeriodoInicio = 2025, PeriodoFin = 2030, Estado = "Activo" }
        });

        var controller = BuildController(serviceMock, mapperMock);

        var result = await controller.GetAll(query);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiPaginatedResponse<MacroPlanNacionalDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.TotalItems.Should().Be(1);
        response.Data.Data.Should().ContainSingle(x => x.PlanNacionalId == 1);
    }

    [Fact]
    public async Task GetById_WhenPlanDoesNotExist_ShouldReturnNotFound()
    {
        var serviceMock = new Mock<IMacroPlanNacionalService>();
        serviceMock.Setup(s => s.GetByIdAsync(77)).ReturnsAsync((PlanesNacionalesDesarrollo?)null);

        var controller = BuildController(serviceMock, new Mock<IMapper>());

        var result = await controller.GetById(77);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFound.Value.Should().BeOfType<ApiResponse<MacroPlanNacionalDto>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Plan nacional no encontrado");
    }

    [Fact]
    public async Task Create_WithValidClaim_ShouldReturnCreated_AndPassActorIdToService()
    {
        var dto = new MacroPlanNacionalCreateDto
        {
            Nombre = "Plan Nacional",
            PeriodoInicio = 2025,
            PeriodoFin = 2030,
            Estado = "Activo"
        };

        var created = BuildPlan(12, "Plan Nacional", 2025, 2030, "Activo");
        var serviceMock = new Mock<IMacroPlanNacionalService>();
        serviceMock.Setup(s => s.CreateAsync(dto, "user-123")).ReturnsAsync(created);

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<MacroPlanNacionalDto>(created)).Returns(new MacroPlanNacionalDto
        {
            PlanNacionalId = 12,
            Nombre = "Plan Nacional",
            PeriodoInicio = 2025,
            PeriodoFin = 2030,
            Estado = "Activo",
            FechaCreacion = created.FechaCreacion
        });

        var controller = BuildController(serviceMock, mapperMock);
        controller.ControllerContext = BuildControllerContext("user-123");

        var result = await controller.Create(dto);

        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(PlanesNacionalesController.GetById));
        var response = createdResult.Value.Should().BeOfType<ApiResponse<MacroPlanNacionalDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.PlanNacionalId.Should().Be(12);

        serviceMock.Verify(s => s.CreateAsync(dto, "user-123"), Times.Once);
    }

    [Fact]
    public async Task Create_WhenServiceThrowsValidation_ShouldReturnBadRequest()
    {
        var dto = new MacroPlanNacionalCreateDto
        {
            Nombre = "Plan Nacional",
            PeriodoInicio = 2031,
            PeriodoFin = 2030,
            Estado = "Activo"
        };

        var serviceMock = new Mock<IMacroPlanNacionalService>();
        serviceMock.Setup(s => s.CreateAsync(dto, It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("PeriodoInicio no puede ser mayor a PeriodoFin"));

        var controller = BuildController(serviceMock, new Mock<IMapper>());
        controller.ControllerContext = BuildControllerContext("user-123");

        var result = await controller.Create(dto);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequest.Value.Should().BeOfType<ApiResponse<MacroPlanNacionalDto>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("PeriodoInicio no puede ser mayor a PeriodoFin");
    }

    [Fact]
    public async Task Update_WhenPlanDoesNotExist_ShouldReturnNotFound()
    {
        var serviceMock = new Mock<IMacroPlanNacionalService>();
        serviceMock.Setup(s => s.UpdateAsync(99, It.IsAny<MacroPlanNacionalUpdateDto>(), It.IsAny<string>()))
            .ReturnsAsync((PlanesNacionalesDesarrollo?)null);

        var controller = BuildController(serviceMock, new Mock<IMapper>());
        controller.ControllerContext = BuildControllerContext("user-123");

        var result = await controller.Update(99, new MacroPlanNacionalUpdateDto { Estado = "Suspendido" });

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFound.Value.Should().BeOfType<ApiResponse<MacroPlanNacionalDto>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Plan nacional no encontrado");
    }

    [Fact]
    public async Task Delete_WhenServiceThrowsValidation_ShouldReturnBadRequest()
    {
        var serviceMock = new Mock<IMacroPlanNacionalService>();
        serviceMock.Setup(s => s.DeleteAsync(15, It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("No se puede eliminar el plan porque tiene objetivos asociados"));

        var controller = BuildController(serviceMock, new Mock<IMapper>());
        controller.ControllerContext = BuildControllerContext("user-123");

        var result = await controller.Delete(15);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequest.Value.Should().BeOfType<ApiResponse<string>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("No se puede eliminar el plan porque tiene objetivos asociados");
    }

    [Fact]
    public async Task GetResumen_WhenServiceThrows_ShouldReturnInternalServerError()
    {
        var serviceMock = new Mock<IMacroPlanNacionalService>();
        serviceMock.Setup(s => s.GetResumenAsync()).ThrowsAsync(new Exception("boom"));

        var controller = BuildController(serviceMock, new Mock<IMapper>());

        var result = await controller.GetResumen();

        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
        var response = objectResult.Value.Should().BeOfType<ApiResponse<MacroPlanNacionalResumenDto>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Error interno del servidor");
    }

    private static PlanesNacionalesDesarrollo BuildPlan(int id, string nombre, int periodoInicio, int periodoFin, string estado)
    {
        return new PlanesNacionalesDesarrollo
        {
            PlanNacionalId = id,
            Nombre = nombre,
            PeriodoInicio = periodoInicio,
            PeriodoFin = periodoFin,
            Estado = estado,
            FechaCreacion = DateTime.UtcNow
        };
    }

    private static PlanesNacionalesController BuildController(Mock<IMacroPlanNacionalService> serviceMock, Mock<IMapper> mapperMock)
    {
        return new PlanesNacionalesController(
            serviceMock.Object,
            mapperMock.Object,
            new Mock<ILogger<PlanesNacionalesController>>().Object);
    }

    private static ControllerContext BuildControllerContext(string actorId)
    {
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, actorId) },
                    "TestAuth"))
            }
        };
    }
}