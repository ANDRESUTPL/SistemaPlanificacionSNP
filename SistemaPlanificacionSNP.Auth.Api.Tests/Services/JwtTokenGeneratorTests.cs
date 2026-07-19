using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SistemaPlanificacionSNP.Domain.Entities.Seguridad;
using SistemaPlanificacionSNP.Infrastructure.JWT;

namespace SistemaPlanificacionSNP.Auth.Api.Tests.Services;

public class JwtTokenGeneratorTests
{
    private readonly JwtSettings _settings = new()
    {
        SecretKey = "super-secret-key-for-tests-1234567890",
        Issuer = "snp-test-issuer",
        Audience = "snp-test-audience",
        ExpirationMinutes = 60,
        RefreshTokenExpirationDays = 7
    };

    [Fact]
    public void GenerateAccessToken_ShouldIncludeRequiredClaimsAndExpiration()
    {
        var generator = new JwtTokenGenerator(_settings, NullLogger<JwtTokenGenerator>.Instance);
        var usuario = BuildUsuario();
        var roles = BuildRoles();

        var token = generator.GenerateAccessToken(usuario, roles);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        token.Should().NotBeNullOrWhiteSpace();
        jwt.Should().NotBeNull();

        jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value.Should().Be(usuario.UsuarioId.ToString());
        jwt.Claims.First(c => c.Type == ClaimTypes.Email).Value.Should().Be(usuario.Email);
        jwt.Claims.Select(c => c.Value).Should().Contain("Administrador");
        var payloadSegment = token.Split('.')[1];
        var paddedPayload = payloadSegment.PadRight(payloadSegment.Length + (4 - payloadSegment.Length % 4) % 4, '=');
        var payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(paddedPayload.Replace('-', '+').Replace('_', '/')));
        payloadJson.Should().Contain("Lectura_10");

        var expiration = generator.GetTokenExpiration(token);
        expiration.Should().BeAfter(DateTime.UtcNow.AddMinutes(55));
        expiration.Should().BeBefore(DateTime.UtcNow.AddMinutes(65));
    }

    [Fact]
    public void ValidateToken_WithTamperedToken_ShouldReturnNull()
    {
        var generator = new JwtTokenGenerator(_settings, NullLogger<JwtTokenGenerator>.Instance);
        var token = generator.GenerateAccessToken(BuildUsuario(), BuildRoles());
        var tamperedToken = token + "tampered";

        var principal = generator.ValidateToken(tamperedToken);

        principal.Should().BeNull();
    }

    [Fact]
    public void GenerateRefreshToken_TwoCalls_ShouldReturnDifferentValues()
    {
        var generator = new JwtTokenGenerator(_settings, NullLogger<JwtTokenGenerator>.Instance);

        var token1 = generator.GenerateRefreshToken();
        var token2 = generator.GenerateRefreshToken();

        token1.Should().NotBeNullOrWhiteSpace();
        token2.Should().NotBeNullOrWhiteSpace();
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GenerateAccessToken_ShouldProduceJwtWithThreeSegments()
    {
        var generator = new JwtTokenGenerator(_settings, NullLogger<JwtTokenGenerator>.Instance);

        var token = generator.GenerateAccessToken(BuildUsuario(), BuildRoles());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Should().NotBeNull();
        token.Split('.').Length.Should().Be(3);
    }

    private static Usuario BuildUsuario()
    {
        return new Usuario
        {
            UsuarioId = 1,
            NombreUsuario = "admin",
            Email = "admin@snp.gob",
            Nombre = "Admin",
            Apellido = "Sistema",
            PasswordHash = "hash",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
    }

    private static List<Rol> BuildRoles()
    {
        var pantalla = new Pantalla
        {
            PantallaId = 10,
            Nombre = "Usuarios",
            Ruta = "/seguridad/usuarios",
            Icono = "fa-users",
            Orden = 1,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        var permiso = new RolPermiso
        {
            RolPermisoId = 100,
            PantallaId = pantalla.PantallaId,
            Pantalla = pantalla,
            Lectura = true,
            Creacion = true,
            Edicion = false,
            Eliminacion = false,
            FechaCreacion = DateTime.UtcNow
        };

        var rol = new Rol
        {
            RolId = 5,
            Nombre = "Administrador",
            Descripcion = "Rol admin",
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            RolPermisos = new List<RolPermiso> { permiso }
        };

        permiso.Rol = rol;

        return new List<Rol> { rol };
    }
}
