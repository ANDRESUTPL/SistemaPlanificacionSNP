using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPlanificacionSNP.Web.Common;
using SistemaPlanificacionSNP.Web.Models;
using SistemaPlanificacionSNP.Web.Services;
using System.Security.Claims;
using System.Text.Json;

namespace SistemaPlanificacionSNP.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IApiClient _apiClient;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IApiClient apiClient, ILogger<DashboardController> logger)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var nombreUsuario = User.Identity?.Name ?? "Usuario";

                ViewBag.NombreUsuario = nombreUsuario;
                ViewBag.UserId = userId;

                // Aquí se puede agregar lógica para cargar datos del dashboard
                // Por ejemplo: obtener estadísticas de planificación

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Dashboard.Index: {ex.Message}", ex);
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            var resumen = new DashboardResumenDto();
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            try
            {
                var taskDashboard = _apiClient.SendAsync(HttpMethod.Get, "/api/planesestrategicos/dashboard");
                var taskPlanes = _apiClient.SendAsync(HttpMethod.Get, "/api/planesestrategicos?pageNumber=1&pageSize=1000");
                // No existe un valor de Estado fijo "Activo": los proyectos usan texto libre (Formulacion, Aprobado, Ejecucion, etc.)
                // y solo "Inactivo" es un valor reservado para el soft-delete, por eso se filtra en memoria.
                var taskProyectos = _apiClient.SendAsync(HttpMethod.Get, "/api/proyectosinversion?pageNumber=1&pageSize=1000");

                await Task.WhenAll(taskDashboard, taskPlanes, taskProyectos);

                var responseDashboard = await taskDashboard;
                if (responseDashboard?.IsSuccessStatusCode == true)
                {
                    var json = await responseDashboard.Content.ReadAsStringAsync();
                    var envelope = JsonSerializer.Deserialize<ApiEnvelope<PlanificacionDashboardApiDto>>(json, jsonOptions);
                    var dashboard = envelope?.Data ?? new PlanificacionDashboardApiDto();

                    resumen.TotalPlanesActivos = dashboard.TotalPlanesActivos;
                    resumen.TotalProyectosActivos = dashboard.TotalProyectosActivos;
                    resumen.TotalProyectos = dashboard.TotalProyectos;
                    resumen.InversionTotal = dashboard.MontoTotalProyectosActivos;
                }
                else
                {
                    _logger.LogWarning("No fue posible obtener /api/planesestrategicos/dashboard");
                }

                var responsePlanes = await taskPlanes;
                if (responsePlanes?.IsSuccessStatusCode == true)
                {
                    var json = await responsePlanes.Content.ReadAsStringAsync();
                    var planes = ExtraerListaPaginada<PlanesEstrategicoApiDto>(json, jsonOptions);

                    resumen.EstadoDistribucion = planes
                        .GroupBy(p => p.Estado)
                        .Select(g => new DashboardEstadoConteoDto { Estado = g.Key, Cantidad = g.Count() })
                        .ToList();

                    resumen.PlanesProximo = planes
                        .Where(p => !string.Equals(p.Estado, "Inactivo", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(p => p.PeriodoFin)
                        .Take(5)
                        .Select(p => new DashboardPlanProximoDto
                        {
                            PlanEstrategicoId = p.PlanEstrategicoId,
                            Entidad = p.Entidad,
                            Estado = p.Estado,
                            PeriodoInicio = p.PeriodoInicio,
                            PeriodoFin = p.PeriodoFin,
                            CantidadProyectos = p.CantidadProyectos
                        })
                        .ToList();
                }
                else
                {
                    _logger.LogWarning("No fue posible obtener /api/planesestrategicos");
                }

                var responseProyectos = await taskProyectos;
                if (responseProyectos?.IsSuccessStatusCode == true)
                {
                    var json = await responseProyectos.Content.ReadAsStringAsync();
                    var proyectos = ExtraerListaPaginada<ProyectosInversionApiDto>(json, jsonOptions);

                    resumen.AvanceProyectos = proyectos
                        .Where(p => !string.Equals(p.Estado, "Inactivo", StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(p => (p.AvanceFisico ?? 0) + (p.AvanceFinanciero ?? 0))
                        .Take(6)
                        .Select(p => new DashboardAvanceProyectoDto
                        {
                            CodigoProyecto = p.CodigoProyecto,
                            Nombre = p.Nombre,
                            AvanceFisico = p.AvanceFisico ?? 0,
                            AvanceFinanciero = p.AvanceFinanciero ?? 0
                        })
                        .ToList();
                }
                else
                {
                    _logger.LogWarning("No fue posible obtener /api/proyectosinversion");
                }

                return Json(new { success = true, data = resumen });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetDashboardData: {ex.Message}", ex);
                return StatusCode(500, new { success = false, message = "Error interno del servidor al cargar dashboard." });
            }
        }

        // Navega el sobre {success, data:{data:[...], totalPages,...}} propio de ApiPaginatedResponse<T>
        private static List<T> ExtraerListaPaginada<T>(string json, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("data", out var dataElement) &&
                dataElement.TryGetProperty("data", out var itemsElement))
            {
                return JsonSerializer.Deserialize<List<T>>(itemsElement.GetRawText(), options) ?? new List<T>();
            }

            return new List<T>();
        }

        [HttpGet]
        public async Task<IActionResult> GetMenuActual()
        {
            try
            {
                var response = await _apiClient.SendAsync(HttpMethod.Get, "/api/usuarios/menu/actual");
                if (response == null)
                {
                    return StatusCode(503, new { success = false, message = "No fue posible conectar con el servicio de menú." });
                }

                if (!response.IsSuccessStatusCode)
                {
                    var apiMessage = await ApiHttpErrorHelper.TryExtractApiMessageAsync(response);
                    var message = apiMessage ?? ApiHttpErrorHelper.BuildStatusMessage(response.StatusCode, "No fue posible obtener el menu dinamico.");
                    return StatusCode((int)response.StatusCode, new { success = false, message });
                }

                var json = await response.Content.ReadAsStringAsync();
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetMenuActual: {ex.Message}", ex);
                return StatusCode(500, new { success = false, message = "Error interno del servidor" });
            }
        }
    }
}
