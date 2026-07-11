using SistemaPlanificacionSNP.Infrastructure.Repositories;

namespace SistemaPlanificacionSNP.Infrastructure.UnitOfWork
{
    public interface IMacroPlanificacionUnitOfWork : IDisposable
    {
        IPlanesNacionalesDesarrolloRepository PlanesNacionales { get; }
        IObjetivosEstrategicoRepository ObjetivosEstrategicos { get; }

        Task<int> SaveChangesAsync();
        Task<bool> BeginTransactionAsync();
        Task<bool> CommitAsync();
        Task<bool> RollbackAsync();
    }
}