using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPlanificacionSNP.Web.Common;
using SistemaPlanificacionSNP.Web.Models;
using SistemaPlanificacionSNP.Web.Services;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SistemaPlanificacionSNP.Web.Controllers
{
	[Authorize]
	[Route("planificacion/institucional")]
	public class PlanificacionInstitucionalController : Controller
	{
		private readonly IApiClient _apiClient;
		private readonly ILogger<PlanificacionInstitucionalController> _logger;

		public PlanificacionInstitucionalController(IApiClient apiClient, ILogger<PlanificacionInstitucionalController> logger)
		{
			_apiClient = apiClient;
			_logger = logger;
		}

		[HttpGet("")]
		public async Task<IActionResult> Index(string? buscar, int page = 1)
		{
			var model = new PlanificacionIndexViewModel { Buscar = buscar, Page = page };

			try
			{
				var queryParams = $"?pageNumber={page}&pageSize=10";
				if (!string.IsNullOrWhiteSpace(buscar)) queryParams += $"&entidad={buscar}";

				var taskPlanes = _apiClient.SendAsync(HttpMethod.Get, $"/api/planesestrategicos{queryParams}");
				var taskDashboard = _apiClient.SendAsync(HttpMethod.Get, "/api/planesestrategicos/dashboard");

				await Task.WhenAll(taskPlanes, taskDashboard);

				var responsePlanes = await taskPlanes;
				if (responsePlanes?.IsSuccessStatusCode == true)
				{
					var json = await responsePlanes.Content.ReadAsStringAsync();
					using var doc = JsonDocument.Parse(json);

					if (doc.RootElement.TryGetProperty("data", out var dataElement))
					{
						if (dataElement.TryGetProperty("data", out var itemsElement))
						{
							model.Planes = JsonSerializer.Deserialize<List<PlanesEstrategicoApiDto>>(itemsElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
						}
						if (dataElement.TryGetProperty("totalPages", out var pagesElement))
						{
							model.TotalPages = pagesElement.GetInt32();
						}
					}
				}

				var responseDashboard = await taskDashboard;
				if (responseDashboard?.IsSuccessStatusCode == true)
				{
					var jsonDash = await responseDashboard.Content.ReadAsStringAsync();
					var envelope = JsonSerializer.Deserialize<ApiEnvelope<PlanificacionDashboardApiDto>>(jsonDash, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					model.Dashboard = envelope?.Data ?? new PlanificacionDashboardApiDto();
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error en PlanificacionInstitucional.Index: {ex.Message}");
				ViewBag.Error = "No fue posible cargar la información de planificación.";
			}

			if (TempData.TryGetValue("Success", out var success)) ViewBag.SwalSuccess = success;
			if (TempData.TryGetValue("Warning", out var warning)) ViewBag.SwalWarning = warning;

			HttpContext.CargarPermisos(model, "/planificacion/institucional");

			return View(model);
		}

		[HttpGet("crear")]
		public async Task<IActionResult> CrearPlan()
		{
			var model = new PlanEstrategicoCreateViewModel();
			await CargarEntidadesDisponibles(model);
			await CargarPeriodosDisponibles(model);
			await CargarPlanesNacionalesDisponibles(model);
			return View(model);
		}

		[HttpPost("{id:int}/eliminar")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EliminarPlan(int id)
		{
			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Delete, $"/api/planesestrategicos/{id}");
				if (response?.IsSuccessStatusCode == true)
				{
					TempData["Success"] = "Plan Estratégico Institucional eliminado exitosamente.";
				}
				else
				{
					TempData["Warning"] = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "No se puede eliminar el plan porque tiene proyectos asociados.");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error eliminando PEI: {ex.Message}");
				TempData["Warning"] = "Error interno al eliminar el plan.";
			}

			return RedirectToAction(nameof(Index));
		}

		[HttpPost("crear")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CrearPlan(PlanEstrategicoCreateViewModel model)
		{
			if (!ModelState.IsValid)
			{
				await CargarEntidadesDisponibles(model);
				await CargarPeriodosDisponibles(model);
				await CargarPlanesNacionalesDisponibles(model);
				return View(model);
			}

			await CargarPeriodosDisponibles(model);
			await CargarPlanesNacionalesDisponibles(model);
			if (!SincronizarPeriodoConPlanNacional(model))
			{
				await CargarEntidadesDisponibles(model);
				return View(model);
			}

			await CargarEntidadesDisponibles(model);
			if (!model.EntidadPublicaId.HasValue)
			{
				ModelState.AddModelError(nameof(model.EntidadPublicaId), "Debe seleccionar una entidad pública.");
				await CargarPeriodosDisponibles(model);
				return View(model);
			}

			var entidadSeleccionada = model.EntidadesDisponibles
				.FirstOrDefault(e => e.EntidadPublicaId == model.EntidadPublicaId.Value);

			if (entidadSeleccionada == null)
			{
				ModelState.AddModelError(nameof(model.EntidadPublicaId), "La entidad seleccionada no es válida.");
				await CargarPeriodosDisponibles(model);
				return View(model);
			}

			var entidadDisplay = BuildEntidadDisplay(entidadSeleccionada);

			try
			{
				var payload = new { model.EntidadPublicaId, Entidad = entidadDisplay, model.PlanNacionalId, model.PeriodoPlanificacionId, model.PeriodoInicio, model.PeriodoFin, Estado = "Borrador" };
				var response = await _apiClient.SendAsync(HttpMethod.Post, "/api/planesestrategicos/crear", payload);

				if (response?.IsSuccessStatusCode == true)
				{
					TempData["Success"] = "Plan Estratégico Institucional (PEI) creado exitosamente.";
					return RedirectToAction(nameof(Index));
				}

				var errorMsg = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "No fue posible crear el Plan Institucional.");
				ModelState.AddModelError(string.Empty, errorMsg);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error creando PEI: {ex.Message}");
				ModelState.AddModelError(string.Empty, "Error interno al procesar la solicitud.");
			}

			await CargarEntidadesDisponibles(model);
			await CargarPeriodosDisponibles(model);
			return View(model);
		}

		[HttpGet("{id:int}")]
		public async Task<IActionResult> Detalle(int id)
		{
			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Get, $"/api/planesestrategicos/{id}");
				if (response?.IsSuccessStatusCode == true)
				{
					var json = await response.Content.ReadAsStringAsync();
					var envelope = JsonSerializer.Deserialize<ApiEnvelope<PlanesEstrategicoDetailApiDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

					if (envelope?.Data != null)
					{
						if (TempData.TryGetValue("Success", out var success)) ViewBag.SwalSuccess = success;
						if (TempData.TryGetValue("Warning", out var warning)) ViewBag.SwalWarning = warning;
						return View(envelope.Data);
					}
				}

				TempData["Warning"] = "Plan Institucional no encontrado.";
				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error cargando detalle PEI: {ex.Message}");
				TempData["Warning"] = "Error interno del servidor.";
				return RedirectToAction(nameof(Index));
			}
		}

		[HttpGet("{id:int}/editar")]
		public async Task<IActionResult> EditarPlan(int id)
		{
			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Get, $"/api/planesestrategicos/{id}");
				if (response?.StatusCode == System.Net.HttpStatusCode.NotFound)
				{
					TempData["Warning"] = "Plan estratégico no encontrado.";
					return RedirectToAction(nameof(Index));
				}

				if (response?.IsSuccessStatusCode != true)
				{
					TempData["Warning"] = "No fue posible cargar el plan para edición.";
					return RedirectToAction(nameof(Index));
				}

				var json = await response.Content.ReadAsStringAsync();
				var envelope = JsonSerializer.Deserialize<ApiEnvelope<PlanesEstrategicoDetailApiDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
				if (envelope?.Data == null)
				{
					TempData["Warning"] = "No fue posible cargar el plan para edición.";
					return RedirectToAction(nameof(Index));
				}

				var model = new PlanEstrategicoEditViewModel
				{
					PlanEstrategicoId = envelope.Data.PlanEstrategicoId,
					EntidadPublicaId = envelope.Data.EntidadPublicaId,
					PlanNacionalId = envelope.Data.PlanNacionalId,
					PeriodoPlanificacionId = envelope.Data.PeriodoPlanificacionId,
					PeriodoInicio = envelope.Data.PeriodoInicio,
					PeriodoFin = envelope.Data.PeriodoFin,
					Estado = envelope.Data.Estado
				};

				await CargarEntidadesDisponibles(model);
				await CargarPlanesNacionalesDisponibles(model);
				if (!model.EntidadPublicaId.HasValue || model.EntidadPublicaId.Value <= 0)
				{
					model.EntidadPublicaId = ResolveEntidadPublicaId(model.EntidadesDisponibles, envelope.Data.Entidad);
				}

				await CargarPeriodosDisponibles(model);
				return View(model);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error cargando edición de PEI: {ex.Message}");
				TempData["Warning"] = "No fue posible cargar el plan para edición.";
				return RedirectToAction(nameof(Index));
			}
		}

		[HttpPost("{id:int}/editar")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditarPlan(int id, PlanEstrategicoEditViewModel model)
		{
			if (id != model.PlanEstrategicoId)
			{
				return BadRequest();
			}

			if (!ModelState.IsValid)
			{
				await CargarEntidadesDisponibles(model);
				await CargarPeriodosDisponibles(model);
				await CargarPlanesNacionalesDisponibles(model);
				return View(model);
			}

			await CargarPeriodosDisponibles(model);
			await CargarPlanesNacionalesDisponibles(model);
			if (!SincronizarPeriodoConPlanNacional(model))
			{
				await CargarEntidadesDisponibles(model);
				return View(model);
			}

			await CargarEntidadesDisponibles(model);
			if (!model.EntidadPublicaId.HasValue)
			{
				ModelState.AddModelError(nameof(model.EntidadPublicaId), "Debe seleccionar una entidad pública.");
				return View(model);
			}

			var entidadSeleccionada = model.EntidadesDisponibles
				.FirstOrDefault(e => e.EntidadPublicaId == model.EntidadPublicaId.Value);

			if (entidadSeleccionada == null)
			{
				ModelState.AddModelError(nameof(model.EntidadPublicaId), "La entidad seleccionada no es válida.");
				return View(model);
			}

			var entidadDisplay = BuildEntidadDisplay(entidadSeleccionada);

			try
			{
				var payload = new { model.EntidadPublicaId, Entidad = entidadDisplay, model.PlanNacionalId, model.PeriodoPlanificacionId, model.PeriodoInicio, model.PeriodoFin, model.Estado };
				var response = await _apiClient.SendAsync(HttpMethod.Put, $"/api/planesestrategicos/{id}", payload);

				if (response?.IsSuccessStatusCode == true)
				{
					TempData["Success"] = "Plan Estratégico Institucional actualizado exitosamente.";
					return RedirectToAction(nameof(Index));
				}

				var errorMsg = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "No fue posible actualizar el Plan Institucional.");
				ModelState.AddModelError(string.Empty, errorMsg);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error actualizando PEI: {ex.Message}");
				ModelState.AddModelError(string.Empty, "Error interno al procesar la solicitud.");
			}

			await CargarEntidadesDisponibles(model);
			return View(model);
		}

		[HttpPost("proyectos/crear")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CrearProyecto(ProyectoInversionCreateViewModel model)
		{
			if (!ModelState.IsValid)
			{
				TempData["Warning"] = "Datos del proyecto incompletos o inválidos.";
				return RedirectToAction(nameof(Detalle), new { id = model.PlanEstrategicoId });
			}

			try
			{
				var payload = new { model.PlanEstrategicoId, model.CodigoProyecto, model.Nombre, model.Monto, Estado = "Formulacion" };
				var response = await _apiClient.SendAsync(HttpMethod.Post, "/api/proyectosinversion/crear", payload);

				if (response?.IsSuccessStatusCode == true)
				{
					TempData["Success"] = "Proyecto de Inversión registrado exitosamente.";
					var json = await response.Content.ReadAsStringAsync();
					var envelope = JsonSerializer.Deserialize<ApiEnvelope<ProyectosInversionApiDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					if (envelope?.Data != null && model.RespaldosEjecucion.Count > 0)
					{
						await CargarRespaldosAsync(envelope.Data.ProyectoInversionId, model.RespaldosEjecucion);
					}
				}
				else
				{
					var errorMsg = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "No se pudo registrar el proyecto de inversión.");
					TempData["Warning"] = errorMsg;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error creando Proyecto: {ex.Message}");
				TempData["Warning"] = "Error interno al guardar el proyecto.";
			}

			return RedirectToAction(nameof(Detalle), new { id = model.PlanEstrategicoId });
		}

		[HttpGet("{planId:int}/proyectos/{proyectoId:int}/editar")]
		public async Task<IActionResult> EditarProyecto(int planId, int proyectoId)
		{
			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Get, $"/api/proyectosinversion/{proyectoId}");
				if (response?.StatusCode == System.Net.HttpStatusCode.NotFound)
				{
					TempData["Warning"] = "Proyecto de inversión no encontrado.";
					return RedirectToAction(nameof(Detalle), new { id = planId });
				}

				if (response?.IsSuccessStatusCode != true)
				{
					TempData["Warning"] = "No fue posible cargar el proyecto para edición.";
					return RedirectToAction(nameof(Detalle), new { id = planId });
				}

				var json = await response.Content.ReadAsStringAsync();
				var envelope = JsonSerializer.Deserialize<ApiEnvelope<ProyectosInversionApiDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

				if (envelope?.Data == null)
				{
					TempData["Warning"] = "No fue posible cargar el proyecto para edición.";
					return RedirectToAction(nameof(Detalle), new { id = planId });
				}

				if (envelope.Data.PlanEstrategicoId != planId)
				{
					TempData["Warning"] = "El proyecto no pertenece al plan estratégico indicado.";
					return RedirectToAction(nameof(Detalle), new { id = planId });
				}

				var model = new ProyectoInversionEditViewModel
				{
					ProyectoInversionId = envelope.Data.ProyectoInversionId,
					PlanEstrategicoId = envelope.Data.PlanEstrategicoId,
					CodigoProyecto = envelope.Data.CodigoProyecto,
					Nombre = envelope.Data.Nombre,
					Monto = envelope.Data.Monto,
					Estado = envelope.Data.Estado,
					Respaldos = envelope.Data.RespaldosEjecucion
				};

				return View(model);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error cargando edición de proyecto: {ex.Message}");
				TempData["Warning"] = "No fue posible cargar el proyecto para edición.";
				return RedirectToAction(nameof(Detalle), new { id = planId });
			}
		}

		[HttpPost("{planId:int}/proyectos/{proyectoId:int}/editar")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditarProyecto(int planId, int proyectoId, ProyectoInversionEditViewModel model)
		{
			if (planId != model.PlanEstrategicoId || proyectoId != model.ProyectoInversionId)
			{
				return BadRequest();
			}

			if (!ModelState.IsValid)
			{
				return View(model);
			}

			try
			{
				var payload = new { model.Nombre, model.Monto, model.Estado };
				var response = await _apiClient.SendAsync(HttpMethod.Put, $"/api/proyectosinversion/{proyectoId}", payload);

				if (response?.IsSuccessStatusCode == true)
				{
					TempData["Success"] = "Proyecto de inversión actualizado exitosamente.";
					if (model.RespaldosEjecucion.Count > 0)
					{
						await CargarRespaldosAsync(proyectoId, model.RespaldosEjecucion);
					}
					return RedirectToAction(nameof(Detalle), new { id = planId });
				}

				var errorMsg = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "No fue posible actualizar el proyecto de inversión.");
				ModelState.AddModelError(string.Empty, errorMsg);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error actualizando proyecto: {ex.Message}");
				ModelState.AddModelError(string.Empty, "Error interno al procesar la solicitud.");
			}

			return View(model);
		}

		private async Task CargarRespaldosAsync(int proyectoId, IEnumerable<IFormFile> archivos)
		{
			using var contenido = new MultipartFormDataContent();
			foreach (var archivo in archivos.Where(a => a.Length > 0))
			{
				var archivoHttp = new StreamContent(archivo.OpenReadStream());
				archivoHttp.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(archivo.ContentType) ? "application/octet-stream" : archivo.ContentType);
				contenido.Add(archivoHttp, "archivos", archivo.FileName);
			}

			if (contenido.Count() == 0)
			{
				return;
			}

			var response = await _apiClient.SendMultipartAsync($"/api/proyectosinversion/{proyectoId}/respaldos-ejecucion", contenido);
			if (response?.IsSuccessStatusCode != true)
			{
				TempData["Warning"] = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "No fue posible cargar los respaldos de ejecución.");
			}
		}

		private async Task CargarEntidadesDisponibles(PlanEstrategicoCreateViewModel model)
		{
			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Get, "/api/instituciones/entidades");
				if (response?.IsSuccessStatusCode == true)
				{
					var json = await response.Content.ReadAsStringAsync();
					var envelope = JsonSerializer.Deserialize<ApiEnvelope<List<EntidadPublicaApiDto>>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					model.EntidadesDisponibles = envelope?.Data ?? new List<EntidadPublicaApiDto>();
				}
			}
			catch
			{
				// Ignorar error, lista quedará vacía
			}
		}

		private async Task CargarPeriodosDisponibles(PlanEstrategicoCreateViewModel model)
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

		private async Task CargarPlanesNacionalesDisponibles(PlanEstrategicoCreateViewModel model)
		{
			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Get, "/api/planesnacionales");
				if (response?.IsSuccessStatusCode == true)
				{
					var json = await response.Content.ReadAsStringAsync();
					using var doc = JsonDocument.Parse(json);
					if (doc.RootElement.TryGetProperty("data", out var dataElement) && dataElement.TryGetProperty("data", out var itemsElement))
					{
						model.PlanesNacionalesDisponibles = JsonSerializer.Deserialize<List<MacroPlanNacionalApiDto>>(itemsElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
					}
				}
			}
			catch
			{
				model.PlanesNacionalesDisponibles = new List<MacroPlanNacionalApiDto>();
			}
		}

		private bool SincronizarPeriodoConPlanNacional(PlanEstrategicoCreateViewModel model)
		{
			var planNacional = model.PlanesNacionalesDisponibles
				.FirstOrDefault(plan => plan.PlanNacionalId == model.PlanNacionalId);

			if (planNacional?.PeriodoPlanificacionId is not int periodoPlanificacionId || periodoPlanificacionId <= 0)
			{
				ModelState.AddModelError(nameof(model.PlanNacionalId), "El Plan Nacional seleccionado no tiene un período de planificación válido.");
				return false;
			}

			var periodo = model.PeriodosDisponibles
				.FirstOrDefault(item => item.PeriodoPlanificacionId == periodoPlanificacionId);

			if (periodo == null)
			{
				ModelState.AddModelError(nameof(model.PlanNacionalId), "No fue posible obtener el período asociado al Plan Nacional seleccionado.");
				return false;
			}

			model.PeriodoPlanificacionId = periodo.PeriodoPlanificacionId;
			model.PeriodoInicio = periodo.FechaInicio.Year;
			model.PeriodoFin = periodo.FechaFin.Year;
			return true;
		}

		private async Task CargarEntidadesDisponibles(PlanEstrategicoEditViewModel model)
		{
			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Get, "/api/instituciones/entidades");
				if (response?.IsSuccessStatusCode == true)
				{
					var json = await response.Content.ReadAsStringAsync();
					var envelope = JsonSerializer.Deserialize<ApiEnvelope<List<EntidadPublicaApiDto>>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					model.EntidadesDisponibles = envelope?.Data ?? new List<EntidadPublicaApiDto>();
				}
			}
			catch
			{
				model.EntidadesDisponibles = new List<EntidadPublicaApiDto>();
			}
		}

		private async Task CargarPeriodosDisponibles(PlanEstrategicoEditViewModel model)
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

		private async Task CargarPlanesNacionalesDisponibles(PlanEstrategicoEditViewModel model)
		{
			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Get, "/api/planesnacionales");
				if (response?.IsSuccessStatusCode == true)
				{
					var json = await response.Content.ReadAsStringAsync();
					using var doc = JsonDocument.Parse(json);
					if (doc.RootElement.TryGetProperty("data", out var dataElement) && dataElement.TryGetProperty("data", out var itemsElement))
					{
						model.PlanesNacionalesDisponibles = JsonSerializer.Deserialize<List<MacroPlanNacionalApiDto>>(itemsElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
					}
				}
			}
			catch
			{
				model.PlanesNacionalesDisponibles = new List<MacroPlanNacionalApiDto>();
			}
		}

		private static int? ResolveEntidadPublicaId(List<EntidadPublicaApiDto> entidades, string? entidadGuardada)
		{
			if (string.IsNullOrWhiteSpace(entidadGuardada) || entidades.Count == 0)
			{
				return null;
			}

			var normalizedStored = entidadGuardada.Trim();
			var exactDisplay = entidades.FirstOrDefault(e =>
				string.Equals(BuildEntidadDisplay(e), normalizedStored, StringComparison.OrdinalIgnoreCase));

			if (exactDisplay != null)
			{
				return exactDisplay.EntidadPublicaId;
			}

			var byName = entidades.FirstOrDefault(e =>
				string.Equals((e.Nombre ?? string.Empty).Trim(), normalizedStored, StringComparison.OrdinalIgnoreCase));

			return byName?.EntidadPublicaId;
		}

		private static string BuildEntidadDisplay(EntidadPublicaApiDto entidad)
		{
			var nombre = (entidad.Nombre ?? string.Empty).Trim();
			var sigla = (entidad.Sigla ?? string.Empty).Trim();

			if (string.IsNullOrWhiteSpace(sigla))
			{
				return nombre;
			}

			return $"{nombre} ({sigla})";
		}
	}
}