using System.Security.Claims;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SistemaPlanificacionSNP.Auth.Api.Controllers;
using SistemaPlanificacionSNP.Infrastructure.Common;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.Repositories;
using SistemaPlanificacionSNP.Infrastructure.Services;
using SistemaPlanificacionSNP.Infrastructure.UnitOfWork;

namespace SistemaPlanificacionSNP.Auth.Api.Tests.Controllers;

public class UsuariosControllerTests
{
    [Fact]
    public async Task Crear_WhenEmailAlreadyExists_ShouldReturnBadRequest()
    {
        var usuarioRepoMock = new Mock<IUsuarioRepository>();
        usuarioRepoMock.Setup(r => r.ExisteNombreUsuarioAsync("nuevo.usuario")).ReturnsAsync(false);
        usuarioRepoMock.Setup(r => r.ExisteEmailAsync("exists@snp.gob")).ReturnsAsync(true);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Usuarios).Returns(usuarioRepoMock.Object);

        var passwordServiceMock = new Mock<IPasswordHashService>();
        var auditoriaServiceMock = new Mock<IAuditoriaService>();
        var menuServiceMock = new Mock<IMenuService>();
        var mapperMock = new Mock<IMapper>();
        var loggerMock = new Mock<ILogger<UsuariosController>>();

        var controller = new UsuariosController(
            unitOfWorkMock.Object,
            passwordServiceMock.Object,
            auditoriaServiceMock.Object,
            menuServiceMock.Object,
            mapperMock.Object,
            loggerMock.Object);

        var payload = new UsuarioCreateDto
        {
            NombreUsuario = "nuevo.usuario",
            Email = "exists@snp.gob",
            Password = "Password123!",
            Nombre = "Nuevo",
            Apellido = "Usuario"
        };

        var result = await controller.Crear(payload);

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequest.Value.Should().BeOfType<ApiResponse<UsuarioDto>>().Subject;

        response.Success.Should().BeFalse();
        response.Message.Should().Contain("email ya está registrado");
    }

    [Fact]
    public async Task ObtenerMenuActual_ShouldReadUserIdClaim_AndCallMenuService()
    {
        var expectedUserId = 55;
        var expectedMenu = new List<MenuPermisoDto>
        {
            new()
            {
                PantallaId = 1,
                Nombre = "Seguridad",
                Icono = "fa-lock",
                Ruta = "/seguridad",
                Orden = 1,
                Subpantallas = new List<MenuPermisoDto>()
            }
        };

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var passwordServiceMock = new Mock<IPasswordHashService>();
        var auditoriaServiceMock = new Mock<IAuditoriaService>();
        var menuServiceMock = new Mock<IMenuService>();
        menuServiceMock.Setup(m => m.ObtenerMenuParaUsuarioAsync(expectedUserId)).ReturnsAsync(expectedMenu);

        var mapperMock = new Mock<IMapper>();
        var loggerMock = new Mock<ILogger<UsuariosController>>();

        var controller = new UsuariosController(
            unitOfWorkMock.Object,
            passwordServiceMock.Object,
            auditoriaServiceMock.Object,
            menuServiceMock.Object,
            mapperMock.Object,
            loggerMock.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, expectedUserId.ToString()) },
                    "TestAuth"))
            }
        };

        var result = await controller.ObtenerMenuActual();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<MenuPermisoDto>>>().Subject;

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Should().HaveCount(1);

        menuServiceMock.Verify(m => m.ObtenerMenuParaUsuarioAsync(expectedUserId), Times.Once);
    }
}
