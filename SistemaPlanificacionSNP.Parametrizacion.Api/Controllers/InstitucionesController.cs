using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPlanificacionSNP.Infrastructure.Common;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Parametrizacion.Api.Services;

namespace SistemaPlanificacionSNP.Parametrizacion.Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class InstitucionesController : ControllerBase
	{
		private readonly IParametrizacionService _service;
		private readonly ILogger<InstitucionesController> _logger;

		public InstitucionesController(IParametrizacionService service, ILogger<InstitucionesController> logger)
		{
			_service = service ?? throw new ArgumentNullException(nameof(service));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		// --- PERIODOS DE PLANIFICACIÓN ---

		[HttpGet("periodos")]
		public async Task<ActionResult<ApiResponse<List<PeriodoPlanificacionDto>>>> GetPeriodos()
		{
			try
			{
				var data = await _service.GetPeriodosAsync();
				return Ok(ApiResponse<List<PeriodoPlanificacionDto>>.SuccessWith(data));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al obtener periodos");
				return StatusCode(500, ApiResponse<List<PeriodoPlanificacionDto>>.FailureWith("Error interno"));
			}
		}

		[HttpPost("periodos")]
		public async Task<ActionResult<ApiResponse<PeriodoPlanificacionDto>>> CreatePeriodo([FromBody] PeriodoPlanificacionCreateUpdateDto dto)
		{
			try
			{
				var data = await _service.CreatePeriodoAsync(dto);
				return Ok(ApiResponse<PeriodoPlanificacionDto>.SuccessWith(data, "Periodo creado exitosamente."));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al crear periodo");
				return StatusCode(500, ApiResponse<PeriodoPlanificacionDto>.FailureWith("Error interno"));
			}
		}

		// --- ENTIDADES PÚBLICAS ---

		[HttpGet("entidades")]
		public async Task<ActionResult<ApiResponse<List<EntidadPublicaDto>>>> GetEntidades()
		{
			try
			{
				var data = await _service.GetEntidadesAsync();
				return Ok(ApiResponse<List<EntidadPublicaDto>>.SuccessWith(data));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al obtener entidades");
				return StatusCode(500, ApiResponse<List<EntidadPublicaDto>>.FailureWith("Error interno"));
			}
		}

		[HttpPost("entidades")]
		public async Task<ActionResult<ApiResponse<EntidadPublicaDto>>> CreateEntidad([FromBody] EntidadPublicaCreateUpdateDto dto)
		{
			try
			{
				var data = await _service.CreateEntidadAsync(dto);
				return Ok(ApiResponse<EntidadPublicaDto>.SuccessWith(data, "Entidad creada exitosamente."));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al crear entidad");
				return StatusCode(500, ApiResponse<EntidadPublicaDto>.FailureWith("Error interno"));
			}
		}
	}
}