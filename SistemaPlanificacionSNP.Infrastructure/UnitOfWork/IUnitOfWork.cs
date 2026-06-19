using SistemaPlanificacionSNP.Infrastructure.Repositories;
using System;
using System.Threading.Tasks;

namespace SistemaPlanificacionSNP.Infrastructure.UnitOfWork
{
    /// <summary>
    /// Interfaz del patrón Unit of Work
    /// Centraliza la gestión de transacciones y repositorios
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        // Repositorios
        IUsuarioRepository Usuarios { get; }
        IAuditoriaRepository Auditorias { get; }
        IPlanificacionRepository Planificacion { get; }
        IRepository<T> GetRepository<T>() where T : class;

        // Transacciones
        Task<int> SaveChangesAsync();
        Task<bool> BeginTransactionAsync();
        Task<bool> CommitAsync();
        Task<bool> RollbackAsync();
    }
}
