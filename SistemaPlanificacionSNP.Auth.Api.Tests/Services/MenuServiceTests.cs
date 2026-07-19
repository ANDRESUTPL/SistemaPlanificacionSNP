using FluentAssertions;
using Moq;
using SistemaPlanificacionSNP.Domain.Entities.Seguridad;
using SistemaPlanificacionSNP.Infrastructure.Repositories;
using SistemaPlanificacionSNP.Infrastructure.Services;
using SistemaPlanificacionSNP.Infrastructure.UnitOfWork;

namespace SistemaPlanificacionSNP.Auth.Api.Tests.Services;

public class MenuServiceTests
{
    [Fact]
    public async Task ObtenerMenuParaUsuarioAsync_ShouldBuildHierarchy_WhenUserHasReadPermissions()
    {
        var userId = 10;
        var parent = new Pantalla
        {
            PantallaId = 1,
            Nombre = "Seguridad",
            Ruta = "/seguridad",
            Icono = "fa-lock",
            Orden = 1,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        var child = new Pantalla
        {
            PantallaId = 2,
            Nombre = "Usuarios",
            Ruta = "/seguridad/usuarios",
            Icono = "fa-users",
            PantallaPadrId = 1,
            PantallaPadr = parent,
            Orden = 1,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        parent.InversePantallaPadr.Add(child);

        var usuario = BuildUsuarioWithPermissions(userId, new[]
        {
            BuildRolPermiso(100, 1, parent, lectura: true, creacion: false, edicion: false, eliminacion: false),
            BuildRolPermiso(101, 2, child, lectura: true, creacion: true, edicion: false, eliminacion: false)
        });

        var service = BuildService(usuario, new[] { parent, child });

        var result = await service.ObtenerMenuParaUsuarioAsync(userId);

        result.Should().HaveCount(1);
        result[0].Nombre.Should().Be("Seguridad");
        result[0].Subpantallas.Should().HaveCount(1);
        result[0].Subpantallas[0].Nombre.Should().Be("Usuarios");
        result[0].Subpantallas[0].RolPermisos[0].Creacion.Should().BeTrue();
    }

    [Fact]
    public async Task ObtenerMenuParaUsuarioAsync_ShouldMergePermissionsFromMultipleRoles()
    {
        var userId = 20;
        var pantalla = new Pantalla
        {
            PantallaId = 5,
            Nombre = "Roles",
            Ruta = "/seguridad/roles",
            Icono = "fa-user-shield",
            Orden = 1,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        var rolPermiso1 = BuildRolPermiso(201, 5, pantalla, lectura: true, creacion: true, edicion: false, eliminacion: false);
        var rolPermiso2 = BuildRolPermiso(202, 5, pantalla, lectura: true, creacion: false, edicion: true, eliminacion: false);

        var usuario = new Usuario
        {
            UsuarioId = userId,
            NombreUsuario = "multirol",
            Email = "multirol@snp.gob",
            PasswordHash = "hash",
            Nombre = "Multi",
            Apellido = "Rol",
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            UsuarioRols = new List<UsuarioRol>
            {
                new()
                {
                    Rol = new Rol
                    {
                        RolId = 1,
                        Nombre = "Rol1",
                        Descripcion = "R1",
                        Activo = true,
                        FechaCreacion = DateTime.UtcNow,
                        RolPermisos = new List<RolPermiso> { rolPermiso1 }
                    }
                },
                new()
                {
                    Rol = new Rol
                    {
                        RolId = 2,
                        Nombre = "Rol2",
                        Descripcion = "R2",
                        Activo = true,
                        FechaCreacion = DateTime.UtcNow,
                        RolPermisos = new List<RolPermiso> { rolPermiso2 }
                    }
                }
            }
        };

        var service = BuildService(usuario, new[] { pantalla });

        var result = await service.ObtenerMenuParaUsuarioAsync(userId);

        result.Should().HaveCount(1);
        var permiso = result[0].RolPermisos[0];
        permiso.Lectura.Should().BeTrue();
        permiso.Creacion.Should().BeTrue();
        permiso.Edicion.Should().BeTrue();
        permiso.Eliminacion.Should().BeFalse();
    }

    [Fact]
    public async Task ObtenerMenuParaUsuarioAsync_WhenUserNotFound_ShouldThrowInvalidOperationException()
    {
        var service = BuildService(usuario: null, pantallas: new List<Pantalla>());

        var action = async () => await service.ObtenerMenuParaUsuarioAsync(999);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Usuario no encontrado");
    }

    private static MenuService BuildService(Usuario? usuario, IEnumerable<Pantalla> pantallas)
    {
        var usuarioRepoMock = new Mock<IUsuarioRepository>();
        usuarioRepoMock
            .Setup(r => r.GetWithRolesAsync(It.IsAny<int>()))
            .ReturnsAsync(usuario);

        var pantallaRepoMock = new Mock<IRepository<Pantalla>>();
        pantallaRepoMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Pantalla, bool>>>() ))
            .ReturnsAsync(pantallas);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock.SetupGet(u => u.Usuarios).Returns(usuarioRepoMock.Object);
        unitOfWorkMock.Setup(u => u.GetRepository<Pantalla>()).Returns(pantallaRepoMock.Object);

        return new MenuService(unitOfWorkMock.Object);
    }

    private static Usuario BuildUsuarioWithPermissions(int userId, IEnumerable<RolPermiso> permisos)
    {
        var rol = new Rol
        {
            RolId = 1,
            Nombre = "Administrador",
            Descripcion = "Rol admin",
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            RolPermisos = permisos.ToList()
        };

        foreach (var permiso in rol.RolPermisos)
        {
            permiso.Rol = rol;
        }

        return new Usuario
        {
            UsuarioId = userId,
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
                    UsuarioId = userId,
                    RolId = rol.RolId,
                    Rol = rol
                }
            }
        };
    }

    private static RolPermiso BuildRolPermiso(int rolPermisoId, int pantallaId, Pantalla pantalla, bool lectura, bool creacion, bool edicion, bool eliminacion)
    {
        return new RolPermiso
        {
            RolPermisoId = rolPermisoId,
            PantallaId = pantallaId,
            Pantalla = pantalla,
            Lectura = lectura,
            Creacion = creacion,
            Edicion = edicion,
            Eliminacion = eliminacion,
            FechaCreacion = DateTime.UtcNow
        };
    }
}
