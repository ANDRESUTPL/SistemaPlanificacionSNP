using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPlanificacionSNP.Web.Common;
using SistemaPlanificacionSNP.Web.Models;
using SistemaPlanificacionSNP.Web.Services;
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

			return View(model);
		}

		[HttpGet("crear")]
		public async Task<IActionResult> CrearPlan()
		{
			var model = new PlanEstrategicoCreateViewModel();
			await CargarEntidadesDisponibles(model);
			await CargarPeriodosDisponibles(model);
			return View(model);
		}

		[HttpPost("crear")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CrearPlan(PlanEstrategicoCreateViewModel model)
		{
			if (!ModelState.IsValid)
			{
				await CargarEntidadesDisponibles(model);
				await CargarPeriodosDisponibles(model);
				return View(model);
			}

			await CargarPeriodosDisponibles(model);

			var periodoSeleccionado = model.PeriodosDisponibles
				.FirstOrDefault(p => p.PeriodoPlanificacionId == model.PeriodoPlanificacionId);

			if (periodoSeleccionado == null)
			{
				ModelState.AddModelError(nameof(model.PeriodoPlanificacionId), "El período seleccionado no es válido.");
				await CargarEntidadesDisponibles(model);
				await CargarPeriodosDisponibles(model);
				return View(model);
			}

			model.PeriodoInicio = periodoSeleccionado.FechaInicio.Year;
			model.PeriodoFin = periodoSeleccionado.FechaFin.Year;

			if (model.PeriodoInicio > model.PeriodoFin)
			{
				ModelState.AddModelError(nameof(model.PeriodoPlanificacionId), "El período seleccionado tiene un rango de fechas inválido.");
				await CargarEntidadesDisponibles(model);
				await CargarPeriodosDisponibles(model);
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
				var payload = new { model.EntidadPublicaId, Entidad = entidadDisplay, model.PeriodoPlanificacionId, model.PeriodoInicio, model.PeriodoFin, Estado = "Borrador" };
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
					PeriodoPlanificacionId = envelope.Data.PeriodoPlanificacionId,
					PeriodoInicio = envelope.Data.PeriodoInicio,
					PeriodoFin = envelope.Data.PeriodoFin,
					Estado = envelope.Data.Estado
				};

				await CargarEntidadesDisponibles(model);
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
				return View(model);
			}

			await CargarPeriodosDisponibles(model);
			if (!model.PeriodoPlanificacionId.HasValue)
			{
				ModelState.AddModelError(nameof(model.PeriodoPlanificacionId), "Debe seleccionar un período de planificación.");
				await CargarEntidadesDisponibles(model);
				return View(model);
			}

			var periodoSeleccionado = model.PeriodosDisponibles
				.FirstOrDefault(p => p.PeriodoPlanificacionId == model.PeriodoPlanificacionId.Value);

			if (periodoSeleccionado == null)
			{
				ModelState.AddModelError(nameof(model.PeriodoPlanificacionId), "El período seleccionado no es válido.");
				await CargarEntidadesDisponibles(model);
				return View(model);
			}

			model.PeriodoInicio = periodoSeleccionado.FechaInicio.Year;
			model.PeriodoFin = periodoSeleccionado.FechaFin.Year;

			if (model.PeriodoInicio > model.PeriodoFin)
			{
				ModelState.AddModelError(nameof(model.PeriodoPlanificacionId), "El período seleccionado tiene un rango de fechas inválido.");
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
				var payload = new { model.EntidadPublicaId, Entidad = entidadDisplay, model.PeriodoPlanificacionId, model.PeriodoInicio, model.PeriodoFin, model.Estado };
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
					Estado = envelope.Data.Estado
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