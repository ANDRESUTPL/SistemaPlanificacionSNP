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
    public class RolesUsuariosController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<RolesUsuariosController> _logger;

        public RolesUsuariosController(IApiClient apiClient, ILogger<RolesUsuariosController> logger)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("/seguridad/roles")]
        public async Task<IActionResult> Index(string? buscar, bool? activo, int? rolId, int page = 1, int pageSize = 10)
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
            if (usuariosResult.IsSuccess)
            {
                await EnrichMissingRolesAsync(usuarios);
            }

            if (!usuariosResult.IsSuccess)
            {
                ViewBag.Error = usuariosResult.ErrorMessage;
            }

            var rolesDisponibles = BuildRoleOptions(usuarios);

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

            if (rolId.HasValue)
            {
                query = query.Where(u => u.Roles.Any(r => r.RolId == rolId.Value));
            }

            var totalCount = query.Count();
            var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
            page = Math.Min(page, totalPages);

            var usuariosPage = query
                .OrderBy(u => u.NombreUsuario)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UsuarioRolListItemViewModel
                {
                    UsuarioId = u.UsuarioId,
                    NombreUsuario = u.NombreUsuario,
                    NombreCompleto = $"{u.Nombre} {u.Apellido}".Trim(),
                    Email = u.Email,
                    Activo = u.Activo,
                    FechaCreacion = u.FechaCreacion,     
                    Roles = (u.Roles ?? Enumerable.Empty<RolApiDto>())
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

            var model = new RolesUsuariosIndexViewModel
            {
                Usuarios = usuariosPage,
                RolesDisponibles = rolesDisponibles,
                Buscar = buscar,
                Activo = activo,
                RolId = rolId,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
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

        [HttpGet("/seguridad/roles/{id:int}/editar")]
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

        [HttpPost("/seguridad/roles/{id:int}/editar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RolesUsuariosEditViewModel model)
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
                _logger.LogError($"Error in RolesUsuarios.Edit POST: {ex.Message}", ex);
                ModelState.AddModelError(string.Empty, "Ocurrio un error inesperado al guardar los cambios.");
            }

            var refreshedModelResult = await BuildEditModelAsync(model.UsuarioId, model.SelectedRolIds);
            var refreshAuthRedirect = HandleUnauthorizedRedirect(refreshedModelResult.StatusCode, refreshedModelResult.ErrorMessage);
            if (refreshAuthRedirect != null)
            {
                return refreshAuthRedirect;
            }

            if (!refreshedModelResult.IsSuccess || refreshedModelResult.Data == null)
            {
                TempData["Warning"] = refreshedModelResult.ErrorMessage ?? "No fue posible recargar los datos del usuario.";
                return RedirectToAction(nameof(Index));
            }

            return View(refreshedModelResult.Data);
        }

        private async Task<ApiCallResult<List<UsuarioApiDto>>> GetUsuariosAsync()
        {
            return await GetFromApiAsync<List<UsuarioApiDto>>("/api/usuarios", "No fue posible obtener los usuarios para administracion de roles.");
        }

        private async Task<ApiCallResult<UsuarioApiDto>> GetUsuarioByIdAsync(int usuarioId)
        {
            return await GetFromApiAsync<UsuarioApiDto>($"/api/usuarios/{usuarioId}", "No fue posible obtener el detalle del usuario.");
        }

        private async Task EnrichMissingRolesAsync(List<UsuarioApiDto> usuarios, int maxUsersToEnrich = 60)
        {
            var usuariosSinRoles = usuarios
                .Where(u => u.Roles == null || u.Roles.Count == 0)
                .Take(maxUsersToEnrich)
                .ToList();

            foreach (var usuario in usuariosSinRoles)
            {
                var detalleResult = await GetUsuarioByIdAsync(usuario.UsuarioId);
                if (detalleResult.IsSuccess && detalleResult.Data?.Roles?.Any() == true)
                {
                    usuario.Roles = detalleResult.Data.Roles;
                }
            }
        }

		private static List<RolOptionViewModel> BuildRoleOptions(IEnumerable<UsuarioApiDto> usuarios)
		{
			// 1. Verificamos que la colección principal no sea nula para evitar excepciones de argumento
			if (usuarios == null)
				return new List<RolOptionViewModel>();

			return usuarios
				// 2. Descartamos cualquier usuario que sea nulo dentro de la lista
				.Where(u => u != null)
				// 3. Manejamos el caso donde la propiedad Roles sea nula
				.SelectMany(u => u.Roles ?? Enumerable.Empty<RolApiDto>())
				// 4. ¡Clave! Descartamos roles nulos ANTES de evaluar r.RolId
				.Where(r => r != null && r.RolId > 0)
				.GroupBy(r => r.RolId)
				.Select(g => g.First())
				.OrderBy(r => r.Nombre)
				.Select(r => new RolOptionViewModel
				{
					RolId = r.RolId,
					Nombre = r.Nombre ?? string.Empty // Protección extra por si el nombre viene nulo
				})
				.ToList();
		}

		private async Task<ApiCallResult<RolesUsuariosEditViewModel>> BuildEditModelAsync(int usuarioId, List<int>? selectedOverride = null)
		{
			var usuarioResult = await GetUsuarioByIdAsync(usuarioId);
			if (!usuarioResult.IsSuccess || usuarioResult.Data == null)
			{
				return ApiCallResult<RolesUsuariosEditViewModel>.Failure(usuarioResult.StatusCode, usuarioResult.ErrorMessage ?? "No fue posible cargar el usuario solicitado.");
			}

			var usuario = usuarioResult.Data;

			var usuariosResult = await GetUsuariosAsync();
			if (!usuariosResult.IsSuccess || usuariosResult.Data == null)
			{
				return ApiCallResult<RolesUsuariosEditViewModel>.Failure(usuariosResult.StatusCode, usuariosResult.ErrorMessage ?? "No fue posible cargar el catalogo de usuarios/roles.");
			}

			var usuarios = usuariosResult.Data;
			await EnrichMissingRolesAsync(usuarios);

			var rolesDisponibles = BuildRoleOptions(usuarios);

			// 1. SOLUCIÓN AL ERROR ACTUAL: Filtramos nulos a nivel de usuario y a nivel de rol
			var roleCatalog = usuarios
				.Where(u => u != null)
				.SelectMany(u => u.Roles ?? Enumerable.Empty<RolApiDto>())
				.Where(r => r != null && r.RolId > 0) // <-- ¡Esta es la línea que soluciona tu crash!
				.GroupBy(r => r.RolId)
				.Select(g => g.First())
				.ToDictionary(r => r.RolId, r => r);

			// 2. PREVENCIÓN: Protegemos el foreach por si usuario.Roles es nulo
			foreach (var rol in usuario.Roles ?? Enumerable.Empty<RolApiDto>())
			{
				// También evitamos que intente leer el RolId de un rol nulo
				if (rol != null && !roleCatalog.ContainsKey(rol.RolId))
				{
					roleCatalog[rol.RolId] = rol;
				}
			}

			// 3. PREVENCIÓN: Protegemos el .Any() y el posterior mapeo
			if (rolesDisponibles.Count == 0 && (usuario.Roles?.Any(r => r != null) == true))
			{
				rolesDisponibles = usuario.Roles
					.Where(r => r != null) // Filtramos nulos antes de ordenar
					.OrderBy(r => r.Nombre)
					.Select(r => new RolOptionViewModel
					{
						RolId = r.RolId,
						Nombre = r.Nombre ?? string.Empty
					})
					.ToList();
			}

			// 4. PREVENCIÓN: Protegemos el Select en caso de que usuario.Roles sea nulo
			var selectedRolIds = selectedOverride ??
								 usuario.Roles?.Where(r => r != null).Select(r => r.RolId).ToList() ??
								 new List<int>();

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

		private static List<PantallaPermisoResumenViewModel> BuildPermissionMatrix(IEnumerable<RolApiDto> roles)
        {
            return roles
                .SelectMany(r => r.Permisos ?? new List<PermisoApiDto>())
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
