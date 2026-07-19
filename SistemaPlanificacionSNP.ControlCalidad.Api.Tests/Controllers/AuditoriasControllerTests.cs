using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using SistemaPlanificacionSNP.ControlCalidad.Api.Controllers;
using SistemaPlanificacionSNP.Domain.Entities.ControlCalidad;
using SistemaPlanificacionSNP.Infrastructure.Common;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.Services;

namespace SistemaPlanificacionSNP.ControlCalidad.Api.Tests.Controllers;

public class AuditoriasControllerTests
{
    [Fact]
    public async Task GetAll_ShouldReturnOkWithPaginatedResponse()
    {
        var query = new AuditoriaQueryDto { PageNumber = 1, PageSize = 10 };
        var entities = new List<Auditoria>
        {
            new()
            {
                AuditoriaId = 1,
                RevisionId = 10,
                Tipo = "Interna",
                Resultado = "Conforme",
                Responsable = "qa.user",
                FechaRegistro = DateTime.UtcNow
            }
        };

        var serviceMock = new Mock<IAuditoriaControlCalidadService>();
        serviceMock.Setup(s => s.GetPagedAsync(query)).ReturnsAsync((entities, 1));

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<List<AuditoriaDto>>(entities)).Returns(new List<AuditoriaDto>
        {
            new() { AuditoriaId = 1, RevisionId = 10, Tipo = "Interna", Resultado = "Conforme", Responsable = "qa.user", FechaRegistro = DateTime.UtcNow }
        });

        var controller = new AuditoriasController(serviceMock.Object, mapperMock.Object, new Mock<ILogger<AuditoriasController>>().Object);

        var result = await controller.GetAll(query);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<ApiPaginatedResponse<AuditoriaDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.TotalItems.Should().Be(1);
    }

    [Fact]
    public async Task GetById_WhenNotFound_ShouldReturnNotFound()
    {
        var serviceMock = new Mock<IAuditoriaControlCalidadService>();
        serviceMock.Setup(s => s.GetByIdAsync(77)).ReturnsAsync((Auditoria?)null);

        var controller = new AuditoriasController(serviceMock.Object, new Mock<IMapper>().Object, new Mock<ILogger<AuditoriasController>>().Object);

        var result = await controller.GetById(77);

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFound.Value.Should().BeOfType<ApiResponse<AuditoriaDto>>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Create_WhenUserClaimsMissing_ShouldReturnUnauthorized()
    {
        var controller = new AuditoriasController(
            new Mock<IAuditoriaControlCalidadService>().Object,
            new Mock<IMapper>().Object,
            new Mock<ILogger<AuditoriasController>>().Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        var result = await controller.Create(new AuditoriaCreateDto { RevisionId = 1, Tipo = "Interna", Resultado = "Conforme" });

        var unauthorized = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var response = unauthorized.Value.Should().BeOfType<ApiResponse<AuditoriaDto>>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Create_WithValidClaims_ShouldReturnCreated()
    {
        var dto = new AuditoriaCreateDto { RevisionId = 1, Tipo = "Interna", Resultado = "Conforme" };
        var created = new Auditoria
        {
            AuditoriaId = 20,
            RevisionId = 1,
            Tipo = "Interna",
            Resultado = "Conforme",
            Responsable = "Ana QA",
            FechaRegistro = DateTime.UtcNow
        };

        var serviceMock = new Mock<IAuditoriaControlCalidadService>();
        serviceMock.Setup(s => s.CreateAsync(dto, "Ana QA")).ReturnsAsync(created);

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<AuditoriaDto>(created)).Returns(new AuditoriaDto
        {
            AuditoriaId = 20,
            RevisionId = 1,
            Tipo = "Interna",
            Resultado = "Conforme",
            Responsable = "Ana QA",
            FechaRegistro = created.FechaRegistro
        });

        var controller = new AuditoriasController(serviceMock.Object, mapperMock.Object, new Mock<ILogger<AuditoriasController>>().Object)
        {
            ControllerContext = BuildControllerContextWithClaims("Ana", "QA")
        };

        var result = await controller.Create(dto);

        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = createdResult.Value.Should().BeOfType<ApiResponse<AuditoriaDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.AuditoriaId.Should().Be(20);
    }

    [Fact]
    public async Task Update_WhenAuditoriaNotFound_ShouldReturnNotFound()
    {
        var serviceMock = new Mock<IAuditoriaControlCalidadService>();
        serviceMock.Setup(s => s.UpdateAsync(80, It.IsAny<AuditoriaUpdateDto>())).ReturnsAsync((Auditoria?)null);

        var controller = new AuditoriasController(serviceMock.Object, new Mock<IMapper>().Object, new Mock<ILogger<AuditoriasController>>().Object);

        var result = await controller.Update(80, new AuditoriaUpdateDto { Resultado = "Conforme" });

        var notFound = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFound.Value.Should().BeOfType<ApiResponse<AuditoriaDto>>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_WhenServiceThrows_ShouldReturnInternalServerError()
    {
        var serviceMock = new Mock<IAuditoriaControlCalidadService>();
        serviceMock.Setup(s => s.DeleteAsync(2)).ThrowsAsync(new Exception("boom"));

        var controller = new AuditoriasController(serviceMock.Object, new Mock<IMapper>().Object, new Mock<ILogger<AuditoriasController>>().Object);

        var result = await controller.Delete(2);

        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
        var response = objectResult.Value.Should().BeOfType<ApiResponse<string>>().Subject;
        response.Success.Should().BeFalse();
    }

    private static ControllerContext BuildControllerContextWithClaims(string nombre, string apellido)
    {
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[]
                    {
                        new Claim("Nombre", nombre),
                        new Claim("Apellido", apellido)
                    },
                    "TestAuth"))
            }
        };
    }
}
