using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.MacroPlanificacion;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.DTOs;

namespace SistemaPlanificacionSNP.Infrastructure.Repositories
{
    public interface IPlanesNacionalesDesarrolloRepository
    {
        Task<List<PlanesNacionalesDesarrollo>> GetPagedAsync(MacroPlanNacionalQueryDto query);
        Task<int> CountFilteredAsync(MacroPlanNacionalQueryDto query);
        Task<PlanesNacionalesDesarrollo?> GetByIdAsync(int planNacionalId);
        Task<PlanesNacionalesDesarrollo?> GetByIdWithObjetivosAsync(int planNacionalId);
        Task<List<MacroConteoEstadoDto>> GetConteoPorEstadoAsync();
        Task<List<MacroConteoVigenciaDto>> GetConteoPorVigenciaAsync();
        Task<int> CountTotalAsync();
        Task AddAsync(PlanesNacionalesDesarrollo entity);
        Task UpdateAsync(PlanesNacionalesDesarrollo entity);
        Task RemoveAsync(PlanesNacionalesDesarrollo entity);
    }

    public class PlanesNacionalesDesarrolloRepository : IPlanesNacionalesDesarrolloRepository
    {
        private readonly MacroPlanificacionDbContext _context;

        public PlanesNacionalesDesarrolloRepository(MacroPlanificacionDbContext context)
        {
            _context = context;
        }

        public async Task<List<PlanesNacionalesDesarrollo>> GetPagedAsync(MacroPlanNacionalQueryDto query)
        {
            var q = BuildQuery(query);
            q = ApplySort(q, query.SortBy, query.SortDirection);

            return await q
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();
        }

        public async Task<int> CountFilteredAsync(MacroPlanNacionalQueryDto query)
        {
            return await BuildQuery(query).CountAsync();
        }

        public async Task<PlanesNacionalesDesarrollo?> GetByIdAsync(int planNacionalId)
        {
            return await _context.PlanesNacionalesDesarrollos
                .FirstOrDefaultAsync(p => p.PlanNacionalId == planNacionalId);
        }

        public async Task<PlanesNacionalesDesarrollo?> GetByIdWithObjetivosAsync(int planNacionalId)
        {
            return await _context.PlanesNacionalesDesarrollos
                .Include(p => p.ObjetivosEstrategicos)
                .FirstOrDefaultAsync(p => p.PlanNacionalId == planNacionalId);
        }

        public async Task<List<MacroConteoEstadoDto>> GetConteoPorEstadoAsync()
        {
            return await _context.PlanesNacionalesDesarrollos
                .GroupBy(x => x.Estado)
                .Select(g => new MacroConteoEstadoDto
                {
                    Estado = g.Key,
                    Total = g.Count()
                })
                .OrderBy(x => x.Estado)
                .ToListAsync();
        }

        public async Task<List<MacroConteoVigenciaDto>> GetConteoPorVigenciaAsync()
        {
            return await _context.PlanesNacionalesDesarrollos
                .GroupBy(x => new { x.PeriodoInicio, x.PeriodoFin })
                .Select(g => new MacroConteoVigenciaDto
                {
                    PeriodoInicio = g.Key.PeriodoInicio,
                    PeriodoFin = g.Key.PeriodoFin,
                    TotalPlanes = g.Count()
                })
                .OrderBy(x => x.PeriodoInicio)
                .ThenBy(x => x.PeriodoFin)
                .ToListAsync();
        }

        public async Task<int> CountTotalAsync()
        {
            return await _context.PlanesNacionalesDesarrollos.CountAsync();
        }

        public async Task AddAsync(PlanesNacionalesDesarrollo entity)
        {
            await _context.PlanesNacionalesDesarrollos.AddAsync(entity);
        }

        public Task UpdateAsync(PlanesNacionalesDesarrollo entity)
        {
            _context.PlanesNacionalesDesarrollos.Update(entity);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(PlanesNacionalesDesarrollo entity)
        {
            _context.PlanesNacionalesDesarrollos.Remove(entity);
            return Task.CompletedTask;
        }

        private IQueryable<PlanesNacionalesDesarrollo> BuildQuery(MacroPlanNacionalQueryDto query)
        {
            var q = _context.PlanesNacionalesDesarrollos.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Estado))
            {
                q = q.Where(p => p.Estado == query.Estado);
            }

            if (query.PeriodoInicio.HasValue)
            {
                q = q.Where(p => p.PeriodoInicio >= query.PeriodoInicio.Value);
            }

            if (query.PeriodoFin.HasValue)
            {
                q = q.Where(p => p.PeriodoFin <= query.PeriodoFin.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Busqueda))
            {
                var term = query.Busqueda.Trim();
                q = q.Where(p => p.Nombre.Contains(term));
            }

            return q;
        }

        private static IQueryable<PlanesNacionalesDesarrollo> ApplySort(
            IQueryable<PlanesNacionalesDesarrollo> q,
            string sortBy,
            string sortDirection)
        {
            var desc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLowerInvariant() switch
            {
                "nombre" => desc ? q.OrderByDescending(x => x.Nombre) : q.OrderBy(x => x.Nombre),
                "estado" => desc ? q.OrderByDescending(x => x.Estado) : q.OrderBy(x => x.Estado),
                "periodoinicio" => desc ? q.OrderByDescending(x => x.PeriodoInicio) : q.OrderBy(x => x.PeriodoInicio),
                "periodofin" => desc ? q.OrderByDescending(x => x.PeriodoFin) : q.OrderBy(x => x.PeriodoFin),
                "fechacreacion" => desc ? q.OrderByDescending(x => x.FechaCreacion) : q.OrderBy(x => x.FechaCreacion),
                _ => desc ? q.OrderByDescending(x => x.PlanNacionalId) : q.OrderBy(x => x.PlanNacionalId)
            };
        }
    }
}