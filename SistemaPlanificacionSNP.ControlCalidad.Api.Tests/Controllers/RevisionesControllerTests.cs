using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SistemaPlanificacionSNP.ControlCalidad.Api.Controllers;
using SistemaPlanificacionSNP.Domain.Entities.ControlCalidad;
using SistemaPlanificacionSNP.Infrastructure.Common;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.Services;

namespace SistemaPlanificacionSNP.ControlCalidad.Api.Tests.Controllers;

public class RevisionesControllerTests
{
    [Fact]
    public async Task GetAll_ShouldReturnOkWithPaginatedResponse()
    {
        var query = new RevisioneQueryDto { PageNumber = 1, PageSize = 20 };
        var entities = new List<Revisione>
        {
            new()
            {
                RevisionId = 1,
                CodigoRevision = "REV-001",
                Modulo = "Planificacion",
                Estado = "Pendiente",
                FechaRevision = DateTime.UtcNow
            }
        };

        var serviceMock = new Mock<IRevisioneControlCalidadService>();
        serviceMock.Setup(s => s.GetPagedAsync(query)).ReturnsAsync((entities, 1));

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<List<RevisioneDto>>(entities)).Returns(new List<RevisioneDto>
        {
            new() { RevisionId = 1, CodigoRevision = "REV-001", Modulo = "Planificacion", Estado = "Pendiente", FechaRevision = DateTime.UtcNow }
        });

        var controller = new RevisionesController(serviceMock.Object, mapperMock.Object, new Mock<ILogger<RevisionesController>>().Object);

        var result = await controller.GetAll(query);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiPaginatedResponse<RevisioneDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.TotalItems.Should().Be(1);
        response.Data.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ShouldReturnNotFound()
    {
        var serviceMock = new Mock<IRevisioneControlCalidadService>();
        serviceMock.Setup(s => s.GetByIdAsync(99, true)).ReturnsAsync((Revisione?)null);

        var controller = new RevisionesController(serviceMock.Object, new Mock<IMapper>().Object, new Mock<ILogger<RevisionesController>>().Object);

        var result = await controller.GetById(99);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFound.Value.Should().BeOfType<ApiResponse<RevisioneDto>>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetByCodigo_WhenCodigoIsEmpty_ShouldReturnBadRequest()
    {
        var controller = new RevisionesController(
            new Mock<IRevisioneControlCalidadService>().Object,
            new Mock<IMapper>().Object,
            new Mock<ILogger<RevisionesController>>().Object);

        var result = await controller.GetByCodigo(" ");

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequest.Value.Should().BeOfType<ApiResponse<RevisioneDto>>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Create_WhenServiceValidationFails_ShouldReturnBadRequest()
    {
        var dto = new RevisioneCreateDto { CodigoRevision = "REV-1", Modulo = "M1", Estado = "Pendiente" };

        var serviceMock = new Mock<IRevisioneControlCalidadService>();
        serviceMock.Setup(s => s.CreateAsync(dto)).ThrowsAsync(new InvalidOperationException("Estado no permitido"));

        var controller = new RevisionesController(serviceMock.Object, new Mock<IMapper>().Object, new Mock<ILogger<RevisionesController>>().Object);

        var result = await controller.Create(dto);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequest.Value.Should().BeOfType<ApiResponse<RevisioneDto>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Be("Estado no permitido");
    }

    [Fact]
    public async Task Delete_WhenRevisionDoesNotExist_ShouldReturnNotFound()
    {
        var serviceMock = new Mock<IRevisioneControlCalidadService>();
        serviceMock.Setup(s => s.DeleteAsync(123)).ReturnsAsync(false);

        var controller = new RevisionesController(serviceMock.Object, new Mock<IMapper>().Object, new Mock<ILogger<RevisionesController>>().Object);

        var result = await controller.Delete(123);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFound.Value.Should().BeOfType<ApiResponse<string>>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task GetDashboard_WhenUnhandledException_ShouldReturnInternalServerError()
    {
        var serviceMock = new Mock<IRevisioneControlCalidadService>();
        serviceMock.Setup(s => s.GetDashboardAsync()).ThrowsAsync(new Exception("boom"));

        var controller = new RevisionesController(serviceMock.Object, new Mock<IMapper>().Object, new Mock<ILogger<RevisionesController>>().Object);

        var result = await controller.GetDashboard();

        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
        var response = objectResult.Value.Should().BeOfType<ApiResponse<ControlCalidadDashboardDto>>().Subject;
        response.Success.Should().BeFalse();
    }
}
