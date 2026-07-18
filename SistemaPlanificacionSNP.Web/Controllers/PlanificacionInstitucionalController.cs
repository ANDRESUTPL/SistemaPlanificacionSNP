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
			return View(model);
		}

		[HttpPost("crear")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CrearPlan(PlanEstrategicoCreateViewModel model)
		{
			if (model.PeriodoInicio > model.PeriodoFin)
			{
				ModelState.AddModelError(nameof(model.PeriodoFin), "El año de fin debe ser mayor o igual al de inicio.");
			}

			if (!ModelState.IsValid)
			{
				await CargarEntidadesDisponibles(model);
				return View(model);
			}

			try
			{
				var payload = new { model.Entidad, model.PeriodoInicio, model.PeriodoFin, Estado = "Borrador" };
				var response = await _apiClient.SendAsync(HttpMethod.Post, "/api/planesestrategicos/crear", payload);

				if (response?.IsSuccessStatusCode == true)
				{
					TempData["Success"] = "Plan Estratégico Institucional (PEI) creado exitosamente.";
					return RedirectToAction(nameof(Index));
				}

				var errorMsg = await ApiHttpErrorHelper.TryExtractApiMessageAsync(response);
				ModelState.AddModelError(string.Empty, errorMsg ?? "No fue posible crear el Plan Institucional.");
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error creando PEI: {ex.Message}");
				ModelState.AddModelError(string.Empty, "Error interno al procesar la solicitud.");
			}

			await CargarEntidadesDisponibles(model);
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
					var errorMsg = await ApiHttpErrorHelper.TryExtractApiMessageAsync(response);
					TempData["Warning"] = errorMsg ?? "No se pudo registrar el proyecto de inversión.";
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error creando Proyecto: {ex.Message}");
				TempData["Warning"] = "Error interno al guardar el proyecto.";
			}

			return RedirectToAction(nameof(Detalle), new { id = model.PlanEstrategicoId });
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
	}
}