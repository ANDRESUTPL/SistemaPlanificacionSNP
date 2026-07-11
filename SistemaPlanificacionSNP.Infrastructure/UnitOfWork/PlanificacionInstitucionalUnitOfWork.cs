using Microsoft.EntityFrameworkCore.Storage;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.Repositories;

namespace SistemaPlanificacionSNP.Infrastructure.UnitOfWork
{
    public interface IPlanificacionInstitucionalUnitOfWork : IDisposable
    {
        IPlanesEstrategicoPiRepository PlanesEstrategicos { get; }
        IProyectosInversionPiRepository ProyectosInversion { get; }

        Task<int> SaveChangesAsync();
        Task<bool> BeginTransactionAsync();
        Task<bool> CommitAsync();
        Task<bool> RollbackAsync();
    }

    public class PlanificacionInstitucionalUnitOfWork : IPlanificacionInstitucionalUnitOfWork
    {
        private readonly PlanificacionInstitucionalDbContext _context;
        private IDbContextTransaction? _transaction;

        private IPlanesEstrategicoPiRepository? _planesEstrategicos;
        private IProyectosInversionPiRepository? _proyectosInversion;

        public PlanificacionInstitucionalUnitOfWork(PlanificacionInstitucionalDbContext context)
        {
            _context = context;
        }

        public IPlanesEstrategicoPiRepository PlanesEstrategicos
            => _planesEstrategicos ??= new PlanesEstrategicoPiRepository(_context);

        public IProyectosInversionPiRepository ProyectosInversion
            => _proyectosInversion ??= new ProyectosInversionPiRepository(_context);

        public Task<int> SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }

        public async Task<bool> BeginTransactionAsync()
        {
            try
            {
                _transaction = await _context.Database.BeginTransactionAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CommitAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                }
                return true;
            }
            catch
            {
                await RollbackAsync();
                return false;
            }
        }

        public async Task<bool> RollbackAsync()
        {
            try
            {
                if (_transaction != null)
                {
                    await _transaction.RollbackAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
