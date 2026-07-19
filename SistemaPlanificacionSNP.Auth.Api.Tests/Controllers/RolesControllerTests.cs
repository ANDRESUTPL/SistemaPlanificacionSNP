using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using SistemaPlanificacionSNP.Auth.Api.Controllers;
using SistemaPlanificacionSNP.Domain.Entities.Seguridad;
using SistemaPlanificacionSNP.Infrastructure.Common;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.Repositories;
using SistemaPlanificacionSNP.Infrastructure.Services;
using SistemaPlanificacionSNP.Infrastructure.UnitOfWork;

namespace SistemaPlanificacionSNP.Auth.Api.Tests.Controllers;

public class RolesControllerTests
{
    [Fact]
    public async Task GetAll_ShouldReturnOkWithRolDtoList()
    {
        await using var context = BuildInMemoryContext();
        context.Rols.Add(new Rol
        {
            Nombre = "Administrador",
            Descripcion = "Rol administrador",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var unitOfWork = new FakeUnitOfWork(context);
        var auditoriaServiceMock = new Mock<IAuditoriaService>();
        var loggerMock = new Mock<ILogger<RolesController>>();
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<List<RolDto>>(It.IsAny<List<Rol>>()))
            .Returns(new List<RolDto>
            {
                new()
                {
                    RolId = 1,
                    Nombre = "Administrador",
                    Descripcion = "Rol administrador",
                    Activo = true
                }
            });

        var controller = new RolesController(context, unitOfWork, auditoriaServiceMock.Object, mapperMock.Object, loggerMock.Object);

        var result = await controller.GetAll();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<RolDto>>>().Subject;

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Should().HaveCount(1);
        response.Data[0].Nombre.Should().Be("Administrador");
    }

    [Fact]
    public async Task Crear_WithValidPayload_ShouldReturnCreated()
    {
        await using var context = BuildInMemoryContext();
        var unitOfWork = new FakeUnitOfWork(context);

        var auditoriaServiceMock = new Mock<IAuditoriaService>();
        auditoriaServiceMock.Setup(a => a.RegistrarCreacionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<RolDto>(It.IsAny<Rol>()))
            .Returns((Rol r) => new RolDto
            {
                RolId = r.RolId,
                Nombre = r.Nombre,
                Descripcion = r.Descripcion,
                Activo = r.Activo,
                Permisos = new List<PermisoDto>()
            });

        var loggerMock = new Mock<ILogger<RolesController>>();

        var controller = new RolesController(context, unitOfWork, auditoriaServiceMock.Object, mapperMock.Object, loggerMock.Object);
        controller.ControllerContext = BuildControllerContextWithUser(1);

        var payload = new RolCreateUpdateDto
        {
            Nombre = "Planificador",
            Descripcion = "Rol de planificación",
            Activo = true,
            Permisos = new List<RolPermisoConfigDto>(),
            PermisoIds = new List<int>()
        };

        var result = await controller.Crear(payload);

        var objectResult = result.Result.Should().BeAssignableTo<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        var response = objectResult.Value.Should().BeOfType<ApiResponse<RolDto>>().Subject;

        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Nombre.Should().Be("Planificador");
    }

    private static AuthDbContext BuildInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase($"RolesControllerTests_{Guid.NewGuid()}")
            .Options;

        return new InMemoryAuthDbContextForTests(options);
    }

    private static ControllerContext BuildControllerContextWithUser(int userId)
    {
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) },
                    "TestAuth"))
            }
        };
    }

    private sealed class InMemoryAuthDbContextForTests : AuthDbContext
    {
        public InMemoryAuthDbContextForTests(DbContextOptions<AuthDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Prevent SQL Server hardcoded configuration in tests.
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private readonly AuthDbContext _context;

        public FakeUnitOfWork(AuthDbContext context)
        {
            _context = context;
        }

        public IUsuarioRepository Usuarios => throw new NotSupportedException();
        public IAuditoriaRepository Auditorias => throw new NotSupportedException();
        public IPlanificacionRepository Planificacion => throw new NotSupportedException();

        public IRepository<T> GetRepository<T>() where T : class
        {
            return new Repository<T>(_context);
        }

        public Task<int> SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public Task<bool> BeginTransactionAsync() => Task.FromResult(true);
        public Task<bool> CommitAsync() => Task.FromResult(true);
        public Task<bool> RollbackAsync() => Task.FromResult(true);
        public void Dispose() => _context.Dispose();
    }
}
