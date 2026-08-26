using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPlanificacionSNP.Web.Common;
using SistemaPlanificacionSNP.Web.Models;
using SistemaPlanificacionSNP.Web.Services;
using System.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace SistemaPlanificacionSNP.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly IAuthService _authService;
        private readonly ILogger<AccountController> _logger;

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
                var response = await _apiClient.SendAsync(HttpMethod.Post, "/api/auth/login", loginDto);

                if (response == null)
                {
                    ModelState.AddModelError(string.Empty, "Error de conexión con el servidor");
                    return View(model);
                }

                if (!response.IsSuccessStatusCode)
                {
                    var apiError = await ApiHttpErrorHelper.TryExtractApiMessageAsync(response);
                    ModelState.AddModelError(
                        string.Empty,
                        apiError ?? ApiHttpErrorHelper.BuildStatusMessage(
                            response.StatusCode,
                            "No fue posible iniciar sesion.",
                            unauthorizedMessage: "Usuario o contrasena incorrectos."));
                    return View(model);
                }

                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json))
                {
                    ModelState.AddModelError(string.Empty, "Respuesta vacía del servidor de autenticación");
                    return View(model);
                }

                var loginResult = JsonSerializer.Deserialize<JsonElement>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (loginResult.ValueKind == JsonValueKind.Undefined || loginResult.ValueKind == JsonValueKind.Null)
                {
                    ModelState.AddModelError(string.Empty, "Respuesta inválida del servidor");
                    return View(model);
                }

                // Parsear respuesta
                var data = loginResult.GetProperty("data");
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

                var claimsIdentity = BuildIdentityFromAccessToken(accessToken);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.Recuerdame,
                    ExpiresUtc = DateTimeOffset.FromUnixTimeSeconds(new JwtSecurityTokenHandler().ReadJwtToken(accessToken).Payload.Expiration ?? DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds())
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation($"User {nombreUsuario} logged in successfully");

				if (string.IsNullOrEmpty(returnUrl) || returnUrl == "/" || returnUrl.Equals("/SNPWeb/", StringComparison.OrdinalIgnoreCase))
				{

					// === INICIO DE SOLUCIÓN TEMPORAL PARA DEMO ===
					try
					{
						// Intentamos buscar la propiedad "roles" o "rol" dentro de usuario o data
						JsonElement rolesElement;
						if (usuario.TryGetProperty("roles", out rolesElement) || data.TryGetProperty("roles", out rolesElement))
						{
							var roles = JsonSerializer.Deserialize<List<SistemaPlanificacionSNP.Infrastructure.DTOs.RolDto>>(
								rolesElement.GetRawText(),
								new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

							if (roles != null)
							{
								// Extraemos todos los PermisoDto de la lista de roles
								var permisos = roles.SelectMany(r => r.Permisos).ToList();
								HttpContext.Session.SetObject("PermisosSesion", permisos);
							}
						}
						else if (usuario.TryGetProperty("rol", out var rolElement) || data.TryGetProperty("rol", out rolElement))
						{
							// Contingencia por si la API devuelve un solo objeto RolDto
							var rol = JsonSerializer.Deserialize<SistemaPlanificacionSNP.Infrastructure.DTOs.RolDto>(
								rolElement.GetRawText(),
								new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

							if (rol != null && rol.Permisos != null)
							{
								HttpContext.Session.SetObject("PermisosSesion", rol.Permisos);
							}
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning($"Fallo al guardar permisos en sesión para demo: {ex.Message}");
					}
					// === FIN DE SOLUCIÓN TEMPORAL ===



					return RedirectToAction("Index", "Dashboard");
				}
				return LocalRedirect(returnUrl);
			}
            catch (Exception ex)
            {
                _logger.LogError($"Error in Login: {ex.Message}", ex);
                ModelState.AddModelError(string.Empty, "Error inesperado durante el login");
                return View(model);
            }
        }

        // Short JWT forms + long .NET URI forms for each essential claim type
        private static readonly HashSet<string> _essentialClaimTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "sub", ClaimTypes.NameIdentifier,
            "unique_name", "name", ClaimTypes.Name,
            "email", ClaimTypes.Email,
            "role", ClaimTypes.Role,
            "Nombre", "Apellido"
        };

		private static ClaimsIdentity BuildIdentityFromAccessToken(string accessToken)
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var token = tokenHandler.ReadJwtToken(accessToken);

			var claims = new List<Claim>();
			foreach (var c in token.Claims)
			{
				if (c.Type == "P")
				{
					var parts = c.Value.Split(':');
					if (parts.Length == 2)
					{
						var pid = parts[0];
						var flags = parts[1];
						if (flags.Contains('L')) claims.Add(new Claim($"Lectura_{pid}", "true"));
						if (flags.Contains('C')) claims.Add(new Claim($"Creacion_{pid}", "true"));
						if (flags.Contains('E')) claims.Add(new Claim($"Edicion_{pid}", "true"));
						if (flags.Contains('D')) claims.Add(new Claim($"Eliminacion_{pid}", "true"));
					}
				}
				else if (!string.Equals(c.Type, "exp", StringComparison.OrdinalIgnoreCase)
						 && !string.Equals(c.Type, "nbf", StringComparison.OrdinalIgnoreCase)
						 && !string.Equals(c.Type, "iat", StringComparison.OrdinalIgnoreCase))
				{
					claims.Add(c);
				}
			}
			
			return new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
		}

		[HttpGet]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            return await ExecuteLogoutAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [ActionName("Logout")]
        public async Task<IActionResult> LogoutPost()
        {
            return await ExecuteLogoutAsync();
        }

        private async Task<IActionResult> ExecuteLogoutAsync()
        {
            try
            {
                await _apiClient.SendAsync(HttpMethod.Post, ConstanteAPI.URL_AUT + "/api/auth/logout");

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
                var changeDto = new
                {
                    model.PasswordActual,
                    model.PasswordNueva,
                    model.PasswordConfirmar
                };

				var response = await _apiClient.SendAsync(HttpMethod.Post, "/api/auth/cambiar-password", changeDto);

				if (response == null)
                {
                    ModelState.AddModelError(string.Empty, "No fue posible conectar con el servidor. Intenta nuevamente.");
                    return View(model);
                }

                if (response.IsSuccessStatusCode)
                {
                    ViewBag.Success = "Contraseña actualizada exitosamente";
                    _logger.LogInformation($"User {User?.Identity?.Name} changed password");
                    return View();
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TempData["Warning"] = "Tu sesión expiró. Inicia sesión nuevamente para continuar.";
                    _authService.ClearAuthData();
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction(nameof(Login));
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    ModelState.AddModelError(string.Empty, ApiHttpErrorHelper.ForbiddenDefaultMessage);
                    return View(model);
                }

                var apiError = await ApiHttpErrorHelper.TryExtractApiMessageAsync(response);
                ModelState.AddModelError(
                    string.Empty,
                    apiError ?? ApiHttpErrorHelper.BuildStatusMessage(response.StatusCode, "Error al cambiar la contrasena"));
                return View(model);
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

                var response = await _apiClient.SendAsync(HttpMethod.Get, $"/api/usuarios/{userId}");
                if (response == null)
                {
                    TempData["Error"] = "No fue posible obtener tu perfil por un error de conexión.";
                    return RedirectToAction("Index", "Dashboard");
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TempData["Warning"] = "Tu sesión expiró. Inicia sesión nuevamente para continuar.";
                    _authService.ClearAuthData();
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction(nameof(Login));
                }

                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    TempData["Error"] = "No tienes permisos para visualizar la información del perfil.";
                    return RedirectToAction(nameof(AccessDenied));
                }

                if (!response.IsSuccessStatusCode)
                {
                    var apiError = await ApiHttpErrorHelper.TryExtractApiMessageAsync(response);
                    TempData["Error"] = apiError ?? ApiHttpErrorHelper.BuildStatusMessage(response.StatusCode, "No fue posible cargar la informacion de perfil.");
                    return RedirectToAction("Index", "Dashboard");
                }

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

                TempData["Warning"] = "No fue posible procesar la respuesta del perfil.";
                return RedirectToAction("Index", "Dashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Profile: {ex.Message}", ex);
                TempData["Error"] = "Ocurrió un error inesperado al cargar el perfil.";
                return RedirectToAction("Index", "Dashboard");
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
