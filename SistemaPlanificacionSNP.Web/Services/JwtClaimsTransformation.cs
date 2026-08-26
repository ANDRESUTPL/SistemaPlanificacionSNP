using Microsoft.AspNetCore.Authentication;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace SistemaPlanificacionSNP.Web.Services
{
    // Rehydrates permission claims per-request from the raw JWT cookie without persisting them to the auth cookie.
    public class JwtClaimsTransformation : IClaimsTransformation
    {
        private const string CacheKey = "ClaimsTransformed";
        private static readonly string[] _permissionPrefixes =
            ["Lectura_", "Creacion_", "Edicion_", "Eliminacion_"];

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAuthService _authService;
        private readonly ILogger<JwtClaimsTransformation> _logger;

        public JwtClaimsTransformation(
            IHttpContextAccessor httpContextAccessor,
            IAuthService authService,
            ILogger<JwtClaimsTransformation> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _authService = authService;
            _logger = logger;
        }

        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var context = _httpContextAccessor.HttpContext;

            // ASP.NET calls TransformAsync multiple times per request — return cached result immediately.
            if (context?.Items.TryGetValue(CacheKey, out var cached) == true && cached is ClaimsPrincipal cachedPrincipal)
                return Task.FromResult(cachedPrincipal);

            var accessToken = _authService.GetAccessToken();
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                if (context != null)
                    context.Items[CacheKey] = principal;
                return Task.FromResult(principal);
            }

            try
            {
                var token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

                var permissionClaims = token.Claims
                    .Where(c => _permissionPrefixes.Any(p => c.Type.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                var enriched = permissionClaims.Count > 0
                    ? new ClaimsPrincipal(principal.Identities.Append(new ClaimsIdentity(permissionClaims, "JwtPermissions")))
                    : principal;

                if (context != null)
                    context.Items[CacheKey] = enriched;
                return Task.FromResult(enriched);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudieron cargar los permisos desde accessToken; se conserva el principal autenticado.");
                if (context != null)
                    context.Items[CacheKey] = principal;
                return Task.FromResult(principal);
            }
        }
    }
}
