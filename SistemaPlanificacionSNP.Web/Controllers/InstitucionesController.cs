using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPlanificacionSNP.Web.Common;
using SistemaPlanificacionSNP.Web.Models;
using SistemaPlanificacionSNP.Web.Services;
using System.Text.Json;
using ClosedXML.Excel;
using System.IO;

namespace SistemaPlanificacionSNP.Web.Controllers
{
	[Authorize]
	[Route("parametrizacion/instituciones")]
	public class InstitucionesController : Controller
	{
		private const string ClaimLectura = "Lectura_11";
		private const string ClaimCreacion = "Creacion_11";
		private const string ClaimEdicion = "Edicion_11";
		private const string ClaimEliminacion = "Eliminacion_11";

		private readonly IApiClient _apiClient;
		private readonly ILogger<InstitucionesController> _logger;

		public InstitucionesController(IApiClient apiClient, ILogger<InstitucionesController> logger)
		{
			_apiClient = apiClient;
			_logger = logger;
		}

		[HttpGet("")]
		public async Task<IActionResult> Index(string? buscar)
		{
			var model = new InstitucionesIndexViewModel { Buscar = buscar };

			var relevantClaims = User.Claims
				.Where(c => c.Type.StartsWith("Lectura_", StringComparison.OrdinalIgnoreCase)
					|| c.Type.StartsWith("Creacion_", StringComparison.OrdinalIgnoreCase)
					|| c.Type.StartsWith("Edicion_", StringComparison.OrdinalIgnoreCase)
					|| c.Type.StartsWith("Eliminacion_", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(c.Type, "role", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(c.Type, "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", StringComparison.OrdinalIgnoreCase))
				.Select(c => $"{c.Type}={c.Value}")
				.ToList();

			_logger.LogInformation("Instituciones.Index claims for user {User}: {Claims}", User.Identity?.Name, string.Join(", ", relevantClaims));

			if (!HasPermission(ClaimLectura))
			{
				_logger.LogWarning("User {User} denied by missing claim {Claim}", User.Identity?.Name, ClaimLectura);
				return RedirectToAccessDenied("No cuentas con permisos para visualizar Entidades Públicas.");
			}

			model.PuedeLeer = HasPermission(ClaimLectura);
			model.PuedeCrear = HasPermission(ClaimCreacion);
			model.PuedeEditar = HasPermission(ClaimEdicion);
			model.PuedeEliminar = HasPermission(ClaimEliminacion);

			try
			{
				// Consumimos ambos endpoints en paralelo para mayor velocidad
				var taskEntidades = _apiClient.SendAsync(HttpMethod.Get, "/api/instituciones/entidades");
				var taskPeriodos = _apiClient.SendAsync(HttpMethod.Get, "/api/instituciones/periodos");

				await Task.WhenAll(taskEntidades, taskPeriodos);

				var responseEntidades = await taskEntidades;
				var responsePeriodos = await taskPeriodos;

				// Procesar Entidades
				if (responseEntidades != null && responseEntidades.IsSuccessStatusCode)
				{
					var json = await responseEntidades.Content.ReadAsStringAsync();
					var envelope = JsonSerializer.Deserialize<ApiEnvelope<List<EntidadPublicaApiDto>>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					model.Entidades = envelope?.Data ?? new List<EntidadPublicaApiDto>();
				}
				else if (responseEntidades?.StatusCode == System.Net.HttpStatusCode.Unauthorized)
				{
					return RedirectToAction("Login", "Account");
				}

				// Procesar Periodos
				if (responsePeriodos != null && responsePeriodos.IsSuccessStatusCode)
				{
					var json = await responsePeriodos.Content.ReadAsStringAsync();
					var envelope = JsonSerializer.Deserialize<ApiEnvelope<List<PeriodoPlanificacionApiDto>>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					model.Periodos = envelope?.Data ?? new List<PeriodoPlanificacionApiDto>();
				}

				// Filtro local si existe búsqueda
				if (!string.IsNullOrWhiteSpace(buscar))
				{
					var term = buscar.Trim().ToLower();
					model.Entidades = model.Entidades.Where(e => e.Nombre.ToLower().Contains(term) || e.Sigla.ToLower().Contains(term)).ToList();
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error in Instituciones.Index: {ex.Message}");
				ViewBag.Error = "Ocurrió un error inesperado al cargar la información.";
			}

			if (TempData.TryGetValue("Success", out var success)) ViewBag.SwalSuccess = success;
			if (TempData.TryGetValue("Warning", out var warning)) ViewBag.SwalWarning = warning;

			return View(model);
		}

		[HttpGet("crear-entidad")]
		public async Task<IActionResult> CrearEntidad()
		{
			if (!HasPermission(ClaimCreacion))
			{
				return RedirectToAccessDenied("No cuentas con permisos para crear entidades.");
			}

			var model = new EntidadPublicaCreateViewModel();
			await CargarPeriodosDisponibles(model);
			return View(model);
		}

		[HttpPost("crear-entidad")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CrearEntidad(EntidadPublicaCreateViewModel model)
		{
			if (!HasPermission(ClaimCreacion))
			{
				return RedirectToAccessDenied("No cuentas con permisos para crear entidades.");
			}

			if (!ModelState.IsValid)
			{
				await CargarPeriodosDisponibles(model);
				return View(model);
			}

			try
			{
				var payload = new { model.Codigo, model.Nombre, model.Sigla, model.Tipo, model.NivelGobierno, model.Mision, model.PeriodoPlanificacionId };
				var response = await _apiClient.SendAsync(HttpMethod.Post, "/api/instituciones/entidades", payload);

				if (response != null && response.IsSuccessStatusCode)
				{
					TempData["Success"] = "Entidad Pública registrada exitosamente.";
					return RedirectToAction(nameof(Index));
				}

				var errorMsg = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "No fue posible registrar la entidad. Verifica los datos.");
				ModelState.AddModelError(string.Empty, errorMsg);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error creating Entidad: {ex.Message}");
				ModelState.AddModelError(string.Empty, "Error interno al procesar la solicitud.");
			}

			await CargarPeriodosDisponibles(model);
			return View(model);
		}

		[HttpPost("crear-periodo")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CrearPeriodo(PeriodoPlanificacionCreateViewModel model)
		{
			if (!HasPermission(ClaimCreacion))
			{
				return RedirectToAccessDenied("No cuentas con permisos para crear períodos.");
			}

			if (!ModelState.IsValid)
			{
				TempData["Warning"] = "Datos del período incompletos o inválidos.";
				return RedirectToAction(nameof(Index));
			}

			if (model.FechaInicio > model.FechaFin)
			{
				TempData["Warning"] = "La fecha de inicio no puede ser posterior a la fecha de fin.";
				return RedirectToAction(nameof(Index));
			}

			try
			{
				var payload = new { model.Codigo, model.Nombre, model.FechaInicio, model.FechaFin, Activo = true };
				var response = await _apiClient.SendAsync(HttpMethod.Post, "/api/instituciones/periodos", payload);

				if (response != null && response.IsSuccessStatusCode)
				{
					TempData["Success"] = "Período de Planificación creado exitosamente.";
				}
				else
				{
					var errorMsg = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "No se pudo crear el período.");
					TempData["Warning"] = errorMsg;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error creating Periodo: {ex.Message}");
				TempData["Warning"] = "Error interno al guardar el período.";
			}

			return RedirectToAction(nameof(Index));
		}

		[HttpGet("editar-entidad/{id:int}")]
		public async Task<IActionResult> EditarEntidad(int id)
		{
			if (!HasPermission(ClaimEdicion))
			{
				return RedirectToAccessDenied("No cuentas con permisos para editar entidades.");
			}

			var model = new EntidadPublicaEditViewModel { EntidadPublicaId = id };

			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Get, $"/api/instituciones/entidades/{id}");
				if (response?.IsSuccessStatusCode != true)
				{
					TempData["Warning"] = "Entidad no encontrada.";
					return RedirectToAction(nameof(Index));
				}

				var json = await response.Content.ReadAsStringAsync();
				var envelope = JsonSerializer.Deserialize<ApiEnvelope<EntidadPublicaApiDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
				if (envelope?.Data == null)
				{
					TempData["Warning"] = "Entidad no encontrada.";
					return RedirectToAction(nameof(Index));
				}

				model.Codigo = envelope.Data.Codigo;
				model.Nombre = envelope.Data.Nombre;
				model.Sigla = envelope.Data.Sigla;
				model.Tipo = envelope.Data.Tipo;
				model.NivelGobierno = envelope.Data.NivelGobierno;
				model.Mision = envelope.Data.Mision;
				model.PeriodoPlanificacionId = envelope.Data.PeriodoPlanificacionId;
				model.Activa = envelope.Data.Activa;
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error loading Entidad edit form: {ex.Message}");
				TempData["Warning"] = "No se pudo cargar la entidad para edición.";
				return RedirectToAction(nameof(Index));
			}

			await CargarPeriodosDisponibles(model);
			return View(model);
		}

		[HttpPost("editar-entidad/{id:int}")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditarEntidad(int id, EntidadPublicaEditViewModel model)
		{
			if (!HasPermission(ClaimEdicion))
			{
				return RedirectToAccessDenied("No cuentas con permisos para editar entidades.");
			}

			if (!ModelState.IsValid)
			{
				await CargarPeriodosDisponibles(model);
				return View(model);
			}

			try
			{
				var payload = new { model.Codigo, model.Nombre, model.Sigla, model.Tipo, model.NivelGobierno, model.Mision, model.PeriodoPlanificacionId };
				var response = await _apiClient.SendAsync(HttpMethod.Put, $"/api/instituciones/entidades/{id}", payload);

				if (response?.IsSuccessStatusCode == true)
				{
					TempData["Success"] = "Entidad actualizada exitosamente.";
					return RedirectToAction(nameof(Index));
				}

				var errorMsg = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "No fue posible actualizar la entidad.");
				ModelState.AddModelError(string.Empty, errorMsg);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error updating Entidad: {ex.Message}");
				ModelState.AddModelError(string.Empty, "Error interno al actualizar la entidad.");
			}

			await CargarPeriodosDisponibles(model);
			return View(model);
		}

		[HttpPost("inactivar-entidad/{id:int}")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> InactivarEntidad(int id)
		{
			if (!HasPermission(ClaimEliminacion))
			{
				return RedirectToAccessDenied("No cuentas con permisos para inactivar entidades.");
			}

			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Delete, $"/api/instituciones/entidades/{id}");
				if (response?.IsSuccessStatusCode == true)
				{
					TempData["Success"] = "Entidad inactivada exitosamente.";
				}
				else
				{
					var errorMsg = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "No se pudo inactivar la entidad.");
					TempData["Warning"] = errorMsg;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error deactivating Entidad: {ex.Message}");
				TempData["Warning"] = "Error interno al inactivar la entidad.";
			}

			return RedirectToAction(nameof(Index));
		}

		[HttpPost("editar-periodo/{id:int}")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditarPeriodo(int id, PeriodoPlanificacionCreateViewModel model)
		{
			if (!HasPermission(ClaimEdicion))
			{
				return RedirectToAccessDenied("No cuentas con permisos para editar períodos.");
			}

			if (!ModelState.IsValid)
			{
				TempData["Warning"] = "Datos del período incompletos o inválidos.";
				return RedirectToAction(nameof(Index));
			}

			if (model.FechaInicio >= model.FechaFin)
			{
				TempData["Warning"] = "La fecha de inicio debe ser menor a la fecha de fin.";
				return RedirectToAction(nameof(Index));
			}

			try
			{
				var payload = new { model.Codigo, model.Nombre, model.FechaInicio, model.FechaFin, model.Activo };
				var response = await _apiClient.SendAsync(HttpMethod.Put, $"/api/instituciones/periodos/{id}", payload);

				if (response?.IsSuccessStatusCode == true)
				{
					TempData["Success"] = "Período actualizado exitosamente.";
				}
				else
				{
					var errorMsg = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "No se pudo actualizar el período.");
					TempData["Warning"] = errorMsg;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error updating Periodo: {ex.Message}");
				TempData["Warning"] = "Error interno al actualizar el período.";
			}

			return RedirectToAction(nameof(Index));
		}

		[HttpPost("inactivar-periodo/{id:int}")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> InactivarPeriodo(int id)
		{
			if (!HasPermission(ClaimEliminacion))
			{
				return RedirectToAccessDenied("No cuentas con permisos para inactivar períodos.");
			}

			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Delete, $"/api/instituciones/periodos/{id}");
				if (response?.IsSuccessStatusCode == true)
				{
					TempData["Success"] = "Período inactivado exitosamente.";
				}
				else
				{
					var errorMsg = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "No se pudo inactivar el período.");
					TempData["Warning"] = errorMsg;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error deactivating Periodo: {ex.Message}");
				TempData["Warning"] = "Error interno al inactivar el período.";
			}

			return RedirectToAction(nameof(Index));
		}

		private async Task CargarPeriodosDisponibles(EntidadPublicaCreateViewModel model)
		{
			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Get, "/api/instituciones/periodos");
				if (response?.IsSuccessStatusCode == true)
				{
					var json = await response.Content.ReadAsStringAsync();
					var envelope = JsonSerializer.Deserialize<ApiEnvelope<List<PeriodoPlanificacionApiDto>>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					model.PeriodosDisponibles = (envelope?.Data ?? new List<PeriodoPlanificacionApiDto>())
						.Where(p => p.Activo)
						.OrderByDescending(p => p.FechaInicio)
						.ToList();
				}
			}
			catch
			{
				model.PeriodosDisponibles = new List<PeriodoPlanificacionApiDto>();
			}
		}

