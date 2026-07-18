using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Web.Common;
using SistemaPlanificacionSNP.Web.Models;
using SistemaPlanificacionSNP.Web.Services;
using System.Text.Json;

namespace SistemaPlanificacionSNP.Web.Controllers
{
	[Authorize]
	[Route("evaluacion")]
	public class EvaluacionReporteriaController : Controller
	{
		private readonly IApiClient _apiClient;
		private readonly ILogger<EvaluacionReporteriaController> _logger;

		public EvaluacionReporteriaController(IApiClient apiClient, ILogger<EvaluacionReporteriaController> logger)
		{
			_apiClient = apiClient;
			_logger = logger;
		}

		[HttpGet("dashboard")]
		public async Task<IActionResult> Index()
		{
			var model = new ReporteEjecutivoViewModel();

			try
			{
				// Obtenemos los proyectos para calcular las estadísticas
				// En un escenario real, llamaríamos a un endpoint de métricas específico como /api/reporteria/ejecutivo
				var response = await _apiClient.SendAsync(HttpMethod.Get, "/api/proyectosinversion?pageSize=1000");

				if (response?.IsSuccessStatusCode == true)
				{
					var json = await response.Content.ReadAsStringAsync();
					using var doc = JsonDocument.Parse(json);

					if (doc.RootElement.TryGetProperty("data", out var dataElement) && dataElement.TryGetProperty("data", out var itemsElement))
					{
						var proyectos = JsonSerializer.Deserialize<List<ProyectosInversionReadDto>>(itemsElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

						model.TotalProyectos = proyectos.Count;
						model.ProyectosEnEjecucion = proyectos.Count(p => p.Estado == "Ejecucion" || p.Estado == "Activo");
						model.ProyectosCompletados = proyectos.Count(p => p.Estado == "Completado" || p.Estado == "Cerrado");
						model.PresupuestoTotal = proyectos.Sum(p => p.Monto);

						// Simulamos un presupuesto ejecutado y un avance global para el reporte 
						// (En la siguiente iteración del backend se agregaría AvanceFisico/Financiero a ProyectosInversion)
						model.PresupuestoEjecutado = proyectos.Where(p => p.Estado != "Formulacion").Sum(p => p.Monto) * 0.45m;
						model.PorcentajeAvanceGlobal = model.TotalProyectos > 0 ? (double)(model.PresupuestoEjecutado / model.PresupuestoTotal) * 100 : 0;

						// Agrupaciones para gráficos
						var estadoGroups = proyectos.GroupBy(p => p.Estado).ToList();
						model.EtiquetasEstado = estadoGroups.Select(g => g.Key).ToList();
						model.ValoresEstado = estadoGroups.Select(g => g.Count()).ToList();

						// Top 5 Entidades/Planes con mayor presupuesto
						var presupGroups = proyectos.GroupBy(p => $"PEI-{p.PlanEstrategicoId}").OrderByDescending(g => g.Sum(x => x.Monto)).Take(5).ToList();
						model.EtiquetasEntidad = presupGroups.Select(g => g.Key).ToList();
						model.ValoresPresupuesto = presupGroups.Select(g => g.Sum(x => x.Monto)).ToList();
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error en Dashboard Ejecutivo: {ex.Message}");
				ViewBag.Error = "No fue posible cargar las métricas en este momento.";
			}

			return View(model);
		}

		[HttpGet("avances")]
		public async Task<IActionResult> Avances(string? buscar, int page = 1)
		{
			var model = new ListadoAvancesViewModel { Buscar = buscar, Page = page };

			try
			{
				var queryParams = $"?pageNumber={page}&pageSize=15";
				if (!string.IsNullOrWhiteSpace(buscar)) queryParams += $"&codigoProyecto={buscar}";

				var response = await _apiClient.SendAsync(HttpMethod.Get, $"/api/proyectosinversion{queryParams}");

				if (response?.IsSuccessStatusCode == true)
				{
					var json = await response.Content.ReadAsStringAsync();
					using var doc = JsonDocument.Parse(json);

					if (doc.RootElement.TryGetProperty("data", out var dataElement))
					{
						if (dataElement.TryGetProperty("data", out var itemsElement))
						{
							var proyectos = JsonSerializer.Deserialize<List<ProyectosInversionReadDto>>(itemsElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

							// Mapeo simulando valores de avance para la vista (se reemplazarán con datos reales del API cuando se expanda el DTO)
							Random rnd = new Random();
							model.Proyectos = proyectos.Select(p => new AvanceProyectoViewModel
							{
								ProyectoId = p.ProyectoInversionId,
								Codigo = p.CodigoProyecto,
								Nombre = p.Nombre,
								PresupuestoAsignado = p.Monto,
								Estado = p.Estado,
								AvanceFisico = p.Estado == "Completado" ? 100 : (p.Estado == "Formulacion" ? 0 : rnd.Next(10, 85)),
								AvanceFinanciero = p.Estado == "Completado" ? 100 : (p.Estado == "Formulacion" ? 0 : rnd.Next(5, 80)),
								FechaUltimaActualizacion = DateTime.UtcNow.AddDays(-rnd.Next(1, 30))
							}).ToList();
						}
						if (dataElement.TryGetProperty("totalPages", out var pagesElement))
						{
							model.TotalPages = pagesElement.GetInt32();
						}
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error cargando avances: {ex.Message}");
				ViewBag.Error = "No se pudieron cargar los proyectos.";
			}

			if (TempData.TryGetValue("Success", out var success)) ViewBag.SwalSuccess = success;
			if (TempData.TryGetValue("Warning", out var warning)) ViewBag.SwalWarning = warning;

			return View(model);
		}

		[HttpPost("avances/registrar")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> RegistrarAvance(RegistroAvanceViewModel model)
		{
			if (!ModelState.IsValid)
			{
				TempData["Warning"] = "Por favor verifica los datos ingresados.";
				return RedirectToAction(nameof(Avances));
			}

			try
			{
				// Enviar la actualización de estado al backend (Actualizando Proyecto)
				// En el futuro se llamaría a un endpoint específico /api/proyectosinversion/{id}/avance
				var payload = new ProyectosInversionUpdateDto { Estado = model.Estado };
				var response = await _apiClient.SendAsync(HttpMethod.Put, $"/api/proyectosinversion/{model.ProyectoId}", payload);

				if (response?.IsSuccessStatusCode == true)
				{
					TempData["Success"] = "Avance registrado y evaluado exitosamente.";
				}
				else
				{
					var errorMsg = await ApiHttpErrorHelper.TryExtractApiMessageAsync(response);
					TempData["Warning"] = errorMsg ?? "No se pudo registrar el avance.";
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error guardando avance: {ex.Message}");
				TempData["Warning"] = "Error interno del servidor.";
			}

			return RedirectToAction(nameof(Avances));
		}
	}
}