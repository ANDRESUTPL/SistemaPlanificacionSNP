using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.PlanificacionInstitucional;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.DTOs;

namespace SistemaPlanificacionSNP.Infrastructure.Repositories
{
    public interface IPlanesEstrategicoPiRepository
    {
        Task<List<PlanesEstrategico>> GetPagedAsync(PlanesEstrategicoQueryDto query);
        Task<int> CountFilteredAsync(PlanesEstrategicoQueryDto query);
        Task<PlanesEstrategico?> GetByIdAsync(int planId);
        Task<PlanesEstrategico?> GetByIdWithProyectosAsync(int planId);
        Task<bool> ExistsByEntidadPeriodoAsync(string entidad, int periodoInicio, int periodoFin, int? excludeId = null);
        Task<bool> HasActiveProjectsAsync(int planId);
        Task AddAsync(PlanesEstrategico entity);
        Task UpdateAsync(PlanesEstrategico entity);
    }

    public interface IProyectosInversionPiRepository
    {
        Task<List<ProyectosInversion>> GetPagedAsync(ProyectosInversionQueryDto query);
        Task<int> CountFilteredAsync(ProyectosInversionQueryDto query);
        Task<ProyectosInversion?> GetByIdAsync(int proyectoId);
        Task<ProyectosInversion?> GetByIdWithPlanAsync(int proyectoId);
        Task<ProyectosInversion?> GetByCodigoAsync(string codigoProyecto);
        Task<bool> ExistsCodigoAsync(string codigoProyecto, int? excludeId = null);
        Task AddAsync(ProyectosInversion entity);
        Task UpdateAsync(ProyectosInversion entity);
    }

    public class PlanesEstrategicoPiRepository : IPlanesEstrategicoPiRepository
    {
        private readonly PlanificacionInstitucionalDbContext _context;

        public PlanesEstrategicoPiRepository(PlanificacionInstitucionalDbContext context)
        {
            _context = context;
        }

        public async Task<List<PlanesEstrategico>> GetPagedAsync(PlanesEstrategicoQueryDto query)
        {
            var q = BuildQuery(query);
            q = ApplySort(q, query.SortBy, query.SortDirection);

            return await q
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();
        }

        public Task<int> CountFilteredAsync(PlanesEstrategicoQueryDto query)
        {
            return BuildQuery(query).CountAsync();
        }

        public Task<PlanesEstrategico?> GetByIdAsync(int planId)
        {
            return _context.PlanesEstrategicos.FirstOrDefaultAsync(x => x.PlanEstrategicoId == planId);
        }

        public Task<PlanesEstrategico?> GetByIdWithProyectosAsync(int planId)
        {
            return _context.PlanesEstrategicos
                .Include(x => x.ProyectosInversions)
                .FirstOrDefaultAsync(x => x.PlanEstrategicoId == planId);
        }

        public Task<bool> ExistsByEntidadPeriodoAsync(string entidad, int periodoInicio, int periodoFin, int? excludeId = null)
        {
            var q = _context.PlanesEstrategicos.Where(x =>
                x.Entidad == entidad &&
                x.PeriodoInicio == periodoInicio &&
                x.PeriodoFin == periodoFin);

            if (excludeId.HasValue)
            {
                q = q.Where(x => x.PlanEstrategicoId != excludeId.Value);
            }

            return q.AnyAsync();
        }

        public Task<bool> HasActiveProjectsAsync(int planId)
        {
            return _context.ProyectosInversions.AnyAsync(x =>
                x.PlanEstrategicoId == planId &&
                !string.Equals(x.Estado, "Inactivo", StringComparison.OrdinalIgnoreCase));
        }

        public async Task AddAsync(PlanesEstrategico entity)
        {
            await _context.PlanesEstrategicos.AddAsync(entity);
        }

        public Task UpdateAsync(PlanesEstrategico entity)
        {
            _context.PlanesEstrategicos.Update(entity);
            return Task.CompletedTask;
        }

        private IQueryable<PlanesEstrategico> BuildQuery(PlanesEstrategicoQueryDto query)
        {
            var q = _context.PlanesEstrategicos.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Estado))
            {
                q = q.Where(x => x.Estado == query.Estado);
            }

            if (!string.IsNullOrWhiteSpace(query.Entidad))
            {
                var filtro = query.Entidad.Trim();
                q = q.Where(x => x.Entidad.Contains(filtro));
            }

            if (query.PeriodoInicio.HasValue)
            {
                q = q.Where(x => x.PeriodoInicio >= query.PeriodoInicio.Value);
            }

            if (query.PeriodoFin.HasValue)
            {
                q = q.Where(x => x.PeriodoFin <= query.PeriodoFin.Value);
            }

