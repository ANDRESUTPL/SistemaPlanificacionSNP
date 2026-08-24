using SistemaPlanificacionSNP.Infrastructure.Repositories;

namespace SistemaPlanificacionSNP.Infrastructure.UnitOfWork
{
    public interface IControlCalidadUnitOfWork : IDisposable
    {
        IRevisioneRepository Revisiones { get; }
        IControlCalidadAuditoriaRepository AuditoriasControlCalidad { get; }
        IAuditoriaDocumentoRepository AuditoriaDocumentos { get; }

        Task<int> SaveChangesAsync();
        Task<bool> BeginTransactionAsync();
        Task<bool> CommitAsync();
        Task<bool> RollbackAsync();
    }
}
