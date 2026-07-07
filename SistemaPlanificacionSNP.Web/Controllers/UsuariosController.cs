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
    public class UsuariosController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(IApiClient apiClient, ILogger<UsuariosController> logger)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("/seguridad/usuarios")]
        public async Task<IActionResult> Index(string? buscar, bool? activo, int page = 1, int pageSize = 10)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            var usuariosResult = await GetUsuariosAsync();
            var authRedirect = HandleUnauthorizedRedirect(usuariosResult.StatusCode, usuariosResult.ErrorMessage);
            if (authRedirect != null)
            {
                return authRedirect;
            }

            var usuarios = usuariosResult.Data ?? new List<UsuarioApiDto>();
            if (!usuariosResult.IsSuccess)
            {
                ViewBag.Error = usuariosResult.ErrorMessage;
            }

            var query = usuarios.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var term = buscar.Trim();
                query = query.Where(u =>
                    u.NombreUsuario.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || u.Nombre.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || u.Apellido.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || u.Email.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (activo.HasValue)
            {
                query = query.Where(u => u.Activo == activo.Value);
            }

            var totalCount = query.Count();
            var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Min(page, totalPages);

            var usuariosPage = query
                .OrderBy(u => u.NombreUsuario)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UsuarioListItemViewModel
                {
                    UsuarioId = u.UsuarioId,
                    NombreUsuario = u.NombreUsuario,
                    NombreCompleto = $"{u.Nombre} {u.Apellido}".Trim(),
                    Email = u.Email,
                    Activo = u.Activo,
                    FechaCreacion = u.FechaCreacion,
                    Roles = (u.Roles ?? new List<RolApiDto>())
                        .Where(r => r != null)
                        .OrderBy(r => r.Nombre)
                        .Select(r => new RolOptionViewModel
                        {
                            RolId = r.RolId,
                            Nombre = r.Nombre
                        })
                        .ToList()
                })
                .ToList();

            var model = new UsuariosIndexViewModel
            {
                Usuarios = usuariosPage,
                Buscar = buscar,
                Activo = activo,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };

            if (TempData.TryGetValue("Success", out var success))
            {
                ViewBag.SwalSuccess = success?.ToString();
            }

            if (TempData.TryGetValue("Warning", out var warning))
            {
                ViewBag.SwalWarning = warning?.ToString();
            }

            return View(model);
        }

        [HttpGet("/seguridad/usuarios/crear")]
        public IActionResult Create()
        {
            return View(new UsuarioCreateViewModel());
        }

        [HttpPost("/seguridad/usuarios/crear")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UsuarioCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var payload = new UsuarioCreateApiDto
            {
                NombreUsuario = model.NombreUsuario,
                Email = model.Email,
                Password = model.Password,
                Nombre = model.Nombre,
                Apellido = model.Apellido
            };

            try
            {
                var response = await _apiClient.SendAsync(HttpMethod.Post, "/api/usuarios/crear", payload);
                if (response?.IsSuccessStatusCode == true)
                {
                    TempData["Success"] = "Usuario creado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }

                await AddResponseErrorToModelStateAsync(response, "No fue posible crear el usuario.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Usuarios.Create POST: {ex.Message}", ex);
                ModelState.AddModelError(string.Empty, "Ocurrio un error inesperado al crear el usuario.");
            }

            return View(model);
        }

        [HttpGet("/seguridad/usuarios/{id:int}/editar")]
        public async Task<IActionResult> Edit(int id)
        {
            var modelResult = await BuildEditModelAsync(id);
            var authRedirect = HandleUnauthorizedRedirect(modelResult.StatusCode, modelResult.ErrorMessage);
            if (authRedirect != null)
            {
                return authRedirect;
            }

            if (!modelResult.IsSuccess || modelResult.Data == null)
            {
                TempData["Warning"] = modelResult.ErrorMessage ?? "No fue posible cargar el usuario seleccionado.";
                return RedirectToAction(nameof(Index));
            }

            return View(modelResult.Data);
        }

        [HttpPost("/seguridad/usuarios/{id:int}/editar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UsuarioEditViewModel model)
        {
            if (id != model.UsuarioId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                await PopulateReadOnlyFieldsAsync(model);
                return View(model);
            }

            var payload = new UsuarioUpdateApiDto
            {
                Email = model.Email,
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                Activo = model.Activo
            };

            try
            {
                var response = await _apiClient.SendAsync(HttpMethod.Put, $"/api/usuarios/{model.UsuarioId}", payload);

                if (response?.IsSuccessStatusCode == true)
                {
                    TempData["Success"] = "Usuario actualizado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }

                var redirectResult = HandleUnauthorizedRedirect(response?.StatusCode, null);
                if (redirectResult != null)
                {
                    return redirectResult;
                }

                await AddResponseErrorToModelStateAsync(response, "No fue posible actualizar el usuario.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Usuarios.Edit POST: {ex.Message}", ex);
                ModelState.AddModelError(string.Empty, "Ocurrio un error inesperado al guardar los cambios.");
            }

            await PopulateReadOnlyFieldsAsync(model);
            return View(model);
        }

        [HttpGet("/seguridad/usuarios/{id:int}")]
        public async Task<IActionResult> Detail(int id)
        {
            var modelResult = await BuildDetailModelAsync(id);
            var authRedirect = HandleUnauthorizedRedirect(modelResult.StatusCode, modelResult.ErrorMessage);
            if (authRedirect != null)
            {
                return authRedirect;
            }

            if (!modelResult.IsSuccess || modelResult.Data == null)
            {
                TempData["Warning"] = modelResult.ErrorMessage ?? "No fue posible cargar el usuario seleccionado.";
                return RedirectToAction(nameof(Index));
            }

            return View(modelResult.Data);
        }

        [HttpGet("/seguridad/usuarios/{id:int}/roles/modal")]
        public async Task<IActionResult> AssignRolesModal(int id)
        {
            var modelResult = await BuildAssignRolesModelAsync(id);
            var authRedirect = HandleUnauthorizedRedirect(modelResult.StatusCode, modelResult.ErrorMessage);
            if (authRedirect != null)
            {
                return authRedirect;
            }

            if (!modelResult.IsSuccess || modelResult.Data == null)
            {
                return Content($"<div class='alert alert-warning mb-0'>{modelResult.ErrorMessage ?? "No fue posible cargar los roles del usuario."}</div>", "text/html");
            }

            return PartialView("_AssignRolesModalContent", modelResult.Data);
        }

        [HttpPost("/seguridad/usuarios/{id:int}/roles")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRoles(int id, RolesUsuariosEditViewModel model)
        {
            if (id != model.UsuarioId)
            {
                return BadRequest();
            }

            model.SelectedRolIds ??= new List<int>();

            var payload = new AsignarRolesApiDto
            {
                RolIds = model.SelectedRolIds.Distinct().ToList()
            };

            try
            {
                var response = await _apiClient.SendAsync(HttpMethod.Post, $"/api/usuarios/{model.UsuarioId}/asignar-roles", payload);

                if (response?.IsSuccessStatusCode == true)
                {
                    if (IsAjaxRequest())
                    {
                        return Json(new { success = true, message = "Roles asignados exitosamente." });
                    }

                    TempData["Success"] = "Roles asignados exitosamente.";
                    return RedirectToAction(nameof(Index));
                }

                if (response == null)
                {
                    ModelState.AddModelError(string.Empty, "No fue posible conectar con el servidor. Intenta nuevamente.");
                }
                else if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    TempData["Warning"] = "Tu sesion expiro. Inicia sesion nuevamente para continuar.";
                    return RedirectToAction("Login", "Account");
                }
                else if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    ModelState.AddModelError(string.Empty, "No cuentas con permisos para actualizar roles de usuario.");
                }
                else if ((int)response.StatusCode >= 500)
                {
                    ModelState.AddModelError(string.Empty, "El servidor presento un error interno. Intenta nuevamente en unos minutos.");
                }
                else
                {
                    var apiError = await ApiHttpErrorHelper.TryExtractApiMessageAsync(response);
                    ModelState.AddModelError(
                        string.Empty,
                        apiError ?? ApiHttpErrorHelper.BuildStatusMessage(response.StatusCode, "No fue posible actualizar los roles del usuario."));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Usuarios.AssignRoles POST: {ex.Message}", ex);
                ModelState.AddModelError(string.Empty, "Ocurrio un error inesperado al guardar los cambios.");
            }

            var refreshedModelResult = await BuildAssignRolesModelAsync(model.UsuarioId, model.SelectedRolIds);
            var refreshAuthRedirect = HandleUnauthorizedRedirect(refreshedModelResult.StatusCode, refreshedModelResult.ErrorMessage);
            if (refreshAuthRedirect != null)
            {
                return refreshAuthRedirect;
            }

            if (!refreshedModelResult.IsSuccess || refreshedModelResult.Data == null)
            {
                if (IsAjaxRequest())
                {
                    return StatusCode(500, "<div class='alert alert-warning mb-0'>No fue posible recargar los datos del usuario.</div>");
                }

                TempData["Warning"] = refreshedModelResult.ErrorMessage ?? "No fue posible recargar los datos del usuario.";
                return RedirectToAction(nameof(Index));
            }

            if (IsAjaxRequest())
            {
                return PartialView("_AssignRolesModalContent", refreshedModelResult.Data);
            }

            TempData["Warning"] = "No fue posible mostrar la gestion de roles en el flujo actual.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("/seguridad/usuarios/{id:int}/eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var response = await _apiClient.SendAsync(HttpMethod.Delete, $"/api/usuarios/{id}");

                if (response?.IsSuccessStatusCode == true)
                {
                    TempData["Success"] = "Usuario eliminado exitosamente.";
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
                        : BuildDetailedHttpStatusMessage(response.StatusCode, "No fue posible eliminar el usuario."));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Usuarios.Delete POST: {ex.Message}", ex);
                TempData["Warning"] = "Ocurrio un error inesperado al eliminar el usuario.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task<ApiCallResult<List<UsuarioApiDto>>> GetUsuariosAsync()
        {
            return await GetFromApiAsync<List<UsuarioApiDto>>(
                "/api/usuarios/con-roles",
                "No fue posible obtener los usuarios para administracion.");
        }

        private async Task<ApiCallResult<UsuarioApiDto>> GetUsuarioByIdAsync(int usuarioId)
        {
            return await GetFromApiAsync<UsuarioApiDto>(
                $"/api/usuarios/{usuarioId}",
                "No fue posible obtener el detalle del usuario.");
        }

        private async Task<ApiCallResult<UsuarioEditViewModel>> BuildEditModelAsync(int usuarioId)
        {
            var usuarioResult = await GetUsuarioByIdAsync(usuarioId);
            if (!usuarioResult.IsSuccess || usuarioResult.Data == null)
            {
                return ApiCallResult<UsuarioEditViewModel>.Failure(
                    usuarioResult.StatusCode,
                    usuarioResult.ErrorMessage ?? "No fue posible cargar el usuario solicitado.");
            }

            var usuario = usuarioResult.Data;
            var selectedRoles = (usuario.Roles ?? new List<RolApiDto>())
                .Where(r => r != null)
                .OrderBy(r => r.Nombre)
                .Select(r => new RolOptionViewModel
                {
                    RolId = r.RolId,
                    Nombre = r.Nombre
                })
                .ToList();

            var model = new UsuarioEditViewModel
            {
                UsuarioId = usuario.UsuarioId,
                NombreUsuario = usuario.NombreUsuario,
                Email = usuario.Email,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Activo = usuario.Activo,
                FechaCreacion = usuario.FechaCreacion,
                Roles = selectedRoles,
                PermisosPorPantalla = BuildPermissionMatrix(usuario.Roles ?? new List<RolApiDto>())
            };

            return ApiCallResult<UsuarioEditViewModel>.Success(model, usuarioResult.StatusCode);
        }

        private async Task<ApiCallResult<UsuarioDetailViewModel>> BuildDetailModelAsync(int usuarioId)
        {
            var usuarioResult = await GetUsuarioByIdAsync(usuarioId);
            if (!usuarioResult.IsSuccess || usuarioResult.Data == null)
            {
                return ApiCallResult<UsuarioDetailViewModel>.Failure(
                    usuarioResult.StatusCode,
                    usuarioResult.ErrorMessage ?? "No fue posible cargar el usuario solicitado.");
            }

            var usuario = usuarioResult.Data;
            var roles = (usuario.Roles ?? new List<RolApiDto>())
                .Where(r => r != null)
                .OrderBy(r => r.Nombre)
                .Select(r => new RolOptionViewModel
                {
                    RolId = r.RolId,
                    Nombre = r.Nombre
                })
                .ToList();

            var model = new UsuarioDetailViewModel
            {
                UsuarioId = usuario.UsuarioId,
                NombreUsuario = usuario.NombreUsuario,
                NombreCompleto = $"{usuario.Nombre} {usuario.Apellido}".Trim(),
                Email = usuario.Email,
                Activo = usuario.Activo,
                FechaCreacion = usuario.FechaCreacion,
                Roles = roles,
                PermisosPorPantalla = BuildPermissionMatrix(usuario.Roles ?? new List<RolApiDto>())
            };

            return ApiCallResult<UsuarioDetailViewModel>.Success(model, usuarioResult.StatusCode);
        }

        private async Task<ApiCallResult<RolesUsuariosEditViewModel>> BuildAssignRolesModelAsync(int usuarioId, List<int>? selectedOverride = null)
        {
            var usuarioResult = await GetUsuarioByIdAsync(usuarioId);
            if (!usuarioResult.IsSuccess || usuarioResult.Data == null)
            {
                return ApiCallResult<RolesUsuariosEditViewModel>.Failure(
                    usuarioResult.StatusCode,
                    usuarioResult.ErrorMessage ?? "No fue posible cargar el usuario solicitado.");
            }

            var usuario = usuarioResult.Data;

            var usuariosResult = await GetUsuariosAsync();
            if (!usuariosResult.IsSuccess || usuariosResult.Data == null)
            {
                return ApiCallResult<RolesUsuariosEditViewModel>.Failure(
                    usuariosResult.StatusCode,
                    usuariosResult.ErrorMessage ?? "No fue posible cargar el catalogo de usuarios/roles.");
            }

            var usuarios = usuariosResult.Data;
            var rolesDisponibles = BuildRoleOptions(usuarios);

            var roleCatalog = usuarios
                .Where(u => u != null)
                .SelectMany(u => u.Roles ?? Enumerable.Empty<RolApiDto>())
                .Where(r => r != null && r.RolId > 0)
                .GroupBy(r => r.RolId)
                .Select(g => g.First())
                .ToDictionary(r => r.RolId, r => r);

            foreach (var rol in usuario.Roles ?? Enumerable.Empty<RolApiDto>())
            {
                if (rol != null && !roleCatalog.ContainsKey(rol.RolId))
                {
                    roleCatalog[rol.RolId] = rol;
                }
            }

            if (rolesDisponibles.Count == 0 && (usuario.Roles?.Any(r => r != null) == true))
            {
                rolesDisponibles = usuario.Roles
                    .Where(r => r != null)
                    .OrderBy(r => r.Nombre)
                    .Select(r => new RolOptionViewModel
                    {
                        RolId = r.RolId,
                        Nombre = r.Nombre ?? string.Empty
                    })
                    .ToList();
            }

            var selectedRolIds = selectedOverride
                                 ?? usuario.Roles?.Where(r => r != null).Select(r => r.RolId).ToList()
                                 ?? new List<int>();

            var selectedRoles = selectedRolIds
                .Distinct()
                .Select(id => roleCatalog.TryGetValue(id, out var rol) ? rol : null)
                .Where(r => r != null)
                .Cast<RolApiDto>()
                .ToList();

            var permisosPorPantalla = BuildPermissionMatrix(selectedRoles);
            var rolesPermisosCliente = roleCatalog.Values
                .OrderBy(r => r.Nombre)
                .Select(r => new RolPermisosClienteViewModel
                {
                    RolId = r.RolId,
                    Nombre = r.Nombre,
                    Permisos = r.Permisos ?? new List<PermisoApiDto>()
                })
                .ToList();

            return ApiCallResult<RolesUsuariosEditViewModel>.Success(new RolesUsuariosEditViewModel
            {
                UsuarioId = usuario.UsuarioId,
                NombreUsuario = usuario.NombreUsuario,
                NombreCompleto = $"{usuario.Nombre} {usuario.Apellido}".Trim(),
                Email = usuario.Email,
                Activo = usuario.Activo,
                SelectedRolIds = selectedRolIds,
                RolesDisponibles = rolesDisponibles,
                PermisosPorPantalla = permisosPorPantalla,
                RolesPermisosCliente = rolesPermisosCliente
            });
        }

        private async Task PopulateReadOnlyFieldsAsync(UsuarioEditViewModel model)
        {
            var modelResult = await BuildEditModelAsync(model.UsuarioId);
            if (!modelResult.IsSuccess || modelResult.Data == null)
            {
                return;
            }

            model.NombreUsuario = modelResult.Data.NombreUsuario;
            model.FechaCreacion = modelResult.Data.FechaCreacion;
            model.Roles = modelResult.Data.Roles;
            model.PermisosPorPantalla = modelResult.Data.PermisosPorPantalla;
        }

        private static List<PantallaPermisoResumenViewModel> BuildPermissionMatrix(IEnumerable<RolApiDto> roles)
        {
            return roles
                .Where(r => r != null)
                .SelectMany(r => r.Permisos ?? new List<PermisoApiDto>())
                .Where(p => p != null)
                .GroupBy(p => p.PantallaId)
                .Select(g => new PantallaPermisoResumenViewModel
                {
                    PantallaId = g.Key,
                    PantallaNombre = g.Select(p => p.NombrePantalla).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? $"Pantalla {g.Key}",
                    Lectura = g.Any(p => p.Lectura),
                    Creacion = g.Any(p => p.Creacion),
                    Edicion = g.Any(p => p.Edicion),
                    Eliminacion = g.Any(p => p.Eliminacion)
                })
                .OrderBy(p => p.PantallaId)
                .ToList();
        }

        private static List<RolOptionViewModel> BuildRoleOptions(IEnumerable<UsuarioApiDto> usuarios)
        {
            if (usuarios == null)
            {
                return new List<RolOptionViewModel>();
            }

            return usuarios
                .Where(u => u != null)
                .SelectMany(u => u.Roles ?? Enumerable.Empty<RolApiDto>())
                .Where(r => r != null && r.RolId > 0)
                .GroupBy(r => r.RolId)
                .Select(g => g.First())
                .OrderBy(r => r.Nombre)
                .Select(r => new RolOptionViewModel
                {
                    RolId = r.RolId,
                    Nombre = r.Nombre ?? string.Empty
                })
                .ToList();
        }

        private bool IsAjaxRequest()
        {
            return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
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

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var apiBadRequest = await ApiHttpErrorHelper.TryExtractApiMessageAsync(response);
                ModelState.AddModelError(
                    string.Empty,
                    apiBadRequest ?? "Los datos enviados no son validos. Revisa los campos e intenta nuevamente.");
                return;
            }

            var apiError = await ApiHttpErrorHelper.TryExtractApiMessageAsync(response);
            var message = apiError ?? BuildDetailedHttpStatusMessage(response.StatusCode, defaultMessage);
            ModelState.AddModelError(string.Empty, message);
        }

        private static string BuildDetailedHttpStatusMessage(HttpStatusCode statusCode, string fallback)
        {
            return statusCode switch
            {
                HttpStatusCode.Conflict => "La operacion genero un conflicto con datos existentes. Verifica que usuario y correo sean unicos.",
                HttpStatusCode.BadRequest => "La solicitud contiene datos invalidos. Corrige los campos e intenta nuevamente.",
                HttpStatusCode.UnprocessableEntity => "No fue posible procesar la solicitud por reglas de negocio. Verifica la informacion enviada.",
                HttpStatusCode.TooManyRequests => "Se alcanzaron demasiados intentos en poco tiempo. Espera unos segundos e intenta nuevamente.",
                _ => ApiHttpErrorHelper.BuildStatusMessage(statusCode, fallback)
            };
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
