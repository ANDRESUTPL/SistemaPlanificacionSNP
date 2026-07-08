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
    public class ObjetivosEstrategicosController : ControllerBase
    {
        private readonly IMacroObjetivoEstrategicoService _service;
        private readonly IMapper _mapper;
        private readonly ILogger<ObjetivosEstrategicosController> _logger;

        public ObjetivosEstrategicosController(
            IMacroObjetivoEstrategicoService service,
            IMapper mapper,
            ILogger<ObjetivosEstrategicosController> logger)
        {
            _service = service;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiPaginatedResponse<MacroObjetivoEstrategicoDto>>> GetAll([FromQuery] MacroObjetivoEstrategicoQueryDto query)
        {
            try
            {
                var (items, total) = await _service.GetPagedAsync(query);
                var data = _mapper.Map<List<MacroObjetivoEstrategicoDto>>(items);

                var paginated = new PaginatedResponse<MacroObjetivoEstrategicoDto>(data, total, query.PageNumber, query.PageSize);
                return Ok(ApiPaginatedResponse<MacroObjetivoEstrategicoDto>.SuccessWith(paginated, "Objetivos estratégicos obtenidos"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener objetivos estratégicos");
                return StatusCode(500, ApiPaginatedResponse<MacroObjetivoEstrategicoDto>.Failure("Error interno del servidor"));
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<MacroObjetivoEstrategicoDto>>> GetById(int id)
        {
            try
            {
                var entity = await _service.GetByIdAsync(id);
                if (entity == null)
                {
                    return NotFound(ApiResponse<MacroObjetivoEstrategicoDto>.FailureWith("Objetivo estratégico no encontrado"));
                }

                return Ok(ApiResponse<MacroObjetivoEstrategicoDto>.SuccessWith(_mapper.Map<MacroObjetivoEstrategicoDto>(entity)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener objetivo estratégico {ObjetivoEstrategicoId}", id);
                return StatusCode(500, ApiResponse<MacroObjetivoEstrategicoDto>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpGet("plan/{planNacionalId:int}")]
        public async Task<ActionResult<ApiResponse<List<MacroObjetivoEstrategicoDto>>>> GetByPlanNacionalId(int planNacionalId)
        {
            try
            {
                var items = await _service.GetByPlanNacionalIdAsync(planNacionalId);
                var data = _mapper.Map<List<MacroObjetivoEstrategicoDto>>(items);

                return Ok(ApiResponse<List<MacroObjetivoEstrategicoDto>>.SuccessWith(data, "Objetivos estratégicos obtenidos"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener objetivos por plan nacional {PlanNacionalId}", planNacionalId);
                return StatusCode(500, ApiResponse<List<MacroObjetivoEstrategicoDto>>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpPost("crear")]
        public async Task<ActionResult<ApiResponse<MacroObjetivoEstrategicoDto>>> Create([FromBody] MacroObjetivoEstrategicoCreateDto dto)
        {
            try
            {
                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var entity = await _service.CreateAsync(dto, actorId ?? string.Empty);
                var response = _mapper.Map<MacroObjetivoEstrategicoDto>(entity);

                return CreatedAtAction(nameof(GetById), new { id = entity.ObjetivoEstrategicoId },
                    ApiResponse<MacroObjetivoEstrategicoDto>.SuccessWith(response, "Objetivo estratégico creado exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<MacroObjetivoEstrategicoDto>.FailureWith(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear objetivo estratégico");
                return StatusCode(500, ApiResponse<MacroObjetivoEstrategicoDto>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<MacroObjetivoEstrategicoDto>>> Update(int id, [FromBody] MacroObjetivoEstrategicoUpdateDto dto)
        {
            try
            {
                var actorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var entity = await _service.UpdateAsync(id, dto, actorId ?? string.Empty);
                if (entity == null)
                {
                    return NotFound(ApiResponse<MacroObjetivoEstrategicoDto>.FailureWith("Objetivo estratégico no encontrado"));
                }

                return Ok(ApiResponse<MacroObjetivoEstrategicoDto>.SuccessWith(_mapper.Map<MacroObjetivoEstrategicoDto>(entity), "Objetivo estratégico actualizado exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<MacroObjetivoEstrategicoDto>.FailureWith(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar objetivo estratégico {ObjetivoEstrategicoId}", id);
                return StatusCode(500, ApiResponse<MacroObjetivoEstrategicoDto>.FailureWith("Error interno del servidor"));
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
                    return NotFound(ApiResponse<string>.FailureWith("Objetivo estratégico no encontrado"));
                }

                return Ok(ApiResponse<string>.Succeeded("Objetivo estratégico eliminado exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<string>.FailureWith(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar objetivo estratégico {ObjetivoEstrategicoId}", id);
                return StatusCode(500, ApiResponse<string>.FailureWith("Error interno del servidor"));
            }
        }
    }
}