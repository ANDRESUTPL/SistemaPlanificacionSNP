using SistemaPlanificacionSNP.Infrastructure.Repositories;

namespace SistemaPlanificacionSNP.Infrastructure.UnitOfWork
{
    public interface IControlCalidadUnitOfWork : IDisposable
    {
        IRevisioneRepository Revisiones { get; }
        IControlCalidadAuditoriaRepository AuditoriasControlCalidad { get; }

        Task<int> SaveChangesAsync();
        Task<bool> BeginTransactionAsync();
        Task<bool> CommitAsync();
        Task<bool> RollbackAsync();
    }
}
