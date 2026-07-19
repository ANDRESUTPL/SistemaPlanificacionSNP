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

public class ObjetivosEstrategicosControllerTests
{
    [Fact]
    public async Task GetAll_ShouldReturnOkWithPaginatedResponse()
    {
        var query = new MacroObjetivoEstrategicoQueryDto { PageNumber = 1, PageSize = 10 };
        var entities = new List<ObjetivosEstrategico>
        {
            BuildObjective(1, 10, "OBJ-001", "Objetivo 1")
        };

        var serviceMock = new Mock<IMacroObjetivoEstrategicoService>();
        serviceMock.Setup(s => s.GetPagedAsync(query)).ReturnsAsync((entities, 1));

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<List<MacroObjetivoEstrategicoDto>>(entities)).Returns(new List<MacroObjetivoEstrategicoDto>
        {
            new() { ObjetivoEstrategicoId = 1, PlanNacionalId = 10, Codigo = "OBJ-001", Nombre = "Objetivo 1" }
        });

        var controller = BuildController(serviceMock, mapperMock);

        var result = await controller.GetAll(query);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiPaginatedResponse<MacroObjetivoEstrategicoDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.TotalItems.Should().Be(1);
        response.Data.Data.Should().ContainSingle(x => x.Codigo == "OBJ-001");
    }

    [Fact]
    public async Task GetByPlanNacionalId_ShouldReturnOkWithMappedObjectives()
    {
        var entities = new List<ObjetivosEstrategico>
        {
            BuildObjective(1, 10, "OBJ-001", "Objetivo 1"),
            BuildObjective(2, 10, "OBJ-002", "Objetivo 2")
        };

        var serviceMock = new Mock<IMacroObjetivoEstrategicoService>();
        serviceMock.Setup(s => s.GetByPlanNacionalIdAsync(10)).ReturnsAsync(entities);

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<List<MacroObjetivoEstrategicoDto>>(entities)).Returns(new List<MacroObjetivoEstrategicoDto>
        {
            new() { ObjetivoEstrategicoId = 1, PlanNacionalId = 10, Codigo = "OBJ-001", Nombre = "Objetivo 1" },
            new() { ObjetivoEstrategicoId = 2, PlanNacionalId = 10, Codigo = "OBJ-002", Nombre = "Objetivo 2" }
        });

        var controller = BuildController(serviceMock, mapperMock);

        var result = await controller.GetByPlanNacionalId(10);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<List<MacroObjetivoEstrategicoDto>>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_WhenObjectiveDoesNotExist_ShouldReturnNotFound()
    {
        var serviceMock = new Mock<IMacroObjetivoEstrategicoService>();
        serviceMock.Setup(s => s.GetByIdAsync(77)).ReturnsAsync((ObjetivosEstrategico?)null);

        var controller = BuildController(serviceMock, new Mock<IMapper>());

        var result = await controller.GetById(77);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFound.Value.Should().BeOfType<ApiResponse<MacroObjetivoEstrategicoDto>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Objetivo estratégico no encontrado");
    }

    [Fact]
    public async Task Create_WithValidClaim_ShouldReturnCreated_AndPassActorIdToService()
    {
        var dto = new MacroObjetivoEstrategicoCreateDto
        {
            PlanNacionalId = 10,
            Codigo = "OBJ-001",
            Nombre = "Objetivo 1"
        };

        var created = BuildObjective(22, 10, "OBJ-001", "Objetivo 1");
        var serviceMock = new Mock<IMacroObjetivoEstrategicoService>();
        serviceMock.Setup(s => s.CreateAsync(dto, "user-123")).ReturnsAsync(created);

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<MacroObjetivoEstrategicoDto>(created)).Returns(new MacroObjetivoEstrategicoDto
        {
            ObjetivoEstrategicoId = 22,
            PlanNacionalId = 10,
            Codigo = "OBJ-001",
            Nombre = "Objetivo 1"
        });

        var controller = BuildController(serviceMock, mapperMock);
        controller.ControllerContext = BuildControllerContext("user-123");

        var result = await controller.Create(dto);

        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(ObjetivosEstrategicosController.GetById));
        var response = createdResult.Value.Should().BeOfType<ApiResponse<MacroObjetivoEstrategicoDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.ObjetivoEstrategicoId.Should().Be(22);

        serviceMock.Verify(s => s.CreateAsync(dto, "user-123"), Times.Once);
    }

    [Fact]
    public async Task Create_WhenServiceThrowsValidation_ShouldReturnBadRequest()
    {
        var dto = new MacroObjetivoEstrategicoCreateDto
        {
            PlanNacionalId = 404,
            Codigo = "OBJ-404",
            Nombre = "Objetivo inválido"
        };

        var serviceMock = new Mock<IMacroObjetivoEstrategicoService>();
        serviceMock.Setup(s => s.CreateAsync(dto, It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("El plan nacional no existe"));

        var controller = BuildController(serviceMock, new Mock<IMapper>());
        controller.ControllerContext = BuildControllerContext("user-123");

        var result = await controller.Create(dto);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequest.Value.Should().BeOfType<ApiResponse<MacroObjetivoEstrategicoDto>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("El plan nacional no existe");
    }

    [Fact]
    public async Task Update_WhenObjectiveDoesNotExist_ShouldReturnNotFound()
    {
        var serviceMock = new Mock<IMacroObjetivoEstrategicoService>();
        serviceMock.Setup(s => s.UpdateAsync(99, It.IsAny<MacroObjetivoEstrategicoUpdateDto>(), It.IsAny<string>()))
            .ReturnsAsync((ObjetivosEstrategico?)null);

        var controller = BuildController(serviceMock, new Mock<IMapper>());
        controller.ControllerContext = BuildControllerContext("user-123");

        var result = await controller.Update(99, new MacroObjetivoEstrategicoUpdateDto { Nombre = "Nuevo" });

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFound.Value.Should().BeOfType<ApiResponse<MacroObjetivoEstrategicoDto>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Objetivo estratégico no encontrado");
    }

    [Fact]
    public async Task Delete_WhenServiceReturnsTrue_ShouldReturnOk()
    {
        var serviceMock = new Mock<IMacroObjetivoEstrategicoService>();
        serviceMock.Setup(s => s.DeleteAsync(22, "user-123")).ReturnsAsync(true);

        var controller = BuildController(serviceMock, new Mock<IMapper>());
        controller.ControllerContext = BuildControllerContext("user-123");

        var result = await controller.Delete(22);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiResponse<string>>().Subject;
        response.Success.Should().BeTrue();
        response.Message.Should().Be("Objetivo estratégico eliminado exitosamente");

        serviceMock.Verify(s => s.DeleteAsync(22, "user-123"), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenServiceThrows_ShouldReturnInternalServerError()
    {
        var serviceMock = new Mock<IMacroObjetivoEstrategicoService>();
        serviceMock.Setup(s => s.GetByIdAsync(33)).ThrowsAsync(new Exception("boom"));

        var controller = BuildController(serviceMock, new Mock<IMapper>());

        var result = await controller.GetById(33);

        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
        var response = objectResult.Value.Should().BeOfType<ApiResponse<MacroObjetivoEstrategicoDto>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Error interno del servidor");
    }

    private static ObjetivosEstrategico BuildObjective(int id, int planId, string codigo, string nombre)
    {
        return new ObjetivosEstrategico
        {
            ObjetivoEstrategicoId = id,
            PlanNacionalId = planId,
            Codigo = codigo,
            Nombre = nombre
        };
    }

    private static ObjetivosEstrategicosController BuildController(Mock<IMacroObjetivoEstrategicoService> serviceMock, Mock<IMapper> mapperMock)
    {
        return new ObjetivosEstrategicosController(
            serviceMock.Object,
            mapperMock.Object,
            new Mock<ILogger<ObjetivosEstrategicosController>>().Object);
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