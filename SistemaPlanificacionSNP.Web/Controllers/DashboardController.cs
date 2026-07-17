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
        private readonly IAuthService _authService;
        private readonly ILogger<DashboardController> _logger;
        private const string ApiGatewayUrl = "https://localhost:7000";

        public DashboardController(IApiClient apiClient, IAuthService authService, ILogger<DashboardController> logger)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
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
                var token = _authService.GetAccessToken();
                if (string.IsNullOrEmpty(token))
                {
                    return Unauthorized();
                }

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                    var response = await client.GetAsync($"{ApiGatewayUrl}/api/planificacion/dashboard");

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        return Content(json, "application/json");
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetDashboardData: {ex.Message}", ex);
                return BadRequest();
            }
        }
    }
}
