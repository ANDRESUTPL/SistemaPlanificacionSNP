using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
            try
            {
                var response = await _apiClient.SendAsync(HttpMethod.Get, "/api/planificacion/dashboard");
                if (response == null)
                {
                    return Unauthorized();
                }

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return Content(json, "application/json");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetDashboardData: {ex.Message}", ex);
                return BadRequest();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMenuActual()
        {
            try
            {
                var response = await _apiClient.SendAsync(HttpMethod.Get, "/api/usuarios/menu/actual");
                if (response == null)
                {
                    return Unauthorized(new { success = false, message = "No autenticado" });
                }

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new { success = false, message = "No fue posible obtener el menu" });
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
