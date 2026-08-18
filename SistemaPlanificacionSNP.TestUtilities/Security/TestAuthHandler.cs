using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SistemaPlanificacionSNP.TestUtilities.Security;

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestAuth";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers.TryGetValue("X-Test-UserId", out var userIdHeader)
            ? userIdHeader.ToString()
            : "1";

        var userName = Request.Headers.TryGetValue("X-Test-UserName", out var userNameHeader)
            ? userNameHeader.ToString()
            : "integration.user";

        var nombre = Request.Headers.TryGetValue("X-Test-Nombre", out var nombreHeader)
            ? nombreHeader.ToString()
            : "Integration";

        var apellido = Request.Headers.TryGetValue("X-Test-Apellido", out var apellidoHeader)
            ? apellidoHeader.ToString()
            : "Tester";

        var entidadPublicaId = Request.Headers.TryGetValue("X-Test-EntidadPublicaId", out var headerValue)
            ? headerValue.ToString()
            : "1";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userName),
            new("Nombre", nombre),
            new("Apellido", apellido),
            new("Lectura_13", "true"),
            new("Creacion_13", "true"),
            new("Edicion_13", "true"),
            new("Eliminacion_13", "true"),
            new("EntidadPublicaId", entidadPublicaId),
            new(ClaimTypes.Role, "Administrador")
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
