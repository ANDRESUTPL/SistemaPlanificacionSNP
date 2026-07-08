using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.ControlCalidad;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.DTOs;

namespace SistemaPlanificacionSNP.Infrastructure.Repositories
{
    public interface IRevisioneRepository
    {
        Task<List<Revisione>> GetPagedAsync(RevisioneQueryDto query);
        Task<int> CountFilteredAsync(RevisioneQueryDto query);
        Task<Revisione?> GetByIdAsync(int revisionId);
        Task<Revisione?> GetByIdWithAuditoriasAsync(int revisionId);
        Task<Revisione?> GetByCodigoAsync(string codigoRevision);
        Task AddAsync(Revisione revisione);
        Task UpdateAsync(Revisione revisione);
        Task RemoveAsync(Revisione revisione);
        Task<int> CountByEstadoAsync(string estado);
    }

    public class RevisioneRepository : IRevisioneRepository
    {
        private readonly ControlCalidadDbContext _context;

        public RevisioneRepository(ControlCalidadDbContext context)
        {
            _context = context;
        }

        public async Task<List<Revisione>> GetPagedAsync(RevisioneQueryDto query)
        {
            var q = BuildQuery(query);
            q = ApplySort(q, query.SortBy, query.SortDirection);

            return await q
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();
        }

        public async Task<int> CountFilteredAsync(RevisioneQueryDto query)
        {
            return await BuildQuery(query).CountAsync();
        }

        public async Task<Revisione?> GetByIdAsync(int revisionId)
        {
            return await _context.Revisiones.FirstOrDefaultAsync(r => r.RevisionId == revisionId);
        }

        public async Task<Revisione?> GetByIdWithAuditoriasAsync(int revisionId)
        {
            return await _context.Revisiones
                .Include(r => r.Auditoria)
                .FirstOrDefaultAsync(r => r.RevisionId == revisionId);
        }

        public async Task<Revisione?> GetByCodigoAsync(string codigoRevision)
        {
            return await _context.Revisiones.FirstOrDefaultAsync(r => r.CodigoRevision == codigoRevision);
        }

        public async Task AddAsync(Revisione revisione)
        {
            await _context.Revisiones.AddAsync(revisione);
        }

        public Task UpdateAsync(Revisione revisione)
        {
            _context.Revisiones.Update(revisione);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Revisione revisione)
        {
            _context.Revisiones.Remove(revisione);
            return Task.CompletedTask;
        }

        public async Task<int> CountByEstadoAsync(string estado)
        {
            return await _context.Revisiones.CountAsync(r => r.Estado == estado);
        }

        private IQueryable<Revisione> BuildQuery(RevisioneQueryDto query)
        {
            var q = _context.Revisiones.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Estado))
            {
                q = q.Where(r => r.Estado == query.Estado);
            }

            if (!string.IsNullOrWhiteSpace(query.Modulo))
            {
                q = q.Where(r => r.Modulo.Contains(query.Modulo));
            }

            if (!string.IsNullOrWhiteSpace(query.CodigoRevision))
            {
                q = q.Where(r => r.CodigoRevision.Contains(query.CodigoRevision));
            }

            if (query.FechaDesde.HasValue)
            {
                q = q.Where(r => r.FechaRevision >= query.FechaDesde.Value);
            }

            if (query.FechaHasta.HasValue)
            {
                q = q.Where(r => r.FechaRevision <= query.FechaHasta.Value);
            }

            return q;
        }

        private static IQueryable<Revisione> ApplySort(IQueryable<Revisione> q, string sortBy, string sortDirection)
        {
            var desc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

            return sortBy.ToLowerInvariant() switch
            {
                "codigorevision" => desc ? q.OrderByDescending(x => x.CodigoRevision) : q.OrderBy(x => x.CodigoRevision),
                "modulo" => desc ? q.OrderByDescending(x => x.Modulo) : q.OrderBy(x => x.Modulo),
                "estado" => desc ? q.OrderByDescending(x => x.Estado) : q.OrderBy(x => x.Estado),
                _ => desc ? q.OrderByDescending(x => x.FechaRevision) : q.OrderBy(x => x.FechaRevision)
            };
        }
    }
}
