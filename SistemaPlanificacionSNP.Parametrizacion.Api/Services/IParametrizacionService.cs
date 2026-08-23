using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.Parametrizacion;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.DTOs;
using System.Data;
using System.Data.Common;


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
		Task<PeriodoPlanificacionDto?> GetPeriodoByIdAsync(int periodoId);
		Task<PeriodoPlanificacionDto?> UpdatePeriodoAsync(int periodoId, PeriodoPlanificacionCreateUpdateDto dto);
		Task<bool> DeactivatePeriodoAsync(int periodoId);
		Task<List<EntidadPublicaDto>> GetEntidadesAsync();
		Task<EntidadPublicaDto> CreateEntidadAsync(EntidadPublicaCreateUpdateDto dto);
		Task<EntidadPublicaDto?> GetEntidadByIdAsync(int entidadId);
		Task<EntidadPublicaDto?> UpdateEntidadAsync(int entidadId, EntidadPublicaCreateUpdateDto dto);
		Task<bool> DeactivateEntidadAsync(int entidadId);
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
			ValidatePeriodoDto(dto);

			var codigo = dto.Codigo.Trim().ToUpperInvariant();
			if (await _context.PeriodosPlanificacion.AnyAsync(p => p.Codigo == codigo))
			{
				throw new InvalidOperationException("Ya existe un período con este código.");
			}

			var periodo = _mapper.Map<PeriodoPlanificacion>(dto);
			periodo.Codigo = codigo;
			periodo.Nombre = dto.Nombre.Trim();
			periodo.FechaCreacion = DateTime.UtcNow;

			await _context.PeriodosPlanificacion.AddAsync(periodo);
			await _context.SaveChangesAsync();

			return _mapper.Map<PeriodoPlanificacionDto>(periodo);
		}

		public async Task<PeriodoPlanificacionDto?> GetPeriodoByIdAsync(int periodoId)
		{
			var periodo = await _context.PeriodosPlanificacion
				.FirstOrDefaultAsync(p => p.PeriodoPlanificacionId == periodoId);

			return periodo == null ? null : _mapper.Map<PeriodoPlanificacionDto>(periodo);
		}

		public async Task<PeriodoPlanificacionDto?> UpdatePeriodoAsync(int periodoId, PeriodoPlanificacionCreateUpdateDto dto)
		{
			ValidatePeriodoDto(dto);

			var periodo = await _context.PeriodosPlanificacion
				.FirstOrDefaultAsync(p => p.PeriodoPlanificacionId == periodoId);

			if (periodo == null)
			{
				return null;
			}

			var codigo = dto.Codigo.Trim().ToUpperInvariant();
			if (!string.Equals(periodo.Codigo, codigo, StringComparison.OrdinalIgnoreCase)
				&& await _context.PeriodosPlanificacion.AnyAsync(p => p.Codigo == codigo && p.PeriodoPlanificacionId != periodoId))
			{
				throw new InvalidOperationException("Ya existe un período con este código.");
			}

			periodo.Codigo = codigo;
			periodo.Nombre = dto.Nombre.Trim();
			periodo.FechaInicio = dto.FechaInicio;
			periodo.FechaFin = dto.FechaFin;
			periodo.Activo = dto.Activo;

			await _context.SaveChangesAsync();

			return _mapper.Map<PeriodoPlanificacionDto>(periodo);
		}

		public async Task<bool> DeactivatePeriodoAsync(int periodoId)
		{
			var periodo = await _context.PeriodosPlanificacion
				.FirstOrDefaultAsync(p => p.PeriodoPlanificacionId == periodoId);

			if (periodo == null)
			{
				return false;
			}

			if (!periodo.Activo)
			{
				return true;
			}

			var hasEntidadesActivas = await _context.EntidadesPublicas
				.AnyAsync(e => e.PeriodoPlanificacionId == periodoId && e.Activo);

			if (hasEntidadesActivas)
			{
				throw new InvalidOperationException("No se puede inactivar el período porque tiene entidades activas asociadas.");
			}

			periodo.Activo = false;
			await _context.SaveChangesAsync();

			return true;
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
			ValidateEntidadDto(dto);

			var periodoId = await ResolvePeriodoForEntidadAsync(dto.PeriodoPlanificacionId);
			var codigo = await ResolveCodigoEntidadAsync(dto.Codigo, dto.Sigla, null);

			var entidad = _mapper.Map<EntidadPublica>(dto);
			entidad.Codigo = codigo;
			entidad.Nombre = dto.Nombre.Trim();
			entidad.Sigla = dto.Sigla.Trim().ToUpperInvariant();
			entidad.Tipo = dto.Tipo.Trim();
			entidad.NivelGobierno = dto.NivelGobierno.Trim();
			entidad.Mision = (dto.Mision ?? string.Empty).Trim();
			entidad.PeriodoPlanificacionId = periodoId;
			entidad.FechaCreacion = DateTime.UtcNow;
			entidad.Activo = true;

			await _context.EntidadesPublicas.AddAsync(entidad);
			await _context.SaveChangesAsync();

			return _mapper.Map<EntidadPublicaDto>(entidad);
		}

		public async Task<EntidadPublicaDto?> GetEntidadByIdAsync(int entidadId)
		{
			var entidad = await _context.EntidadesPublicas
				.Include(e => e.PeriodoPlanificacion)
				.FirstOrDefaultAsync(e => e.EntidadPublicaId == entidadId);

			return entidad == null ? null : _mapper.Map<EntidadPublicaDto>(entidad);
		}

		public async Task<EntidadPublicaDto?> UpdateEntidadAsync(int entidadId, EntidadPublicaCreateUpdateDto dto)
		{
			ValidateEntidadDto(dto);

			var entidad = await _context.EntidadesPublicas
				.FirstOrDefaultAsync(e => e.EntidadPublicaId == entidadId);

			if (entidad == null)
			{
				return null;
			}

			var periodoId = await ResolvePeriodoForEntidadAsync(dto.PeriodoPlanificacionId);
			var codigo = await ResolveCodigoEntidadAsync(dto.Codigo, dto.Sigla, entidadId);

			entidad.Codigo = codigo;
			entidad.Nombre = dto.Nombre.Trim();
			entidad.Sigla = dto.Sigla.Trim().ToUpperInvariant();
			entidad.Tipo = dto.Tipo.Trim();
			entidad.NivelGobierno = dto.NivelGobierno.Trim();
			entidad.Mision = (dto.Mision ?? string.Empty).Trim();
			entidad.PeriodoPlanificacionId = periodoId;

			await _context.SaveChangesAsync();

			return _mapper.Map<EntidadPublicaDto>(entidad);
		}

		public async Task<bool> DeactivateEntidadAsync(int entidadId)
		{
			var entidad = await _context.EntidadesPublicas
				.FirstOrDefaultAsync(e => e.EntidadPublicaId == entidadId);

			if (entidad == null)
			{
				return false;
			}

			if (!entidad.Activo)
			{
				return true;
			}

			if (await HasActivePeiDependenciesAsync(entidadId))
			{
				throw new InvalidOperationException("No se puede inactivar la entidad porque tiene planes estratégicos institucionales activos.");
			}

			entidad.Activo = false;
			await _context.SaveChangesAsync();

			return true;
		}

		private static void ValidatePeriodoDto(PeriodoPlanificacionCreateUpdateDto dto)
		{
			if (string.IsNullOrWhiteSpace(dto.Codigo))
			{
				throw new InvalidOperationException("El código del período es obligatorio.");
			}

			if (string.IsNullOrWhiteSpace(dto.Nombre))
			{
				throw new InvalidOperationException("El nombre del período es obligatorio.");
			}

			if (dto.FechaInicio >= dto.FechaFin)
			{
				throw new InvalidOperationException("La fecha de inicio debe ser menor a la fecha de fin.");
			}
		}

		private static void ValidateEntidadDto(EntidadPublicaCreateUpdateDto dto)
		{
			if (string.IsNullOrWhiteSpace(dto.Nombre) ||
				string.IsNullOrWhiteSpace(dto.Sigla) ||
				string.IsNullOrWhiteSpace(dto.Tipo) ||
				string.IsNullOrWhiteSpace(dto.NivelGobierno))
			{
				throw new InvalidOperationException("Nombre, Sigla, Tipo y Nivel de Gobierno son obligatorios.");
			}
		}

		private async Task<int> ResolvePeriodoForEntidadAsync(int? requestedPeriodoId)
		{
			if (requestedPeriodoId.HasValue && requestedPeriodoId.Value > 0)
			{
				var periodoExists = await _context.PeriodosPlanificacion
					.AnyAsync(p => p.PeriodoPlanificacionId == requestedPeriodoId.Value);
				if (!periodoExists)
				{
					throw new InvalidOperationException("El período de planificación seleccionado no existe.");
				}

				return requestedPeriodoId.Value;
			}

			var fallbackPeriodoId = await _context.PeriodosPlanificacion
				.Where(p => p.Activo)
				.OrderByDescending(p => p.FechaInicio)
				.Select(p => (int?)p.PeriodoPlanificacionId)
				.FirstOrDefaultAsync();

			if (!fallbackPeriodoId.HasValue)
			{
				throw new InvalidOperationException("No existe un período activo para asociar la entidad.");
			}

			return fallbackPeriodoId.Value;
		}

		private async Task<string> ResolveCodigoEntidadAsync(string? requestedCode, string sigla, int? excludeEntidadId)
		{
			if (!string.IsNullOrWhiteSpace(requestedCode))
			{
				var normalized = requestedCode.Trim().ToUpperInvariant();
				var exists = await _context.EntidadesPublicas.AnyAsync(e =>
					e.Codigo == normalized &&
					(!excludeEntidadId.HasValue || e.EntidadPublicaId != excludeEntidadId.Value));

				if (exists)
				{
					throw new InvalidOperationException("Ya existe una entidad con el mismo código.");
				}

				return normalized;
			}

			var baseCode = new string(sigla
				.Trim()
				.ToUpperInvariant()
				.Where(char.IsLetterOrDigit)
				.ToArray());

			if (string.IsNullOrWhiteSpace(baseCode))
			{
				baseCode = "ENT";
			}

			baseCode = baseCode.Length > 12 ? baseCode[..12] : baseCode;

			var candidate = baseCode;
			var suffix = 1;
			while (await _context.EntidadesPublicas.AnyAsync(e =>
				e.Codigo == candidate &&
				(!excludeEntidadId.HasValue || e.EntidadPublicaId != excludeEntidadId.Value)))
			{
				candidate = $"{baseCode}{suffix}";
				suffix++;
			}

			return candidate;
		}

		private async Task<bool> HasActivePeiDependenciesAsync(int entidadPublicaId)
		{
			var connection = _context.Database.GetDbConnection();
			var shouldClose = false;

			if (connection.State != ConnectionState.Open)
			{
				await connection.OpenAsync();
				shouldClose = true;
			}

			try
			{
				if (!await TableExistsAsync(connection, "PlanesEstrategicos"))
				{
					return false;
				}

				await using var cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT COUNT(1) FROM PlanesEstrategicos WHERE EntidadPublicaId = @entidadPublicaId AND Activo = 1";
				var parameter = cmd.CreateParameter();
				parameter.ParameterName = "@entidadPublicaId";
				parameter.Value = entidadPublicaId;
				cmd.Parameters.Add(parameter);

				var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
				return count > 0;
			}
			finally
			{
				if (shouldClose)
				{
					await connection.CloseAsync();
				}
			}
		}

		private async Task<bool> TableExistsAsync(DbConnection connection, string tableName)
		{
			await using var cmd = connection.CreateCommand();
			if (_context.Database.IsSqlServer())
			{
				cmd.CommandText = "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @tableName";
			}
			else
			{
				cmd.CommandText = "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = @tableName";
			}

			var parameter = cmd.CreateParameter();
			parameter.ParameterName = "@tableName";
			parameter.Value = tableName;
			cmd.Parameters.Add(parameter);

			var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
			return count > 0;
		}
	}
}