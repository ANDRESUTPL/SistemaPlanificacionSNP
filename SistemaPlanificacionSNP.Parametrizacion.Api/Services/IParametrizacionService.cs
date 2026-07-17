using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.Parametrizacion;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using SistemaPlanificacionSNP.Infrastructure.UnitOfWork;

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
		private readonly ApplicationDbContext _context;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;

		public ParametrizacionService(ApplicationDbContext context, IUnitOfWork unitOfWork, IMapper mapper)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
			_unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
			_mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
		}

		// --- CATÁLOGOS ---

		public async Task<List<CatalogoDto>> GetCatalogosAsync()
		{
			// Usamos ApplicationDbContext directamente para poder hacer .Include
			var catalogos = await _context.Set<Catalogo>()
				.Include(c => c.Items.Where(i => i.Activo).OrderBy(i => i.Orden))
				.OrderBy(c => c.Nombre)
				.ToListAsync();

			return _mapper.Map<List<CatalogoDto>>(catalogos);
		}

		public async Task<CatalogoDto?> GetCatalogoByCodigoAsync(string codigo)
		{
			var catalogo = await _context.Set<Catalogo>()
				.Include(c => c.Items.Where(i => i.Activo).OrderBy(i => i.Orden))
				.FirstOrDefaultAsync(c => c.Codigo == codigo);

			return catalogo == null ? null : _mapper.Map<CatalogoDto>(catalogo);
		}

		public async Task<CatalogoDto> CreateCatalogoAsync(CatalogoCreateDto dto)
		{
			var repo = _unitOfWork.GetRepository<Catalogo>();

			if (await repo.AnyAsync(c => c.Codigo == dto.Codigo))
			{
				throw new InvalidOperationException("Ya existe un catálogo con este código.");
			}

			var catalogo = _mapper.Map<Catalogo>(dto);
			catalogo.FechaCreacion = DateTime.UtcNow;
			catalogo.Activo = true;

			await repo.AddAsync(catalogo);
			await _unitOfWork.SaveChangesAsync();

			return _mapper.Map<CatalogoDto>(catalogo);
		}

		public async Task<ItemCatalogoDto> CreateItemCatalogoAsync(ItemCatalogoCreateDto dto)
		{
			var catalogoRepo = _unitOfWork.GetRepository<Catalogo>();
			var itemRepo = _unitOfWork.GetRepository<ItemCatalogo>();

			if (!await catalogoRepo.AnyAsync(c => c.CatalogoId == dto.CatalogoId))
			{
				throw new InvalidOperationException("El catálogo padre no existe.");
			}

			var item = _mapper.Map<ItemCatalogo>(dto);
			item.FechaCreacion = DateTime.UtcNow;
			item.Activo = true;

			await itemRepo.AddAsync(item);
			await _unitOfWork.SaveChangesAsync();

			return _mapper.Map<ItemCatalogoDto>(item);
		}

		// --- INSTITUCIONES Y PERIODOS ---

		public async Task<List<PeriodoPlanificacionDto>> GetPeriodosAsync()
		{
			var repo = _unitOfWork.GetRepository<PeriodoPlanificacion>();
			// Obtenemos todos y los ordenamos en memoria para simplificar (o crear método en repo)
			var periodos = await repo.FindAsync(p => true);
			return _mapper.Map<List<PeriodoPlanificacionDto>>(periodos.OrderByDescending(p => p.FechaInicio));
		}

		public async Task<PeriodoPlanificacionDto> CreatePeriodoAsync(PeriodoPlanificacionCreateUpdateDto dto)
		{
			var repo = _unitOfWork.GetRepository<PeriodoPlanificacion>();

			var periodo = _mapper.Map<PeriodoPlanificacion>(dto);
			periodo.FechaCreacion = DateTime.UtcNow;

			await repo.AddAsync(periodo);
			await _unitOfWork.SaveChangesAsync();

			return _mapper.Map<PeriodoPlanificacionDto>(periodo);
		}

		public async Task<List<EntidadPublicaDto>> GetEntidadesAsync()
		{
			var entidades = await _context.Set<EntidadPublica>()
				.Include(e => e.PeriodoPlanificacion)
				.OrderBy(e => e.Nombre)
				.ToListAsync();

			return _mapper.Map<List<EntidadPublicaDto>>(entidades);
		}

		public async Task<EntidadPublicaDto> CreateEntidadAsync(EntidadPublicaCreateUpdateDto dto)
		{
			var repo = _unitOfWork.GetRepository<EntidadPublica>();

			var entidad = _mapper.Map<EntidadPublica>(dto);
			entidad.FechaCreacion = DateTime.UtcNow;
			entidad.Activo = true;

			// Nota: Si el DTO no envía PeriodoPlanificacionId y Codigo, asegúrate de asignarlos
			// aquí antes de guardar, ya que la base de datos los requerirá.
			if (entidad.PeriodoPlanificacionId == 0) entidad.PeriodoPlanificacionId = 1; // Valor por defecto si aplica
			if (string.IsNullOrEmpty(entidad.Codigo)) entidad.Codigo = Guid.NewGuid().ToString().Substring(0, 8);

			await repo.AddAsync(entidad);
			await _unitOfWork.SaveChangesAsync();

			return _mapper.Map<EntidadPublicaDto>(entidad);
		}
	}
}