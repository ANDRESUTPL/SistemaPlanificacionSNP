using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPlanificacionSNP.Web.Common;
using SistemaPlanificacionSNP.Web.Models;
using SistemaPlanificacionSNP.Web.Services;
using System.Net;
using System.Text.Json;

namespace SistemaPlanificacionSNP.Web.Controllers
{
    [Authorize]
    public class CatalogoRolesController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<CatalogoRolesController> _logger;

        public CatalogoRolesController(IApiClient apiClient, ILogger<CatalogoRolesController> logger)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("/seguridad/catalogo-roles")]
        public async Task<IActionResult> Index(string? buscar, bool? activo)
        {
            var rolesResult = await GetRolesAsync();
            var authRedirect = HandleUnauthorizedRedirect(rolesResult.StatusCode, rolesResult.ErrorMessage);
            if (authRedirect != null)
            {
                return authRedirect;
            }

            var roles = rolesResult.Data ?? new List<RolApiDto>();
            if (!rolesResult.IsSuccess)
            {
                ViewBag.Error = rolesResult.ErrorMessage;
            }

            var usuariosResult = await GetUsuariosAsync();
            var usuarios = usuariosResult.Data ?? new List<UsuarioApiDto>();
            var usuariosAsignadosPorRol = BuildUsuariosCountByRol(usuarios);

            var query = roles.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var term = buscar.Trim();
                query = query.Where(r =>
                    r.Nombre.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || r.Descripcion.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (activo.HasValue)
            {
                query = query.Where(r => r.Activo == activo.Value);
            }

            var model = new CatalogoRolesIndexViewModel
            {
                Buscar = buscar,
                Activo = activo,
                Roles = query
                    .OrderBy(r => r.Nombre)
                    .Select(r => new RolCatalogoListItemViewModel
                    {
                        RolId = r.RolId,
                        Nombre = r.Nombre,
                        Descripcion = r.Descripcion,
                        Activo = r.Activo,
                        UsuariosAsignados = usuariosAsignadosPorRol.TryGetValue(r.RolId, out var count) ? count : 0
                    })
                    .ToList()
            };

            if (TempData.TryGetValue("Success", out var success))
            {
                ViewBag.Success = success?.ToString();
            }

            if (TempData.TryGetValue("Warning", out var warning))
            {
                ViewBag.Warning = warning?.ToString();
            }

            return View(model);
        }

        [HttpGet("/seguridad/catalogo-roles/crear")]
        public async Task<IActionResult> Create()
        {
            var model = new CatalogoRolCreateViewModel();
            await PopulatePermissionMatrixAsync(model, null);
            return View(model);
        }

        [HttpPost("/seguridad/catalogo-roles/crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CatalogoRolCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulatePermissionMatrixAsync(model, BuildSelectionDictionary(model.PermisosPantalla));
                return View(model);
            }

            var payload = new RolCreateUpdateApiDto
            {
                Nombre = model.Nombre,
                Descripcion = model.Descripcion,
                Activo = model.Activo,
                Permisos = BuildPermissionPayload(model.PermisosPantalla)
            };

            try
            {
                var response = await _apiClient.SendAsync(HttpMethod.Post, "/api/roles/crear", payload);
                if (response?.IsSuccessStatusCode == true)
                {
                    TempData["Success"] = "Rol creado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }

                await AddResponseErrorToModelStateAsync(response, "No fue posible crear el rol.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CatalogoRoles.Create POST: {ex.Message}", ex);
                ModelState.AddModelError(string.Empty, "Ocurrio un error inesperado al crear el rol.");
            }

            return View(model);
        }

        [HttpGet("/seguridad/catalogo-roles/{id:int}/editar")]
        public async Task<IActionResult> Edit(int id)
        {
            var rolResult = await GetRolByIdAsync(id);
            var authRedirect = HandleUnauthorizedRedirect(rolResult.StatusCode, rolResult.ErrorMessage);
            if (authRedirect != null)
            {
                return authRedirect;
            }

            if (!rolResult.IsSuccess || rolResult.Data == null)
            {
                TempData["Warning"] = rolResult.ErrorMessage ?? "No fue posible cargar el rol.";
                return RedirectToAction(nameof(Index));
            }

            var model = new CatalogoRolEditViewModel
            {
                RolId = rolResult.Data.RolId,
                Nombre = rolResult.Data.Nombre,
                Descripcion = rolResult.Data.Descripcion,
                Activo = rolResult.Data.Activo
            };

            await PopulatePermissionMatrixAsync(model, BuildSelectionDictionary(rolResult.Data.Permisos));

            return View(model);
        }

        [HttpPost("/seguridad/catalogo-roles/{id:int}/editar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CatalogoRolEditViewModel model)
        {
            if (id != model.RolId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                await PopulatePermissionMatrixAsync(model, BuildSelectionDictionary(model.PermisosPantalla));
                return View(model);
            }

            var payload = new RolCreateUpdateApiDto
            {
                Nombre = model.Nombre,
                Descripcion = model.Descripcion,
                Activo = model.Activo,
                Permisos = BuildPermissionPayload(model.PermisosPantalla)
            };

            try
            {
                var response = await _apiClient.SendAsync(HttpMethod.Put, $"/api/roles/{model.RolId}", payload);
                if (response?.IsSuccessStatusCode == true)
                {
                    TempData["Success"] = "Rol actualizado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }

                await AddResponseErrorToModelStateAsync(response, "No fue posible actualizar el rol.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CatalogoRoles.Edit POST: {ex.Message}", ex);
                ModelState.AddModelError(string.Empty, "Ocurrio un error inesperado al actualizar el rol.");
            }

            return View(model);
        }

        [HttpGet("/seguridad/catalogo-roles/{id:int}")]
        public async Task<IActionResult> Detail(int id)
        {
            var rolResult = await GetRolByIdAsync(id);
            var authRedirect = HandleUnauthorizedRedirect(rolResult.StatusCode, rolResult.ErrorMessage);
            if (authRedirect != null)
            {
                return authRedirect;
            }

            if (!rolResult.IsSuccess || rolResult.Data == null)
            {
                TempData["Warning"] = rolResult.ErrorMessage ?? "No fue posible cargar el rol.";
                return RedirectToAction(nameof(Index));
            }

            var model = new CatalogoRolEditViewModel
            {
                RolId = rolResult.Data.RolId,
                Nombre = rolResult.Data.Nombre,
                Descripcion = rolResult.Data.Descripcion,
                Activo = rolResult.Data.Activo
            };

            await PopulatePermissionMatrixAsync(model, BuildSelectionDictionary(rolResult.Data.Permisos));

            return View(model);
        }

        [HttpPost("/seguridad/catalogo-roles/{id:int}/eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var response = await _apiClient.SendAsync(HttpMethod.Delete, $"/api/roles/{id}");
                if (response?.IsSuccessStatusCode == true)
                {
                    TempData["Success"] = "Rol eliminado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }

                var authRedirect = HandleUnauthorizedRedirect(response?.StatusCode, null);
                if (authRedirect != null)
                {
                    return authRedirect;
                }

                var apiError = await ApiHttpErrorHelper.TryExtractApiMessageAsync(response);
                TempData["Warning"] = apiError
                    ?? (response == null
                        ? "No fue posible conectar con el servidor. Intenta nuevamente."
                        : ApiHttpErrorHelper.BuildStatusMessage(response.StatusCode, "No fue posible eliminar el rol."));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CatalogoRoles.Delete POST: {ex.Message}", ex);
                TempData["Warning"] = "Ocurrio un error inesperado al eliminar el rol.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("/seguridad/catalogo-roles/{id:int}/desactivar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            try
            {
                var rolResult = await GetRolByIdAsync(id);
                if (!rolResult.IsSuccess || rolResult.Data == null)
                {
                    TempData["Warning"] = rolResult.ErrorMessage ?? "No fue posible ubicar el rol.";
                    return RedirectToAction(nameof(Index));
                }

                var payload = new RolCreateUpdateApiDto
                {
                    Nombre = rolResult.Data.Nombre,
                    Descripcion = rolResult.Data.Descripcion,
                    Activo = false
                };

                var response = await _apiClient.SendAsync(HttpMethod.Put, $"/api/roles/{id}", payload);
                if (response?.IsSuccessStatusCode == true)
                {
                    TempData["Success"] = "Rol desactivado exitosamente.";
                }
                else
                {
                    var apiError = await ApiHttpErrorHelper.TryExtractApiMessageAsync(response);
                    TempData["Warning"] = apiError ?? "No fue posible desactivar el rol.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CatalogoRoles.Deactivate POST: {ex.Message}", ex);
                TempData["Warning"] = "Ocurrio un error inesperado al desactivar el rol.";
            }

            return RedirectToAction(nameof(Index));
        }

        private static Dictionary<int, int> BuildUsuariosCountByRol(List<UsuarioApiDto> usuarios)
        {
            return usuarios
                .Where(u => u != null)
                .SelectMany(u => u.Roles ?? new List<RolApiDto>())
                .Where(r => r != null && r.RolId > 0)
                .GroupBy(r => r.RolId)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        private async Task PopulatePermissionMatrixAsync(CatalogoRolCreateViewModel model, Dictionary<int, CatalogoRolPermisoViewModel>? selectedPermissions)
        {
            var pantallasResult = await GetPantallasAsync();
            var pantallas = pantallasResult.Data ?? new List<PantallaCatalogoApiDto>();
            var selectedMap = selectedPermissions ?? new Dictionary<int, CatalogoRolPermisoViewModel>();

            model.PermisosPantalla = pantallas
                .OrderBy(p => p.Orden)
                .ThenBy(p => p.Nombre)
                .Select(p =>
                {
                    selectedMap.TryGetValue(p.PantallaId, out var permiso);
                    return new CatalogoRolPermisoViewModel
                    {
                        PantallaId = p.PantallaId,
                        NombrePantalla = p.Nombre,
                        Ruta = p.Ruta,
                        Icono = p.Icono,
                        Orden = p.Orden,
                        Lectura = permiso?.Lectura ?? false,
                        Creacion = permiso?.Creacion ?? false,
                        Edicion = permiso?.Edicion ?? false,
                        Eliminacion = permiso?.Eliminacion ?? false
                    };
                })
                .ToList();
        }

        private static Dictionary<int, CatalogoRolPermisoViewModel> BuildSelectionDictionary(IEnumerable<CatalogoRolPermisoViewModel>? permisos)
        {
            return (permisos ?? Enumerable.Empty<CatalogoRolPermisoViewModel>())
                .GroupBy(p => p.PantallaId)
                .ToDictionary(g => g.Key, g => g.First());
        }

        private static Dictionary<int, CatalogoRolPermisoViewModel> BuildSelectionDictionary(IEnumerable<RolPermisoApiDto>? permisos)
        {
            return (permisos ?? Enumerable.Empty<RolPermisoApiDto>())
                .GroupBy(p => p.PantallaId)
                .ToDictionary(
                    g => g.Key,
                    g => new CatalogoRolPermisoViewModel
                    {
                        PantallaId = g.Key,
                        Lectura = g.First().Lectura,
                        Creacion = g.First().Creacion,
                        Edicion = g.First().Edicion,
                        Eliminacion = g.First().Eliminacion
                    });
        }

        private static Dictionary<int, CatalogoRolPermisoViewModel> BuildSelectionDictionary(IEnumerable<PermisoApiDto>? permisos)
        {
            return (permisos ?? Enumerable.Empty<PermisoApiDto>())
                .GroupBy(p => p.PantallaId)
                .ToDictionary(
                    g => g.Key,
                    g => new CatalogoRolPermisoViewModel
                    {
                        PantallaId = g.Key,
                        NombrePantalla = g.First().NombrePantalla,
                        Ruta = string.Empty,
                        Icono = string.Empty,
                        Orden = 0,
                        Lectura = g.First().Lectura,
                        Creacion = g.First().Creacion,
                        Edicion = g.First().Edicion,
                        Eliminacion = g.First().Eliminacion
                    });
        }

        private static List<RolPermisoApiDto> BuildPermissionPayload(IEnumerable<CatalogoRolPermisoViewModel> permisos)
        {
            return permisos
                .Where(p => p.PantallaId > 0)
                .Select(p => new RolPermisoApiDto
                {
                    PantallaId = p.PantallaId,
                    Lectura = p.Lectura,
                    Creacion = p.Creacion,
                    Edicion = p.Edicion,
                    Eliminacion = p.Eliminacion
                })
                .ToList();
        }

        private async Task<ApiCallResult<List<PantallaCatalogoApiDto>>> GetPantallasAsync()
        {
            return await GetFromApiAsync<List<PantallaCatalogoApiDto>>("/api/roles/pantallas", "No fue posible cargar el catálogo de pantallas.");
        }

        private async Task<ApiCallResult<List<RolApiDto>>> GetRolesAsync()
        {
            return await GetFromApiAsync<List<RolApiDto>>("/api/roles", "No fue posible cargar el catálogo de roles.");
        }

        private async Task<ApiCallResult<List<UsuarioApiDto>>> GetUsuariosAsync()
        {
            return await GetFromApiAsync<List<UsuarioApiDto>>("/api/usuarios/con-roles", "No fue posible cargar usuarios para cálculo de asignaciones.");
        }

        private async Task<ApiCallResult<RolApiDto>> GetRolByIdAsync(int id)
        {
            return await GetFromApiAsync<RolApiDto>($"/api/roles/{id}", "No fue posible cargar el rol.");
        }

        private async Task AddResponseErrorToModelStateAsync(HttpResponseMessage? response, string defaultMessage)
        {
            if (response == null)
            {
                ModelState.AddModelError(string.Empty, "No fue posible conectar con el servidor. Intenta nuevamente.");
                return;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                TempData["Warning"] = "Tu sesion expiro. Inicia sesion nuevamente para continuar.";
                ModelState.AddModelError(string.Empty, TempData["Warning"]?.ToString() ?? string.Empty);
                return;
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                ModelState.AddModelError(string.Empty, "No cuentas con permisos para realizar esta accion.");
                return;
            }

            var apiError = await ApiHttpErrorHelper.TryExtractApiMessageAsync(response);
            ModelState.AddModelError(string.Empty, apiError ?? ApiHttpErrorHelper.BuildStatusMessage(response.StatusCode, defaultMessage));
        }

        private async Task<ApiCallResult<T>> GetFromApiAsync<T>(string endpoint, string defaultErrorMessage) where T : class
        {
            try
            {
                var response = await _apiClient.SendAsync(HttpMethod.Get, endpoint);
                if (response == null)
                {
                    return ApiCallResult<T>.Failure(null, "No fue posible conectar con el servidor. Intenta nuevamente.");
                }

                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var apiMessage = ApiHttpErrorHelper.TryExtractApiMessage(json);
                    var statusMessage = ApiHttpErrorHelper.BuildStatusMessage(response.StatusCode, defaultErrorMessage);
                    return ApiCallResult<T>.Failure(response.StatusCode, apiMessage ?? statusMessage);
                }

                if (string.IsNullOrWhiteSpace(json))
                {
                    return ApiCallResult<T>.Failure(response.StatusCode, "La API devolvio una respuesta vacia.");
                }

                var envelope = JsonSerializer.Deserialize<ApiEnvelope<T>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (envelope?.Data == null)
                {
                    return ApiCallResult<T>.Failure(response.StatusCode, envelope?.Message ?? "La respuesta no contiene datos validos.");
                }

                return ApiCallResult<T>.Success(envelope.Data, response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calling {endpoint}: {ex.Message}", ex);
                return ApiCallResult<T>.Failure(null, "Ocurrio un error inesperado al consultar la API.");
            }
        }

        private IActionResult? HandleUnauthorizedRedirect(HttpStatusCode? statusCode, string? message)
        {
            if (statusCode == HttpStatusCode.Unauthorized)
            {
                TempData["Warning"] = message ?? ApiHttpErrorHelper.BuildStatusMessage(HttpStatusCode.Unauthorized, "Sesion expirada.");
                return RedirectToAction("Login", "Account");
            }

            return null;
        }

        private sealed class ApiCallResult<T> where T : class
        {
            public bool IsSuccess { get; private init; }
            public T? Data { get; private init; }
            public HttpStatusCode? StatusCode { get; private init; }
            public string? ErrorMessage { get; private init; }

            public static ApiCallResult<T> Success(T data, HttpStatusCode? statusCode = null)
            {
                return new ApiCallResult<T>
                {
                    IsSuccess = true,
                    Data = data,
                    StatusCode = statusCode
                };
            }

            public static ApiCallResult<T> Failure(HttpStatusCode? statusCode, string errorMessage)
            {
                return new ApiCallResult<T>
                {
                    IsSuccess = false,
                    StatusCode = statusCode,
                    ErrorMessage = errorMessage
                };
            }
        }
    }
}
