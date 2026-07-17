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
	public class CatalogosController : ControllerBase
	{
		private readonly IParametrizacionService _service;
		private readonly ILogger<CatalogosController> _logger;

		public CatalogosController(IParametrizacionService service, ILogger<CatalogosController> logger)
		{
			_service = service ?? throw new ArgumentNullException(nameof(service));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		[HttpGet]
		public async Task<ActionResult<ApiResponse<List<CatalogoDto>>>> GetAll()
		{
			try
			{
				var data = await _service.GetCatalogosAsync();
				return Ok(ApiResponse<List<CatalogoDto>>.SuccessWith(data, "Catálogos obtenidos exitosamente."));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al obtener catálogos");
				return StatusCode(500, ApiResponse<List<CatalogoDto>>.FailureWith("Error interno del servidor"));
			}
		}

		[HttpGet("{codigo}")]
		public async Task<ActionResult<ApiResponse<CatalogoDto>>> GetByCodigo(string codigo)
		{
			try
			{
				var data = await _service.GetCatalogoByCodigoAsync(codigo);
				if (data == null) return NotFound(ApiResponse<CatalogoDto>.FailureWith("Catálogo no encontrado."));

				return Ok(ApiResponse<CatalogoDto>.SuccessWith(data));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al obtener catálogo {Codigo}", codigo);
				return StatusCode(500, ApiResponse<CatalogoDto>.FailureWith("Error interno del servidor"));
			}
		}

		[HttpPost]
		public async Task<ActionResult<ApiResponse<CatalogoDto>>> Create([FromBody] CatalogoCreateDto dto)
		{
			try
			{
				var data = await _service.CreateCatalogoAsync(dto);
				return Ok(ApiResponse<CatalogoDto>.SuccessWith(data, "Catálogo creado exitosamente."));
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(ApiResponse<CatalogoDto>.FailureWith(ex.Message));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al crear catálogo");
				return StatusCode(500, ApiResponse<CatalogoDto>.FailureWith("Error interno del servidor"));
			}
		}

		[HttpPost("items")]
		public async Task<ActionResult<ApiResponse<ItemCatalogoDto>>> CreateItem([FromBody] ItemCatalogoCreateDto dto)
		{
			try
			{
				var data = await _service.CreateItemCatalogoAsync(dto);
				return Ok(ApiResponse<ItemCatalogoDto>.SuccessWith(data, "Ítem creado exitosamente."));
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(ApiResponse<ItemCatalogoDto>.FailureWith(ex.Message));
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error al crear ítem de catálogo");
				return StatusCode(500, ApiResponse<ItemCatalogoDto>.FailureWith("Error interno del servidor"));
			}
		}
	}
}