        [HttpGet("exportar")]
        public async Task<IActionResult> ExportarExcel(string? buscar)
        {
			if (!HasPermission(ClaimLectura))
			{
				return RedirectToAccessDenied("No cuentas con permisos para exportar información de Entidades Públicas.");
			}

            try
            {
                // 1. Obtener datos de ambas APIs en paralelo
                var taskEntidades = _apiClient.SendAsync(HttpMethod.Get, "/api/instituciones/entidades");
                var taskPeriodos = _apiClient.SendAsync(HttpMethod.Get, "/api/instituciones/periodos");

                await Task.WhenAll(taskEntidades, taskPeriodos);

                var entidades = new List<EntidadPublicaApiDto>();
                var periodos = new List<PeriodoPlanificacionApiDto>();

                var responseEntidades = await taskEntidades;
                var responsePeriodos = await taskPeriodos;

                // 2. Procesar y filtrar Entidades
                if (responseEntidades?.IsSuccessStatusCode == true)
                {
                    var json = await responseEntidades.Content.ReadAsStringAsync();
                    var envelope = JsonSerializer.Deserialize<ApiEnvelope<List<EntidadPublicaApiDto>>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    entidades = envelope?.Data ?? new List<EntidadPublicaApiDto>();
                }

                if (!string.IsNullOrWhiteSpace(buscar))
                {
                    var term = buscar.Trim().ToLower();
                    entidades = entidades.Where(e => e.Nombre.ToLower().Contains(term) || e.Sigla.ToLower().Contains(term)).ToList();
                }

                // 3. Procesar Periodos
                if (responsePeriodos?.IsSuccessStatusCode == true)
                {
                    var json = await responsePeriodos.Content.ReadAsStringAsync();
                    var envelope = JsonSerializer.Deserialize<ApiEnvelope<List<PeriodoPlanificacionApiDto>>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    periodos = envelope?.Data ?? new List<PeriodoPlanificacionApiDto>();
                }

                // 4. Crear el archivo Excel con 2 Hojas (Worksheets)
                using var workbook = new XLWorkbook();

                // ---- HOJA 1: ENTIDADES PÚBLICAS ----
                var wsEntidades = workbook.Worksheets.Add("Entidades Públicas");
                wsEntidades.Cell(1, 1).Value = "Sigla";
                wsEntidades.Cell(1, 2).Value = "Nombre de la Entidad";
                wsEntidades.Cell(1, 3).Value = "Tipo";
                wsEntidades.Cell(1, 4).Value = "Nivel de Gobierno";
                wsEntidades.Cell(1, 5).Value = "Estado";

                var headerEntidades = wsEntidades.Range(1, 1, 1, 5);
                headerEntidades.Style.Font.Bold = true;
                headerEntidades.Style.Fill.BackgroundColor = XLColor.LightGray;

                int rowEnt = 2;
                foreach (var ent in entidades)
                {
                    wsEntidades.Cell(rowEnt, 1).Value = ent.Sigla;
                    wsEntidades.Cell(rowEnt, 2).Value = ent.Nombre;
                    wsEntidades.Cell(rowEnt, 3).Value = ent.Tipo;
                    wsEntidades.Cell(rowEnt, 4).Value = ent.NivelGobierno;
                    wsEntidades.Cell(rowEnt, 5).Value = ent.Activa ? "Activa" : "Inactiva";
                    rowEnt++;
                }
                wsEntidades.Columns().AdjustToContents();

                // ---- HOJA 2: PERÍODOS DE PLANIFICACIÓN ----
                var wsPeriodos = workbook.Worksheets.Add("Períodos Planificación");
                wsPeriodos.Cell(1, 1).Value = "Código";
                wsPeriodos.Cell(1, 2).Value = "Nombre del Período";
                wsPeriodos.Cell(1, 3).Value = "Fecha Inicio";
                wsPeriodos.Cell(1, 4).Value = "Fecha Fin";
                wsPeriodos.Cell(1, 5).Value = "Estado";

                var headerPeriodos = wsPeriodos.Range(1, 1, 1, 5);
                headerPeriodos.Style.Font.Bold = true;
                headerPeriodos.Style.Fill.BackgroundColor = XLColor.LightGray;

                int rowPer = 2;
                foreach (var per in periodos)
                {
                    wsPeriodos.Cell(rowPer, 1).Value = per.Codigo;
                    wsPeriodos.Cell(rowPer, 2).Value = per.Nombre;
                    wsPeriodos.Cell(rowPer, 3).Value = per.FechaInicio.ToString("dd/MM/yyyy");
                    wsPeriodos.Cell(rowPer, 4).Value = per.FechaFin.ToString("dd/MM/yyyy");
                    wsPeriodos.Cell(rowPer, 5).Value = per.Activo ? "Vigente" : "Histórico";
                    rowPer++;
                }
                wsPeriodos.Columns().AdjustToContents();

                // 5. Convertir a MemoryStream y descargar
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var content = stream.ToArray();

                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                string fileName = $"Instituciones_y_Periodos_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(content, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al exportar a Excel en Instituciones: {ex.Message}");
                TempData["Warning"] = "Ocurrió un error al intentar generar el archivo Excel.";
                return RedirectToAction(nameof(Index), new { buscar });
            }
        }

		private IActionResult RedirectToAccessDenied(string message)
		{
			TempData["Warning"] = message;
			return RedirectToAction("AccessDenied", "Account");
		}

		private bool HasPermission(string claimType)
		{
			var hasGranularClaim = User.Claims.Any(c =>
				string.Equals(c.Type, claimType, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(c.Value, "true", StringComparison.OrdinalIgnoreCase));

			if (hasGranularClaim)
			{
				return true;
			}

			var isAdministrador = User.IsInRole("Administrador") || User.Claims.Any(c =>
				(string.Equals(c.Type, "role", StringComparison.OrdinalIgnoreCase)
				 || string.Equals(c.Type, "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", StringComparison.OrdinalIgnoreCase))
				&& string.Equals(c.Value, "Administrador", StringComparison.OrdinalIgnoreCase));

			if (isAdministrador)
			{
				_logger.LogWarning("User {User} granted permission {Claim} by Administrador fallback because granular claim was not present.", User.Identity?.Name, claimType);
			}

			return isAdministrador;
		}
    }
}