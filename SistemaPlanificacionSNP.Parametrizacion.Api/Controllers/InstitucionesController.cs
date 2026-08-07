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
		[Authorize(Policy = "Instituciones.Lectura")]
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

		[HttpGet("periodos/{id:int}")]
		[Authorize(Policy = "Instituciones.Lectura")]
		public async Task<ActionResult<ApiResponse<PeriodoPlanificacionDto>>> GetPeriodoById(int id)
		{
			try
			{
				var data = await _service.GetPeriodoByIdAsync(id);
				if (data == null)
				{
					return NotFound(ApiResponse<PeriodoPlanificacionDto>.FailureWith("Período no encontrado."));
				}

				return Ok(ApiResponse<PeriodoPlanificacionDto>.SuccessWith(data));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al obtener periodo {PeriodoId}", id);
				return StatusCode(500, ApiResponse<PeriodoPlanificacionDto>.FailureWith("Error interno"));
			}
		}

		[HttpPost("periodos")]
		[Authorize(Policy = "Instituciones.Creacion")]
		public async Task<ActionResult<ApiResponse<PeriodoPlanificacionDto>>> CreatePeriodo([FromBody] PeriodoPlanificacionCreateUpdateDto dto)
		{
			try
			{
				var data = await _service.CreatePeriodoAsync(dto);
				return Ok(ApiResponse<PeriodoPlanificacionDto>.SuccessWith(data, "Periodo creado exitosamente."));
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(ApiResponse<PeriodoPlanificacionDto>.FailureWith(ex.Message));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al crear periodo");
				return StatusCode(500, ApiResponse<PeriodoPlanificacionDto>.FailureWith("Error interno"));
			}
		}

		[HttpPut("periodos/{id:int}")]
		[Authorize(Policy = "Instituciones.Edicion")]
		public async Task<ActionResult<ApiResponse<PeriodoPlanificacionDto>>> UpdatePeriodo(int id, [FromBody] PeriodoPlanificacionCreateUpdateDto dto)
		{
			try
			{
				var data = await _service.UpdatePeriodoAsync(id, dto);
				if (data == null)
				{
					return NotFound(ApiResponse<PeriodoPlanificacionDto>.FailureWith("Período no encontrado."));
				}

				return Ok(ApiResponse<PeriodoPlanificacionDto>.SuccessWith(data, "Período actualizado exitosamente."));
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(ApiResponse<PeriodoPlanificacionDto>.FailureWith(ex.Message));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al actualizar periodo {PeriodoId}", id);
				return StatusCode(500, ApiResponse<PeriodoPlanificacionDto>.FailureWith("Error interno"));
			}
		}

		[HttpDelete("periodos/{id:int}")]
		[Authorize(Policy = "Instituciones.Eliminacion")]
		public async Task<ActionResult<ApiResponse<string>>> DeactivatePeriodo(int id)
		{
			try
			{
				var deactivated = await _service.DeactivatePeriodoAsync(id);
				if (!deactivated)
				{
					return NotFound(ApiResponse<string>.FailureWith("Período no encontrado."));
				}

				return Ok(ApiResponse<string>.Succeeded("Período inactivado exitosamente."));
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(ApiResponse<string>.FailureWith(ex.Message));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al inactivar periodo {PeriodoId}", id);
				return StatusCode(500, ApiResponse<string>.FailureWith("Error interno"));
			}
		}

		// --- ENTIDADES PÚBLICAS ---

		[HttpGet("entidades")]
		[Authorize(Policy = "Instituciones.Lectura")]
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

		[HttpGet("entidades/{id:int}")]
		[Authorize(Policy = "Instituciones.Lectura")]
		public async Task<ActionResult<ApiResponse<EntidadPublicaDto>>> GetEntidadById(int id)
		{
			try
			{
				var data = await _service.GetEntidadByIdAsync(id);
				if (data == null)
				{
					return NotFound(ApiResponse<EntidadPublicaDto>.FailureWith("Entidad no encontrada."));
				}

				return Ok(ApiResponse<EntidadPublicaDto>.SuccessWith(data));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al obtener entidad {EntidadId}", id);
				return StatusCode(500, ApiResponse<EntidadPublicaDto>.FailureWith("Error interno"));
			}
		}

		[HttpPost("entidades")]
		[Authorize(Policy = "Instituciones.Creacion")]
		public async Task<ActionResult<ApiResponse<EntidadPublicaDto>>> CreateEntidad([FromBody] EntidadPublicaCreateUpdateDto dto)
		{
			try
			{
				var data = await _service.CreateEntidadAsync(dto);
				return Ok(ApiResponse<EntidadPublicaDto>.SuccessWith(data, "Entidad creada exitosamente."));
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(ApiResponse<EntidadPublicaDto>.FailureWith(ex.Message));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al crear entidad");
				return StatusCode(500, ApiResponse<EntidadPublicaDto>.FailureWith("Error interno"));
			}
		}

		[HttpPut("entidades/{id:int}")]
		[Authorize(Policy = "Instituciones.Edicion")]
		public async Task<ActionResult<ApiResponse<EntidadPublicaDto>>> UpdateEntidad(int id, [FromBody] EntidadPublicaCreateUpdateDto dto)
		{
			try
			{
				var data = await _service.UpdateEntidadAsync(id, dto);
				if (data == null)
				{
					return NotFound(ApiResponse<EntidadPublicaDto>.FailureWith("Entidad no encontrada."));
				}

				return Ok(ApiResponse<EntidadPublicaDto>.SuccessWith(data, "Entidad actualizada exitosamente."));
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(ApiResponse<EntidadPublicaDto>.FailureWith(ex.Message));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al actualizar entidad {EntidadId}", id);
				return StatusCode(500, ApiResponse<EntidadPublicaDto>.FailureWith("Error interno"));
			}
		}

		[HttpDelete("entidades/{id:int}")]
		[Authorize(Policy = "Instituciones.Eliminacion")]
		public async Task<ActionResult<ApiResponse<string>>> DeactivateEntidad(int id)
		{
			try
			{
				var deactivated = await _service.DeactivateEntidadAsync(id);
				if (!deactivated)
				{
					return NotFound(ApiResponse<string>.FailureWith("Entidad no encontrada."));
				}

				return Ok(ApiResponse<string>.Succeeded("Entidad inactivada exitosamente."));
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(ApiResponse<string>.FailureWith(ex.Message));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al inactivar entidad {EntidadId}", id);
				return StatusCode(500, ApiResponse<string>.FailureWith("Error interno"));
			}
		}
	}
}