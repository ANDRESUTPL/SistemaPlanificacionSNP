using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using SistemaPlanificacionSNP.Infrastructure.Data;
using SistemaPlanificacionSNP.Infrastructure.Repositories;

namespace SistemaPlanificacionSNP.Infrastructure.UnitOfWork
{
    /// <summary>
    /// Implementación del patrón Unit of Work
    /// Gestiona transacciones y múltiples repositorios
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AuthDbContext _context;
        private IDbContextTransaction? _transaction;
        private Dictionary<Type, object>? _repositories;

        private IUsuarioRepository? _usuarioRepository;
        private IAuditoriaRepository? _auditoriaRepository;
        private IPlanificacionRepository? _planificacionRepository;

        public UnitOfWork(AuthDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IUsuarioRepository Usuarios
        {
            get { return _usuarioRepository ??= new UsuarioRepository(_context); }
        }

        public IAuditoriaRepository Auditorias
        {
            get { return _auditoriaRepository ??= new AuditoriaRepository(_context); }
        }

        public IPlanificacionRepository Planificacion
        {
            get { return _planificacionRepository ??= new PlanificacionRepository(_context); }
        }

        public IRepository<T> GetRepository<T>() where T : class
        {
            _repositories ??= new Dictionary<Type, object>();

            Type type = typeof(T);
            if (!_repositories.ContainsKey(type))
            {
                Type repositoryType = typeof(Repository<>).MakeGenericType(type);
                object repositoryInstance = Activator.CreateInstance(repositoryType, _context)!;
                _repositories.Add(type, repositoryInstance);
            }

            return (IRepository<T>)_repositories[type];
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
                await _transaction?.CommitAsync()!;
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
                await _transaction?.RollbackAsync()!;
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
            _context?.Dispose();
        }
    }
}
