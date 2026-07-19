using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SistemaPlanificacionSNP.Auth.Api.Controllers;
using SistemaPlanificacionSNP.Domain.Entities.Seguridad;
using SistemaPlanificacionSNP.Infrastructure.Common;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.JWT;
using SistemaPlanificacionSNP.Infrastructure.Repositories;
using SistemaPlanificacionSNP.Infrastructure.Services;
using SistemaPlanificacionSNP.Infrastructure.UnitOfWork;

namespace SistemaPlanificacionSNP.Auth.Api.Tests.Controllers;

public class AuthControllerTests
{
    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnOkWithToken()
    {
        var usuario = new Usuario
        {
            UsuarioId = 1,
            NombreUsuario = "admin",
            Email = "admin@snp.gob",
            PasswordHash = "hash",
            Nombre = "Admin",
            Apellido = "Sistema",
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            UsuarioRols = new List<UsuarioRol>
            {
                new()
                {
                    Rol = new Rol
                    {
                        RolId = 5,
                        Nombre = "Administrador",
                        Descripcion = "Rol admin",
                        Activo = true,
                        FechaCreacion = DateTime.UtcNow,
                        RolPermisos = new List<RolPermiso>()
                    }
                }
            }
        };

        var usuarioRepoMock = new Mock<IUsuarioRepository>();
        usuarioRepoMock.Setup(r => r.GetByNombreUsuarioAsync("admin")).ReturnsAsync(usuario);
        usuarioRepoMock.Setup(r => r.GetWithRolesAsync(usuario.UsuarioId)).ReturnsAsync(usuario);
        usuarioRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Usuario>())).ReturnsAsync((Usuario u) => u);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Usuarios).Returns(usuarioRepoMock.Object);
        unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var passwordServiceMock = new Mock<IPasswordHashService>();
        passwordServiceMock.Setup(p => p.VerifyPassword("secret123", usuario.PasswordHash)).Returns(true);

        var tokenGeneratorMock = new Mock<IJwtTokenGenerator>();
        tokenGeneratorMock.Setup(t => t.GenerateAccessToken(It.IsAny<Usuario>(), It.IsAny<List<Rol>>())).Returns("fake.jwt.token");
        tokenGeneratorMock.Setup(t => t.GenerateRefreshToken()).Returns("refresh-token");
        tokenGeneratorMock.Setup(t => t.GetTokenExpiration("fake.jwt.token")).Returns(DateTime.UtcNow.AddMinutes(30));

        var auditoriaServiceMock = new Mock<IAuditoriaService>();
        auditoriaServiceMock.Setup(a => a.RegistrarActualizacionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<object>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<UsuarioDto>(It.IsAny<Usuario>())).Returns(new UsuarioDto
        {
            UsuarioId = usuario.UsuarioId,
            NombreUsuario = usuario.NombreUsuario,
            Email = usuario.Email,
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Activo = usuario.Activo,
            FechaCreacion = usuario.FechaCreacion
        });

        var loggerMock = new Mock<ILogger<AuthController>>();

        var controller = new AuthController(
            unitOfWorkMock.Object,
            passwordServiceMock.Object,
            tokenGeneratorMock.Object,
            auditoriaServiceMock.Object,
            mapperMock.Object,
            loggerMock.Object);

        var result = await controller.Login(new LoginDto { NombreUsuario = "admin", Password = "secret123" });

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<LoginResponseDto>>().Subject;

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.AccessToken.Should().Be("fake.jwt.token");
        response.Data.RefreshToken.Should().Be("refresh-token");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        var usuario = new Usuario
        {
            UsuarioId = 1,
            NombreUsuario = "admin",
            Email = "admin@snp.gob",
            PasswordHash = "hash",
            Nombre = "Admin",
            Apellido = "Sistema",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        var usuarioRepoMock = new Mock<IUsuarioRepository>();
        usuarioRepoMock.Setup(r => r.GetByNombreUsuarioAsync("admin")).ReturnsAsync(usuario);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Usuarios).Returns(usuarioRepoMock.Object);

        var passwordServiceMock = new Mock<IPasswordHashService>();
        passwordServiceMock.Setup(p => p.VerifyPassword("wrong-password", usuario.PasswordHash)).Returns(false);

        var tokenGeneratorMock = new Mock<IJwtTokenGenerator>();
        var auditoriaServiceMock = new Mock<IAuditoriaService>();
        var mapperMock = new Mock<IMapper>();
        var loggerMock = new Mock<ILogger<AuthController>>();

        var controller = new AuthController(
            unitOfWorkMock.Object,
            passwordServiceMock.Object,
            tokenGeneratorMock.Object,
            auditoriaServiceMock.Object,
            mapperMock.Object,
            loggerMock.Object);

        var result = await controller.Login(new LoginDto { NombreUsuario = "admin", Password = "wrong-password" });

        var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var response = unauthorizedResult.Value.Should().BeOfType<ApiResponse<LoginResponseDto>>().Subject;

        response.Success.Should().BeFalse();
        response.Message.Should().Contain("incorrectos");
    }
}
