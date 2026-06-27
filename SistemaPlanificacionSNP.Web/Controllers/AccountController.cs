using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPlanificacionSNP.Web.Models;
using SistemaPlanificacionSNP.Web.Services;
using System.Security.Claims;
using System.Text.Json;

namespace SistemaPlanificacionSNP.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly IAuthService _authService;
        private readonly ILogger<AccountController> _logger;
        private const string ApiGatewayUrl = "https://localhost:52555";

        public AccountController(IApiClient apiClient, IAuthService authService, ILogger<AccountController> logger)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Llamar a API de login
                var loginDto = new { model.NombreUsuario, model.Password, model.Recuerdame };
                var response = await _apiClient.PostAsync<JsonElement>($"{ApiGatewayUrl}/api/auth/login", loginDto);

                if (response.ValueKind == JsonValueKind.Undefined || response.ValueKind == JsonValueKind.Null)
                {
                    ModelState.AddModelError(string.Empty, "Error de conexión con el servidor");
                    return View(model);
                }

                // Parsear respuesta
                var data = response.GetProperty("data");
                var usuario = data.GetProperty("usuario");
                var accessToken = data.GetProperty("accessToken").GetString();
                var refreshToken = data.GetProperty("refreshToken").GetString();
                var nombreUsuario = usuario.GetProperty("nombreUsuario").GetString();

                if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
                {
                    ModelState.AddModelError(string.Empty, "Respuesta inválida del servidor");
                    return View(model);
                }

                // Guardar tokens en cookies
                _authService.SaveAuthData(accessToken, refreshToken, nombreUsuario ?? "Usuario");

                // Crear claims principal para autenticación local
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.GetProperty("usuarioId").GetInt32().ToString()),
                    new Claim(ClaimTypes.Name, nombreUsuario ?? "Usuario"),
                    new Claim(ClaimTypes.Email, usuario.GetProperty("email").GetString() ?? ""),
                    new Claim("Nombre", usuario.GetProperty("nombre").GetString() ?? ""),
                    new Claim("Apellido", usuario.GetProperty("apellido").GetString() ?? "")
                };

                // Agregar roles
                if (usuario.TryGetProperty("roles", out var rolesElement))
                {
                    foreach (var rol in rolesElement.EnumerateArray())
                    {
                        claims.Add(new Claim(ClaimTypes.Role, rol.GetProperty("nombre").GetString() ?? ""));
                    }
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.Recuerdame,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation($"User {nombreUsuario} logged in successfully");

                return LocalRedirect(returnUrl ?? "/");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Login: {ex.Message}", ex);
                ModelState.AddModelError(string.Empty, "Error inesperado durante el login");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Llamar a API de logout
                var token = _authService.GetAccessToken();
                if (!string.IsNullOrEmpty(token))
                {
                    // Pasar token en header Authorization
                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                        await client.PostAsync($"{ApiGatewayUrl}/api/auth/logout", null);
                    }
                }

                _authService.ClearAuthData();
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                _logger.LogInformation($"User {User?.Identity?.Name} logged out");
                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Logout: {ex.Message}", ex);
                return RedirectToAction(nameof(Login));
            }
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.PasswordNueva != model.PasswordConfirmar)
            {
                ModelState.AddModelError(nameof(model.PasswordConfirmar), "Las contraseñas no coinciden");
                return View(model);
            }

            try
            {
                var token = _authService.GetAccessToken();
                if (string.IsNullOrEmpty(token))
                {
                    return RedirectToAction(nameof(Login));
                }

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                    var changeDto = new
                    {
                        model.PasswordActual,
                        model.PasswordNueva,
                        model.PasswordConfirmar
                    };

                    var content = new StringContent(
                        JsonSerializer.Serialize(changeDto),
                        System.Text.Encoding.UTF8,
                        "application/json"
                    );

                    var response = await client.PostAsync(
                        $"{ApiGatewayUrl}/api/auth/cambiar-password",
                        content
                    );

                    if (response.IsSuccessStatusCode)
                    {
                        ViewBag.Success = "Contraseña actualizada exitosamente";
                        _logger.LogInformation($"User {User?.Identity?.Name} changed password");
                        return View();
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Error al cambiar la contraseña");
                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ChangePassword: {ex.Message}", ex);
                ModelState.AddModelError(string.Empty, "Error inesperado");
                return View(model);
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction(nameof(Login));
                }

                var token = _authService.GetAccessToken();
                if (string.IsNullOrEmpty(token))
                {
                    return RedirectToAction(nameof(Login));
                }

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                    var response = await client.GetAsync($"{ApiGatewayUrl}/api/usuarios/{userId}");

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var data = JsonSerializer.Deserialize<JsonElement>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (data.ValueKind == JsonValueKind.Object && data.TryGetProperty("data", out var userElement))
                        {
                            var model = new UserProfileViewModel
                            {
                                UsuarioId = userElement.GetProperty("usuarioId").GetInt32(),
                                NombreUsuario = userElement.GetProperty("nombreUsuario").GetString() ?? "",
                                Email = userElement.GetProperty("email").GetString() ?? "",
                                Nombre = userElement.GetProperty("nombre").GetString() ?? "",
                                Apellido = userElement.GetProperty("apellido").GetString() ?? "",
                                Activo = userElement.GetProperty("activo").GetBoolean(),
                                FechaCreacion = userElement.GetProperty("fechaCreacion").GetDateTime()
                            };

                            return View(model);
                        }
                    }
                }

                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Profile: {ex.Message}", ex);
                return RedirectToAction(nameof(Login));
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
