using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SistemaPlanificacionSNP.Infrastructure.Common;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.Services;

namespace SistemaPlanificacionSNP.ControlCalidad.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AuditoriasController : ControllerBase
    {
        private readonly IAuditoriaControlCalidadService _service;
        private readonly IMapper _mapper;
        private readonly ILogger<AuditoriasController> _logger;

        public AuditoriasController(
            IAuditoriaControlCalidadService service,
            IMapper mapper,
            ILogger<AuditoriasController> logger)
        {
            _service = service;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<ApiPaginatedResponse<AuditoriaDto>>> GetAll([FromQuery] AuditoriaQueryDto query)
        {
            try
            {
                var (items, total) = await _service.GetPagedAsync(query);
                var data = _mapper.Map<List<AuditoriaDto>>(items);

                var paginated = new PaginatedResponse<AuditoriaDto>(data, total, query.PageNumber, query.PageSize);
                return Ok(ApiPaginatedResponse<AuditoriaDto>.SuccessWith(paginated, "Auditorías obtenidas"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener auditorías");
                return StatusCode(500, ApiPaginatedResponse<AuditoriaDto>.Failure("Error interno del servidor"));
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<AuditoriaDto>>> GetById(int id)
        {
            try
            {
                var auditoria = await _service.GetByIdAsync(id);
                if (auditoria == null)
                {
                    return NotFound(ApiResponse<AuditoriaDto>.FailureWith("Auditoría no encontrada"));
                }

                return Ok(ApiResponse<AuditoriaDto>.SuccessWith(_mapper.Map<AuditoriaDto>(auditoria)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener auditoría {AuditoriaId}", id);
                return StatusCode(500, ApiResponse<AuditoriaDto>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpGet("revision/{revisionId:int}")]
        public async Task<ActionResult<ApiResponse<List<AuditoriaDto>>>> GetByRevisionId(int revisionId)
        {
            try
            {
                var auditorias = await _service.GetByRevisionIdAsync(revisionId);
                var data = _mapper.Map<List<AuditoriaDto>>(auditorias);
                return Ok(ApiResponse<List<AuditoriaDto>>.SuccessWith(data, "Auditorías por revisión obtenidas"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener auditorías de revisión {RevisionId}", revisionId);
                return StatusCode(500, ApiResponse<List<AuditoriaDto>>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpPost("crear")]
        public async Task<ActionResult<ApiResponse<AuditoriaDto>>> Create([FromBody] AuditoriaCreateDto dto)
        {
            try
            {
                var responsable = BuildResponsableFromClaims(User);
                if (string.IsNullOrWhiteSpace(responsable))
                {
                    return Unauthorized(ApiResponse<AuditoriaDto>.FailureWith("No se pudo determinar la identidad del usuario desde el token"));
                }

                var created = await _service.CreateAsync(dto, responsable);
                return CreatedAtAction(nameof(GetById), new { id = created.AuditoriaId },
                    ApiResponse<AuditoriaDto>.SuccessWith(_mapper.Map<AuditoriaDto>(created), "Auditoría creada exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<AuditoriaDto>.FailureWith(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear auditoría");
                return StatusCode(500, ApiResponse<AuditoriaDto>.FailureWith("Error interno del servidor"));
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<AuditoriaDto>>> Update(int id, [FromBody] AuditoriaUpdateDto dto)
        {
            try
            {
                var updated = await _service.UpdateAsync(id, dto);
                if (updated == null)
                {
                    return NotFound(ApiResponse<AuditoriaDto>.FailureWith("Auditoría no encontrada"));
                }

                return Ok(ApiResponse<AuditoriaDto>.SuccessWith(_mapper.Map<AuditoriaDto>(updated), "Auditoría actualizada exitosamente"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<AuditoriaDto>.FailureWith(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar auditoría {AuditoriaId}", id);
                return StatusCode(500, ApiResponse<AuditoriaDto>.FailureWith("Error interno del servidor"));
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
                    return NotFound(ApiResponse<string>.FailureWith("Auditoría no encontrada"));
                }

                return Ok(ApiResponse<string>.Succeeded("Auditoría eliminada exitosamente"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar auditoría {AuditoriaId}", id);
                return StatusCode(500, ApiResponse<string>.FailureWith("Error interno del servidor"));
            }
        }

        private static string BuildResponsableFromClaims(ClaimsPrincipal user)
        {
            var nombre = user.FindFirst("Nombre")?.Value;
            var apellido = user.FindFirst("Apellido")?.Value;
            var username = user.FindFirst(ClaimTypes.Name)?.Value;

            var fullName = $"{nombre} {apellido}".Trim();
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                return fullName.Length > 120 ? fullName[..120] : fullName;
            }

            if (!string.IsNullOrWhiteSpace(username))
            {
                return username.Length > 120 ? username[..120] : username;
            }

            return string.Empty;
        }
    }
}
