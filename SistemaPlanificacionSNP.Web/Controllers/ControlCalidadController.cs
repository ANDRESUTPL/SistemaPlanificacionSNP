using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPlanificacionSNP.Web.Common;
using SistemaPlanificacionSNP.Web.Models;
using SistemaPlanificacionSNP.Web.Services;
using System.Drawing;
using System.Text.Json;
using ClosedXML.Excel;
using System.IO;
using System.Net.Http.Headers;

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
		public async Task<IActionResult> CrearRevision()
		{
			var planes = await CargarPlanesDisponiblesAsync();
			var proyectos = await CargarProyectosDisponiblesAsync();
			var model = new RevisionCreateViewModel
			{
				PlanesDisponibles = planes,
				ProyectosDisponibles = proyectos,
				EntidadesDisponibles = ExtraerEntidadesConPlanes(planes)
			};
			return View(model);
		}

		[HttpPost("crear")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CrearRevision(RevisionCreateViewModel model)
		{
			model.PlanesDisponibles = await CargarPlanesDisponiblesAsync();
			model.EntidadesDisponibles = ExtraerEntidadesConPlanes(model.PlanesDisponibles);
			model.ProyectosDisponibles = await CargarProyectosDisponiblesAsync();
			SincronizarSeleccionProyecto(model);
			if (!ModelState.IsValid) return View(model);

			try
			{
				var planSeleccionado = model.PlanesDisponibles.FirstOrDefault(p => p.PlanEstrategicoId == model.PlanEstrategicoId);
				var proyectoSeleccionado = model.ProyectosDisponibles.FirstOrDefault(p => p.ProyectoInversionId == model.ProyectoInversionId && p.PlanEstrategicoId == model.PlanEstrategicoId);

				if (proyectoSeleccionado == null)
				{
					ModelState.AddModelError(nameof(model.ProyectoInversionId), "El proyecto seleccionado no pertenece al PEI indicado.");
					return View(model);
				}

				var payload = new
				{
					model.CodigoRevision,
					Modulo = ConstruirModulo(planSeleccionado, proyectoSeleccionado),
					model.Estado,
					model.Observaciones,
					model.PlanEstrategicoId,
					model.ProyectoInversionId,
					EntidadPublicaId = planSeleccionado?.EntidadPublicaId ?? model.EntidadPublicaId,
					EntidadNombre = planSeleccionado?.Entidad,
					CodigoProyecto = proyectoSeleccionado.CodigoProyecto
				};
				var response = await _apiClient.SendAsync(HttpMethod.Post, "/api/revisiones/crear", payload);

				if (response?.IsSuccessStatusCode == true)
				{
					TempData["Success"] = "Revisión técnica aperturada exitosamente.";
					return RedirectToAction(nameof(Index));
				}

				var errorMsg = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "No fue posible crear el registro de revisión.");
				ModelState.AddModelError(string.Empty, errorMsg);
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

		[HttpGet("{id:int}/editar")]
		public async Task<IActionResult> EditarRevision(int id)
		{
			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Get, $"/api/revisiones/{id}");
				if (response?.StatusCode == System.Net.HttpStatusCode.NotFound)
				{
					TempData["Warning"] = "Revisión no encontrada.";
					return RedirectToAction(nameof(Index));
				}

				if (response?.IsSuccessStatusCode != true)
				{
					TempData["Warning"] = "No fue posible cargar la revisión para edición.";
					return RedirectToAction(nameof(Index));
				}

				var json = await response.Content.ReadAsStringAsync();
				var envelope = JsonSerializer.Deserialize<ApiEnvelope<RevisioneApiDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

				if (envelope?.Data == null)
				{
					TempData["Warning"] = "No fue posible cargar la revisión para edición.";
					return RedirectToAction(nameof(Index));
				}

				var model = new RevisionEditViewModel
				{
					RevisionId = envelope.Data.RevisionId,
					CodigoRevision = envelope.Data.CodigoRevision,
					Modulo = envelope.Data.Modulo,
					PlanEstrategicoId = envelope.Data.PlanEstrategicoId,
					ProyectoInversionId = envelope.Data.ProyectoInversionId,
					EntidadPublicaId = envelope.Data.EntidadPublicaId,
					Estado = envelope.Data.Estado,
					Observaciones = envelope.Data.Observaciones,
					FechaRevision = envelope.Data.FechaRevision,
					PlanesDisponibles = await CargarPlanesDisponiblesAsync(),
					ProyectosDisponibles = await CargarProyectosDisponiblesAsync()
				};

				model.EntidadesDisponibles = ExtraerEntidadesConPlanes(model.PlanesDisponibles);

				if (!model.EntidadPublicaId.HasValue)
				{
					model.EntidadPublicaId = model.PlanesDisponibles
						.FirstOrDefault(p => p.PlanEstrategicoId == model.PlanEstrategicoId)?.EntidadPublicaId;
				}

				return View(model);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error cargando edición de revisión: {ex.Message}");
				TempData["Warning"] = "No fue posible cargar la revisión para edición.";
				return RedirectToAction(nameof(Index));
			}
		}

		[HttpPost("{id:int}/editar")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditarRevision(int id, RevisionEditViewModel model)
		{
			model.PlanesDisponibles = await CargarPlanesDisponiblesAsync();
			model.EntidadesDisponibles = ExtraerEntidadesConPlanes(model.PlanesDisponibles);
			model.ProyectosDisponibles = await CargarProyectosDisponiblesAsync();
			SincronizarSeleccionProyecto(model);

			if (id != model.RevisionId)
			{
				return BadRequest();
			}

			if (!ModelState.IsValid)
			{
				return View(model);
			}

			try
			{
				var planSeleccionado = model.PlanesDisponibles.FirstOrDefault(p => p.PlanEstrategicoId == model.PlanEstrategicoId);
				var proyectoSeleccionado = model.ProyectosDisponibles.FirstOrDefault(p => p.ProyectoInversionId == model.ProyectoInversionId && p.PlanEstrategicoId == model.PlanEstrategicoId);

				if (proyectoSeleccionado == null)
				{
					ModelState.AddModelError(nameof(model.ProyectoInversionId), "El proyecto seleccionado no pertenece al PEI indicado.");
					return View(model);
				}

				var payload = new
				{
					Modulo = ConstruirModulo(planSeleccionado, proyectoSeleccionado),
					model.PlanEstrategicoId,
					model.ProyectoInversionId,
					EntidadPublicaId = planSeleccionado?.EntidadPublicaId ?? model.EntidadPublicaId,
					EntidadNombre = planSeleccionado?.Entidad,
					CodigoProyecto = proyectoSeleccionado.CodigoProyecto,
					model.Estado,
					model.FechaRevision,
					model.Observaciones
				};
				var response = await _apiClient.SendAsync(HttpMethod.Put, $"/api/revisiones/{id}", payload);

				if (response?.IsSuccessStatusCode == true)
				{
					TempData["Success"] = "Revisión actualizada exitosamente.";
					return RedirectToAction(nameof(Index));
				}

				var errorMsg = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "No fue posible actualizar la revisión.");
				ModelState.AddModelError(string.Empty, errorMsg);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error actualizando revisión: {ex.Message}");
				ModelState.AddModelError(string.Empty, "Error interno al procesar la solicitud.");
			}

			return View(model);
		}

		private async Task<List<PlanesEstrategicoApiDto>> CargarPlanesDisponiblesAsync()
		{
			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Get, "/api/planesestrategicos?pageNumber=1&pageSize=1000");
				if (response?.IsSuccessStatusCode != true)
				{
					return new List<PlanesEstrategicoApiDto>();
				}

				var json = await response.Content.ReadAsStringAsync();
				using var doc = JsonDocument.Parse(json);
				if (doc.RootElement.TryGetProperty("data", out var dataElement) &&
				    dataElement.TryGetProperty("data", out var itemsElement))
				{
					return JsonSerializer.Deserialize<List<PlanesEstrategicoApiDto>>(itemsElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error cargando planes estratégicos para revisiones: {ex.Message}");
			}

			return new List<PlanesEstrategicoApiDto>();
		}

		[HttpGet("planes-por-entidad")]
		public async Task<IActionResult> PlanesPorEntidad(int entidadPublicaId)
		{
			var planes = await CargarPlanesDisponiblesAsync();
			var filtrados = planes
				.Where(p => p.EntidadPublicaId == entidadPublicaId)
				.OrderByDescending(p => p.PeriodoInicio)
				.Select(p => new
				{
					p.PlanEstrategicoId,
					Descripcion = $"PEI #{p.PlanEstrategicoId} · {p.PeriodoInicio}-{p.PeriodoFin} · {p.Estado}"
				})
				.ToList();

			return Json(filtrados);
		}

		[HttpGet("proyectos-por-plan")]
		public async Task<IActionResult> ProyectosPorPlan(int planEstrategicoId)
		{
			var proyectos = await CargarProyectosPorPlanAsync(planEstrategicoId);
			var resultado = proyectos
				.OrderBy(p => p.CodigoProyecto)
				.Select(p => new
				{
					p.ProyectoInversionId,
					Descripcion = $"{p.CodigoProyecto} · {p.Nombre}"
				})
				.ToList();

			return Json(resultado);
		}

		private async Task<List<ProyectosInversionApiDto>> CargarProyectosPorPlanAsync(int? planEstrategicoId)
		{
			if (!planEstrategicoId.HasValue || planEstrategicoId.Value <= 0)
			{
				return new List<ProyectosInversionApiDto>();
			}

			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Get, $"/api/proyectosinversion?planEstrategicoId={planEstrategicoId.Value}&pageNumber=1&pageSize=1000");
				if (response?.IsSuccessStatusCode != true)
				{
					return new List<ProyectosInversionApiDto>();
				}

				var json = await response.Content.ReadAsStringAsync();
				using var doc = JsonDocument.Parse(json);
				if (doc.RootElement.TryGetProperty("data", out var dataElement) &&
				    dataElement.TryGetProperty("data", out var itemsElement))
				{
					return JsonSerializer.Deserialize<List<ProyectosInversionApiDto>>(itemsElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error cargando proyectos de inversión para revisiones: {ex.Message}");
			}

			return new List<ProyectosInversionApiDto>();
		}

		private async Task<List<ProyectosInversionApiDto>> CargarProyectosDisponiblesAsync()
		{
			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Get, "/api/proyectosinversion?pageNumber=1&pageSize=1000");
				if (response?.IsSuccessStatusCode != true)
				{
					return new List<ProyectosInversionApiDto>();
				}

				var json = await response.Content.ReadAsStringAsync();
				using var doc = JsonDocument.Parse(json);
				if (doc.RootElement.TryGetProperty("data", out var dataElement) &&
				    dataElement.TryGetProperty("data", out var itemsElement))
				{
					return JsonSerializer.Deserialize<List<ProyectosInversionApiDto>>(itemsElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error cargando proyectos de inversión para revisiones: {ex.Message}");
			}

			return new List<ProyectosInversionApiDto>();
		}

		private void SincronizarSeleccionProyecto(RevisionCreateViewModel model)
		{
			var proyectoSeleccionado = model.ProyectosDisponibles.FirstOrDefault(p => p.ProyectoInversionId == model.ProyectoInversionId);
			if (proyectoSeleccionado == null)
			{
				return;
			}

			model.PlanEstrategicoId = proyectoSeleccionado.PlanEstrategicoId;
			ModelState.Remove(nameof(model.PlanEstrategicoId));

			var planSeleccionado = model.PlanesDisponibles.FirstOrDefault(p => p.PlanEstrategicoId == proyectoSeleccionado.PlanEstrategicoId);
			model.EntidadPublicaId = planSeleccionado?.EntidadPublicaId ?? model.EntidadPublicaId;
			ModelState.Remove(nameof(model.EntidadPublicaId));
		}

		private static List<EntidadConPlanesViewModel> ExtraerEntidadesConPlanes(List<PlanesEstrategicoApiDto> planes)
		{
			return planes
				.Where(p => p.EntidadPublicaId.HasValue && !string.IsNullOrWhiteSpace(p.Entidad))
				.GroupBy(p => p.EntidadPublicaId!.Value)
				.Select(g => new EntidadConPlanesViewModel
				{
					EntidadPublicaId = g.Key,
					Nombre = g.First().Entidad
				})
				.OrderBy(e => e.Nombre)
				.ToList();
		}

		private static string ConstruirModulo(PlanesEstrategicoApiDto? plan, ProyectosInversionApiDto proyecto)
		{
			var entidad = plan?.Entidad ?? "Entidad no identificada";
			var modulo = $"{entidad} · PEI #{plan?.PlanEstrategicoId} · {proyecto.CodigoProyecto}";

			return modulo.Length > 100 ? modulo[..100] : modulo;
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
					var json = await response.Content.ReadAsStringAsync();
					var envelope = JsonSerializer.Deserialize<ApiEnvelope<AuditoriaApiDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					if (envelope?.Data != null && model.DocumentosAuditoria?.Any(a => a.Length > 0) == true)
					{
						await CargarDocumentosAuditoriaAsync(envelope.Data.AuditoriaId, model.DocumentosAuditoria);
					}

					TempData["Success"] = "Auditoría anexada exitosamente.";
				}
				else
				{
					var errorMsg = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "No se pudo registrar la auditoría.");
					TempData["Warning"] = errorMsg;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error creando Auditoría: {ex.Message}");
				TempData["Warning"] = "Error interno al guardar la auditoría.";
			}

			return RedirectToAction(nameof(Detalle), new { id = model.RevisionId });
		}

		[HttpGet("{revisionId:int}/auditorias/{auditoriaId:int}/editar")]
		public async Task<IActionResult> EditarAuditoria(int revisionId, int auditoriaId)
		{
			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Get, $"/api/auditorias/{auditoriaId}");
				if (response?.StatusCode == System.Net.HttpStatusCode.NotFound)
				{
					TempData["Warning"] = "Auditoría no encontrada.";
					return RedirectToAction(nameof(Detalle), new { id = revisionId });
				}

				if (response?.IsSuccessStatusCode != true)
				{
					TempData["Warning"] = "No fue posible cargar la auditoría para edición.";
					return RedirectToAction(nameof(Detalle), new { id = revisionId });
				}

				var json = await response.Content.ReadAsStringAsync();
				var envelope = JsonSerializer.Deserialize<ApiEnvelope<AuditoriaApiDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

				if (envelope?.Data == null)
				{
					TempData["Warning"] = "No fue posible cargar la auditoría para edición.";
					return RedirectToAction(nameof(Detalle), new { id = revisionId });
				}

				if (envelope.Data.RevisionId != revisionId)
				{
					TempData["Warning"] = "La auditoría no pertenece a la revisión indicada.";
					return RedirectToAction(nameof(Detalle), new { id = revisionId });
				}

				var model = new AuditoriaEditViewModel
				{
					AuditoriaId = envelope.Data.AuditoriaId,
					RevisionId = envelope.Data.RevisionId,
					Tipo = envelope.Data.Tipo,
					Resultado = envelope.Data.Resultado,
					FechaRegistro = envelope.Data.FechaRegistro,
					DocumentosExistentes = envelope.Data.Documentos
				};

				return View(model);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error cargando edición de auditoría: {ex.Message}");
				TempData["Warning"] = "No fue posible cargar la auditoría para edición.";
				return RedirectToAction(nameof(Detalle), new { id = revisionId });
			}
		}

		[HttpPost("{revisionId:int}/auditorias/{auditoriaId:int}/editar")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditarAuditoria(int revisionId, int auditoriaId, AuditoriaEditViewModel model)
		{
			if (revisionId != model.RevisionId || auditoriaId != model.AuditoriaId)
			{
				return BadRequest();
			}

			if (!ModelState.IsValid)
			{
				return View(model);
			}

			try
			{
				var payload = new { model.Tipo, model.Resultado, model.FechaRegistro };
				var response = await _apiClient.SendAsync(HttpMethod.Put, $"/api/auditorias/{auditoriaId}", payload);

				if (response?.IsSuccessStatusCode == true)
				{
					if (model.DocumentosAuditoria?.Any(a => a.Length > 0) == true)
					{
						await CargarDocumentosAuditoriaAsync(auditoriaId, model.DocumentosAuditoria);
					}

					TempData["Success"] = "Auditoría actualizada exitosamente.";
					return RedirectToAction(nameof(Detalle), new { id = revisionId });
				}

				var errorMsg = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "No fue posible actualizar la auditoría.");
				ModelState.AddModelError(string.Empty, errorMsg);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error actualizando auditoría: {ex.Message}");
				ModelState.AddModelError(string.Empty, "Error interno al procesar la solicitud.");
			}

			return View(model);
		}

		[HttpGet("{revisionId:int}/auditorias/{auditoriaId:int}/documentos/{documentoId:int}")]
		public async Task<IActionResult> DescargarDocumentoAuditoria(int revisionId, int auditoriaId, int documentoId)
		{
			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Get, $"/api/auditorias/{auditoriaId}/documentos/{documentoId}");
				if (response?.IsSuccessStatusCode == true)
				{
					var content = await response.Content.ReadAsByteArrayAsync();
					var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
					var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
						?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
						?? $"documento-auditoria-{documentoId}";

					return File(content, contentType, fileName);
				}

				TempData["Warning"] = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "No fue posible descargar el documento.");
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error descargando documento de auditoría: {ex.Message}");
				TempData["Warning"] = "Error interno al descargar el documento.";
			}

			return RedirectToAction(nameof(EditarAuditoria), new { revisionId, auditoriaId });
		}

		[HttpPost("{revisionId:int}/auditorias/{auditoriaId:int}/documentos/{documentoId:int}/eliminar")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EliminarDocumentoAuditoria(int revisionId, int auditoriaId, int documentoId)
		{
			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Delete, $"/api/auditorias/{auditoriaId}/documentos/{documentoId}");
				if (response?.IsSuccessStatusCode == true)
				{
					TempData["Success"] = "Documento eliminado exitosamente.";
				}
				else
				{
					TempData["Warning"] = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "No fue posible eliminar el documento.");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error eliminando documento de auditoría: {ex.Message}");
				TempData["Warning"] = "Error interno al eliminar el documento.";
			}

			return RedirectToAction(nameof(EditarAuditoria), new { revisionId, auditoriaId });
		}

		private async Task CargarDocumentosAuditoriaAsync(int auditoriaId, IEnumerable<IFormFile> archivos)
		{
			using var contenido = new MultipartFormDataContent();
			foreach (var archivo in archivos.Where(a => a.Length > 0))
			{
				var archivoHttp = new StreamContent(archivo.OpenReadStream());
				archivoHttp.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(archivo.ContentType) ? "application/octet-stream" : archivo.ContentType);
				contenido.Add(archivoHttp, "documentos", archivo.FileName);
			}

			if (contenido.Count() == 0)
			{
				return;
			}

			var response = await _apiClient.SendMultipartAsync($"/api/auditorias/{auditoriaId}/documentos", contenido);
			if (response?.IsSuccessStatusCode != true)
			{
				TempData["Warning"] = await ApiHttpErrorHelper.ResolveMutationErrorMessageAsync(response, "La auditoría se guardó, pero no fue posible cargar los documentos.");
			}
		}


		[HttpGet("exportar")]
		public async Task<IActionResult> ExportarExcel(string? buscar)
		{
			try
			{
				// Pedimos un pageSize alto para traer todos los registros de la búsqueda
				var queryParams = $"?pageNumber=1&pageSize=10000";
				if (!string.IsNullOrWhiteSpace(buscar)) queryParams += $"&codigoRevision={buscar}";

				var response = await _apiClient.SendAsync(HttpMethod.Get, $"/api/revisiones{queryParams}");
				var revisiones = new List<RevisioneApiDto>();

				if (response?.IsSuccessStatusCode == true)
				{
					var json = await response.Content.ReadAsStringAsync();
					using var doc = JsonDocument.Parse(json);

					if (doc.RootElement.TryGetProperty("data", out var dataElement) &&
						dataElement.TryGetProperty("data", out var itemsElement))
					{
						revisiones = JsonSerializer.Deserialize<List<RevisioneApiDto>>(itemsElement.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
					}
				}

				// Crear el archivo Excel
				using var workbook = new XLWorkbook();
				var worksheet = workbook.Worksheets.Add("Revisiones");

				// Cabeceras
				var currentRow = 1;
				worksheet.Cell(currentRow, 1).Value = "Código de Revisión";
				worksheet.Cell(currentRow, 2).Value = "Módulo";
				worksheet.Cell(currentRow, 3).Value = "Fecha de Registro";
				worksheet.Cell(currentRow, 4).Value = "Estado";

				// Formato de la cabecera
				var headerRange = worksheet.Range(1, 1, 1, 4);
				headerRange.Style.Font.Bold = true;
				headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

				// Filas de datos
				foreach (var rev in revisiones)
				{
					currentRow++;
					worksheet.Cell(currentRow, 1).Value = rev.CodigoRevision;
					worksheet.Cell(currentRow, 2).Value = rev.Modulo;
					worksheet.Cell(currentRow, 3).Value = rev.FechaRevision.ToString("dd/MM/yyyy");
					worksheet.Cell(currentRow, 4).Value = rev.Estado;
				}

				// Ajustar el ancho de las columnas automáticamente
				worksheet.Columns().AdjustToContents();

				// Convertir a MemoryStream para retornar el archivo
				using var stream = new MemoryStream();
				workbook.SaveAs(stream);
				var content = stream.ToArray();

				string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
				string fileName = $"Revisiones_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

				return File(content, contentType, fileName);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error al exportar a Excel: {ex.Message}");
				TempData["Warning"] = "Ocurrió un error al intentar generar el archivo Excel.";
				return RedirectToAction(nameof(Index), new { buscar });
			}
		}
	}
}