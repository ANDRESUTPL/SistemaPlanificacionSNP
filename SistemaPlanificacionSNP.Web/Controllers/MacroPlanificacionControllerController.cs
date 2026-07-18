using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPlanificacionSNP.Web.Common;
using SistemaPlanificacionSNP.Web.Models;
using SistemaPlanificacionSNP.Web.Services;
using System.Text.Json;

namespace SistemaPlanificacionSNP.Web.Controllers
{
	[Authorize]
	[Route("macroplanificacion/planes")]
	public class MacroPlanificacionController : Controller
	{
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
		public IActionResult CrearPlan()
		{
			return View(new PlanNacionalCreateViewModel());
		}

		[HttpPost("crear")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CrearPlan(PlanNacionalCreateViewModel model)
		{
			if (model.PeriodoInicio > model.PeriodoFin)
			{
				ModelState.AddModelError(nameof(model.PeriodoFin), "El año de fin debe ser mayor o igual al de inicio.");
			}

			if (!ModelState.IsValid) return View(model);

			try
			{
				var payload = new { model.Nombre, model.PeriodoInicio, model.PeriodoFin, Estado = "Borrador" };
				var response = await _apiClient.SendAsync(HttpMethod.Post, "/api/planesnacionales/crear", payload);

				if (response?.IsSuccessStatusCode == true)
				{
					TempData["Success"] = "Plan Nacional de Desarrollo creado exitosamente.";
					return RedirectToAction(nameof(Index));
				}

				var errorMsg = await ApiHttpErrorHelper.TryExtractApiMessageAsync(response);
				ModelState.AddModelError(string.Empty, errorMsg ?? "No fue posible crear el Plan Nacional.");
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error creando PND: {ex.Message}");
				ModelState.AddModelError(string.Empty, "Error interno al procesar la solicitud.");
			}

			return View(model);
		}

		[HttpGet("{id:int}")]
		public async Task<IActionResult> Detalle(int id)
		{
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
					var errorMsg = await ApiHttpErrorHelper.TryExtractApiMessageAsync(response);
					TempData["Warning"] = errorMsg ?? "No se pudo agregar el objetivo.";
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error agregando objetivo macro: {ex.Message}");
				TempData["Warning"] = "Error interno al guardar el objetivo.";
			}

			return RedirectToAction(nameof(Detalle), new { id = model.PlanNacionalId });
		}
	}
}