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
	public class ProyectosInversionController : ControllerBase
	{
		private readonly IProyectosInversionPiService _service;
		private readonly IMapper _mapper;
		private readonly ILogger<ProyectosInversionController> _logger;

		public ProyectosInversionController(
			IProyectosInversionPiService service,
			IMapper mapper,
			ILogger<ProyectosInversionController> logger)
		{
			_service = service;
			_mapper = mapper;
			_logger = logger;
		}

		[HttpGet]
		public async Task<ActionResult<ApiPaginatedResponse<ProyectosInversionReadDto>>> GetAll([FromQuery] ProyectosInversionQueryDto query)
		{
			try
			{
				var (items, total) = await _service.GetPagedAsync(query);
				var data = _mapper.Map<List<ProyectosInversionReadDto>>(items);
				var paginated = new PaginatedResponse<ProyectosInversionReadDto>(data, total, query.PageNumber, query.PageSize);
				return Ok(ApiPaginatedResponse<ProyectosInversionReadDto>.SuccessWith(paginated, "Proyectos obtenidos"));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al obtener proyectos de inversión");
				return StatusCode(500, ApiPaginatedResponse<ProyectosInversionReadDto>.Failure("Error interno del servidor"));
			}
		}

		[HttpGet("{id:int}")]
		public async Task<ActionResult<ApiResponse<ProyectosInversionDetailDto>>> GetById(int id)
		{
			try
			{
				var entity = await _service.GetByIdAsync(id, includePlan: true);
				if (entity == null)
				{
					return NotFound(ApiResponse<ProyectosInversionDetailDto>.FailureWith("Proyecto no encontrado"));
				}

				return Ok(ApiResponse<ProyectosInversionDetailDto>.SuccessWith(_mapper.Map<ProyectosInversionDetailDto>(entity)));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al obtener proyecto {ProyectoId}", id);
				return StatusCode(500, ApiResponse<ProyectosInversionDetailDto>.FailureWith("Error interno del servidor"));
			}
		}

		[HttpPost("crear")]
		public async Task<ActionResult<ApiResponse<ProyectosInversionReadDto>>> Create([FromBody] ProyectosInversionCreateDto dto)
		{
			try
			{
				var entity = await _service.CreateAsync(dto);
				var response = _mapper.Map<ProyectosInversionReadDto>(entity);

				return CreatedAtAction(nameof(GetById), new { id = entity.ProyectoInversionId },
					ApiResponse<ProyectosInversionReadDto>.SuccessWith(response, "Proyecto creado exitosamente"));
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(ApiResponse<ProyectosInversionReadDto>.FailureWith(ex.Message));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al crear proyecto");
				return StatusCode(500, ApiResponse<ProyectosInversionReadDto>.FailureWith("Error interno del servidor"));
			}
		}

		[HttpPut("{id:int}")]
		public async Task<ActionResult<ApiResponse<ProyectosInversionReadDto>>> Update(int id, [FromBody] ProyectosInversionUpdateDto dto)
		{
			try
			{
				var entity = await _service.UpdateAsync(id, dto);
				if (entity == null)
				{
					return NotFound(ApiResponse<ProyectosInversionReadDto>.FailureWith("Proyecto no encontrado"));
				}

				return Ok(ApiResponse<ProyectosInversionReadDto>.SuccessWith(_mapper.Map<ProyectosInversionReadDto>(entity),
					"Proyecto actualizado exitosamente"));
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(ApiResponse<ProyectosInversionReadDto>.FailureWith(ex.Message));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al actualizar proyecto {ProyectoId}", id);
				return StatusCode(500, ApiResponse<ProyectosInversionReadDto>.FailureWith("Error interno del servidor"));
			}
		}

		[HttpPost("{id:int}/respaldos-ejecucion")]
		[RequestSizeLimit(50 * 1024 * 1024)]
		public async Task<ActionResult<ApiResponse<List<RespaldoEjecucionReadDto>>>> UploadRespaldos(int id, [FromForm] List<IFormFile> archivos)
		{
			try
			{
				if (archivos == null || archivos.Count == 0)
				{
					return BadRequest(ApiResponse<List<RespaldoEjecucionReadDto>>.FailureWith("Debe seleccionar al menos un respaldo."));
				}

				var createDtos = archivos.Select(archivo => new RespaldoEjecucionCreateDto
				{
					NombreArchivo = archivo.FileName,
					TipoContenido = archivo.ContentType,
					TamanoBytes = archivo.Length,
					Contenido = archivo.OpenReadStream()
				});
				var respaldos = await _service.AddRespaldosAsync(id, createDtos);
				return Ok(ApiResponse<List<RespaldoEjecucionReadDto>>.SuccessWith(_mapper.Map<List<RespaldoEjecucionReadDto>>(respaldos), "Respaldos cargados exitosamente"));
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(ApiResponse<List<RespaldoEjecucionReadDto>>.FailureWith(ex.Message));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al cargar respaldos del proyecto {ProyectoId}", id);
				return StatusCode(500, ApiResponse<List<RespaldoEjecucionReadDto>>.FailureWith("Error interno al cargar los respaldos"));
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
					return NotFound(ApiResponse<string>.FailureWith("Proyecto no encontrado"));
				}

				return Ok(ApiResponse<string>.Succeeded("Proyecto inactivado exitosamente"));
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(ApiResponse<string>.FailureWith(ex.Message));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al inactivar proyecto {ProyectoId}", id);
				return StatusCode(500, ApiResponse<string>.FailureWith("Error interno del servidor"));
			}
		}
	}
}
