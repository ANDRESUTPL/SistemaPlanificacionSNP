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
	[Route("macroplanificacion/planes")]
	public class MacroPlanificacionController : Controller
	{
		private const string ClaimLectura = "Lectura_13";
		private const string ClaimCreacion = "Creacion_13";
		private const string ClaimEdicion = "Edicion_13";
		private const string ClaimEliminacion = "Eliminacion_13";

		private readonly IApiClient _apiClient;
		private readonly ILogger<MacroPlanificacionController> _logger;

		public MacroPlanificacionController(IApiClient apiClient, ILogger<MacroPlanificacionController> logger)
		{
			_apiClient = apiClient;
			_logger = logger;
		}

		[HttpGet("")]
		public async Task<IActionResult> Index(string? buscar)
		{
			var model = new MacroPlanificacionIndexViewModel { Buscar = buscar };

			if (!HasPermission(ClaimLectura))
			{
				return RedirectToAccessDenied("No cuentas con permisos para visualizar Planes Nacionales.");
			}

			model.PuedeLeer = true;
			model.PuedeCrear = HasPermission(ClaimCreacion);
			model.PuedeEditar = HasPermission(ClaimEdicion);
			model.PuedeEliminar = HasPermission(ClaimEliminacion);

			try
			{
				// Llamadas en paralelo a Planes y Resumen
				var taskPlanes = _apiClient.SendAsync(HttpMethod.Get, "/api/planesnacionales");
				var taskResumen = _apiClient.SendAsync(HttpMethod.Get, "/api/planesnacionales/resumen");

				await Task.WhenAll(taskPlanes, taskResumen);

				var responsePlanes = await taskPlanes;
				var responseResumen = await taskResumen;

				if (responsePlanes?.IsSuccessStatusCode == true)
				{
					var json = await responsePlanes.Content.ReadAsStringAsync();
					// Como el API devuelve ApiPaginatedResponse, extraemos .Data.Data
					using var doc = JsonDocument.Parse(json);
					if (doc.RootElement.TryGetProperty("data", out var dataElement) && dataElement.TryGetProperty("data", out var itemsElement))
					{
						model.PlanesNacionales = JsonSerializer.Deserialize<List<MacroPlanNacionalApiDto>>(itemsElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
					}
				}
				else if (responsePlanes?.StatusCode == System.Net.HttpStatusCode.Unauthorized)
				{
					return RedirectToAction("Login", "Account");
				}

				if (responseResumen?.IsSuccessStatusCode == true)
				{
					var jsonResumen = await responseResumen.Content.ReadAsStringAsync();
					var envelope = JsonSerializer.Deserialize<ApiEnvelope<MacroPlanNacionalResumenApiDto>>(jsonResumen, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					model.Resumen = envelope?.Data ?? new MacroPlanNacionalResumenApiDto();
				}

				// Filtrado en memoria
				if (!string.IsNullOrWhiteSpace(buscar))
				{
					var term = buscar.Trim().ToLower();
					model.PlanesNacionales = model.PlanesNacionales.Where(p => p.Nombre.ToLower().Contains(term)).ToList();
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error en MacroPlanificacion.Index: {ex.Message}");
				ViewBag.Error = "No fue posible cargar los Planes Nacionales.";
			}

			if (TempData.TryGetValue("Success", out var success)) ViewBag.SwalSuccess = success;
			if (TempData.TryGetValue("Warning", out var warning)) ViewBag.SwalWarning = warning;

			return View(model);
		}

		[HttpGet("crear")]
		public async Task<IActionResult> CrearPlan()
		{
			if (!HasPermission(ClaimCreacion))
			{
				return RedirectToAccessDenied("No cuentas con permisos para crear planes nacionales.");
			}

			var model = new PlanNacionalCreateViewModel();
			await CargarPeriodosDisponibles(model);
			return View(model);
		}

		[HttpPost("crear")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CrearPlan(PlanNacionalCreateViewModel model)
		{
			if (!HasPermission(ClaimCreacion))
			{
				return RedirectToAccessDenied("No cuentas con permisos para crear planes nacionales.");
			}

			if (!ModelState.IsValid)
			{
				await CargarPeriodosDisponibles(model);
				return View(model);
			}

			await CargarPeriodosDisponibles(model);

			var periodoSeleccionado = model.PeriodosDisponibles
				.FirstOrDefault(p => p.PeriodoPlanificacionId == model.PeriodoPlanificacionId);

			if (periodoSeleccionado == null)
			{
				ModelState.AddModelError(nameof(model.PeriodoPlanificacionId), "El período seleccionado no es válido.");
				await CargarPeriodosDisponibles(model);
				return View(model);
			}

			model.PeriodoInicio = periodoSeleccionado.FechaInicio.Year;
			model.PeriodoFin = periodoSeleccionado.FechaFin.Year;

			if (model.PeriodoInicio > model.PeriodoFin)
			{
				ModelState.AddModelError(nameof(model.PeriodoPlanificacionId), "El período seleccionado tiene un rango de fechas inválido.");
				await CargarPeriodosDisponibles(model);
				return View(model);
			}

			try
			{
				var payload = new { model.Nombre, model.PeriodoPlanificacionId, model.PeriodoInicio, model.PeriodoFin, Estado = "Borrador" };
				var response = await _apiClient.SendAsync(HttpMethod.Post, "/api/planesnacionales/crear", payload);

				if (response?.IsSuccessStatusCode == true)
				{
					TempData["Success"] = "Plan Nacional de Desarrollo creado exitosamente.";
					return RedirectToAction(nameof(Index));
				}

				var errorMsg = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "No fue posible crear el Plan Nacional.");
				ModelState.AddModelError(string.Empty, errorMsg);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error creando PND: {ex.Message}");
				ModelState.AddModelError(string.Empty, "Error interno al procesar la solicitud.");
			}

			return View(model);
		}

		[HttpGet("{id:int}/editar")]
		public async Task<IActionResult> EditarPlan(int id)
		{
			if (!HasPermission(ClaimEdicion))
			{
				return RedirectToAccessDenied("No cuentas con permisos para editar planes nacionales.");
			}

			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Get, $"/api/planesnacionales/{id}");
				if (response?.StatusCode == System.Net.HttpStatusCode.Unauthorized)
				{
					return RedirectToAction("Login", "Account");
				}

				if (response?.StatusCode == System.Net.HttpStatusCode.NotFound)
				{
					TempData["Warning"] = "Plan Nacional no encontrado.";
					return RedirectToAction(nameof(Index));
				}

				if (response?.IsSuccessStatusCode != true)
				{
					TempData["Warning"] = "No fue posible cargar el plan para edición.";
					return RedirectToAction(nameof(Index));
				}

				var json = await response.Content.ReadAsStringAsync();
				var envelope = JsonSerializer.Deserialize<ApiEnvelope<MacroPlanNacionalApiDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
				if (envelope?.Data == null)
				{
					TempData["Warning"] = "No fue posible cargar el plan para edición.";
					return RedirectToAction(nameof(Index));
				}

				var model = new PlanNacionalEditViewModel
				{
					PlanNacionalId = envelope.Data.PlanNacionalId,
					Nombre = envelope.Data.Nombre,
					PeriodoPlanificacionId = envelope.Data.PeriodoPlanificacionId,
					PeriodoInicio = envelope.Data.PeriodoInicio,
					PeriodoFin = envelope.Data.PeriodoFin,
					Estado = envelope.Data.Estado
				};

				await CargarPeriodosDisponibles(model);
				return View(model);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error loading Plan edit form: {ex.Message}");
				TempData["Warning"] = "No fue posible cargar el plan para edición.";
				return RedirectToAction(nameof(Index));
			}
		}

		[HttpPost("{id:int}/editar")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditarPlan(int id, PlanNacionalEditViewModel model)
		{
			if (!HasPermission(ClaimEdicion))
			{
				return RedirectToAccessDenied("No cuentas con permisos para editar planes nacionales.");
			}

			if (id != model.PlanNacionalId)
			{
				return BadRequest();
			}

			if (!ModelState.IsValid)
			{
				await CargarPeriodosDisponibles(model);
				return View(model);
			}

			await CargarPeriodosDisponibles(model);
			if (!model.PeriodoPlanificacionId.HasValue)
			{
				ModelState.AddModelError(nameof(model.PeriodoPlanificacionId), "Debe seleccionar un período de planificación.");
				return View(model);
			}

			var periodoSeleccionado = model.PeriodosDisponibles
				.FirstOrDefault(p => p.PeriodoPlanificacionId == model.PeriodoPlanificacionId.Value);

			if (periodoSeleccionado == null)
			{
				ModelState.AddModelError(nameof(model.PeriodoPlanificacionId), "El período seleccionado no es válido.");
				return View(model);
			}

			model.PeriodoInicio = periodoSeleccionado.FechaInicio.Year;
			model.PeriodoFin = periodoSeleccionado.FechaFin.Year;

			if (model.PeriodoInicio > model.PeriodoFin)
			{
				ModelState.AddModelError(nameof(model.PeriodoPlanificacionId), "El período seleccionado tiene un rango de fechas inválido.");
				return View(model);
			}

			try
			{
				var payload = new
				{
					model.Nombre,
					model.PeriodoPlanificacionId,
					model.PeriodoInicio,
					model.PeriodoFin,
					model.Estado
				};
				var response = await _apiClient.SendAsync(HttpMethod.Put, $"/api/planesnacionales/{id}", payload);

				if (response?.IsSuccessStatusCode == true)
				{
					TempData["Success"] = "Plan Nacional actualizado exitosamente.";
					return RedirectToAction(nameof(Index));
				}

				var errorMsg = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "No fue posible actualizar el Plan Nacional.");
				ModelState.AddModelError(string.Empty, errorMsg);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error updating PND: {ex.Message}");
				ModelState.AddModelError(string.Empty, "Error interno al procesar la solicitud.");
			}

			return View(model);
		}

		[HttpPost("{id:int}/eliminar")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EliminarPlan(int id)
		{
			if (!HasPermission(ClaimEliminacion))
			{
				return RedirectToAccessDenied("No cuentas con permisos para eliminar planes nacionales.");
			}

			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Delete, $"/api/planesnacionales/{id}");
				if (response?.IsSuccessStatusCode == true)
				{
					TempData["Success"] = "Plan Nacional eliminado exitosamente.";
				}
				else
				{
					var errorMsg = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "No fue posible eliminar el Plan Nacional.");
					TempData["Warning"] = errorMsg;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error deleting PND: {ex.Message}");
				TempData["Warning"] = "Error interno al eliminar el Plan Nacional.";
			}

			return RedirectToAction(nameof(Index));
		}

		private async Task CargarPeriodosDisponibles(PlanNacionalCreateViewModel model)
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

		private async Task CargarPeriodosDisponibles(PlanNacionalEditViewModel model)
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

		[HttpGet("{id:int}")]
		public async Task<IActionResult> Detalle(int id)
		{
			if (!HasPermission(ClaimLectura))
			{
				return RedirectToAccessDenied("No cuentas con permisos para visualizar el detalle del plan nacional.");
			}

			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Get, $"/api/planesnacionales/{id}/jerarquia");
				if (response?.StatusCode == System.Net.HttpStatusCode.NotFound)
				{
					TempData["Warning"] = "Plan Nacional no encontrado.";
					return RedirectToAction(nameof(Index));
				}

				if (response?.IsSuccessStatusCode == true)
				{
					var json = await response.Content.ReadAsStringAsync();
					var envelope = JsonSerializer.Deserialize<ApiEnvelope<MacroPlanNacionalDetalleApiDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

					if (envelope?.Data != null)
					{
						ViewBag.PuedeCrear = HasPermission(ClaimCreacion);
						ViewBag.PuedeEditar = HasPermission(ClaimEdicion);
						ViewBag.PuedeEliminar = HasPermission(ClaimEliminacion);
						if (TempData.TryGetValue("Success", out var success)) ViewBag.SwalSuccess = success;
						if (TempData.TryGetValue("Warning", out var warning)) ViewBag.SwalWarning = warning;
						return View(envelope.Data);
					}
				}

				TempData["Warning"] = "No fue posible cargar el detalle del plan.";
				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error cargando detalle PND: {ex.Message}");
				TempData["Warning"] = "Error interno del servidor.";
				return RedirectToAction(nameof(Index));
			}
		}

		[HttpPost("objetivos/crear")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CrearObjetivo(ObjetivoMacroCreateViewModel model)
		{
			if (!HasPermission(ClaimCreacion))
			{
				return RedirectToAccessDenied("No cuentas con permisos para crear objetivos estratégicos nacionales.");
			}

			if (!ModelState.IsValid)
			{
				TempData["Warning"] = "Datos del objetivo incompletos o inválidos.";
				return RedirectToAction(nameof(Detalle), new { id = model.PlanNacionalId });
			}

			try
			{
				var payload = new { model.PlanNacionalId, model.Codigo, model.Nombre, model.Descripcion };
				var response = await _apiClient.SendAsync(HttpMethod.Post, "/api/objetivosestrategicos/crear", payload);

				if (response?.IsSuccessStatusCode == true)
				{
					TempData["Success"] = "Objetivo Macro agregado exitosamente.";
				}
				else
				{
					var errorMsg = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "No se pudo agregar el objetivo.");
					TempData["Warning"] = errorMsg;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error agregando objetivo macro: {ex.Message}");
				TempData["Warning"] = "Error interno al guardar el objetivo.";
			}

			return RedirectToAction(nameof(Detalle), new { id = model.PlanNacionalId });
		}

		[HttpGet("{planId:int}/objetivos/{objetivoId:int}/editar")]
		public async Task<IActionResult> EditarObjetivo(int planId, int objetivoId)
		{
			if (!HasPermission(ClaimEdicion))
			{
				return RedirectToAccessDenied("No cuentas con permisos para editar objetivos estratégicos nacionales.");
			}

			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Get, $"/api/objetivosestrategicos/{objetivoId}");
				if (response?.StatusCode == System.Net.HttpStatusCode.NotFound)
				{
					TempData["Warning"] = "Objetivo estratégico no encontrado.";
					return RedirectToAction(nameof(Detalle), new { id = planId });
				}

				if (response?.IsSuccessStatusCode != true)
				{
					TempData["Warning"] = "No fue posible cargar el objetivo para edición.";
					return RedirectToAction(nameof(Detalle), new { id = planId });
				}

				var json = await response.Content.ReadAsStringAsync();
				var envelope = JsonSerializer.Deserialize<ApiEnvelope<MacroObjetivoEstrategicoApiDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
				if (envelope?.Data == null)
				{
					TempData["Warning"] = "No fue posible cargar el objetivo para edición.";
					return RedirectToAction(nameof(Detalle), new { id = planId });
				}

				if (envelope.Data.PlanNacionalId != planId)
				{
					TempData["Warning"] = "El objetivo no pertenece al plan nacional indicado.";
					return RedirectToAction(nameof(Detalle), new { id = planId });
				}

				var model = new ObjetivoMacroEditViewModel
				{
					ObjetivoEstrategicoId = envelope.Data.ObjetivoEstrategicoId,
					PlanNacionalId = envelope.Data.PlanNacionalId,
					Codigo = envelope.Data.Codigo,
					Nombre = envelope.Data.Nombre,
					Descripcion = envelope.Data.Descripcion
				};

				return View(model);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error cargando edición de objetivo macro: {ex.Message}");
				TempData["Warning"] = "No fue posible cargar el objetivo para edición.";
				return RedirectToAction(nameof(Detalle), new { id = planId });
			}
		}

		[HttpPost("{planId:int}/objetivos/{objetivoId:int}/editar")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditarObjetivo(int planId, int objetivoId, ObjetivoMacroEditViewModel model)
		{
			if (!HasPermission(ClaimEdicion))
			{
				return RedirectToAccessDenied("No cuentas con permisos para editar objetivos estratégicos nacionales.");
			}

			if (planId != model.PlanNacionalId || objetivoId != model.ObjetivoEstrategicoId)
			{
				return BadRequest();
			}

			if (!ModelState.IsValid)
			{
				return View(model);
			}

			try
			{
				var payload = new { model.Codigo, model.Nombre, model.Descripcion };
				var response = await _apiClient.SendAsync(HttpMethod.Put, $"/api/objetivosestrategicos/{objetivoId}", payload);

				if (response?.IsSuccessStatusCode == true)
				{
					TempData["Success"] = "Objetivo estratégico actualizado exitosamente.";
					return RedirectToAction(nameof(Detalle), new { id = planId });
				}

				var errorMsg = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "No fue posible actualizar el objetivo estratégico.");
				ModelState.AddModelError(string.Empty, errorMsg);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error actualizando objetivo macro: {ex.Message}");
				ModelState.AddModelError(string.Empty, "Error interno al procesar la solicitud.");
			}

			return View(model);
		}

		[HttpPost("{planId:int}/objetivos/{objetivoId:int}/eliminar")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EliminarObjetivo(int planId, int objetivoId)
		{
			if (!HasPermission(ClaimEliminacion))
			{
				return RedirectToAccessDenied("No cuentas con permisos para eliminar objetivos estratégicos nacionales.");
			}

			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Delete, $"/api/objetivosestrategicos/{objetivoId}");
				if (response?.IsSuccessStatusCode == true)
				{
					TempData["Success"] = "Objetivo estratégico eliminado exitosamente.";
				}
				else
				{
					var errorMsg = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "No fue posible eliminar el objetivo estratégico.");
					TempData["Warning"] = errorMsg;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error eliminando objetivo macro: {ex.Message}");
				TempData["Warning"] = "Error interno al eliminar el objetivo estratégico.";
			}

			return RedirectToAction(nameof(Detalle), new { id = planId });
		}

        [HttpGet("exportar")]
        public async Task<IActionResult> ExportarExcel(string? buscar)
        {
            try
            {
                // Obtenemos los datos desde la API
                var response = await _apiClient.SendAsync(HttpMethod.Get, "/api/planesnacionales");
                var planes = new List<MacroPlanNacionalApiDto>();

                if (response?.IsSuccessStatusCode == true)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("data", out var dataElement) &&
                        dataElement.TryGetProperty("data", out var itemsElement))
                    {
                        planes = JsonSerializer.Deserialize<List<MacroPlanNacionalApiDto>>(
                            itemsElement.GetRawText(),
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
                    }
                }

                // Aplicamos el mismo filtro en memoria que existe en el Index
                if (!string.IsNullOrWhiteSpace(buscar))
                {
                    var term = buscar.Trim().ToLower();
                    planes = planes.Where(p => p.Nombre.ToLower().Contains(term)).ToList();
                }

                // Crear el archivo Excel
                using var workbook = new XLWorkbook();
                var worksheet = workbook.Worksheets.Add("PlanesNacionales");

                // Cabeceras
                var currentRow = 1;
                worksheet.Cell(currentRow, 1).Value = "ID";
                worksheet.Cell(currentRow, 2).Value = "Nombre del Plan";
                worksheet.Cell(currentRow, 3).Value = "Periodo Inicio";
                worksheet.Cell(currentRow, 4).Value = "Periodo Fin";
                worksheet.Cell(currentRow, 5).Value = "Estado";

                // Formato de la cabecera
                var headerRange = worksheet.Range(1, 1, 1, 5);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                // Filas de datos
                foreach (var plan in planes)
                {
                    currentRow++;
                    worksheet.Cell(currentRow, 1).Value = $"PND-{plan.PlanNacionalId}";
                    worksheet.Cell(currentRow, 2).Value = plan.Nombre;
                    worksheet.Cell(currentRow, 3).Value = plan.PeriodoInicio;
                    worksheet.Cell(currentRow, 4).Value = plan.PeriodoFin;
                    worksheet.Cell(currentRow, 5).Value = plan.Estado;
                }

                // Ajustar el ancho de las columnas automáticamente
                worksheet.Columns().AdjustToContents();

                // Convertir a MemoryStream para retornar el archivo
                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                var content = stream.ToArray();

                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                string fileName = $"PlanesNacionales_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                return File(content, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al exportar a Excel en MacroPlanificacion: {ex.Message}");
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

			return User.IsInRole("Administrador");
		}
    }
}