using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.Seguridad;
using SistemaPlanificacionSNP.Infrastructure.Data;

namespace SistemaPlanificacionSNP.Infrastructure.Repositories
{
    /// <summary>
    /// Interfaz del repositorio específico para Auditoría
    /// </summary>
    public interface IAuditoriaRepository : IRepository<AuditoriaTransaccional>
    {
        Task<IEnumerable<AuditoriaTransaccional>> GetByUsuarioAsync(int usuarioId);
        Task<IEnumerable<AuditoriaTransaccional>> GetByEntidadAsync(string nombreEntidad);
        Task<IEnumerable<AuditoriaTransaccional>> GetByFechaRangoAsync(DateTime desde, DateTime hasta);
        Task<IEnumerable<AuditoriaTransaccional>> GetByOperacionAsync(string tipoOperacion);
    }

    /// <summary>
    /// Implementación del repositorio específico para Auditoría
    /// </summary>
    public class AuditoriaRepository : Repository<AuditoriaTransaccional>, IAuditoriaRepository
    {
        public AuditoriaRepository(AuthDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<AuditoriaTransaccional>> GetByUsuarioAsync(int usuarioId)
        {
            return await _dbSet
                .Where(a => a.UsuarioId == usuarioId)
                .OrderByDescending(a => a.FechaOperacion)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditoriaTransaccional>> GetByEntidadAsync(string nombreEntidad)
        {
            return await _dbSet
                .Where(a => a.Entidad == nombreEntidad)
                .OrderByDescending(a => a.FechaOperacion)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditoriaTransaccional>> GetByFechaRangoAsync(DateTime desde, DateTime hasta)
        {
            return await _dbSet
                .Where(a => a.FechaOperacion >= desde && a.FechaOperacion <= hasta)
                .OrderByDescending(a => a.FechaOperacion)
                .ToListAsync();
        }

        public async Task<IEnumerable<AuditoriaTransaccional>> GetByOperacionAsync(string tipoOperacion)
        {
            return await _dbSet
                .Where(a => a.TipoOperacion == tipoOperacion)
                .OrderByDescending(a => a.FechaOperacion)
                .ToListAsync();
        }
    }
}
