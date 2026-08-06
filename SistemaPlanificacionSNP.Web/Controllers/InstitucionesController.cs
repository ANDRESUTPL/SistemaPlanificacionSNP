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

        [HttpGet("exportar")]
        public async Task<IActionResult> ExportarExcel(string? buscar)
        {
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
    }
}