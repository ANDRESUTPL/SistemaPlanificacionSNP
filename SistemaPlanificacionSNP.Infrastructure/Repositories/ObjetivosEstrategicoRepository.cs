using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.MacroPlanificacion;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.DTOs;

namespace SistemaPlanificacionSNP.Infrastructure.Repositories
{
    public interface IObjetivosEstrategicoRepository
    {
        Task<List<ObjetivosEstrategico>> GetPagedAsync(MacroObjetivoEstrategicoQueryDto query);
        Task<int> CountFilteredAsync(MacroObjetivoEstrategicoQueryDto query);
        Task<ObjetivosEstrategico?> GetByIdAsync(int objetivoEstrategicoId);
        Task<ObjetivosEstrategico?> GetByCodigoAsync(int planNacionalId, string codigo);
        Task<List<ObjetivosEstrategico>> GetByPlanNacionalIdAsync(int planNacionalId);
        Task<int> CountTotalAsync();
        Task AddAsync(ObjetivosEstrategico entity);
        Task UpdateAsync(ObjetivosEstrategico entity);
        Task RemoveAsync(ObjetivosEstrategico entity);
    }

    public class ObjetivosEstrategicoRepository : IObjetivosEstrategicoRepository
    {
        private readonly MacroPlanificacionDbContext _context;

        public ObjetivosEstrategicoRepository(MacroPlanificacionDbContext context)
        {
            _context = context;
        }

        public async Task<List<ObjetivosEstrategico>> GetPagedAsync(MacroObjetivoEstrategicoQueryDto query)
        {
            var q = BuildQuery(query);
            q = ApplySort(q, query.SortBy, query.SortDirection);

            return await q
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();
        }

        public async Task<int> CountFilteredAsync(MacroObjetivoEstrategicoQueryDto query)
        {
            return await BuildQuery(query).CountAsync();
        }

        public async Task<ObjetivosEstrategico?> GetByIdAsync(int objetivoEstrategicoId)
        {
            return await _context.ObjetivosEstrategicos
                .FirstOrDefaultAsync(x => x.ObjetivoEstrategicoId == objetivoEstrategicoId);
        }

        public async Task<ObjetivosEstrategico?> GetByCodigoAsync(int planNacionalId, string codigo)
        {
            return await _context.ObjetivosEstrategicos
                .FirstOrDefaultAsync(x => x.PlanNacionalId == planNacionalId && x.Codigo == codigo);
        }

        public async Task<List<ObjetivosEstrategico>> GetByPlanNacionalIdAsync(int planNacionalId)
        {
            return await _context.ObjetivosEstrategicos
                .Where(x => x.PlanNacionalId == planNacionalId)
                .OrderBy(x => x.Codigo)
                .ToListAsync();
        }

        public async Task<int> CountTotalAsync()
        {
            return await _context.ObjetivosEstrategicos.CountAsync();
        }

        public async Task AddAsync(ObjetivosEstrategico entity)
        {
            await _context.ObjetivosEstrategicos.AddAsync(entity);
        }

        public Task UpdateAsync(ObjetivosEstrategico entity)
        {
            _context.ObjetivosEstrategicos.Update(entity);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(ObjetivosEstrategico entity)
        {
            _context.ObjetivosEstrategicos.Remove(entity);
            return Task.CompletedTask;
        }

        private IQueryable<ObjetivosEstrategico> BuildQuery(MacroObjetivoEstrategicoQueryDto query)
        {
            var q = _context.ObjetivosEstrategicos.AsQueryable();

            if (query.PlanNacionalId.HasValue)
            {
                q = q.Where(x => x.PlanNacionalId == query.PlanNacionalId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Codigo))
            {
                var codigo = query.Codigo.Trim();
                q = q.Where(x => x.Codigo.Contains(codigo));
            }

            if (!string.IsNullOrWhiteSpace(query.Busqueda))
            {
                var term = query.Busqueda.Trim();
                q = q.Where(x => x.Codigo.Contains(term) || x.Nombre.Contains(term));
            }

            return q;
        }

        private static IQueryable<ObjetivosEstrategico> ApplySort(
            IQueryable<ObjetivosEstrategico> q,
            string sortBy,
            string sortDirection)
        {
            var desc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLowerInvariant() switch
            {
                "codigo" => desc ? q.OrderByDescending(x => x.Codigo) : q.OrderBy(x => x.Codigo),
                "nombre" => desc ? q.OrderByDescending(x => x.Nombre) : q.OrderBy(x => x.Nombre),
                "plannacionalid" => desc ? q.OrderByDescending(x => x.PlanNacionalId) : q.OrderBy(x => x.PlanNacionalId),
                _ => desc ? q.OrderByDescending(x => x.ObjetivoEstrategicoId) : q.OrderBy(x => x.ObjetivoEstrategicoId)
            };
        }
    }
}