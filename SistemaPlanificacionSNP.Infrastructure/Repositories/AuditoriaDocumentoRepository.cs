using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.ControlCalidad;
using SistemaPlanificacionSNP.Infrastructure.Data;

namespace SistemaPlanificacionSNP.Infrastructure.Repositories
{
    public interface IAuditoriaDocumentoRepository
    {
        Task<List<AuditoriaDocumento>> GetByAuditoriaIdAsync(int auditoriaId);
        Task<AuditoriaDocumento?> GetByIdAsync(int auditoriaId, int documentoId);
        Task AddRangeAsync(IEnumerable<AuditoriaDocumento> documentos);
        Task RemoveAsync(AuditoriaDocumento documento);
    }

    public class AuditoriaDocumentoRepository : IAuditoriaDocumentoRepository
    {
        private readonly ControlCalidadDbContext _context;

        public AuditoriaDocumentoRepository(ControlCalidadDbContext context)
        {
            _context = context;
        }

        public async Task<List<AuditoriaDocumento>> GetByAuditoriaIdAsync(int auditoriaId)
        {
            return await _context.AuditoriaDocumentos
                .Where(d => d.AuditoriaId == auditoriaId)
                .OrderByDescending(d => d.FechaCarga)
                .ToListAsync();
        }

        public async Task<AuditoriaDocumento?> GetByIdAsync(int auditoriaId, int documentoId)
        {
            return await _context.AuditoriaDocumentos
                .FirstOrDefaultAsync(d => d.AuditoriaId == auditoriaId && d.AuditoriaDocumentoId == documentoId);
        }

        public async Task AddRangeAsync(IEnumerable<AuditoriaDocumento> documentos)
        {
            await _context.AuditoriaDocumentos.AddRangeAsync(documentos);
        }

        public Task RemoveAsync(AuditoriaDocumento documento)
        {
            _context.AuditoriaDocumentos.Remove(documento);
            return Task.CompletedTask;
        }
    }
}