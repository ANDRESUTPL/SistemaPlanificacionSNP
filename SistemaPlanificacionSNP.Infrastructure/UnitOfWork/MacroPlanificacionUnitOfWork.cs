using Microsoft.EntityFrameworkCore.Storage;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.Repositories;

namespace SistemaPlanificacionSNP.Infrastructure.UnitOfWork
{
    public class MacroPlanificacionUnitOfWork : IMacroPlanificacionUnitOfWork
    {
        private readonly MacroPlanificacionDbContext _context;
        private IDbContextTransaction? _transaction;

        private IPlanesNacionalesDesarrolloRepository? _planesNacionalesRepository;
        private IObjetivosEstrategicoRepository? _objetivosEstrategicosRepository;

        public MacroPlanificacionUnitOfWork(MacroPlanificacionDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IPlanesNacionalesDesarrolloRepository PlanesNacionales
        {
            get
            {
                return _planesNacionalesRepository ??=
                    new PlanesNacionalesDesarrolloRepository(_context);
            }
        }

        public IObjetivosEstrategicoRepository ObjetivosEstrategicos
        {
            get
            {
                return _objetivosEstrategicosRepository ??=
                    new ObjetivosEstrategicoRepository(_context);
            }
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
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