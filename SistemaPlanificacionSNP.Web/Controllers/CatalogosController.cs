using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPlanificacionSNP.Web.Common;
using SistemaPlanificacionSNP.Web.Models;
using SistemaPlanificacionSNP.Web.Services;
using System.Text.Json;

namespace SistemaPlanificacionSNP.Web.Controllers
{
	[Authorize]
	[Route("parametrizacion/catalogos")]
	public class CatalogosController : Controller
	{
		private readonly IApiClient _apiClient;
		private readonly ILogger<CatalogosController> _logger;

		public CatalogosController(IApiClient apiClient, ILogger<CatalogosController> logger)
		{
			_apiClient = apiClient;
			_logger = logger;
		}

		[HttpGet("")]
		public async Task<IActionResult> Index(string? buscar)
		{
			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Get, "/api/catalogos");

				if (response?.StatusCode == System.Net.HttpStatusCode.Unauthorized)
					return RedirectToAction("Login", "Account");

				var catalogos = new List<CatalogoApiDto>();

				if (response != null && response.IsSuccessStatusCode)
				{
					var json = await response.Content.ReadAsStringAsync();
					var envelope = JsonSerializer.Deserialize<ApiEnvelope<List<CatalogoApiDto>>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					catalogos = envelope?.Data ?? new List<CatalogoApiDto>();
				}
				else
				{
					ViewBag.Error = "No se pudo cargar el listado de catálogos desde el servidor.";
				}

				if (!string.IsNullOrWhiteSpace(buscar))
				{
					var term = buscar.Trim().ToLower();
					catalogos = catalogos.Where(c => c.Nombre.ToLower().Contains(term) || c.Codigo.ToLower().Contains(term)).ToList();
				}

				var model = new CatalogosIndexViewModel
				{
					Catalogos = catalogos,
					Buscar = buscar
				};

				// Alertas de TempData para SweetAlert
				if (TempData.TryGetValue("Success", out var success)) ViewBag.SwalSuccess = success;
				if (TempData.TryGetValue("Warning", out var warning)) ViewBag.SwalWarning = warning;

				return View(model);
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error in Catalogos.Index: {ex.Message}");
				ViewBag.Error = "Ocurrió un error inesperado.";
				return View(new CatalogosIndexViewModel());
			}
		}

		[HttpGet("crear")]
		public IActionResult Create()
		{
			return View(new CatalogoCreateViewModel());
		}

		[HttpPost("crear")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(CatalogoCreateViewModel model)
		{
			if (!ModelState.IsValid) return View(model);

			try
			{
				var payload = new { model.Codigo, model.Nombre, model.Descripcion };
				var response = await _apiClient.SendAsync(HttpMethod.Post, "/api/catalogos", payload);

				if (response != null && response.IsSuccessStatusCode)
				{
					TempData["Success"] = "Catálogo maestro creado exitosamente.";
					return RedirectToAction(nameof(Index));
				}

				var errorMsg = await ApiHttpErrorHelper.TryExtractApiMessageAsync(response);
				ModelState.AddModelError(string.Empty, errorMsg ?? "No fue posible crear el catálogo. Verifica los datos.");
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error creating catalogo: {ex.Message}");
				ModelState.AddModelError(string.Empty, "Error interno al procesar la solicitud.");
			}

			return View(model);
		}

		[HttpGet("{codigo}")]
		public async Task<IActionResult> Detalle(string codigo)
		{
			try
			{
				var response = await _apiClient.SendAsync(HttpMethod.Get, $"/api/catalogos/{codigo}");
				if (response?.StatusCode == System.Net.HttpStatusCode.NotFound)
				{
					TempData["Warning"] = "Catálogo no encontrado.";
					return RedirectToAction(nameof(Index));
				}

				if (response != null && response.IsSuccessStatusCode)
				{
					var json = await response.Content.ReadAsStringAsync();
					var envelope = JsonSerializer.Deserialize<ApiEnvelope<CatalogoApiDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

					if (envelope?.Data != null)
					{
						if (TempData.TryGetValue("Success", out var success)) ViewBag.SwalSuccess = success;
						return View(envelope.Data);
					}
				}

				TempData["Warning"] = "No fue posible cargar el detalle del catálogo.";
				return RedirectToAction(nameof(Index));
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error loading catalogo detail: {ex.Message}");
				TempData["Warning"] = "Error interno del servidor.";
				return RedirectToAction(nameof(Index));
			}
		}

		[HttpPost("{codigo}/items")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddItem(string codigo, ItemCatalogoCreateViewModel model)
		{
			if (!ModelState.IsValid)
			{
				TempData["Warning"] = "Datos del ítem incompletos o inválidos.";
				return RedirectToAction(nameof(Detalle), new { codigo });
			}

			try
			{
				var payload = new { model.CatalogoId, model.Codigo, model.Nombre, model.Descripcion, model.Orden };
				var response = await _apiClient.SendAsync(HttpMethod.Post, "/api/catalogos/items", payload);

				if (response != null && response.IsSuccessStatusCode)
				{
					TempData["Success"] = "Ítem agregado exitosamente al catálogo.";
				}
				else
				{
					var errorMsg = await ApiHttpErrorHelper.TryExtractApiMessageAsync(response);
					TempData["Warning"] = errorMsg ?? "No se pudo agregar el ítem.";
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error adding item: {ex.Message}");
				TempData["Warning"] = "Error interno al guardar el ítem.";
			}

			return RedirectToAction(nameof(Detalle), new { codigo });
		}
	}
}