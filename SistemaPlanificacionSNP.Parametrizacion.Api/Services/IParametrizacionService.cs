using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.Parametrizacion;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.DTOs;


namespace SistemaPlanificacionSNP.Parametrizacion.Api.Services
{
	public interface IParametrizacionService
	{
		Task<List<CatalogoDto>> GetCatalogosAsync();
		Task<CatalogoDto?> GetCatalogoByCodigoAsync(string codigo);
		Task<CatalogoDto> CreateCatalogoAsync(CatalogoCreateDto dto);
		Task<ItemCatalogoDto> CreateItemCatalogoAsync(ItemCatalogoCreateDto dto);

		Task<List<PeriodoPlanificacionDto>> GetPeriodosAsync();
		Task<PeriodoPlanificacionDto> CreatePeriodoAsync(PeriodoPlanificacionCreateUpdateDto dto);
		Task<List<EntidadPublicaDto>> GetEntidadesAsync();
		Task<EntidadPublicaDto> CreateEntidadAsync(EntidadPublicaCreateUpdateDto dto);
	}

	public class ParametrizacionService : IParametrizacionService
	{
		private readonly ParametrizacionDbContext _context;
		private readonly IMapper _mapper;

		// Inyectamos nuestro nuevo ParametrizacionDbContext
		public ParametrizacionService(ParametrizacionDbContext context, IMapper mapper)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
			_mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
		}

		// --- CATÁLOGOS ---

		public async Task<List<CatalogoDto>> GetCatalogosAsync()
		{
			var catalogos = await _context.Catalogos
				.Include(c => c.Items.Where(i => i.Activo).OrderBy(i => i.Orden))
				.OrderBy(c => c.Nombre)
				.ToListAsync();

			return _mapper.Map<List<CatalogoDto>>(catalogos);
		}

		public async Task<CatalogoDto?> GetCatalogoByCodigoAsync(string codigo)
		{
			var catalogo = await _context.Catalogos
				.Include(c => c.Items.Where(i => i.Activo).OrderBy(i => i.Orden))
				.FirstOrDefaultAsync(c => c.Codigo == codigo);

			return catalogo == null ? null : _mapper.Map<CatalogoDto>(catalogo);
		}

		public async Task<CatalogoDto> CreateCatalogoAsync(CatalogoCreateDto dto)
		{
			if (await _context.Catalogos.AnyAsync(c => c.Codigo == dto.Codigo))
			{
				throw new InvalidOperationException("Ya existe un catálogo con este código.");
			}

			var catalogo = _mapper.Map<Catalogo>(dto);
			catalogo.FechaCreacion = DateTime.UtcNow;
			catalogo.Activo = true;

			await _context.Catalogos.AddAsync(catalogo);
			await _context.SaveChangesAsync();

			return _mapper.Map<CatalogoDto>(catalogo);
		}

		public async Task<ItemCatalogoDto> CreateItemCatalogoAsync(ItemCatalogoCreateDto dto)
		{
			if (!await _context.Catalogos.AnyAsync(c => c.CatalogoId == dto.CatalogoId))
			{
				throw new InvalidOperationException("El catálogo padre no existe.");
			}

			var item = _mapper.Map<ItemCatalogo>(dto);
			item.FechaCreacion = DateTime.UtcNow;
			item.Activo = true;

			await _context.ItemsCatalogo.AddAsync(item);
			await _context.SaveChangesAsync();

			return _mapper.Map<ItemCatalogoDto>(item);
		}

		// --- INSTITUCIONES Y PERIODOS ---

		public async Task<List<PeriodoPlanificacionDto>> GetPeriodosAsync()
		{
			var periodos = await _context.PeriodosPlanificacion
				.OrderByDescending(p => p.FechaInicio)
				.ToListAsync();

			return _mapper.Map<List<PeriodoPlanificacionDto>>(periodos);
		}

		public async Task<PeriodoPlanificacionDto> CreatePeriodoAsync(PeriodoPlanificacionCreateUpdateDto dto)
		{
			var periodo = _mapper.Map<PeriodoPlanificacion>(dto);
			periodo.FechaCreacion = DateTime.UtcNow;

			await _context.PeriodosPlanificacion.AddAsync(periodo);
			await _context.SaveChangesAsync();

			return _mapper.Map<PeriodoPlanificacionDto>(periodo);
		}

		public async Task<List<EntidadPublicaDto>> GetEntidadesAsync()
		{
			var entidades = await _context.EntidadesPublicas
				.Include(e => e.PeriodoPlanificacion)
				.OrderBy(e => e.Nombre)
				.ToListAsync();

			return _mapper.Map<List<EntidadPublicaDto>>(entidades);
		}

		public async Task<EntidadPublicaDto> CreateEntidadAsync(EntidadPublicaCreateUpdateDto dto)
		{
			var entidad = _mapper.Map<EntidadPublica>(dto);
			entidad.FechaCreacion = DateTime.UtcNow;
			entidad.Activo = true;

			if (entidad.PeriodoPlanificacionId == 0) entidad.PeriodoPlanificacionId = 1;
			if (string.IsNullOrEmpty(entidad.Codigo)) entidad.Codigo = Guid.NewGuid().ToString().Substring(0, 8);

			await _context.EntidadesPublicas.AddAsync(entidad);
			await _context.SaveChangesAsync();

			return _mapper.Map<EntidadPublicaDto>(entidad);
		}
	}
}