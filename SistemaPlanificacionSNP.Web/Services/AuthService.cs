using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace SistemaPlanificacionSNP.Web.Services
{
    /// <summary>
    /// Servicio para manejar autenticación y tokens JWT
    /// </summary>
    public interface IAuthService
    {
        Task<bool> LoginAsync(string nombreUsuario, string password);
        Task LogoutAsync();
        Task<bool> RefreshTokenAsync();
        bool IsAuthenticated();
        string? GetAccessToken();
        string? GetRefreshToken();
        string? GetUserName();
        void SaveAuthData(string accessToken, string refreshToken, string usuario);
        void ClearAuthData();
    }

    public class AuthService : IAuthService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiGatewayBaseUrl;
        private readonly ILogger<AuthService> _logger;
        private const string AccessTokenKey = "accessToken";
        private const string RefreshTokenKey = "refreshToken";
        private const string UserNameKey = "userName";

        public AuthService(IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<AuthService> logger)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _apiGatewayBaseUrl = configuration["ApiGateway:BaseUrl"] ?? "https://localhost:52555";
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> LoginAsync(string nombreUsuario, string password)
        {
            // Este método es decorativo; el flujo real ocurre en AccountController
            return await Task.FromResult(true);
        }

        public async Task LogoutAsync()
        {
            ClearAuthData();
            await Task.CompletedTask;
        }

        public async Task<bool> RefreshTokenAsync()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                return false;
            }

            var accessToken = GetAccessToken();
            var refreshToken = GetRefreshToken();

            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
            {
                return false;
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri(_apiGatewayBaseUrl);

                var response = await client.PostAsJsonAsync("/api/auth/refresh-token", new
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                });

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }

                var payload = JsonSerializer.Deserialize<JsonElement>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (!payload.TryGetProperty("data", out var data))
                {
                    return false;
                }

                var newAccessToken = data.GetProperty("accessToken").GetString();
                var newRefreshToken = data.GetProperty("refreshToken").GetString();

                if (string.IsNullOrWhiteSpace(newAccessToken) || string.IsNullOrWhiteSpace(newRefreshToken))
                {
                    return false;
                }

                var principal = BuildPrincipalFromJwt(newAccessToken);
                if (principal == null)
                {
                    return false;
                }

                var userName = principal.FindFirstValue(ClaimTypes.Name)
                    ?? principal.FindFirstValue("unique_name")
                    ?? GetUserName()
                    ?? "Usuario";

                SaveAuthData(newAccessToken, newRefreshToken, userName);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = GetTokenExpiration(newAccessToken)
                };

                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Refresh token failed: {ex.Message}", ex);
                return false;
            }
        }

        public bool IsAuthenticated()
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.User?.Identity?.IsAuthenticated ?? false;
        }

        public string? GetAccessToken()
        {
            var context = _httpContextAccessor.HttpContext;
            string? token = null;

            if (context != null)
            {
                context.Request.Cookies.TryGetValue(AccessTokenKey, out token);
            }

            return token;
        }

        public string? GetRefreshToken()
        {
            var context = _httpContextAccessor.HttpContext;
            string? token = null;

            if (context != null)
            {
                context.Request.Cookies.TryGetValue(RefreshTokenKey, out token);
            }

            return token;
        }

        public string? GetUserName()
        {
            var context = _httpContextAccessor.HttpContext;
            string? userName = null;

            if (context != null)
            {
                context.Request.Cookies.TryGetValue(UserNameKey, out userName);
            }

            return userName;
        }

        public void SaveAuthData(string accessToken, string refreshToken, string usuario)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return;

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            };

            context.Response.Cookies.Append(AccessTokenKey, accessToken, cookieOptions);
            context.Response.Cookies.Append(RefreshTokenKey, refreshToken, cookieOptions);
            
            var userCookieOptions = new CookieOptions
            {
                HttpOnly = false,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            };
            
            context.Response.Cookies.Append(UserNameKey, usuario, userCookieOptions);
        }

        public void ClearAuthData()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return;

            context.Response.Cookies.Delete(AccessTokenKey);
            context.Response.Cookies.Delete(RefreshTokenKey);
            context.Response.Cookies.Delete(UserNameKey);
        }

        private static ClaimsPrincipal? BuildPrincipalFromJwt(string accessToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(accessToken);

            var claims = token.Claims
                .Where(c => !string.Equals(c.Type, "exp", StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(c.Type, "nbf", StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(c.Type, "iat", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!claims.Any())
            {
                return null;
            }

            // El JWT emite los roles con el tipo corto "role"; sin esto User.IsInRole no los encuentra.
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, "role");
            return new ClaimsPrincipal(identity);
        }

        private static DateTimeOffset GetTokenExpiration(string accessToken)
        {
            var token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
            return token.Payload.Expiration.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(token.Payload.Expiration.Value)
                : DateTimeOffset.UtcNow.AddHours(1);
        }
    }
}
