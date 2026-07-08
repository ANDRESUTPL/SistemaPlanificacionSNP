using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPlanificacionSNP.Infrastructure.Common;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.Services;
using System.Security.Claims;

namespace SistemaPlanificacionSNP.MacroPlanificacion.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PlanesNacionalesController : ControllerBase
    {
        private readonly IMacroPlanNacionalService _service;
        private readonly IMapper _mapper;
        private readonly ILogger<PlanesNacionalesController> _logger;

        public PlanesNacionalesController(
            IMacroPlanNacionalService service,
            IMapper mapper,
            ILogger<PlanesNacionalesController> logger)
        {
            _service = service;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiPaginatedResponse<MacroPlanNacionalDto>>> GetAll([FromQuery] MacroPlanNacionalQueryDto query)
        {
            try
            {
                var (items, total) = await _service.GetPagedAsync(query);
                var data = _mapper.Map<List<MacroPlanNacionalDto>>(items);

                var paginated = new PaginatedResponse<MacroPlanNacionalDto>(data, total, query.PageNumber, query.PageSize);
                return Ok(ApiPaginatedResponse<MacroPlanNacionalDto>.SuccessWith(paginated, "Planes nacionales obtenidos"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener planes nacionales");
                return StatusCode(500, ApiPaginatedResponse<MacroPlanNacionalDto>.Failure("Error interno del servidor"));
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<MacroPlanNacionalDto>>> GetById(int id)
        {
            try
            {
                var entity = await _service.GetByIdAsync(id);
                if (entity == null)
                {
                    return NotFound(ApiResponse<MacroPlanNacionalDto>.FailureWith("Plan nacional no encontrado"));
                }

                return Ok(ApiResponse<MacroPlanNacionalDto>.SuccessWith(_mapper.Map<MacroPlanNacionalDto>(entity)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener plan nacional {PlanNacionalId}", id);
                return StatusCode(500, ApiResponse<MacroPlanNacionalDto>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpGet("{id:int}/jerarquia")]
        public async Task<ActionResult<ApiResponse<MacroPlanNacionalDetalleDto>>> GetJerarquia(int id)
        {
            try
            {
                var entity = await _service.GetByIdWithObjetivosAsync(id);
                if (entity == null)
                {
                    return NotFound(ApiResponse<MacroPlanNacionalDetalleDto>.FailureWith("Plan nacional no encontrado"));
                }

                return Ok(ApiResponse<MacroPlanNacionalDetalleDto>.SuccessWith(_mapper.Map<MacroPlanNacionalDetalleDto>(entity)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener jerarquía del plan nacional {PlanNacionalId}", id);
                return StatusCode(500, ApiResponse<MacroPlanNacionalDetalleDto>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpGet("resumen")]
        public async Task<ActionResult<ApiResponse<MacroPlanNacionalResumenDto>>> GetResumen()
        {
            try
            {
                var data = await _service.GetResumenAsync();
                return Ok(ApiResponse<MacroPlanNacionalResumenDto>.SuccessWith(data, "Resumen generado"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar resumen de macro planificación");
                return StatusCode(500, ApiResponse<MacroPlanNacionalResumenDto>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpPost("crear")]
        public async Task<ActionResult<ApiResponse<MacroPlanNacionalDto>>> Create([FromBody] MacroPlanNacionalCreateDto dto)
        {
            try
            {
                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var entity = await _service.CreateAsync(dto, actorId ?? string.Empty);
                var response = _mapper.Map<MacroPlanNacionalDto>(entity);

                return CreatedAtAction(nameof(GetById), new { id = entity.PlanNacionalId },
                    ApiResponse<MacroPlanNacionalDto>.SuccessWith(response, "Plan nacional creado exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<MacroPlanNacionalDto>.FailureWith(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear plan nacional");
                return StatusCode(500, ApiResponse<MacroPlanNacionalDto>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<MacroPlanNacionalDto>>> Update(int id, [FromBody] MacroPlanNacionalUpdateDto dto)
        {
            try
            {
                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var entity = await _service.UpdateAsync(id, dto, actorId ?? string.Empty);
                if (entity == null)
                {
                    return NotFound(ApiResponse<MacroPlanNacionalDto>.FailureWith("Plan nacional no encontrado"));
                }

                return Ok(ApiResponse<MacroPlanNacionalDto>.SuccessWith(_mapper.Map<MacroPlanNacionalDto>(entity), "Plan nacional actualizado exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<MacroPlanNacionalDto>.FailureWith(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar plan nacional {PlanNacionalId}", id);
                return StatusCode(500, ApiResponse<MacroPlanNacionalDto>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<string>>> Delete(int id)
        {
            try
            {
                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var deleted = await _service.DeleteAsync(id, actorId ?? string.Empty);
                if (!deleted)
                {
                    return NotFound(ApiResponse<string>.FailureWith("Plan nacional no encontrado"));
                }

                return Ok(ApiResponse<string>.Succeeded("Plan nacional eliminado exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<string>.FailureWith(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar plan nacional {PlanNacionalId}", id);
                return StatusCode(500, ApiResponse<string>.FailureWith("Error interno del servidor"));
            }
        }
    }
}