using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IO;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SistemaPlanificacionSNP.Domain.Entities.Seguridad;
using SistemaPlanificacionSNP.Infrastructure.Common;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.JWT;

namespace SistemaPlanificacionSNP.Auth.Api.Tests.Integration;

public class AuthApiIntegrationTests : IClassFixture<AuthApiWebApplicationFactory>
{
    private readonly AuthApiWebApplicationFactory _factory;

    public AuthApiIntegrationTests(AuthApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_Then_GetMenuActual_WithBearerToken_ShouldSucceed()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        await _factory.EnsureSeedDataAsync();

        var loginPayload = new LoginDto
        {
            NombreUsuario = "admin.integration",
            Password = "Password123!"
        };

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginPayload);
        loginResponse.EnsureSuccessStatusCode();

        var loginBody = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();
        loginBody.Should().NotBeNull();
        loginBody!.Success.Should().BeTrue();
        loginBody.Data.Should().NotBeNull();
        loginBody.Data!.AccessToken.Should().NotBeNullOrWhiteSpace();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.Data.AccessToken);

        var menuResponse = await client.GetAsync("/api/usuarios/menu/actual");
        menuResponse.EnsureSuccessStatusCode();

        var menuBody = await menuResponse.Content.ReadFromJsonAsync<ApiResponse<List<MenuPermisoDto>>>();
        menuBody.Should().NotBeNull();
        menuBody!.Success.Should().BeTrue();
        menuBody.Data.Should().NotBeNull();
        menuBody.Data!.Should().HaveCount(1);
        menuBody.Data[0].Nombre.Should().Be("Seguridad");
    }
}

public sealed class AuthApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath;
    private readonly string _connectionString;

    public AuthApiWebApplicationFactory()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"AuthApiIntegration_{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_dbPath}";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                ["DatabaseProvider"] = "Sqlite",
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["Jwt:SecretKey"] = "integration-tests-secret-key-1234567890",
                ["Jwt:Issuer"] = "IntegrationIssuer",
                ["Jwt:Audience"] = "IntegrationAudience",
                ["Jwt:ExpirationMinutes"] = "60",
                ["Jwt:RefreshTokenExpirationDays"] = "7",
                ["DatabaseMigration:AutoMigrateOnStartup"] = "false"
            };

            configBuilder.AddInMemoryCollection(inMemorySettings);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AuthDbContext>>();
            services.RemoveAll<IConfigureOptions<DbContextOptions<AuthDbContext>>>();

            services.AddDbContext<AuthDbContext>(options =>
            {
                options.UseSqlite(_connectionString);
            });
        });
    }

    public async Task EnsureSeedDataAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await db.Database.EnsureCreatedAsync();
        Seed(db);
    }

    private static void Seed(AuthDbContext db)
    {
        if (db.Usuarios.Any(u => u.NombreUsuario == "admin.integration"))
        {
            return;
        }

        var parentPantalla = new Pantalla
        {
            Nombre = "Seguridad",
            Ruta = "/seguridad",
            Icono = "fa-shield-alt",
            Orden = 1,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        db.Pantallas.Add(parentPantalla);
        db.SaveChanges();

        var rol = new Rol
        {
            Nombre = "Administrador",
            Descripcion = "Rol admin integration",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        db.Rols.Add(rol);
        db.SaveChanges();

        db.RolPermisos.Add(new RolPermiso
        {
            RolId = rol.RolId,
            PantallaId = parentPantalla.PantallaId,
            Lectura = true,
            Creacion = true,
            Edicion = true,
            Eliminacion = true,
            FechaCreacion = DateTime.UtcNow
        });

        var usuario = new Usuario
        {
            NombreUsuario = "admin.integration",
            Email = "admin.integration@snp.gob",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", workFactor: 12),
            Nombre = "Admin",
            Apellido = "Integration",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        db.Usuarios.Add(usuario);
        db.SaveChanges();

        db.UsuarioRols.Add(new UsuarioRol
        {
            UsuarioId = usuario.UsuarioId,
            RolId = rol.RolId,
            FechaAsignacion = DateTime.UtcNow
        });

        db.SaveChanges();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                if (File.Exists(_dbPath))
                {
                    File.Delete(_dbPath);
                }
            }
            catch (IOException)
            {
                // Best effort cleanup for temp integration DB file.
            }
        }

        base.Dispose(disposing);
    }
}
