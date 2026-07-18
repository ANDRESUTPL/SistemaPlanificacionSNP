using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPlanificacionSNP.Web.Common;
using SistemaPlanificacionSNP.Web.Models;
using SistemaPlanificacionSNP.Web.Services;
using System.Text.Json;

namespace SistemaPlanificacionSNP.Web.Controllers
{
	[Authorize]
	[Route("controlcalidad/revisiones")]
	public class ControlCalidadController : Controller
	{
		private readonly IApiClient _apiClient;
		private readonly ILogger<ControlCalidadController> _logger;

		public ControlCalidadController(IApiClient apiClient, ILogger<ControlCalidadController> logger)
		{
			_apiClient = apiClient;
			_logger = logger;
		}

		[HttpGet("")]
		public async Task<IActionResult> Index(string? buscar, int page = 1)
		{
			var model = new ControlCalidadIndexViewModel { Buscar = buscar, Page = page };

			try
			{
				var queryParams = $"?pageNumber={page}&pageSize=10";
				if (!string.IsNullOrWhiteSpace(buscar)) queryParams += $"&codigoRevision={buscar}";

				var taskRevisiones = _apiClient.SendAsync(HttpMethod.Get, $"/api/revisiones{queryParams}");
				var taskDashboard = _apiClient.SendAsync(HttpMethod.Get, "/api/revisiones/dashboard");

				await Task.WhenAll(taskRevisiones, taskDashboard);

				var responseRevisiones = await taskRevisiones;
				if (responseRevisiones?.IsSuccessStatusCode == true)
				{
					var json = await responseRevisiones.Content.ReadAsStringAsync();
					using var doc = JsonDocument.Parse(json);

					if (doc.RootElement.TryGetProperty("data", out var dataElement))
					{
						if (dataElement.TryGetProperty("data", out var itemsElement))
						{
							model.Revisiones = JsonSerializer.Deserialize<List<RevisioneApiDto>>(itemsElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
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
					var envelope = JsonSerializer.Deserialize<ApiEnvelope<ControlCalidadDashboardApiDto>>(jsonDash, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					model.Dashboard = envelope?.Data ?? new ControlCalidadDashboardApiDto();
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error en ControlCalidad.Index: {ex.Message}");
				ViewBag.Error = "No fue posible cargar la información de auditorías.";
			}

			if (TempData.TryGetValue("Success", out var success)) ViewBag.SwalSuccess = success;
			if (TempData.TryGetValue("Warning", out var warning)) ViewBag.SwalWarning = warning;

			return View(model);
		}

		[HttpGet("crear")]
		public IActionResult CrearRevision()
		{
			return View(new RevisionCreateViewModel());
		}

		[HttpPost("crear")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CrearRevision(RevisionCreateViewModel model)
		{
			if (!ModelState.IsValid) return View(model);

			try
			{
				var payload = new { model.CodigoRevision, model.Modulo, model.Estado, model.Observaciones };
				var response = await _apiClient.SendAsync(HttpMethod.Post, "/api/revisiones/crear", payload);

				if (response?.IsSuccessStatusCode == true)
				{
					TempData["Success"] = "Revisión técnica aperturada exitosamente.";
					return RedirectToAction(nameof(Index));
				}

				var errorMsg = await ApiHttpErrorHelper.TryExtractApiMessageAsync(response);
				ModelState.AddModelError(string.Empty, errorMsg ?? "No fue posible crear el registro de revisión.");
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error creando Revisión: {ex.Message}");
				ModelState.AddModelError(string.Empty, "Error interno al procesar la solicitud.");
			}

			return View(model);
		}

		[HttpGet("{id:int}")]
		public async Task<IActionResult> Detalle(int id)
		{
			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Get, $"/api/revisiones/{id}");
				if (response?.IsSuccessStatusCode == true)
				{
					var json = await response.Content.ReadAsStringAsync();
					var envelope = JsonSerializer.Deserialize<ApiEnvelope<RevisioneApiDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

					if (envelope?.Data != null)
					{
						if (TempData.TryGetValue("Success", out var success)) ViewBag.SwalSuccess = success;
						if (TempData.TryGetValue("Warning", out var warning)) ViewBag.SwalWarning = warning;
						return View(envelope.Data);
					}
				}

				TempData["Warning"] = "Revisión no encontrada.";
				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error cargando detalle Revisión: {ex.Message}");
				TempData["Warning"] = "Error interno del servidor.";
				return RedirectToAction(nameof(Index));
			}
		}

		[HttpPost("auditorias/crear")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CrearAuditoria(AuditoriaCreateViewModel model)
		{
			if (!ModelState.IsValid)
			{
				TempData["Warning"] = "Datos de la auditoría incompletos o inválidos.";
				return RedirectToAction(nameof(Detalle), new { id = model.RevisionId });
			}

			try
			{
				var payload = new { model.RevisionId, model.Tipo, model.Resultado };
				var response = await _apiClient.SendAsync(HttpMethod.Post, "/api/auditorias/crear", payload);

				if (response?.IsSuccessStatusCode == true)
				{
					TempData["Success"] = "Auditoría anexada exitosamente.";
				}
				else
				{
					var errorMsg = await ApiHttpErrorHelper.TryExtractApiMessageAsync(response);
					TempData["Warning"] = errorMsg ?? "No se pudo registrar la auditoría.";
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error creando Auditoría: {ex.Message}");
				TempData["Warning"] = "Error interno al guardar la auditoría.";
			}

			return RedirectToAction(nameof(Detalle), new { id = model.RevisionId });
		}
	}
}