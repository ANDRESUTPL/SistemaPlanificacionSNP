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
		public async Task<IActionResult> CrearPlan()
		{
			var model = new PlanNacionalCreateViewModel();
			await CargarPeriodosDisponibles(model);
			return View(model);
		}

		[HttpPost("crear")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CrearPlan(PlanNacionalCreateViewModel model)
		{
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
    }
}