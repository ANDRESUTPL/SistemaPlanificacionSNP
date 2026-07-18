using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPlanificacionSNP.Web.Common;
using SistemaPlanificacionSNP.Web.Models;
using SistemaPlanificacionSNP.Web.Services;
using System.Text.Json;

namespace SistemaPlanificacionSNP.Web.Controllers
{
	[Authorize]
	[Route("parametrizacion/instituciones")]
	public class InstitucionesController : Controller
	{
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
		public IActionResult CrearEntidad()
		{
			return View(new EntidadPublicaCreateViewModel());
		}

		[HttpPost("crear-entidad")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CrearEntidad(EntidadPublicaCreateViewModel model)
		{
			if (!ModelState.IsValid) return View(model);

			try
			{
				var payload = new { model.Nombre, model.Sigla, model.Tipo, model.NivelGobierno };
				var response = await _apiClient.SendAsync(HttpMethod.Post, "/api/instituciones/entidades", payload);

				if (response != null && response.IsSuccessStatusCode)
				{
					TempData["Success"] = "Entidad Pública registrada exitosamente.";
					return RedirectToAction(nameof(Index));
				}

				var errorMsg = await ApiHttpErrorHelper.TryExtractApiMessageAsync(response);
				ModelState.AddModelError(string.Empty, errorMsg ?? "No fue posible registrar la entidad. Verifica los datos.");
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error creating Entidad: {ex.Message}");
				ModelState.AddModelError(string.Empty, "Error interno al procesar la solicitud.");
			}

			return View(model);
		}

		[HttpPost("crear-periodo")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CrearPeriodo(PeriodoPlanificacionCreateViewModel model)
		{
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
					var errorMsg = await ApiHttpErrorHelper.TryExtractApiMessageAsync(response);
					TempData["Warning"] = errorMsg ?? "No se pudo crear el período.";
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error creating Periodo: {ex.Message}");
				TempData["Warning"] = "Error interno al guardar el período.";
			}

			return RedirectToAction(nameof(Index));
		}
	}
}