using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.ControlCalidad;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.DTOs;

namespace SistemaPlanificacionSNP.Infrastructure.Repositories
{
    public interface IControlCalidadAuditoriaRepository
    {
        Task<List<Auditoria>> GetPagedAsync(AuditoriaQueryDto query);
        Task<int> CountFilteredAsync(AuditoriaQueryDto query);
        Task<Auditoria?> GetByIdAsync(int auditoriaId);
        Task<List<Auditoria>> GetByRevisionIdAsync(int revisionId);
        Task AddAsync(Auditoria auditoria);
        Task UpdateAsync(Auditoria auditoria);
        Task RemoveAsync(Auditoria auditoria);
        Task<int> CountByResultadoAsync(string resultado);
        Task<int> CountAllAsync();
    }

    public class ControlCalidadAuditoriaRepository : IControlCalidadAuditoriaRepository
    {
        private readonly ControlCalidadDbContext _context;

        public ControlCalidadAuditoriaRepository(ControlCalidadDbContext context)
        {
            _context = context;
        }

        public async Task<List<Auditoria>> GetPagedAsync(AuditoriaQueryDto query)
        {
            var q = BuildQuery(query);
            q = ApplySort(q, query.SortBy, query.SortDirection);

            return await q
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();
        }

        public async Task<int> CountFilteredAsync(AuditoriaQueryDto query)
        {
            return await BuildQuery(query).CountAsync();
        }

        public async Task<Auditoria?> GetByIdAsync(int auditoriaId)
        {
            return await _context.Auditorias.FirstOrDefaultAsync(a => a.AuditoriaId == auditoriaId);
        }

        public async Task<List<Auditoria>> GetByRevisionIdAsync(int revisionId)
        {
            return await _context.Auditorias
                .Where(a => a.RevisionId == revisionId)
                .OrderByDescending(a => a.FechaRegistro)
                .ToListAsync();
        }

        public async Task AddAsync(Auditoria auditoria)
        {
            await _context.Auditorias.AddAsync(auditoria);
        }

        public Task UpdateAsync(Auditoria auditoria)
        {
            _context.Auditorias.Update(auditoria);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Auditoria auditoria)
        {
            _context.Auditorias.Remove(auditoria);
            return Task.CompletedTask;
        }

        public async Task<int> CountByResultadoAsync(string resultado)
        {
            return await _context.Auditorias.CountAsync(a => a.Resultado == resultado);
        }

        public async Task<int> CountAllAsync()
        {
            return await _context.Auditorias.CountAsync();
        }

        private IQueryable<Auditoria> BuildQuery(AuditoriaQueryDto query)
        {
            var q = _context.Auditorias.AsQueryable();

            if (query.RevisionId.HasValue)
            {
                q = q.Where(a => a.RevisionId == query.RevisionId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Tipo))
            {
                q = q.Where(a => a.Tipo.Contains(query.Tipo));
            }

            if (!string.IsNullOrWhiteSpace(query.Resultado))
            {
                q = q.Where(a => a.Resultado == query.Resultado);
            }

            if (!string.IsNullOrWhiteSpace(query.Responsable))
            {
                q = q.Where(a => a.Responsable.Contains(query.Responsable));
            }

            if (query.FechaDesde.HasValue)
            {
                q = q.Where(a => a.FechaRegistro >= query.FechaDesde.Value);
            }

            if (query.FechaHasta.HasValue)
            {
                q = q.Where(a => a.FechaRegistro <= query.FechaHasta.Value);
            }

            return q;
        }

        private static IQueryable<Auditoria> ApplySort(IQueryable<Auditoria> q, string sortBy, string sortDirection)
        {
            var desc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLowerInvariant() switch
            {
                "tipo" => desc ? q.OrderByDescending(x => x.Tipo) : q.OrderBy(x => x.Tipo),
                "resultado" => desc ? q.OrderByDescending(x => x.Resultado) : q.OrderBy(x => x.Resultado),
                "responsable" => desc ? q.OrderByDescending(x => x.Responsable) : q.OrderBy(x => x.Responsable),
                _ => desc ? q.OrderByDescending(x => x.FechaRegistro) : q.OrderBy(x => x.FechaRegistro)
            };
        }
    }
}
