using Microsoft.EntityFrameworkCore.Storage;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.Repositories;

namespace SistemaPlanificacionSNP.Infrastructure.UnitOfWork
{
    public class ControlCalidadUnitOfWork : IControlCalidadUnitOfWork
    {
        private readonly ControlCalidadDbContext _context;
        private IDbContextTransaction? _transaction;

        private IRevisioneRepository? _revisioneRepository;
        private IControlCalidadAuditoriaRepository? _auditoriaRepository;

        public ControlCalidadUnitOfWork(ControlCalidadDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IRevisioneRepository Revisiones
        {
            get { return _revisioneRepository ??= new RevisioneRepository(_context); }
        }

        public IControlCalidadAuditoriaRepository AuditoriasControlCalidad
        {
            get { return _auditoriaRepository ??= new ControlCalidadAuditoriaRepository(_context); }
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
