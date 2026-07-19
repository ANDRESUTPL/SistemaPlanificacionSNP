using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.Seguridad;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.Repositories;

namespace SistemaPlanificacionSNP.Auth.Api.Tests.Repositories;

public class UsuarioRepositoryTests
{
    [Fact]
    public async Task GetWithRolesAsync_ShouldReturnUserWithRolesPermissionsAndPantallas()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new SqliteAuthDbContextForTests(options);
        await context.Database.EnsureCreatedAsync();

        var pantalla = new Pantalla
        {
            Nombre = "Usuarios",
            Ruta = "/seguridad/usuarios",
            Icono = "fa-users",
            Orden = 1,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        var rol = new Rol
        {
            Nombre = "Administrador",
            Descripcion = "Rol con acceso total",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        var permiso = new RolPermiso
        {
            Rol = rol,
            Pantalla = pantalla,
            Lectura = true,
            Creacion = true,
            Edicion = true,
            Eliminacion = false,
            FechaCreacion = DateTime.UtcNow
        };

        var usuario = new Usuario
        {
            NombreUsuario = "admin",
            Email = "admin@snp.gob",
            PasswordHash = "hash",
            Nombre = "Admin",
            Apellido = "Sistema",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        var usuarioRol = new UsuarioRol
        {
            Usuario = usuario,
            Rol = rol,
            FechaAsignacion = DateTime.UtcNow
        };

        context.Pantallas.Add(pantalla);
        context.Rols.Add(rol);
        context.RolPermisos.Add(permiso);
        context.Usuarios.Add(usuario);
        context.UsuarioRols.Add(usuarioRol);
        await context.SaveChangesAsync();

        var repository = new UsuarioRepository(context);

        var result = await repository.GetWithRolesAsync(usuario.UsuarioId);

        result.Should().NotBeNull();
        result!.UsuarioId.Should().Be(usuario.UsuarioId);
        result.UsuarioRols.Should().HaveCount(1);

        var rolCargado = result.UsuarioRols.First().Rol;
        rolCargado.Should().NotBeNull();
        rolCargado.Nombre.Should().Be("Administrador");
        rolCargado.RolPermisos.Should().HaveCount(1);
        rolCargado.RolPermisos.First().Pantalla.Should().NotBeNull();
        rolCargado.RolPermisos.First().Pantalla.Nombre.Should().Be("Usuarios");
        rolCargado.RolPermisos.First().Lectura.Should().BeTrue();
    }

    [Fact]
    public async Task GetWithRolesAsync_WhenUserDoesNotExist_ShouldReturnNull()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new SqliteAuthDbContextForTests(options);
        await context.Database.EnsureCreatedAsync();

        var repository = new UsuarioRepository(context);

        var result = await repository.GetWithRolesAsync(99999);

        result.Should().BeNull();
    }

    private sealed class SqliteAuthDbContextForTests : AuthDbContext
    {
        public SqliteAuthDbContextForTests(DbContextOptions<AuthDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Avoid SQL Server hardcoded configuration during tests.
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (!string.IsNullOrWhiteSpace(property.GetDefaultValueSql()))
                    {
                        property.SetDefaultValueSql(null);
                    }
                }
            }
        }
    }
}
