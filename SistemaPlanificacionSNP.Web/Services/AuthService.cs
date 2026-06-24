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
        private readonly ILogger<AuthService> _logger;
        private const string AccessTokenKey = "accessToken";
        private const string RefreshTokenKey = "refreshToken";
        private const string UserNameKey = "userName";

        public AuthService(IHttpContextAccessor httpContextAccessor, ILogger<AuthService> logger)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
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
            // Implementado en AccountController
            return await Task.FromResult(false);
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
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            };

            context.Response.Cookies.Append(AccessTokenKey, accessToken, cookieOptions);
            context.Response.Cookies.Append(RefreshTokenKey, refreshToken, cookieOptions);
            
            var userCookieOptions = new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Strict,
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
    }
}
