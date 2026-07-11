using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPlanificacionSNP.Infrastructure.Common;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.Services;

namespace SistemaPlanificacionSNP.PlanificacionInstitucional.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PlanesEstrategicosController : ControllerBase
    {
        private readonly IPlanesEstrategicosPiService _service;
        private readonly IMapper _mapper;
        private readonly ILogger<PlanesEstrategicosController> _logger;

        public PlanesEstrategicosController(
            IPlanesEstrategicosPiService service,
            IMapper mapper,
            ILogger<PlanesEstrategicosController> logger)
        {
            _service = service;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiPaginatedResponse<PlanesEstrategicoReadDto>>> GetAll([FromQuery] PlanesEstrategicoQueryDto query)
        {
            try
            {
                var (items, total) = await _service.GetPagedAsync(query);
                var data = _mapper.Map<List<PlanesEstrategicoReadDto>>(items);
                var paginated = new PaginatedResponse<PlanesEstrategicoReadDto>(data, total, query.PageNumber, query.PageSize);
                return Ok(ApiPaginatedResponse<PlanesEstrategicoReadDto>.SuccessWith(paginated, "Planes obtenidos"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener planes estratégicos");
                return StatusCode(500, ApiPaginatedResponse<PlanesEstrategicoReadDto>.Failure("Error interno del servidor"));
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<PlanesEstrategicoDetailDto>>> GetById(int id)
        {
            try
            {
                var entity = await _service.GetByIdAsync(id, includeProyectos: true);
                if (entity == null)
                {
                    return NotFound(ApiResponse<PlanesEstrategicoDetailDto>.FailureWith("Plan no encontrado"));
                }

                var dto = _mapper.Map<PlanesEstrategicoDetailDto>(entity);
                return Ok(ApiResponse<PlanesEstrategicoDetailDto>.SuccessWith(dto));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener plan {PlanId}", id);
                return StatusCode(500, ApiResponse<PlanesEstrategicoDetailDto>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpPost("crear")]
        public async Task<ActionResult<ApiResponse<PlanesEstrategicoReadDto>>> Create([FromBody] PlanesEstrategicoCreateDto dto)
        {
            try
            {
                var entity = await _service.CreateAsync(dto);
                var response = _mapper.Map<PlanesEstrategicoReadDto>(entity);

                return CreatedAtAction(nameof(GetById), new { id = entity.PlanEstrategicoId },
                    ApiResponse<PlanesEstrategicoReadDto>.SuccessWith(response, "Plan creado exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<PlanesEstrategicoReadDto>.FailureWith(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear plan");
                return StatusCode(500, ApiResponse<PlanesEstrategicoReadDto>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<PlanesEstrategicoReadDto>>> Update(int id, [FromBody] PlanesEstrategicoUpdateDto dto)
        {
            try
            {
                var entity = await _service.UpdateAsync(id, dto);
                if (entity == null)
                {
                    return NotFound(ApiResponse<PlanesEstrategicoReadDto>.FailureWith("Plan no encontrado"));
                }

                return Ok(ApiResponse<PlanesEstrategicoReadDto>.SuccessWith(_mapper.Map<PlanesEstrategicoReadDto>(entity),
                    "Plan actualizado exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<PlanesEstrategicoReadDto>.FailureWith(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar plan {PlanId}", id);
                return StatusCode(500, ApiResponse<PlanesEstrategicoReadDto>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<string>>> Delete(int id)
        {
            try
            {
                var deleted = await _service.SoftDeleteAsync(id);
                if (!deleted)
                {
                    return NotFound(ApiResponse<string>.FailureWith("Plan no encontrado"));
                }

                return Ok(ApiResponse<string>.Succeeded("Plan inactivado exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<string>.FailureWith(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al inactivar plan {PlanId}", id);
                return StatusCode(500, ApiResponse<string>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<ApiResponse<PlanificacionInstitucionalDashboardDbFirstDto>>> GetDashboard()
        {
            try
            {
                var dashboard = await _service.GetDashboardAsync();
                return Ok(ApiResponse<PlanificacionInstitucionalDashboardDbFirstDto>.SuccessWith(dashboard, "Dashboard generado"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar dashboard de planificación institucional");
                return StatusCode(500, ApiResponse<PlanificacionInstitucionalDashboardDbFirstDto>.FailureWith("Error interno del servidor"));
            }
        }
    }
}