            return q;
        }

        private static IQueryable<PlanesEstrategico> ApplySort(IQueryable<PlanesEstrategico> q, string sortBy, string sortDirection)
        {
            var desc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            return sortBy.ToLowerInvariant() switch
            {
                "entidad" => desc ? q.OrderByDescending(x => x.Entidad) : q.OrderBy(x => x.Entidad),
                "periodoinicio" => desc ? q.OrderByDescending(x => x.PeriodoInicio) : q.OrderBy(x => x.PeriodoInicio),
                "periodofin" => desc ? q.OrderByDescending(x => x.PeriodoFin) : q.OrderBy(x => x.PeriodoFin),
                "estado" => desc ? q.OrderByDescending(x => x.Estado) : q.OrderBy(x => x.Estado),
                _ => desc ? q.OrderByDescending(x => x.PlanEstrategicoId) : q.OrderBy(x => x.PlanEstrategicoId)
            };
        }
    }

    public class ProyectosInversionPiRepository : IProyectosInversionPiRepository
    {
        private readonly PlanificacionInstitucionalDbContext _context;

        public ProyectosInversionPiRepository(PlanificacionInstitucionalDbContext context)
        {
            _context = context;
        }

        public async Task<List<ProyectosInversion>> GetPagedAsync(ProyectosInversionQueryDto query)
        {
            var q = BuildQuery(query);
            q = ApplySort(q, query.SortBy, query.SortDirection);

            return await q
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();
        }

        public Task<int> CountFilteredAsync(ProyectosInversionQueryDto query)
        {
            return BuildQuery(query).CountAsync();
        }

        public Task<ProyectosInversion?> GetByIdAsync(int proyectoId)
        {
            return _context.ProyectosInversions.FirstOrDefaultAsync(x => x.ProyectoInversionId == proyectoId);
        }

        public Task<ProyectosInversion?> GetByIdWithPlanAsync(int proyectoId)
        {
            return _context.ProyectosInversions
                .Include(x => x.PlanEstrategico)
                .FirstOrDefaultAsync(x => x.ProyectoInversionId == proyectoId);
        }

        public Task<ProyectosInversion?> GetByCodigoAsync(string codigoProyecto)
        {
            return _context.ProyectosInversions.FirstOrDefaultAsync(x => x.CodigoProyecto == codigoProyecto);
        }

        public Task<bool> ExistsCodigoAsync(string codigoProyecto, int? excludeId = null)
        {
            var q = _context.ProyectosInversions.Where(x => x.CodigoProyecto == codigoProyecto);
            if (excludeId.HasValue)
            {
                q = q.Where(x => x.ProyectoInversionId != excludeId.Value);
            }

            return q.AnyAsync();
        }

        public async Task AddAsync(ProyectosInversion entity)
        {
            await _context.ProyectosInversions.AddAsync(entity);
        }

        public Task UpdateAsync(ProyectosInversion entity)
        {
            _context.ProyectosInversions.Update(entity);
            return Task.CompletedTask;
        }

        private IQueryable<ProyectosInversion> BuildQuery(ProyectosInversionQueryDto query)
        {
            var q = _context.ProyectosInversions.AsQueryable();

            if (query.PlanEstrategicoId.HasValue)
            {
                q = q.Where(x => x.PlanEstrategicoId == query.PlanEstrategicoId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Estado))
            {
                q = q.Where(x => x.Estado == query.Estado);
            }

            if (!string.IsNullOrWhiteSpace(query.CodigoProyecto))
            {
                q = q.Where(x => x.CodigoProyecto.Contains(query.CodigoProyecto.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(query.Nombre))
            {
                q = q.Where(x => x.Nombre.Contains(query.Nombre.Trim()));
            }

            return q;
        }

        private static IQueryable<ProyectosInversion> ApplySort(IQueryable<ProyectosInversion> q, string sortBy, string sortDirection)
        {
            var desc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            return sortBy.ToLowerInvariant() switch
            {
                "codigo" => desc ? q.OrderByDescending(x => x.CodigoProyecto) : q.OrderBy(x => x.CodigoProyecto),
                "nombre" => desc ? q.OrderByDescending(x => x.Nombre) : q.OrderBy(x => x.Nombre),
                "monto" => desc ? q.OrderByDescending(x => x.Monto) : q.OrderBy(x => x.Monto),
                "estado" => desc ? q.OrderByDescending(x => x.Estado) : q.OrderBy(x => x.Estado),
                _ => desc ? q.OrderByDescending(x => x.ProyectoInversionId) : q.OrderBy(x => x.ProyectoInversionId)
            };
        }
    }
}
