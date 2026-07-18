using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaPlanificacionSNP.Domain.Entities.PlanificacionInstitucional;
using SistemaPlanificacionSNP.Infrastructure.Data;

namespace SistemaPlanificacionSNP.Infrastructure.Repositories
{
    /// <summary>
    /// Interfaz del repositorio específico para Planificación
    /// Recupera jerarquías completas de PEI
    /// </summary>
    public interface IPlanificacionRepository : IRepository<PlanEstrategicoInstitucional>
    {
        Task<PlanEstrategicoInstitucional?> GetWithHierarchyAsync(int peiId);
        Task<IEnumerable<PlanEstrategicoInstitucional>> GetByEntidadAsync(int entidadPublicaId);
        Task<IEnumerable<PlanEstrategicoInstitucional>> GetByEstadoAsync(string estado);
    }

    /// <summary>
    /// Implementación del repositorio específico para Planificación
    /// </summary>
    public class PlanificacionRepository : Repository<PlanEstrategicoInstitucional>, IPlanificacionRepository
    {
        public PlanificacionRepository(AuthDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Obtiene un PEI con toda su jerarquía (OEI, Programas, Indicadores, etc.)
        /// </summary>
        public async Task<PlanEstrategicoInstitucional?> GetWithHierarchyAsync(int peiId)
        {
            return await _dbSet
                .Include(p => p.EntidadPublica)
                .Include(p => p.ObjetivosEstrategicos)
                    .ThenInclude(o => o.ProgramasPresupuestarios)
                        .ThenInclude(pp => pp.MatricesIndicadores)
                            .ThenInclude(mi => mi.MetasTerritorial)
                .Include(p => p.ObjetivosEstrategicos)
                    .ThenInclude(o => o.ProgramasPresupuestarios)
                        .ThenInclude(pp => pp.ProyectosInversion)
                .Include(p => p.Revisiones)
                .FirstOrDefaultAsync(p => p.PeiId == peiId);
        }

        public async Task<IEnumerable<PlanEstrategicoInstitucional>> GetByEntidadAsync(int entidadPublicaId)
        {
            return await _dbSet
                .Where(p => p.EntidadPublicaId == entidadPublicaId)
                .Include(p => p.ObjetivosEstrategicos)
                .ToListAsync();
        }

        public async Task<IEnumerable<PlanEstrategicoInstitucional>> GetByEstadoAsync(string estado)
        {
            return await _dbSet
                .Where(p => p.Estado == estado)
                .Include(p => p.EntidadPublica)
                .ToListAsync();
        }
    }
}
