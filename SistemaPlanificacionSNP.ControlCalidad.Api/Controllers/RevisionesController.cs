using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPlanificacionSNP.Infrastructure.Common;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.Services;

namespace SistemaPlanificacionSNP.ControlCalidad.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RevisionesController : ControllerBase
    {
        private readonly IRevisioneControlCalidadService _service;
        private readonly IMapper _mapper;
        private readonly ILogger<RevisionesController> _logger;

        public RevisionesController(
            IRevisioneControlCalidadService service,
            IMapper mapper,
            ILogger<RevisionesController> logger)
        {
            _service = service;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiPaginatedResponse<RevisioneDto>>> GetAll([FromQuery] RevisioneQueryDto query)
        {
            try
            {
                var (items, total) = await _service.GetPagedAsync(query);
                var data = _mapper.Map<List<RevisioneDto>>(items);

                var paginated = new PaginatedResponse<RevisioneDto>(data, total, query.PageNumber, query.PageSize);
                return Ok(ApiPaginatedResponse<RevisioneDto>.SuccessWith(paginated, "Revisiones obtenidas"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener revisiones");
                return StatusCode(500, ApiPaginatedResponse<RevisioneDto>.Failure("Error interno del servidor"));
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<RevisioneDto>>> GetById(int id)
        {
            try
            {
                var revisione = await _service.GetByIdAsync(id, includeAuditorias: true);
                if (revisione == null)
                {
                    return NotFound(ApiResponse<RevisioneDto>.FailureWith("Revisión no encontrada"));
                }

                return Ok(ApiResponse<RevisioneDto>.SuccessWith(_mapper.Map<RevisioneDto>(revisione)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener revisión {RevisionId}", id);
                return StatusCode(500, ApiResponse<RevisioneDto>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpGet("codigo/{codigo}")]
        public async Task<ActionResult<ApiResponse<RevisioneDto>>> GetByCodigo(string codigo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigo))
                {
                    return BadRequest(ApiResponse<RevisioneDto>.FailureWith("El código es requerido"));
                }

                var revisione = await _service.GetByCodigoAsync(codigo);
                if (revisione == null)
                {
                    return NotFound(ApiResponse<RevisioneDto>.FailureWith("Revisión no encontrada"));
                }

                return Ok(ApiResponse<RevisioneDto>.SuccessWith(_mapper.Map<RevisioneDto>(revisione)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener revisión por código {Codigo}", codigo);
                return StatusCode(500, ApiResponse<RevisioneDto>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpPost("crear")]
        public async Task<ActionResult<ApiResponse<RevisioneDto>>> Create([FromBody] RevisioneCreateDto dto)
        {
            try
            {
                var created = await _service.CreateAsync(dto);
                var responseDto = _mapper.Map<RevisioneDto>(created);

                return CreatedAtAction(nameof(GetById), new { id = created.RevisionId },
                    ApiResponse<RevisioneDto>.SuccessWith(responseDto, "Revisión creada exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<RevisioneDto>.FailureWith(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear revisión");
                return StatusCode(500, ApiResponse<RevisioneDto>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<RevisioneDto>>> Update(int id, [FromBody] RevisioneUpdateDto dto)
        {
            try
            {
                var updated = await _service.UpdateAsync(id, dto);
                if (updated == null)
                {
                    return NotFound(ApiResponse<RevisioneDto>.FailureWith("Revisión no encontrada"));
                }

                return Ok(ApiResponse<RevisioneDto>.SuccessWith(_mapper.Map<RevisioneDto>(updated), "Revisión actualizada exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<RevisioneDto>.FailureWith(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar revisión {RevisionId}", id);
                return StatusCode(500, ApiResponse<RevisioneDto>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<string>>> Delete(int id)
        {
            try
            {
                var deleted = await _service.DeleteAsync(id);
                if (!deleted)
                {
                    return NotFound(ApiResponse<string>.FailureWith("Revisión no encontrada"));
                }

                return Ok(ApiResponse<string>.Succeeded("Revisión eliminada exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<string>.FailureWith(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar revisión {RevisionId}", id);
                return StatusCode(500, ApiResponse<string>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<ApiResponse<ControlCalidadDashboardDto>>> GetDashboard()
        {
            try
            {
                var dashboard = await _service.GetDashboardAsync();
                return Ok(ApiResponse<ControlCalidadDashboardDto>.SuccessWith(dashboard, "Dashboard generado"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar dashboard");
                return StatusCode(500, ApiResponse<ControlCalidadDashboardDto>.FailureWith("Error interno del servidor"));
            }
        }
    }
}
